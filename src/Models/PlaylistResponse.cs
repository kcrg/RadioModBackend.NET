using System.Text.Json.Serialization;

namespace RadioModBackend.NET.Models;

public class PlaylistResponse : BaseResponse
{
    [JsonPropertyName("playlistId")]
    public string? PlaylistId { get; set; }

    [JsonPropertyName("title")]
    public string? Title { get; set; }

    [JsonPropertyName("results")]
    public List<SearchResult>? Results { get; set; }
}