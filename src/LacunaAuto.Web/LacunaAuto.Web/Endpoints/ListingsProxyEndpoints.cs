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
            ForwardAsync(BuildListingsRoute(make, sort), "api/listings", clients, context, logs, allowBadRequestPassthrough: false));
        endpoints.MapGet("/api/listings/page", (string? make, string? model, string? sort, string? page, string? pageSize, IHttpClientFactory clients, HttpContext context, ILoggerFactory logs) =>
            ForwardAsync(BuildListingsPageRoute(make, model, sort, page, pageSize), "api/listings/page", clients, context, logs, allowBadRequestPassthrough: true));
        endpoints.MapGet("/api/listings/makes", (IHttpClientFactory clients, HttpContext context, ILoggerFactory logs) =>
            ForwardAsync("api/listings/makes", "api/listings/makes", clients, context, logs, allowBadRequestPassthrough: false));
        endpoints.MapGet("/api/listings/models", (string? make, IHttpClientFactory clients, HttpContext context, ILoggerFactory logs) =>
            ForwardAsync(BuildListingsModelsRoute(make), "api/listings/models", clients, context, logs, allowBadRequestPassthrough: true));
        endpoints.MapGet("/api/listings/{id:int}", (int id, IHttpClientFactory clients, HttpContext context, ILoggerFactory logs) =>
            ForwardAsync($"api/listings/{id}", $"api/listings/{id}", clients, context, logs, allowBadRequestPassthrough: false));
        return endpoints;
    }

    private static async Task<IResult> ForwardAsync(
        string route,
        string routeForLogs,
        IHttpClientFactory clients,
        HttpContext context,
        ILoggerFactory logs,
        bool allowBadRequestPassthrough)
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

            if (response.IsSuccessStatusCode ||
                allowBadRequestPassthrough && response.StatusCode == HttpStatusCode.BadRequest)
            {
                return await ForwardContentAsync(response, context);
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

    private static async Task<IResult> ForwardContentAsync(HttpResponseMessage response, HttpContext context)
    {
        var content = await response.Content.ReadAsByteArrayAsync(context.RequestAborted);
        return new ProxiedContentResult(
            content,
            (int)response.StatusCode,
            response.Content.Headers.ContentType?.ToString() ?? "application/json");
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

    private static string BuildListingsPageRoute(string? make, string? model, string? sort, string? page, string? pageSize)
    {
        var queryValues = new List<string>(5);
        if (!string.IsNullOrWhiteSpace(make))
        {
            queryValues.Add($"make={Uri.EscapeDataString(make.Trim())}");
        }

        if (!string.IsNullOrWhiteSpace(model))
        {
            queryValues.Add($"model={Uri.EscapeDataString(model.Trim())}");
        }

        var normalizedSort = NormalizeSort(sort);
        if (normalizedSort is not null)
        {
            queryValues.Add($"sort={Uri.EscapeDataString(normalizedSort)}");
        }

        AppendRawQueryValue(queryValues, "page", page);
        AppendRawQueryValue(queryValues, "pageSize", pageSize);

        return queryValues.Count == 0
            ? "api/listings/page"
            : $"api/listings/page?{string.Join("&", queryValues)}";
    }

    private static string BuildListingsModelsRoute(string? make)
    {
        var queryValues = new List<string>(1);
        AppendRawQueryValue(queryValues, "make", make);
        return queryValues.Count == 0
            ? "api/listings/models"
            : $"api/listings/models?{string.Join("&", queryValues)}";
    }

    private static void AppendRawQueryValue(List<string> queryValues, string queryName, string? queryValue)
    {
        if (string.IsNullOrWhiteSpace(queryValue))
        {
            return;
        }

        queryValues.Add($"{queryName}={Uri.EscapeDataString(queryValue.Trim())}");
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

    private sealed class ProxiedContentResult(byte[] payload, int statusCode, string contentType) : IResult
    {
        private readonly byte[] _payload = payload;
        private readonly int _statusCode = statusCode;
        private readonly string _contentType = contentType;

        public async Task ExecuteAsync(HttpContext httpContext)
        {
            httpContext.Response.StatusCode = _statusCode;
            httpContext.Response.ContentType = _contentType;
            await httpContext.Response.Body.WriteAsync(_payload, httpContext.RequestAborted);
        }
    }
}
