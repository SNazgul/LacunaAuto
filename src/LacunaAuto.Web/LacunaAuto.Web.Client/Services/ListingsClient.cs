using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using LacunaAuto.UI.Shared.Models;

namespace LacunaAuto.Web.Client.Services;

public sealed class ListingsClient(HttpClient httpClient)
{
    private readonly HttpClient _httpClient = httpClient;
    private static readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter(allowIntegerValues: false) }
    };

    public Task<IReadOnlyList<VehicleListingCardModel>> GetListingsAsync(CancellationToken cancellationToken)
    {
        return GetListingsAsync(make: null, cancellationToken);
    }

    public async Task<IReadOnlyList<VehicleListingCardModel>> GetListingsAsync(string? make, CancellationToken cancellationToken)
    {
        var route = "api/listings";
        if (!string.IsNullOrWhiteSpace(make))
        {
            route = $"api/listings?make={Uri.EscapeDataString(make.Trim())}";
        }

        return await _httpClient.GetFromJsonAsync<List<VehicleListingCardModel>>(
            route, _jsonOptions, cancellationToken)
            ?? throw new JsonException("The listings response was empty.");
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
}
