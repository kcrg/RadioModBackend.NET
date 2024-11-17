using Microsoft.Extensions.FileProviders;
using RadioModBackend.AOT.Models;
using RadioModBackend.NET;
using RadioModBackend.NET.Models;
using RadioModBackend.NET.Services;
using YoutubeExplode;

var builder = WebApplication.CreateSlimBuilder(args);

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

// Use static files
var downloadDir = Path.Combine(Directory.GetCurrentDirectory(), "cache");
Directory.CreateDirectory(downloadDir);

app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(downloadDir),
    RequestPath = ""
});

app.UseOutputCache();

// Map endpoints
app.MapPost("/Queue", async (QueueRequest request, DownloadService downloadService, DiscordService discordService, AppSettings config) =>
{
    if (request == null)
    {
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

    var result = await downloadService.DownloadAsync(request.VideoId);

    if (result is null)
    {
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
        await discordService.SendWebhookAsync(request.PlayfabId, request.PlayerName, request.VideoTitle, request.VideoId, request.ServerName, request.ServerWebhook);
    }

    var uuid = $"{config.Endpoint}:{config.Port}/{result.Uuid}";

    return Results.Json(new QueueResponse
    {
        IsValid = result.Valid,
        VideoId = request.VideoId,
        Uuid = uuid,
        MaxRes = result.MaxRes,
        VideoTitle = request.VideoTitle
    }, JsonContext.Default.QueueResponse);
}).CacheOutput("Default");

app.MapPost("/Search", async (SearchRequest request, SearchService searchService) =>
{
    if (request == null || string.IsNullOrEmpty(request.SearchString))
    {
        return Results.Json(new SearchResponse { IsValid = false, SearchResults = null }, JsonContext.Default.SearchResponse);
    }

    var results = await searchService.SearchAsync(request.SearchString);
    return Results.Json(new SearchResponse { IsValid = true, SearchResults = results }, JsonContext.Default.SearchResponse);
}).CacheOutput("Default");

app.MapPost("/playlist", async (HttpRequest request, PlaylistService playlistService) =>
{
    // Retrieve the 'playlistId' from the request headers
    var playlistId = request.Headers["playlistId"].FirstOrDefault();
    Console.WriteLine("Playlist Id: " + playlistId);

    if (string.IsNullOrEmpty(playlistId))
    {
        return Results.Json(new BaseResponse { IsValid = false }, JsonContext.Default.BaseResponse);
    }

    var result = await playlistService.GetPlaylistAsync(playlistId);

    if (result.IsValid)
    {
        return Results.Json(result, JsonContext.Default.PlaylistResponse);
    }
    else
    {
        Console.WriteLine("Decode failed.");
        return Results.Json(new BaseResponse { IsValid = false }, JsonContext.Default.BaseResponse);
    }
}).CacheOutput("Default");

if (radioModConfig is null)
{
    throw new InvalidOperationException("There is no Port in appsettings.");
}

app.Urls.Add($"http://*:{radioModConfig.Port}");

app.Run();
