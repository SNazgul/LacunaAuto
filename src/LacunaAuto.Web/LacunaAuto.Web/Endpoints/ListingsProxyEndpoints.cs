using System.Diagnostics;
using System.Net;

namespace LacunaAuto.Web.Endpoints;

public static class ListingsProxyEndpoints
{
    private const string _sortYearDesc = "year-desc";
    private const string _sortYearAsc = "year-asc";

    public static IEndpointRouteBuilder MapListingsProxyEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/api/listings", (string? make, string? sort, IHttpClientFactory clients, HttpContext context, ILoggerFactory logs) =>
            ForwardAsync(BuildListingsRoute(make, sort), "api/listings", clients, context, logs));
        endpoints.MapGet("/api/listings/{id:int}", (int id, IHttpClientFactory clients, HttpContext context, ILoggerFactory logs) =>
            ForwardAsync($"api/listings/{id}", $"api/listings/{id}", clients, context, logs));
        return endpoints;
    }

    private static async Task<IResult> ForwardAsync(
        string route,
        string routeForLogs,
        IHttpClientFactory clients,
        HttpContext context,
        ILoggerFactory logs)
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
                (int)response.StatusCode, routeForLogs, Activity.Current?.TraceId, Activity.Current?.SpanId);
        }
        catch (Exception exception) when (exception is HttpRequestException ||
            exception is OperationCanceledException && !context.RequestAborted.IsCancellationRequested)
        {
            logger.LogWarning("Listings API unavailable for {RequestPath}. ExceptionType {ExceptionType}. TraceId {TraceId}, SpanId {SpanId}",
                routeForLogs, exception.GetType().Name, Activity.Current?.TraceId, Activity.Current?.SpanId);
        }

        return Results.Problem(statusCode: StatusCodes.Status502BadGateway, title: "Listings are temporarily unavailable.",
            extensions: new Dictionary<string, object?> { ["traceId"] = context.TraceIdentifier });
    }

    private static string BuildListingsRoute(string? make, string? sort)
    {
        var queryValues = new List<string>(2);
        if (!string.IsNullOrWhiteSpace(make))
        {
            queryValues.Add($"make={Uri.EscapeDataString(make.Trim())}");
        }

        var normalizedSort = NormalizeSort(sort);
        if (normalizedSort is not null)
        {
            queryValues.Add($"sort={Uri.EscapeDataString(normalizedSort)}");
        }

        return queryValues.Count == 0
            ? "api/listings"
            : $"api/listings?{string.Join("&", queryValues)}";
    }

    private static string? NormalizeSort(string? sort)
    {
        if (string.IsNullOrWhiteSpace(sort))
        {
            return null;
        }

        var normalizedSort = sort.Trim();
        return normalizedSort is _sortYearDesc or _sortYearAsc ? normalizedSort : null;
    }
}
