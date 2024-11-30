using RadioModBackend.NET.Models;
using RadioModBackend.NET.Services;

namespace RadioModBackend.NET.Endpoints;

public static class QueueEndpoint
{
    public static async Task<IResult> HandleQueueRequest(QueueRequest request, DownloadService downloadService, DiscordService discordService, PermissionsService permissionsService, AppSettings config, ILogger<Startup> endpointLogger)
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

        if (permissionsService.IsPlayfabIdBanned(request.PlayfabId))
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

        if (permissionsService.IsVideoIdBanned(request.VideoId))
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

        if (permissionsService.ContainsBannedTerms(request.VideoTitle))
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

        endpointLogger.LogInformation("Initiating download for VideoId {VideoId}", request.VideoId);
        DownloadResult? result = await downloadService.DownloadAsync(request.VideoId);

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

        string uuid = $"{config.Endpoint}:{config.Port}/{result.Uuid}";

        endpointLogger.LogInformation("Queue response prepared for VideoId {VideoId} with UUID {Uuid}", request.VideoId, uuid);

        return Results.Json(new QueueResponse
        {
            IsValid = result.Valid,
            VideoId = request.VideoId,
            Uuid = uuid,
            MaxRes = result.MaxRes,
            VideoTitle = request.VideoTitle
        }, JsonContext.Default.QueueResponse);
    }
}