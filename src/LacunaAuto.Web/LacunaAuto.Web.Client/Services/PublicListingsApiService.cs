using System.Net.Http.Json;
using System.Text.Json;
using LacunaAuto.UI.Shared.Models;

namespace LacunaAuto.Web.Client.Services;

public sealed class PublicListingsApiService : IPublicListingsService
{
    private const string ListingsEndpoint = "api/listings";
    private static readonly JsonSerializerOptions _jsonSerializerOptions = new(JsonSerializerDefaults.Web);
    private readonly HttpClient _httpClient;

    public PublicListingsApiService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<IReadOnlyList<VehicleListingCardModel>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var listingsResponse = await _httpClient.GetFromJsonAsync<List<PublicListingCardApiDto>>(
            ListingsEndpoint,
            _jsonSerializerOptions,
            cancellationToken);

        if (listingsResponse is null)
        {
            throw new InvalidOperationException("Listings API returned an empty payload.");
        }

        return listingsResponse.Select(MapToVehicleListingCardModel).ToList();
    }

    public async Task<VehicleListingCardModel?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var listings = await GetAllAsync(cancellationToken);
        return listings.FirstOrDefault(listing => listing.Id == id);
    }

    private static VehicleListingCardModel MapToVehicleListingCardModel(PublicListingCardApiDto dto)
    {
        return new VehicleListingCardModel
        {
            Id = dto.Id,
            Make = dto.Make,
            Model = dto.Model,
            TrimOrVariant = dto.TrimOrVariant,
            Year = dto.Year,
            Price = dto.Price,
            OriginalCurrencyCode = dto.OriginalCurrencyCode,
            Mileage = dto.Mileage,
            FuelType = ParseFuelType(dto.FuelType),
            Transmission = ParseTransmissionType(dto.Transmission),
            Location = dto.Location,
            ImageUrl = dto.ImageUrl,
            IsNew = dto.IsNew
        };
    }

    private static FuelType? ParseFuelType(string? fuelType)
    {
        if (string.IsNullOrWhiteSpace(fuelType))
        {
            return null;
        }

        if (!Enum.TryParse<FuelType>(fuelType, ignoreCase: true, out var parsedFuelType))
        {
            throw new InvalidOperationException($"Unsupported fuel type value '{fuelType}'.");
        }

        return parsedFuelType;
    }

    private static TransmissionType? ParseTransmissionType(string? transmissionType)
    {
        if (string.IsNullOrWhiteSpace(transmissionType))
        {
            return null;
        }

        if (!Enum.TryParse<TransmissionType>(transmissionType, ignoreCase: true, out var parsedTransmissionType))
        {
            throw new InvalidOperationException($"Unsupported transmission type value '{transmissionType}'.");
        }

        return parsedTransmissionType;
    }

    private sealed class PublicListingCardApiDto
    {
        public required int Id { get; init; }

        public required string Make { get; init; }

        public required string Model { get; init; }

        public string? TrimOrVariant { get; init; }

        public required int Year { get; init; }

        public required decimal Price { get; init; }

        public required string OriginalCurrencyCode { get; init; }

        public required int Mileage { get; init; }

        public string? FuelType { get; init; }

        public string? Transmission { get; init; }

        public string? Location { get; init; }

        public string? ImageUrl { get; init; }

        public required bool IsNew { get; init; }
    }
}
