using System.Text.Json.Serialization;

namespace RadioModBackend.NET.Models;

public class QueueResponse : BaseResponse
{
    [JsonPropertyName("videoId")]
    public string? VideoId { get; set; }

    [JsonPropertyName("uuid")]
    public string? Uuid { get; set; }

    [JsonPropertyName("maxRes")]
    public bool? MaxRes { get; set; }

    [JsonPropertyName("videoTitle")]
    public string? VideoTitle { get; set; }
}
