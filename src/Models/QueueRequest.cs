using System.Text.Json.Serialization;

namespace RadioModBackend.NET.Models;

public class QueueRequest
{
    [JsonPropertyName("videoId")]
    public required string VideoId { get; set; }

    [JsonPropertyName("videoTitle")]
    public required string VideoTitle { get; set; }

    [JsonPropertyName("server")]
    public required string ServerName { get; set; }

    [JsonPropertyName("playfabId")]
    public required string PlayfabId { get; set; }

    [JsonPropertyName("playerName")]
    public required string PlayerName { get; set; }

    [JsonPropertyName("serverWebhook")]
    public required string ServerWebhook { get; set; }
}
