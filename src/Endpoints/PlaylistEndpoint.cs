using RadioModBackend.NET.Models;
using RadioModBackend.NET.Services;

namespace RadioModBackend.NET.Endpoints;

public static class PlaylistEndpoint
{
    public static async Task<IResult> HandlePlaylistRequest(HttpRequest request, PlaylistService playlistService, ILogger<Startup> endpointLogger)
    {
        // Retrieve the 'playlistId' from the request headers
        string? playlistId = request.Headers["playlistId"].FirstOrDefault();
        endpointLogger.LogInformation("Received /playlist request with PlaylistId: {PlaylistId}", playlistId);

        if (string.IsNullOrEmpty(playlistId))
        {
            endpointLogger.LogWarning("PlaylistId header is missing or empty");
            return Results.Json(new BaseResponse { IsValid = false }, JsonContext.Default.BaseResponse);
        }

        try
        {
            PlaylistResponse result = await playlistService.GetPlaylistAsync(playlistId);

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
    }
}
