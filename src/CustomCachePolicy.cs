using Microsoft.AspNetCore.OutputCaching;
using Microsoft.Extensions.Primitives;
using System.Security.Cryptography;
using System.Text;

namespace RadioModBackend.NET;

public sealed class CustomCachePolicy : IOutputCachePolicy
{
    public static readonly CustomCachePolicy Instance = new();

    private CustomCachePolicy()
    {
    }

    async ValueTask IOutputCachePolicy.CacheRequestAsync(
        OutputCacheContext context,
        CancellationToken cancellationToken)
    {
        var attemptOutputCaching = AttemptOutputCaching(context);
        context.EnableOutputCaching = true;
        context.AllowCacheLookup = attemptOutputCaching;
        context.AllowCacheStorage = attemptOutputCaching;
        context.AllowLocking = true;

        // Vary by any query by default
        context.CacheVaryByRules.QueryKeys = "*";

        var request = context.HttpContext.Request;

        if (HttpMethods.IsPost(request.Method))
        {
            // Enable buffering to allow the action to read the body later
            request.EnableBuffering();

            // Read the request body
            using var reader = new StreamReader(request.Body, Encoding.UTF8, leaveOpen: true);
            var bodyContent = await reader.ReadToEndAsync(cancellationToken);

            // Reset the stream position so the action can read it
            request.Body.Position = 0;

            // Option 1: Vary by the raw request body content
            // context.CacheVaryByRules.VaryByValues["RequestBody"] = bodyContent;

            // Option 2: (Recommended) Compute a hash of the request body and vary by the hash
            context.CacheVaryByRules.VaryByValues["RequestBodyHash"] = ComputeHash(bodyContent);
        }
    }

    ValueTask IOutputCachePolicy.ServeFromCacheAsync(
        OutputCacheContext context,
        CancellationToken cancellationToken)
    {
        return ValueTask.CompletedTask;
    }

    ValueTask IOutputCachePolicy.ServeResponseAsync(
        OutputCacheContext context,
        CancellationToken cancellationToken)
    {
        var response = context.HttpContext.Response;

        // Verify existence of cookie headers
        if (!StringValues.IsNullOrEmpty(response.Headers.SetCookie))
        {
            context.AllowCacheStorage = false;
            return ValueTask.CompletedTask;
        }

        // Check response code
        if (response.StatusCode != StatusCodes.Status200OK &&
            response.StatusCode != StatusCodes.Status301MovedPermanently)
        {
            context.AllowCacheStorage = false;
            return ValueTask.CompletedTask;
        }

        return ValueTask.CompletedTask;
    }

    private static bool AttemptOutputCaching(OutputCacheContext context)
    {
        var request = context.HttpContext.Request;

        // Verify the method
        if (!HttpMethods.IsGet(request.Method) &&
            !HttpMethods.IsHead(request.Method) &&
            !HttpMethods.IsPost(request.Method))
        {
            return false;
        }

        // Verify existence of authorization headers
        return StringValues.IsNullOrEmpty(request.Headers.Authorization) &&
               (request.HttpContext.User?.Identity?.IsAuthenticated) != true;
    }

    private static string ComputeHash(string input)
    {
        var bytes = Encoding.UTF8.GetBytes(input);
        var hashBytes = SHA256.HashData(bytes);
        return Convert.ToBase64String(hashBytes);
    }
}
