namespace LacunaAuto.Core.Listings;

public sealed class VehicleListing
{
    public int Id { get; set; }

    public string Make { get; set; } = string.Empty;

    public string Model { get; set; } = string.Empty;

    public string? TrimOrVariant { get; set; }

    public int Year { get; set; }

    public decimal Price { get; set; }

    public string OriginalCurrencyCode { get; set; } = string.Empty;

    public int Mileage { get; set; }

    public FuelType? FuelType { get; set; }

    public TransmissionType? Transmission { get; set; }

    public string? Location { get; set; }

    public string? ImageUrl { get; set; }

    public bool IsNew { get; set; }

    public bool IsPublished { get; set; }
}
