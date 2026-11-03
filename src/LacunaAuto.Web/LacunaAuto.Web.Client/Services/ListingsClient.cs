using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using LacunaAuto.UI.Shared.Models;

namespace LacunaAuto.Web.Client.Services;

public sealed class ListingsClient(HttpClient httpClient)
{
    private const string _sortYearDesc = "year-desc";
    private const string _sortYearAsc = "year-asc";
    private const int _maximumPageSize = 24;

    private readonly HttpClient _httpClient = httpClient;
    private static readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter(allowIntegerValues: false) }
    };

    public Task<IReadOnlyList<VehicleListingCardModel>> GetListingsAsync(CancellationToken cancellationToken)
    {
        return GetListingsAsync(make: null, sort: null, cancellationToken);
    }

    public Task<IReadOnlyList<VehicleListingCardModel>> GetListingsAsync(string? make, CancellationToken cancellationToken)
    {
        return GetListingsAsync(make, sort: null, cancellationToken);
    }

    public async Task<IReadOnlyList<VehicleListingCardModel>> GetListingsAsync(
        string? make,
        string? sort,
        CancellationToken cancellationToken)
    {
        var route = BuildListingsRoute(make, sort);

        return await _httpClient.GetFromJsonAsync<List<VehicleListingCardModel>>(
            route, _jsonOptions, cancellationToken)
            ?? throw new JsonException("The listings response was empty.");
    }

    public async Task<PagedVehicleListingsModel> GetListingsPageAsync(
        string? make,
        string? sort,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        ValidatePagination(page, pageSize);
        var route = BuildListingsPageRoute(make, sort, page, pageSize);
        return await _httpClient.GetFromJsonAsync<PagedVehicleListingsModel>(route, _jsonOptions, cancellationToken)
            ?? throw new JsonException("The paged listings response was empty.");
    }

    public async Task<IReadOnlyList<string>> GetListingMakesAsync(CancellationToken cancellationToken)
    {
        var makes = await _httpClient.GetFromJsonAsync<List<string>>("api/listings/makes", _jsonOptions, cancellationToken)
            ?? throw new JsonException("The listing makes response was empty.");

        var distinctMakes = new List<string>(makes.Count);
        var seenMakes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var make in makes)
        {
            if (seenMakes.Add(make))
            {
                distinctMakes.Add(make);
            }
        }

        distinctMakes.Sort(StringComparer.OrdinalIgnoreCase);
        return distinctMakes;
    }

    public async Task<VehicleListingCardModel?> GetListingAsync(int id, CancellationToken cancellationToken)
    {
        using var response = await _httpClient.GetAsync($"api/listings/{id}", cancellationToken);
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<VehicleListingCardModel>(_jsonOptions, cancellationToken)
            ?? throw new JsonException("The listing response was empty.");
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

    private static string BuildListingsPageRoute(string? make, string? sort, int page, int pageSize)
    {
        var queryValues = new List<string>(4);

        if (!string.IsNullOrWhiteSpace(make))
        {
            queryValues.Add($"make={Uri.EscapeDataString(make.Trim())}");
        }

        var normalizedSort = NormalizeSort(sort);
        if (normalizedSort is not null)
        {
            queryValues.Add($"sort={Uri.EscapeDataString(normalizedSort)}");
        }

        queryValues.Add($"page={page}");
        queryValues.Add($"pageSize={pageSize}");

        return $"api/listings/page?{string.Join("&", queryValues)}";
    }

    private static void ValidatePagination(int page, int pageSize)
    {
        if (page < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(page), page, "Page must be greater than or equal to 1.");
        }

        if (pageSize < 1 || pageSize > _maximumPageSize)
        {
            throw new ArgumentOutOfRangeException(nameof(pageSize), pageSize, $"Page size must be between 1 and {_maximumPageSize}.");
        }
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
