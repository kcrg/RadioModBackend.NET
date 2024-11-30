using RadioModBackend.NET.Models;
using RadioModBackend.NET.Services;

namespace RadioModBackend.NET.Endpoints;

public static class SearchEndpoint
{
    public static async Task<IResult> HandleSearchRequest(SearchRequest request, SearchService searchService, ILogger<Startup> endpointLogger)
    {
        endpointLogger.LogInformation("Received /Search request: {@Request}", request);

        if (request == null || string.IsNullOrEmpty(request.SearchString))
        {
            endpointLogger.LogWarning("Search request is invalid or empty");
            return Results.Json(new SearchResponse { IsValid = false, SearchResults = null }, JsonContext.Default.SearchResponse);
        }

        try
        {
            endpointLogger.LogInformation("Performing search with query: {SearchString}", request.SearchString);
            List<SearchResult>? results = await searchService.SearchAsync(request.SearchString);
            endpointLogger.LogInformation("Search completed with {ResultCount} results", results?.Count ?? 0);
            return Results.Json(new SearchResponse { IsValid = true, SearchResults = results }, JsonContext.Default.SearchResponse);
        }
        catch (Exception ex)
        {
            endpointLogger.LogError(ex, "Search operation failed for query: {SearchString}", request.SearchString);
            return Results.Json(new SearchResponse { IsValid = false, SearchResults = null }, JsonContext.Default.SearchResponse);
        }
    }
}
