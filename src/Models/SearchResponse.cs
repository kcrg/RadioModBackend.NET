using System.Text.Json.Serialization;

namespace RadioModBackend.NET.Models;

public class SearchResponse : BaseResponse
{
    [JsonPropertyName("results")]
    public List<SearchResult>? SearchResults { get; set; }
}
