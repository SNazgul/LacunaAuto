namespace LacunaAuto.UI.Shared.Models;

/// <summary>
/// Presentation model for vehicle listing cards.
/// </summary>
public sealed class VehicleListingCardModel
{
    public required int Id { get; init; }
    public required string Make { get; init; }
    public required string Model { get; init; }
    public string? TrimOrVariant { get; init; }
    public required int Year { get; init; }
    public required decimal Price { get; init; }
    
    /// <summary>
    /// The currency in which this listing's price is originally expressed.
    /// This is displayed to users and is NOT replaced by user's preferred currency.
    /// </summary>
    public required string OriginalCurrencyCode { get; init; }
    
    public required int Mileage { get; init; }
    public FuelType? FuelType { get; init; }
    public TransmissionType? Transmission { get; init; }
    public string? Location { get; init; }
    public string? ImageUrl { get; init; }
    public bool IsNew { get; init; }
}
