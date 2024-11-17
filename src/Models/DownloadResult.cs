namespace RadioModBackend.NET.Models;

public class DownloadResult
{
    public bool Valid { get; set; }
    public string? Uuid { get; set; }
    public bool? MaxRes { get; set; }
    public string? Proxy { get; set; }
    public string? Error { get; set; }
}
