using System.Text.Json.Serialization;

namespace RadioModBackend.NET.Models;

public class DiscordMessage
{
    [JsonPropertyName("embeds")]
    public DiscordEmbed[]? Embeds { get; set; } = null;
}
