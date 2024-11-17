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
            logger.LogDebug("Fetching videos for query: {Query}", query);
            var searchResponse = await youtubeClient.Search.GetVideosAsync(query).CollectAsync(appSettings.MaxSearchCount);

            logger.LogInformation("Fetched {Count} videos for query: {Query}", searchResponse.Count, query);

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
                    Ago = video?.SimpleUploadDate ?? "No upload date",
                    Views = video?.ViewCount.ToCompactNumberString(),
                    Seconds = (int?)video?.Duration.Value.TotalSeconds
                };
                searchResults.Add(result);
                logger.LogDebug("Added video {VideoId} to search results", video?.Id);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred during search operation for query: {Query}", query);

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
            logger.LogInformation("Added default search result due to error");
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