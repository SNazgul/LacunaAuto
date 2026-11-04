using System.Globalization;
using LacunaAuto.Api.Contracts.Listings;
using LacunaAuto.Core.Listings;
using LacunaAuto.Data.Persistence;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace LacunaAuto.Api.Endpoints;

public static class PublicListingsEndpoints
{
    private const string _sortYearDesc = "year-desc";
    private const string _sortYearAsc = "year-asc";
    private const int _defaultPage = 1;
    private const int _defaultPageSize = 8;
    private const int _maxPageSize = 24;

    public static IEndpointRouteBuilder MapPublicListingsEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet(
                "/api/listings",
                async (string? make, string? sort, LacunaAutoDbContext dbContext, CancellationToken cancellationToken) =>
                {
                    var normalizedMake = string.IsNullOrWhiteSpace(make) ? null : make.Trim();
                    var normalizedSort = NormalizeSort(sort);

                    var publishedListingsQuery = CreatePublishedListingsQuery(dbContext, normalizedMake);
                    var orderedListingsQuery = ApplySort(publishedListingsQuery, normalizedSort);

                    var publishedListings = await SelectPublicListingProjection(orderedListingsQuery)
                        .ToListAsync(cancellationToken);

                    var response = publishedListings
                        .Select(MapPublicListingCard)
                        .ToList();

                    return TypedResults.Ok(response);
                })
            .AllowAnonymous()
            .WithName("GetListings")
            .WithSummary("Gets published listings for public browsing.")
            .WithDescription("Returns listing card data for published listings. Optional `make` applies an exact case-insensitive match. Optional `sort` supports `year-desc` and `year-asc`; default ordering uses listing ID ascending.")
            .Produces<List<PublicListingCardDto>>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status500InternalServerError);

        endpoints.MapGet("/api/listings/page", GetListingsPageAsync)
            .AllowAnonymous()
            .WithName("GetListingsPage")
            .WithSummary("Gets one published listings page for public browsing.")
            .WithDescription("Returns paged listing card data for published listings. Applies published and optional make filters before counting and paging.")
            .Produces<PublicListingsPageDto>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status500InternalServerError);

        endpoints.MapGet(
                "/api/listings/makes",
                async (LacunaAutoDbContext dbContext, CancellationToken cancellationToken) =>
                {
                    var publishedMakes = await dbContext.Listings
                        .AsNoTracking()
                        .Where(listing => listing.IsPublished)
                        .Select(listing => listing.Make)
                        .Distinct()
                        .OrderBy(make => make)
                        .ToListAsync(cancellationToken);

                    var visibleMakes = new List<string>(publishedMakes.Count);
                    var seenMakes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    foreach (var publishedMake in publishedMakes)
                    {
                        if (seenMakes.Add(publishedMake))
                        {
                            visibleMakes.Add(publishedMake);
                        }
                    }

                    return TypedResults.Ok(visibleMakes);
                })
            .AllowAnonymous()
            .WithName("GetListingMakes")
            .WithSummary("Gets published listing makes for public browsing.")
            .WithDescription("Returns case-insensitive distinct make options from all published listings.")
            .Produces<List<string>>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status500InternalServerError);

        endpoints.MapGet("/api/listings/{id:int}", GetListingAsync)
            .AllowAnonymous()
            .WithName("GetListing")
            .WithSummary("Gets a published listing by ID.")
            .WithDescription("Returns the fields used by the current details screen. Unpublished and missing listings return 404.")
            .ProducesProblem(StatusCodes.Status500InternalServerError);

        return endpoints;
    }

    private static async Task<Results<Ok<PublicListingCardDto>, NotFound>> GetListingAsync(
        int id, LacunaAutoDbContext dbContext, CancellationToken cancellationToken)
    {
        var listing = await SelectPublicListingProjection(
                dbContext.Listings.AsNoTracking()
                    .Where(listing => listing.IsPublished && listing.Id == id))
            .SingleOrDefaultAsync(cancellationToken);

        return listing is null ? TypedResults.NotFound() : TypedResults.Ok(MapPublicListingCard(listing));
    }

    private static async Task<IResult> GetListingsPageAsync(
        string? make,
        string? sort,
        string? page,
        string? pageSize,
        HttpContext context,
        LacunaAutoDbContext dbContext,
        CancellationToken cancellationToken)
    {
        if (!TryParsePagination(page, pageSize, out var normalizedPage, out var normalizedPageSize, out var parseError))
        {
            return CreateInvalidPaginationProblem(context, parseError);
        }

        var normalizedMake = string.IsNullOrWhiteSpace(make) ? null : make.Trim();
        var normalizedSort = NormalizeSort(sort);

        var publishedListingsQuery = CreatePublishedListingsQuery(dbContext, normalizedMake);
        var totalCount = await publishedListingsQuery.CountAsync(cancellationToken);
        var totalPages = totalCount == 0
            ? 0
            : (int)Math.Ceiling(totalCount / (double)normalizedPageSize);

        if (totalCount == 0 || normalizedPage > totalPages)
        {
            return TypedResults.Ok(
                new PublicListingsPageDto
                {
                    Items = [],
                    Page = normalizedPage,
                    PageSize = normalizedPageSize,
                    TotalCount = totalCount,
                    TotalPages = totalPages
                });
        }

        var offset = (int)(((long)normalizedPage - 1L) * normalizedPageSize);
        var pageItems = await SelectPublicListingProjection(
                ApplySort(publishedListingsQuery, normalizedSort)
                    .Skip(offset)
                    .Take(normalizedPageSize))
            .ToListAsync(cancellationToken);

        return TypedResults.Ok(
            new PublicListingsPageDto
            {
                Items = pageItems.Select(MapPublicListingCard).ToList(),
                Page = normalizedPage,
                PageSize = normalizedPageSize,
                TotalCount = totalCount,
                TotalPages = totalPages
            });
    }

    private static IQueryable<VehicleListing> CreatePublishedListingsQuery(
        LacunaAutoDbContext dbContext,
        string? normalizedMake)
    {
        var publishedListingsQuery = dbContext.Listings
            .AsNoTracking()
            .Where(listing => listing.IsPublished);

        if (normalizedMake is null)
        {
            return publishedListingsQuery;
        }

        return ApplyMakeFilter(publishedListingsQuery, dbContext, normalizedMake);
    }

    private static IQueryable<VehicleListing> ApplyMakeFilter(
        IQueryable<VehicleListing> listings,
        LacunaAutoDbContext dbContext,
        string make)
    {
        if (dbContext.Database.IsNpgsql())
        {
            var escapedMake = EscapeLikePattern(make);
            return listings.Where(listing => EF.Functions.ILike(listing.Make, escapedMake, "\\"));
        }

        return listings.Where(listing => string.Equals(listing.Make, make, StringComparison.InvariantCultureIgnoreCase));
    }

    private static IOrderedQueryable<VehicleListing> ApplySort(IQueryable<VehicleListing> listings, string? sort)
    {
        return sort switch
        {
            _sortYearDesc => listings
                .OrderByDescending(listing => listing.Year)
                .ThenBy(listing => listing.Id),
            _sortYearAsc => listings
                .OrderBy(listing => listing.Year)
                .ThenBy(listing => listing.Id),
            _ => listings.OrderBy(listing => listing.Id)
        };
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

    private static bool TryParsePagination(
        string? page,
        string? pageSize,
        out int normalizedPage,
        out int normalizedPageSize,
        out string parseError)
    {
        if (!TryParsePositiveQueryValue(page, "page", _defaultPage, out normalizedPage, out parseError))
        {
            normalizedPageSize = _defaultPageSize;
            return false;
        }

        if (!TryParsePositiveQueryValue(pageSize, "pageSize", _defaultPageSize, out normalizedPageSize, out parseError))
        {
            return false;
        }

        if (normalizedPageSize > _maxPageSize)
        {
            parseError = $"Query parameter 'pageSize' must be between 1 and {_maxPageSize}.";
            return false;
        }

        parseError = string.Empty;
        return true;
    }

    private static bool TryParsePositiveQueryValue(
        string? value,
        string parameterName,
        int defaultValue,
        out int parsedValue,
        out string parseError)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            parsedValue = defaultValue;
            parseError = string.Empty;
            return true;
        }

        if (!int.TryParse(value.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out parsedValue))
        {
            parseError = $"Query parameter '{parameterName}' must be a valid 32-bit integer.";
            return false;
        }

        if (parsedValue < 1)
        {
            parseError = $"Query parameter '{parameterName}' must be greater than or equal to 1.";
            return false;
        }

        parseError = string.Empty;
        return true;
    }

    private static ProblemHttpResult CreateInvalidPaginationProblem(HttpContext context, string detail)
    {
        return TypedResults.Problem(
            statusCode: StatusCodes.Status400BadRequest,
            title: "Invalid pagination parameters.",
            detail: detail,
            extensions: new Dictionary<string, object?>
            {
                ["traceId"] = context.TraceIdentifier
            });
    }

    private static string EscapeLikePattern(string value)
    {
        return value
            .Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("%", "\\%", StringComparison.Ordinal)
            .Replace("_", "\\_", StringComparison.Ordinal);
    }

    private static IQueryable<PublicListingProjection> SelectPublicListingProjection(IQueryable<VehicleListing> listings)
    {
        return listings.Select(
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
                });
    }

    private static PublicListingCardDto MapPublicListingCard(PublicListingProjection listing)
    {
        return new PublicListingCardDto
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
        };
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
