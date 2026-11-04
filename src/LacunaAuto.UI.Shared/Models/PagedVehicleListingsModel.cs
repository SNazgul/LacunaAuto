namespace LacunaAuto.UI.Shared.Models;

public sealed class PagedVehicleListingsModel
{
    public required List<VehicleListingCardModel> Items { get; init; }

    public required int Page { get; init; }

    public required int PageSize { get; init; }

    public required int TotalCount { get; init; }

    public required int TotalPages { get; init; }
}
