namespace LacunaAuto.Api.Contracts.Listings;

public sealed class PublicListingsPageDto
{
    public required List<PublicListingCardDto> Items { get; init; }

    public required int Page { get; init; }

    public required int PageSize { get; init; }

    public required int TotalCount { get; init; }

    public required int TotalPages { get; init; }
}
