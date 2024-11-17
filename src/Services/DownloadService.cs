using YoutubeExplode;
using YoutubeExplode.Videos.Streams;
using RadioModBackend.NET.Models;

namespace RadioModBackend.NET.Services;

public class DownloadService(YoutubeClient youtubeClient, HttpClient httpClient, ILogger<DownloadService> logger)
{
    public async Task<DownloadResult> DownloadAsync(string videoId)
    {
        logger.LogInformation("Starting download process for VideoId: {VideoId}", videoId);

        var result = new DownloadResult();

        try
        {
            // Check for existing cached file
            logger.LogDebug("Checking for cached files for VideoId: {VideoId}", videoId);
            string? matchingFile = Directory.GetFiles("cache").FirstOrDefault(file =>
            {
                string nameWithoutExtension = Path.GetFileNameWithoutExtension(file);
                return string.Equals(nameWithoutExtension, videoId, StringComparison.Ordinal);
            });

            if (!string.IsNullOrEmpty(matchingFile))
            {
                logger.LogInformation("Cached file found for VideoId: {VideoId} at {FilePath}", videoId, matchingFile);
                result.Valid = true;
                result.Uuid = Path.GetFileName(matchingFile);
                result.MaxRes = await CheckStatusCodeAsync($"https://img.youtube.com/vi/{videoId}/maxresdefault.jpg");
                logger.LogInformation("DownloadResult prepared from cache for VideoId: {VideoId}", videoId);
                return result;
            }

            // If no cached file, proceed to download
            logger.LogInformation("No cached file found for VideoId: {VideoId}. Initiating download.", videoId);
            var video = await youtubeClient.Videos.GetAsync(videoId);
            logger.LogDebug("Fetched video details for VideoId: {VideoId}. Title: {Title}", videoId, video.Title);

            var manifest = await youtubeClient.Videos.Streams.GetManifestAsync(videoId);
            logger.LogDebug("Retrieved stream manifest for VideoId: {VideoId}", videoId);

            var streamInfo = manifest.GetAudioOnlyStreams().GetWithHighestBitrate();
            if (streamInfo == null)
            {
                logger.LogWarning("No audio streams available for VideoId: {VideoId}", videoId);
                result.Error = "No audio streams available.";
                return result;
            }

            string fileName = $"{video.Id}.{streamInfo.Container.Name}";
            string filePath = Path.Combine("cache", fileName);
            logger.LogInformation("Downloading audio stream for VideoId: {VideoId} to {FilePath}", videoId, filePath);

            await youtubeClient.Videos.Streams.DownloadAsync(streamInfo, filePath);
            logger.LogInformation("Download completed for VideoId: {VideoId}", videoId);

            result.Valid = true;
            result.Uuid = fileName;
            result.MaxRes = await CheckStatusCodeAsync($"https://img.youtube.com/vi/{videoId}/maxresdefault.jpg");
            logger.LogInformation("DownloadResult prepared after downloading for VideoId: {VideoId}", videoId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while downloading VideoId: {VideoId}", videoId);
            result.Error = ex.Message;
        }

        return result;
    }

    private async Task<bool> CheckStatusCodeAsync(string url)
    {
        logger.LogDebug("Checking status code for URL: {Url}", url);
        try
        {
            var response = await httpClient.GetAsync(url);
            logger.LogDebug("Received status code {StatusCode} for URL: {Url}", response.StatusCode, url);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
           logger.LogError(ex, "Failed to check status code for URL: {Url}", url);
            return false;
        }
    }
}
