using System.Text.Json.Serialization;

namespace RadioModBackend.NET.Models;

public class DiscordEmbed
{
    [JsonPropertyName("title")]
    public string? Title { get; set; }

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("color")]
    public int? Color { get; set; }

    [JsonPropertyName("footer")]
    public DiscordFooter? Footer { get; set; }
}
