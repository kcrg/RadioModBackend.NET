using Microsoft.Extensions.FileProviders;
using RadioModBackend.AOT.Models;
using RadioModBackend.NET;
using RadioModBackend.NET.Models;
using RadioModBackend.NET.Services;
using YoutubeExplode;

var builder = WebApplication.CreateSlimBuilder(args);

// Configure Logging
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

// Add configuration from config.json
builder.Configuration.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

// Configure services
builder.Services.Configure<AppSettings>(builder.Configuration.GetSection("RadioMod"));
var radioModConfig = builder.Configuration.GetSection("RadioMod").Get<AppSettings>();
builder.Services.AddSingleton(radioModConfig!);

builder.Services.ConfigureHttpJsonOptions(options => options.SerializerOptions.TypeInfoResolverChain.Insert(0, JsonContext.Default));

builder.Services.AddSingleton<YoutubeClient>();
builder.Services.AddSingleton<SearchService>();
builder.Services.AddSingleton<PlaylistService>();
builder.Services.AddHttpClient<DownloadService>();
builder.Services.AddHttpClient<DiscordService>();

// Add logging
builder.Services.AddLogging();

builder.Services.AddOutputCache(options =>
{
    options.AddBasePolicy(builder => builder
        .Expire(TimeSpan.FromDays(30)));
    options.AddPolicy("Default", CustomCachePolicy.Instance);
});

// Build the app
var app = builder.Build();

// Create a logger instance
var logger = app.Services.GetRequiredService<ILogger<Program>>();

// Log application startup
logger.LogInformation("Starting RadioModBackend application");

// Use static files
var downloadDir = Path.Combine(Directory.GetCurrentDirectory(), "cache");
Directory.CreateDirectory(downloadDir);
logger.LogInformation("Static files directory set to: {DownloadDir}", downloadDir);

app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(downloadDir),
    RequestPath = ""
});

app.UseOutputCache();

// Middleware for logging requests and responses
app.Use(async (context, next) =>
{
    logger.LogInformation("Handling request: {Method} {Path}", context.Request.Method, context.Request.Path);
    await next.Invoke();
    logger.LogInformation("Finished handling request.");
});

// Map endpoints

app.MapPost("/Queue", async (
    QueueRequest request,
    DownloadService downloadService,
    DiscordService discordService,
    AppSettings config,
    ILogger<Program> endpointLogger) =>
{
    endpointLogger.LogInformation("Received /Queue request: {@Request}", request);

    if (request == null)
    {
        endpointLogger.LogWarning("Queue request is null");
        return Results.Json(new QueueResponse
        {
            IsValid = false,
            Error = "Invalid request",
            VideoId = null,
            Uuid = null,
            MaxRes = null,
            VideoTitle = null
        }, JsonContext.Default.QueueResponse);
    }

    if (config.BannedPlayfabIDs?.Contains(request.PlayfabId) ?? false)
    {
        endpointLogger.LogWarning("PlayfabId {PlayfabId} is banned", request.PlayfabId);
        return Results.Json(new QueueResponse
        {
            IsValid = false,
            Error = "You are banned from using Radio.",
            VideoId = null,
            Uuid = null,
            MaxRes = null,
            VideoTitle = null
        }, JsonContext.Default.QueueResponse);
    }

    if (config.BannedVideoIDs?.Contains(request.VideoId) ?? false)
    {
        endpointLogger.LogWarning("VideoId {VideoId} is banned", request.VideoId);
        return Results.Json(new QueueResponse
        {
            IsValid = false,
            Error = "This video is banned.",
            VideoId = null,
            Uuid = null,
            MaxRes = null,
            VideoTitle = null
        }, JsonContext.Default.QueueResponse);
    }

    var lowerTitleWords = request.VideoTitle.ToLower().Split(' ');
    if (lowerTitleWords.Any(word => config.BannedTerms?.Contains(word) ?? false))
    {
        endpointLogger.LogWarning("Video title contains banned terms: {VideoTitle}", request.VideoTitle);
        return Results.Json(new QueueResponse
        {
            IsValid = false,
            Error = "This video contains banned terms.",
            VideoId = null,
            Uuid = null,
            MaxRes = null,
            VideoTitle = null
        }, JsonContext.Default.QueueResponse);
    }

    endpointLogger.LogInformation("Initiating download for VideoId {VideoId}", request.VideoId);
    var result = await downloadService.DownloadAsync(request.VideoId);

    if (result is null)
    {
        endpointLogger.LogError("Download failed for VideoId {VideoId}", request.VideoId);
        return Results.Json(new QueueResponse
        {
            IsValid = false,
            VideoId = null,
            Uuid = null,
            MaxRes = null,
            VideoTitle = null
        }, JsonContext.Default.QueueResponse);
    }

    // Send Discord webhook if needed
    if (result.Valid && !string.IsNullOrEmpty(request.ServerWebhook))
    {
        try
        {
            endpointLogger.LogInformation("Sending Discord webhook for PlayfabId {PlayfabId}", request.PlayfabId);
            await discordService.SendWebhookAsync(request.PlayfabId, request.PlayerName, request.VideoTitle, request.VideoId, request.ServerName, request.ServerWebhook);
            endpointLogger.LogInformation("Discord webhook sent successfully");
        }
        catch (Exception ex)
        {
            endpointLogger.LogError(ex, "Failed to send Discord webhook for PlayfabId {PlayfabId}", request.PlayfabId);
        }
    }

    var uuid = $"{config.Endpoint}:{config.Port}/{result.Uuid}";

    endpointLogger.LogInformation("Queue response prepared for VideoId {VideoId} with UUID {Uuid}", request.VideoId, uuid);

    return Results.Json(new QueueResponse
    {
        IsValid = result.Valid,
        VideoId = request.VideoId,
        Uuid = uuid,
        MaxRes = result.MaxRes,
        VideoTitle = request.VideoTitle
    }, JsonContext.Default.QueueResponse);
}).CacheOutput("Default");

app.MapPost("/Search", async (
    SearchRequest request,
    SearchService searchService,
    ILogger<Program> endpointLogger) =>
{
    endpointLogger.LogInformation("Received /Search request: {@Request}", request);

    if (request == null || string.IsNullOrEmpty(request.SearchString))
    {
        endpointLogger.LogWarning("Search request is invalid or empty");
        return Results.Json(new SearchResponse { IsValid = false, SearchResults = null }, JsonContext.Default.SearchResponse);
    }

    try
    {
        endpointLogger.LogInformation("Performing search with query: {SearchString}", request.SearchString);
        var results = await searchService.SearchAsync(request.SearchString);
        endpointLogger.LogInformation("Search completed with {ResultCount} results", results?.Count ?? 0);
        return Results.Json(new SearchResponse { IsValid = true, SearchResults = results }, JsonContext.Default.SearchResponse);
    }
    catch (Exception ex)
    {
        endpointLogger.LogError(ex, "Search operation failed for query: {SearchString}", request.SearchString);
        return Results.Json(new SearchResponse { IsValid = false, SearchResults = null }, JsonContext.Default.SearchResponse);
    }
}).CacheOutput("Default");

app.MapPost("/playlist", async (
    HttpRequest request,
    PlaylistService playlistService,
    ILogger<Program> endpointLogger) =>
{
    // Retrieve the 'playlistId' from the request headers
    var playlistId = request.Headers["playlistId"].FirstOrDefault();
    endpointLogger.LogInformation("Received /playlist request with PlaylistId: {PlaylistId}", playlistId);

    if (string.IsNullOrEmpty(playlistId))
    {
        endpointLogger.LogWarning("PlaylistId header is missing or empty");
        return Results.Json(new BaseResponse { IsValid = false }, JsonContext.Default.BaseResponse);
    }

    try
    {
        var result = await playlistService.GetPlaylistAsync(playlistId);

        if (result.IsValid)
        {
            endpointLogger.LogInformation("Playlist retrieved successfully for PlaylistId: {PlaylistId}", playlistId);
            return Results.Json(result, JsonContext.Default.PlaylistResponse);
        }
        else
        {
            endpointLogger.LogWarning("Failed to retrieve playlist for PlaylistId: {PlaylistId}", playlistId);
            return Results.Json(new BaseResponse { IsValid = false }, JsonContext.Default.BaseResponse);
        }
    }
    catch (Exception ex)
    {
        endpointLogger.LogError(ex, "Exception occurred while retrieving playlist for PlaylistId: {PlaylistId}", playlistId);
        return Results.Json(new BaseResponse { IsValid = false }, JsonContext.Default.BaseResponse);
    }
}).CacheOutput("Default");

if (radioModConfig is null)
{
    logger.LogCritical("RadioModConfig is null. Application cannot start.");
    throw new InvalidOperationException("There is no Port in appsettings.");
}

app.Urls.Add($"http://*:{radioModConfig.Port}");
logger.LogInformation("Application is configured to listen on port {Port}", radioModConfig.Port);

// Handle application shutdown
app.Lifetime.ApplicationStarted.Register(() => logger.LogInformation("RadioModBackend application has started."));

app.Lifetime.ApplicationStopping.Register(() => logger.LogInformation("RadioModBackend application is stopping..."));

app.Run();

logger.LogInformation("RadioModBackend application has stopped.");
