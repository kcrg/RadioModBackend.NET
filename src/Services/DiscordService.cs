using System.Text;
using System.Text.Json;
using RadioModBackend.AOT.Models;
using RadioModBackend.NET.Models;

namespace RadioModBackend.NET.Services;

public class DiscordService(HttpClient httpClient)
{
    public async Task SendWebhookAsync(string playfabId, string playerName, string videoTitle, string videoId, string serverName, string webhookUrl)
    {
        // Build the Discord message
        var message = new DiscordMessage
        {
            Embeds =
            [
                new DiscordEmbed
                {
                    Title = "New Song Queued",
                    Description = $"**Sender:** {playerName} [{playfabId}]\n" +
                                  $"**Song:** [{videoTitle}](https://youtu.be/{videoId})\n" +
                                  $"**Time:** {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC",
                    Color = 0x5227cd, // Example color
                    Footer = new DiscordFooter
                    {
                        Text = serverName
                    }
                }
            ]
        };

        // Serialize the message using the JsonSerializerContext
        var jsonContent = JsonSerializer.Serialize(message, JsonContext.Default.DiscordMessage);
        var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

        // Send the webhook
        var response = await httpClient.PostAsync(webhookUrl, content);
        response.EnsureSuccessStatusCode();
    }
}
