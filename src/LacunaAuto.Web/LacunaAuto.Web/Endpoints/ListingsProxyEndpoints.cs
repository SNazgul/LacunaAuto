using System.Diagnostics;
using System.Net;

namespace LacunaAuto.Web.Endpoints;

public static class ListingsProxyEndpoints
{
    public static IEndpointRouteBuilder MapListingsProxyEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/api/listings", (IHttpClientFactory clients, HttpContext context, ILoggerFactory logs) =>
            ForwardAsync("api/listings", clients, context, logs));
        endpoints.MapGet("/api/listings/{id:int}", (int id, IHttpClientFactory clients, HttpContext context, ILoggerFactory logs) =>
            ForwardAsync($"api/listings/{id}", clients, context, logs));
        return endpoints;
    }

    private static async Task<IResult> ForwardAsync(
        string route, IHttpClientFactory clients, HttpContext context, ILoggerFactory logs)
    {
        var logger = logs.CreateLogger("ListingsApi");
        try
        {
            using var response = await clients.CreateClient("ListingsApi")
                .GetAsync(route, context.RequestAborted);
            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                // Write a body so the Web host does not re-execute its HTML 404 page.
                return Results.Json(new { title = "Listing not found", status = 404 },
                    contentType: "application/problem+json", statusCode: 404);
            }

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsByteArrayAsync(context.RequestAborted);
                return Results.Bytes(content, response.Content.Headers.ContentType?.ToString() ?? "application/json");
            }

            logger.LogWarning("Listings API returned {StatusCode} for {RequestPath}. TraceId {TraceId}, SpanId {SpanId}",
                (int)response.StatusCode, route, Activity.Current?.TraceId, Activity.Current?.SpanId);
        }
        catch (Exception exception) when (exception is HttpRequestException ||
            exception is OperationCanceledException && !context.RequestAborted.IsCancellationRequested)
        {
            logger.LogWarning(exception, "Listings API unavailable for {RequestPath}. TraceId {TraceId}, SpanId {SpanId}",
                route, Activity.Current?.TraceId, Activity.Current?.SpanId);
        }

        return Results.Problem(statusCode: StatusCodes.Status502BadGateway, title: "Listings are temporarily unavailable.",
            extensions: new Dictionary<string, object?> { ["traceId"] = context.TraceIdentifier });
    }
}
