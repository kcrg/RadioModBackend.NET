using System.Text.Json.Serialization;

namespace RadioModBackend.NET.Models;

public class BaseResponse
{
    [JsonPropertyName("valid")]
    public bool IsValid { get; set; }

    [JsonPropertyName("error")]
    public string? Error { get; set; }
}
