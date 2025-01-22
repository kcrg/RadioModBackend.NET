using Microsoft.Extensions.FileProviders;
using RadioModBackend.NET.Endpoints;
using RadioModBackend.NET.Models;
using RadioModBackend.NET.Services;
using YoutubeExplode;

namespace RadioModBackend.NET;

public class Startup
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateSlimBuilder(args);

        // Configure Logging
        builder.Logging.ClearProviders();
        builder.Logging.AddConsole();
        builder.Logging.AddDebug();

        // Add configuration from appsettings.json
        builder.Configuration.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

        // Configure services
        builder.Services.Configure<AppSettings>(builder.Configuration.GetSection("RadioMod"));
        var radioModConfig = builder.Configuration.GetSection("RadioMod").Get<AppSettings>();
        builder.Services.AddSingleton(radioModConfig!);

        builder.Services.ConfigureHttpJsonOptions(options => options.SerializerOptions.TypeInfoResolverChain.Insert(0, JsonContext.Default));

        builder.Services.AddSingleton<YoutubeClient>();
        builder.Services.AddSingleton<PermissionsService>();
        builder.Services.AddSingleton<SearchService>();
        builder.Services.AddSingleton<PlaylistService>();
        builder.Services.AddHttpClient<DownloadService>();
        builder.Services.AddHttpClient<DiscordService>();

        // Add logging
        builder.Services.AddLogging();

        builder.Services.AddOutputCache(options =>
        {
            options.AddBasePolicy(builder => builder
                .Expire(TimeSpan.FromDays(60)));
            options.AddPolicy("Default", CustomCachePolicy.Instance);
            options.SizeLimit = 250_000_000; // 2GB
        });

        // Build the app
        var app = builder.Build();

        // Create a logger instance
        var logger = app.Services.GetRequiredService<ILogger<Startup>>();

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

        // Map /Queue endpoint
        app.MapPost("/Queue", QueueEndpoint.HandleQueueRequest).CacheOutput("Default");

        // Map /Search endpoint
        app.MapPost("/Search", SearchEndpoint.HandleSearchRequest).CacheOutput("Default");

        // Map /playlist endpoint
        app.MapPost("/playlist", PlaylistEndpoint.HandlePlaylistRequest).CacheOutput("Default");

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
    }
}
