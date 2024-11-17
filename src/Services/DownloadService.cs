using YoutubeExplode;
using YoutubeExplode.Videos.Streams;
using RadioModBackend.NET.Models;

namespace RadioModBackend.NET.Services;

public class DownloadService(YoutubeClient youtubeClient, HttpClient httpClient)
{
    public async Task<DownloadResult> DownloadAsync(string videoId)
    {
        var result = new DownloadResult();

        string? matchingFile = Directory.GetFiles("cache").FirstOrDefault(file =>
        {
            string nameWithoutExtension = Path.GetFileNameWithoutExtension(file);
            return string.Equals(nameWithoutExtension, videoId, StringComparison.Ordinal);
        });

        if (!string.IsNullOrEmpty(matchingFile))
        {
            result.Valid = true;
            result.Uuid = Path.GetFileName(matchingFile);
            result.MaxRes = await CheckStatusCodeAsync($"https://img.youtube.com/vi/{videoId}/maxresdefault.jpg");
            return result;
        }

        try
        {
            var video = await youtubeClient.Videos.GetAsync(videoId);

            var manifest = await youtubeClient.Videos.Streams.GetManifestAsync(videoId);
            var streamInfo = manifest.GetAudioOnlyStreams().GetWithHighestBitrate();

            if (streamInfo == null)
            {
                result.Error = "No audio streams available.";
                return result;
            }

            string fileName = $"{video.Id}.{streamInfo.Container.Name}";
            string filePath = $"cache/{fileName}";

            await youtubeClient.Videos.Streams.DownloadAsync(streamInfo, filePath);

            result.Valid = true;
            result.Uuid = fileName;
            result.MaxRes = await CheckStatusCodeAsync($"https://img.youtube.com/vi/{videoId}/maxresdefault.jpg");

            return result;
        }
        catch (Exception ex)
        {
            result.Error = ex.Message;
            return result;
        }
    }

    private async Task<bool> CheckStatusCodeAsync(string url)
    {
        try
        {
            var response = await httpClient.GetAsync(url);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }
}
