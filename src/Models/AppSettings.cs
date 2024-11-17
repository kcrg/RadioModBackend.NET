namespace RadioModBackend.NET.Models;

public class AppSettings
{
    public string? Endpoint { get; set; }
    public string? Port { get; set; }
    public bool EnableWebhookOnQueue { get; set; } = true;
    public int MaxSearchCount { get; set; } = 15;
    public List<string>? BannedVideoIDs { get; set; }
    public List<string>? BannedTerms { get; set; }
    public List<string>? BannedPlayfabIDs { get; set; }
}
