using RadioModBackend.NET.Helpers;
using RadioModBackend.NET.Models;
using YoutubeExplode;
using YoutubeExplode.Common;

namespace RadioModBackend.NET.Services;

public class SearchService(YoutubeClient youtubeClient, AppSettings appSettings)
{
    public async Task<List<SearchResult>> SearchAsync(string query)
    {
        List<SearchResult> searchResults = new(appSettings.MaxSearchCount);

        try
        {
            var searchResponse = await youtubeClient.Search.GetVideosAsync(query).CollectAsync(appSettings.MaxSearchCount);

            foreach (var video in searchResponse)
            {
                if (video.Duration == null || video.Duration.Value.TotalSeconds <= 0 || video.Duration.Value.TotalSeconds >= 600)
                {
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
            }
        }
        catch
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

        return searchResults;
    }
}
