using RadioModBackend.NET.Helpers;
using RadioModBackend.NET.Models;
using YoutubeExplode;
using YoutubeExplode.Common;

namespace RadioModBackend.NET.Services;

public class PlaylistService(YoutubeClient youtubeClient, ILogger<PlaylistService> logger)
{
    public async Task<PlaylistResponse> GetPlaylistAsync(string playlistId)
    {
        logger.LogInformation("Initiating retrieval of playlist with ID: {PlaylistId}", playlistId);

        try
        {
            logger.LogDebug("Fetching playlist details for ID: {PlaylistId}", playlistId);
            var playlist = await youtubeClient.Playlists.GetAsync(playlistId);
            logger.LogInformation("Successfully fetched playlist: {PlaylistTitle} (ID: {PlaylistId})", playlist.Title, playlist.Id.Value);

            logger.LogDebug("Fetching videos for playlist ID: {PlaylistId}", playlistId);
            var videos = await youtubeClient.Playlists.GetVideosAsync(playlistId);
            logger.LogInformation("Retrieved {VideoCount} videos for playlist ID: {PlaylistId}", videos.Count, playlistId);

            var results = new List<SearchResult>();

            foreach (var video in videos)
            {
                logger.LogDebug("Processing video ID: {VideoId}, Title: {VideoTitle}", video.Id.Value, video.Title);

                if (video.Duration == null || video.Duration.Value.TotalSeconds <= 0 || video.Duration.Value.TotalSeconds >= 3600)
                {
                    logger.LogWarning("Skipping video ID: {VideoId} due to invalid duration: {DurationSeconds} seconds", video.Id.Value, video.Duration?.TotalSeconds);
                    continue;
                }

                var info = new SearchResult
                {
                    Id = video.Id.Value,
                    Title = video.Title,
                    Timestamp = video.Duration?.TotalSeconds.ToTimestampString(),
                    Author = video.Author.ChannelTitle,
                    Seconds = (int?)video.Duration?.TotalSeconds
                };

                results.Add(info);
                logger.LogDebug("Added video ID: {VideoId} to search results", video.Id.Value);
            }

            if (results.Count == 0)
            {
                logger.LogWarning("No valid videos found in playlist ID: {PlaylistId}", playlistId);
            }

            logger.LogInformation("Completed retrieval of playlist ID: {PlaylistId} with {ValidVideoCount} valid videos", playlistId, results.Count);

            return new PlaylistResponse
            {
                IsValid = true,
                PlaylistId = playlist.Id.Value,
                Title = playlist.Title,
                Results = results
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while retrieving playlist ID: {PlaylistId}", playlistId);
            return new PlaylistResponse { IsValid = false };
        }
    }
}
