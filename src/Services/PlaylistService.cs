using RadioModBackend.NET.Helpers;
using RadioModBackend.NET.Models;
using YoutubeExplode;
using YoutubeExplode.Common;

namespace RadioModBackend.NET.Services;

public class PlaylistService(YoutubeClient youtubeClient)
{
    public async Task<PlaylistResponse> GetPlaylistAsync(string playlistId)
    {
        try
        {
            var playlist = await youtubeClient.Playlists.GetAsync(playlistId);
            var videos = await youtubeClient.Playlists.GetVideosAsync(playlistId);

            var results = new List<SearchResult>();
            foreach (var video in videos)
            {
                var info = new SearchResult
                {
                    Id = video.Id.Value,
                    Title = video.Title,
                    Timestamp = video.Duration?.TotalSeconds.ToTimestampString(),
                    Author = video.Author.ChannelTitle,
                    Seconds = (int?)video.Duration?.TotalSeconds
                };

                results.Add(info);
            }

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
            Console.WriteLine("Playlist Error: " + ex.Message);
            return new PlaylistResponse { IsValid = false };
        }
    }
}
