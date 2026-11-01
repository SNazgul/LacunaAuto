namespace LacunaAuto.Api.Contracts.Listings;

public sealed class PublicListingCardDto
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
