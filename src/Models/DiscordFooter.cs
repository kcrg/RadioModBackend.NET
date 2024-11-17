using System.Text.Json.Serialization;

namespace RadioModBackend.NET.Models;

public class DiscordFooter
{
    [JsonPropertyName("text")]
    public string? Text { get; set; }
}
