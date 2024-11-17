using System.Text.Json.Serialization;

namespace RadioModBackend.NET.Models;

public class SearchRequest
{
    [JsonPropertyName("searchString")]
    public required string SearchString { get; set; }
}
