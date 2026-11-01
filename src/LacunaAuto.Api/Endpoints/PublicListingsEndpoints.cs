using LacunaAuto.Api.Contracts.Listings;
using LacunaAuto.Core.Listings;
using LacunaAuto.Data.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LacunaAuto.Api.Endpoints;

public static class PublicListingsEndpoints
{
    public static IEndpointRouteBuilder MapPublicListingsEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet(
                "/api/listings",
                async (LacunaAutoDbContext dbContext, CancellationToken cancellationToken) =>
                {
                    var publishedListings = await dbContext.Listings
                        .AsNoTracking()
                        .Where(listing => listing.IsPublished)
                        .OrderBy(listing => listing.Id)
                        .Select(
                            listing =>
                                new PublicListingProjection
                                {
                                    Id = listing.Id,
                                    Make = listing.Make,
                                    Model = listing.Model,
                                    TrimOrVariant = listing.TrimOrVariant,
                                    Year = listing.Year,
                                    Price = listing.Price,
                                    OriginalCurrencyCode = listing.OriginalCurrencyCode,
                                    Mileage = listing.Mileage,
                                    FuelType = listing.FuelType,
                                    Transmission = listing.Transmission,
                                    Location = listing.Location,
                                    ImageUrl = listing.ImageUrl,
                                    IsNew = listing.IsNew
                                })
                        .ToListAsync(cancellationToken);

                    var response = publishedListings
                        .Select(
                            listing =>
                                new PublicListingCardDto
                                {
                                    Id = listing.Id,
                                    Make = listing.Make,
                                    Model = listing.Model,
                                    TrimOrVariant = listing.TrimOrVariant,
                                    Year = listing.Year,
                                    Price = listing.Price,
                                    OriginalCurrencyCode = listing.OriginalCurrencyCode,
                                    Mileage = listing.Mileage,
                                    FuelType = listing.FuelType?.ToString(),
                                    Transmission = listing.Transmission?.ToString(),
                                    Location = listing.Location,
                                    ImageUrl = listing.ImageUrl,
                                    IsNew = listing.IsNew
                                })
                        .ToList();

                    return TypedResults.Ok(response);
                })
            .AllowAnonymous()
            .WithName("GetListings")
            .WithSummary("Gets published listings for public browsing.")
            .WithDescription("Returns listing card data for published listings ordered by listing ID.")
            .Produces<List<PublicListingCardDto>>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status500InternalServerError);

        return endpoints;
    }

    private sealed class PublicListingProjection
    {
        public required int Id { get; init; }

        public required string Make { get; init; }

        public required string Model { get; init; }

        public string? TrimOrVariant { get; init; }

        public required int Year { get; init; }

        public required decimal Price { get; init; }

        public required string OriginalCurrencyCode { get; init; }

        public required int Mileage { get; init; }

        public FuelType? FuelType { get; init; }

        public TransmissionType? Transmission { get; init; }

        public string? Location { get; init; }

        public string? ImageUrl { get; init; }

        public required bool IsNew { get; init; }
    }
}
