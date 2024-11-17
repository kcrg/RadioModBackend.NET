using System.Text.Json.Serialization;

namespace RadioModBackend.NET.Models;

public class SearchResult
{
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("title")]
    public string? Title { get; set; }

    [JsonPropertyName("timestamp")]
    public string? Timestamp { get; set; }

    [JsonPropertyName("author")]
    public string? Author { get; set; }

    [JsonPropertyName("ago")]
    public string? Ago { get; set; }

    [JsonPropertyName("views")]
    public string? Views { get; set; }

    [JsonPropertyName("seconds")]
    public int? Seconds { get; set; }
}
