using System.Diagnostics;
using YoutubeExplode;
using YoutubeExplode.Videos.Streams;
using RadioModBackend.NET.Models;
using System.Runtime.InteropServices;

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
                return string.Equals(nameWithoutExtension, videoId, StringComparison.OrdinalIgnoreCase);
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

            // If no cached file, proceed to download using YoutubeExplode
            logger.LogInformation("No cached file found for VideoId: {VideoId}. Initiating download via YoutubeExplode.", videoId);
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
            logger.LogInformation("Download completed via YoutubeExplode for VideoId: {VideoId}", videoId);

            result.Valid = true;
            result.Uuid = fileName;
            result.MaxRes = await CheckStatusCodeAsync($"https://img.youtube.com/vi/{videoId}/maxresdefault.jpg");
            logger.LogInformation("DownloadResult prepared after downloading for VideoId: {VideoId}", videoId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while downloading VideoId: {VideoId} using YoutubeExplode. Attempting yt-dlp fallback.", videoId);
            // Attempt fallback with yt-dlp
            try
            {
                string fileName = $"{videoId}.webm";
                string filePath = Path.Combine("cache", fileName);

                var ytDlpArgs = $"-f bestaudio/webm --output \"cache/%(id)s.%(ext)s\" \"https://www.youtube.com/watch?v={videoId}\"";

                logger.LogInformation("Attempting to download VideoId: {VideoId} using yt-dlp with args: {Args}", videoId, ytDlpArgs);

                string ytdlpName = RuntimeInformation.IsOSPlatform(OSPlatform.Linux) ? "yt-dlp" : "yt-dlp.exe";
                using var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = ytdlpName,
                        Arguments = ytDlpArgs,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    }
                };

                process.Start();
                await process.WaitForExitAsync();

                if (process.ExitCode == 0 && File.Exists(filePath))
                {
                    logger.LogInformation("yt-dlp successfully downloaded VideoId: {VideoId} to {FilePath}", videoId, filePath);
                    result.Valid = true;
                    result.Uuid = fileName;
                    result.MaxRes = await CheckStatusCodeAsync($"https://img.youtube.com/vi/{videoId}/maxresdefault.jpg");
                    logger.LogInformation("DownloadResult prepared after downloading for VideoId: {VideoId} using yt-dlp", videoId);
                }
                else
                {
                    var errorOutput = await process.StandardError.ReadToEndAsync();
                    logger.LogError("yt-dlp failed with exit code {ExitCode} for VideoId: {VideoId}. Error: {ErrorOutput}", process.ExitCode, videoId, errorOutput);
                    result.Error = $"yt-dlp failed: {errorOutput}";
                }
            }
            catch (Exception ytDlpEx)
            {
                logger.LogError(ytDlpEx, "An error occurred while trying yt-dlp for VideoId: {VideoId}", videoId);
                result.Error = ytDlpEx.Message;
            }
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
