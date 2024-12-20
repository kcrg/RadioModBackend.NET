using System.Text.Json.Serialization;

namespace RadioModBackend.NET.Models;

public class YtDlpSearchResult
{
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("title")]
    public string? Title { get; set; }
    [JsonPropertyName("duration")]
    public double Duration { get; set; }

    [JsonPropertyName("duration_string")]
    public string? DurationString { get; set; }

    [JsonPropertyName("uploader")]
    public string? Uploader { get; set; }

    [JsonPropertyName("view_count")]
    public long ViewCount { get; set; }

    [JsonPropertyName("upload_date")]
    public string? UploadDate { get; set; }
}