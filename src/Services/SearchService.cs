using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text.Json;
using RadioModBackend.NET.Helpers;
using RadioModBackend.NET.Models;
using YoutubeExplode;
using YoutubeExplode.Common;

namespace RadioModBackend.NET.Services;

public class SearchService(YoutubeClient youtubeClient, AppSettings appSettings, ILogger<SearchService> logger)
{
    public async Task<List<SearchResult>> SearchAsync(string query)
    {
        logger.LogInformation("Initiating search with query: {Query}", query);
        List<SearchResult> searchResults = new(appSettings.MaxSearchCount);

        try
        {
            logger.LogDebug("Fetching videos for query: {Query} using YoutubeExplode", query);

            var searchResponse = await youtubeClient.Search.GetVideosAsync(query).CollectAsync(appSettings.MaxSearchCount);

            logger.LogInformation("Fetched {Count} videos for query: {Query} from YoutubeExplode", searchResponse.Count, query);

            foreach (var video in searchResponse)
            {
                if (video.Duration == null || video.Duration.Value.TotalSeconds <= 0 || video.Duration.Value.TotalSeconds >= 600)
                {
                    logger.LogDebug("Skipping video {VideoId} due to invalid duration: {Duration} seconds", video.Id, video.Duration?.TotalSeconds);
                    continue;
                }

                var result = new SearchResult
                {
                    Id = video.Id,
                    Title = video.Title,
                    Timestamp = video.Duration!.Value.TotalSeconds.ToTimestampString(),
                    Author = video.Author.ChannelTitle,
                    Ago = video.SimpleUploadDate ?? "No upload date",
                    Views = video.ViewCount.ToCompactNumberString(),
                    Seconds = (int?)video.Duration.Value.TotalSeconds
                };
                searchResults.Add(result);
                logger.LogDebug("Added video {VideoId} to search results", video.Id);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred during search operation for query: {Query} using YoutubeExplode. Attempting yt-dlp fallback.", query);

            // Attempt fallback with yt-dlp
            try
            {
                logger.LogInformation("Attempting fallback search with yt-dlp for query: {Query}", query);

                var maxCount = appSettings.MaxSearchCount;
                var ytDlpArgs = $"--flat-playlist --skip-download --print-json --ignore-config \"ytsearch{maxCount}:{query}\"";

                // We'll write yt-dlp output directly to results.json
                const string resultsFilePath = "results.json";
                if (File.Exists(resultsFilePath))
                {
                    File.Delete(resultsFilePath);
                }

                string ytdlpName = RuntimeInformation.IsOSPlatform(OSPlatform.Linux) ? "yt-dlp" : "yt-dlp.exe";
                using (var process = new Process
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
                })
                {
                    process.Start();

                    await using (var fileStream = File.Create(resultsFilePath))
                    await using (var writer = new StreamWriter(fileStream))
                    {
                        while (!process.StandardOutput.EndOfStream)
                        {
                            var line = await process.StandardOutput.ReadLineAsync();
                            if (line != null)
                                await writer.WriteLineAsync(line);
                        }
                    }

                    await process.WaitForExitAsync();

                    if (process.ExitCode != 0)
                    {
                        var errorOutput = await process.StandardError.ReadToEndAsync();
                        logger.LogError("yt-dlp search failed with exit code {ExitCode}. Error: {Error}", process.ExitCode, errorOutput);
                    }
                }

                if (File.Exists(resultsFilePath))
                {
                    var lines = await File.ReadAllLinesAsync(resultsFilePath);

                    // Wrap the separate JSON objects (one per line) into a JSON array
                    var jsonArray = "[" + string.Join(",", lines) + "]";

                    // Deserialize the entire array at once
                    var ytResults = JsonSerializer.Deserialize(jsonArray, JsonContext.Default.ListYtDlpSearchResult);

                    if (ytResults != null)
                    {
                        foreach (var r in ytResults)
                        {
                            // Skip invalid durations
                            if (r.Duration <= 0 || r.Duration >= 600)
                            {
                                logger.LogDebug("Skipping video {Id} from yt-dlp results due to invalid duration: {Duration} seconds", r.Id, r.Duration);
                                continue;
                            }

                            var uploadDate = "No upload date";
                            if (!string.IsNullOrEmpty(r.UploadDate) && r.UploadDate.Length == 8)
                            {
                                uploadDate = $"{r.UploadDate.Substring(0, 4)}-{r.UploadDate.Substring(4, 2)}-{r.UploadDate.Substring(6, 2)}";
                            }

                            var searchResult = new SearchResult
                            {
                                Id = r.Id ?? "",
                                Title = r.Title ?? "Unknown Title",
                                Timestamp = r.DurationString ?? r.Duration.ToTimestampString(),
                                Author = r.Uploader ?? "Unknown",
                                Ago = uploadDate,
                                Views = r.ViewCount.ToCompactNumberString(),
                                Seconds = (int)r.Duration
                            };

                            searchResults.Add(searchResult);
                            logger.LogDebug("Added video {Id} from yt-dlp to search results", r.Id);
                        }
                    }

                    // If still no results, add a default
                    if (searchResults.Count == 0)
                    {
                        logger.LogInformation("No results found via yt-dlp fallback, adding default search result.");
                        searchResults.Add(new SearchResult
                        {
                            Id = "tkzY_VwNIek",
                            Title = "Ween - Ocean Man",
                            Timestamp = "2:08",
                            Author = "Ween",
                            Ago = "6 years ago",
                            Views = "22M",
                            Seconds = 128
                        });
                    }
                }
            }
            catch (Exception ytDlpEx)
            {
                logger.LogError(ytDlpEx, "An error occurred while trying yt-dlp for query: {Query}", query);

                // If yt-dlp also fails, add a default
                if (searchResults.Count == 0)
                {
                    searchResults.Add(new SearchResult
                    {
                        Id = "tkzY_VwNIek",
                        Title = "Ween - Ocean Man",
                        Timestamp = "2:08",
                        Author = "Ween",
                        Ago = "6 years ago",
                        Views = "22M",
                        Seconds = 128
                    });
                }
            }
        }

        if (searchResults.Count == 0)
        {
            logger.LogWarning("No valid search results found for query: {Query}. Adding default search result.", query);
            searchResults.Add(new SearchResult
            {
                Id = "tkzY_VwNIek",
                Title = "Ween - Ocean Man",
                Timestamp = "2:08",
                Author = "Ween",
                Ago = "6 years ago",
                Views = "22M",
                Seconds = 128
            });
        }

        logger.LogInformation("Search operation completed for query: {Query}. Total results: {Count}", query, searchResults.Count);
        return searchResults;
    }
}