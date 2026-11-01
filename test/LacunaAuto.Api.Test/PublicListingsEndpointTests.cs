using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using LacunaAuto.Core.Listings;
using LacunaAuto.Data.Initialization;
using LacunaAuto.Data.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace LacunaAuto.Api.Test;

public sealed class PublicListingsEndpointTests
{
    [Fact]
    public async Task GetListings_ShouldExcludeUnpublishedListings()
    {
        var cancellationToken = TestContext.Current.CancellationToken;

        await using var factory = new ListingsApiFactory();
        await factory.SeedAsync(
        [
            CreateListing(id: 1, isPublished: true, make: "Tesla", model: "Model 3"),
            CreateListing(id: 2, isPublished: false, make: "BMW", model: "X5")
        ],
            cancellationToken);

        using var client = factory.CreateClient();
        using var response = await client.GetAsync("/api/listings", cancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var listings = await response.Content.ReadFromJsonAsync<List<PublicListingCardResponse>>(cancellationToken);

        Assert.NotNull(listings);
        Assert.Single(listings);
        Assert.Equal(1, listings[0].Id);
    }

    [Fact]
    public async Task GetListings_ShouldReturnPublishedListingsInIdOrderWithExpectedValues()
    {
        var cancellationToken = TestContext.Current.CancellationToken;

        await using var factory = new ListingsApiFactory();
        await factory.SeedAsync(
        [
            CreateListing(
                id: 3,
                isPublished: false,
                make: "Ford",
                model: "Mustang",
                trimOrVariant: "GT"),
            CreateListing(
                id: 2,
                isPublished: true,
                make: "BMW",
                model: "X5",
                trimOrVariant: "xDrive40i",
                fuelType: FuelType.Petrol,
                transmissionType: TransmissionType.Automatic,
                location: "Lviv, Ukraine",
                imageUrl: "/images/vehicles/bmw-x5.svg",
                isNew: false,
                year: 2022,
                price: 62000m,
                mileage: 25000),
            CreateListing(
                id: 1,
                isPublished: true,
                make: "Tesla",
                model: "Model 3",
                trimOrVariant: "Long Range",
                fuelType: FuelType.Electric,
                transmissionType: TransmissionType.Automatic,
                location: "Kyiv, Ukraine",
                imageUrl: "/images/vehicles/tesla-model-3.svg",
                isNew: true,
                year: 2023,
                price: 45000m,
                mileage: 12000)
        ],
            cancellationToken);

        using var client = factory.CreateClient();
        using var response = await client.GetAsync("/api/listings", cancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var listings = await response.Content.ReadFromJsonAsync<List<PublicListingCardResponse>>(cancellationToken);

        Assert.NotNull(listings);
        Assert.Equal(2, listings.Count);

        Assert.Equal(1, listings[0].Id);
        Assert.Equal("Tesla", listings[0].Make);
        Assert.Equal("Model 3", listings[0].Model);
        Assert.Equal("Long Range", listings[0].TrimOrVariant);
        Assert.Equal(2023, listings[0].Year);
        Assert.Equal(45000m, listings[0].Price);
        Assert.Equal("USD", listings[0].OriginalCurrencyCode);
        Assert.Equal(12000, listings[0].Mileage);
        Assert.Equal("Electric", listings[0].FuelType);
        Assert.Equal("Automatic", listings[0].Transmission);
        Assert.Equal("Kyiv, Ukraine", listings[0].Location);
        Assert.Equal("/images/vehicles/tesla-model-3.svg", listings[0].ImageUrl);
        Assert.True(listings[0].IsNew);

        Assert.Equal(2, listings[1].Id);
        Assert.Equal("BMW", listings[1].Make);
    }

    [Fact]
    public async Task GetListings_ShouldSerializeNullOptionalFields()
    {
        var cancellationToken = TestContext.Current.CancellationToken;

        await using var factory = new ListingsApiFactory();
        await factory.SeedAsync(
        [
            CreateListing(
                id: 1,
                isPublished: true,
                make: "Tesla",
                model: "Model 3",
                trimOrVariant: null,
                fuelType: null,
                transmissionType: null,
                location: null,
                imageUrl: null,
                isNew: true)
        ],
            cancellationToken);

        using var client = factory.CreateClient();
        using var response = await client.GetAsync("/api/listings", cancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payload = await response.Content.ReadAsStringAsync(cancellationToken);

        Assert.Contains("\"trimOrVariant\":null", payload);
        Assert.Contains("\"fuelType\":null", payload);
        Assert.Contains("\"transmission\":null", payload);
        Assert.Contains("\"location\":null", payload);
        Assert.Contains("\"imageUrl\":null", payload);

        var listings = JsonSerializer.Deserialize<List<PublicListingCardResponse>>(
            payload,
            new JsonSerializerOptions(JsonSerializerDefaults.Web));

        Assert.NotNull(listings);
        Assert.Single(listings);

        var listing = listings[0];
        Assert.Equal(1, listing.Id);
        Assert.Null(listing.TrimOrVariant);
        Assert.Null(listing.FuelType);
        Assert.Null(listing.Transmission);
        Assert.Null(listing.Location);
        Assert.Null(listing.ImageUrl);
    }

    [Fact]
    public async Task DevelopmentListingsSeeder_ShouldBeIdempotent_WhenRunTwice()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var options = new DbContextOptionsBuilder<LacunaAutoDbContext>()
            .UseInMemoryDatabase($"development-seeder-{Guid.NewGuid()}")
            .Options;

        await using var dbContext = new LacunaAutoDbContext(options);

        await DevelopmentListingsSeeder.SeedAsync(dbContext, cancellationToken);
        await DevelopmentListingsSeeder.SeedAsync(dbContext, cancellationToken);

        var listings = await dbContext.Listings
            .OrderBy(listing => listing.Id)
            .ToListAsync(cancellationToken);

        Assert.Equal(8, listings.Count);
        Assert.Equal([1, 2, 3, 4, 5, 6, 7, 8], listings.Select(listing => listing.Id));
    }

    private static VehicleListing CreateListing(
        int id,
        bool isPublished,
        string make,
        string model,
        string? trimOrVariant = "Base",
        int year = 2023,
        decimal price = 10000m,
        string originalCurrencyCode = "USD",
        int mileage = 1000,
        FuelType? fuelType = FuelType.Petrol,
        TransmissionType? transmissionType = TransmissionType.Automatic,
        string? location = "Kyiv, Ukraine",
        string? imageUrl = "/images/vehicles/default.svg",
        bool isNew = false)
    {
        return new VehicleListing
        {
            Id = id,
            Make = make,
            Model = model,
            TrimOrVariant = trimOrVariant,
            Year = year,
            Price = price,
            OriginalCurrencyCode = originalCurrencyCode,
            Mileage = mileage,
            FuelType = fuelType,
            Transmission = transmissionType,
            Location = location,
            ImageUrl = imageUrl,
            IsNew = isNew,
            IsPublished = isPublished
        };
    }

    private sealed class PublicListingCardResponse
    {
        public int Id { get; init; }

        public string Make { get; init; } = string.Empty;

        public string Model { get; init; } = string.Empty;

        public string? TrimOrVariant { get; init; }

        public int Year { get; init; }

        public decimal Price { get; init; }

        public string OriginalCurrencyCode { get; init; } = string.Empty;

        public int Mileage { get; init; }

        public string? FuelType { get; init; }

        public string? Transmission { get; init; }

        public string? Location { get; init; }

        public string? ImageUrl { get; init; }

        public bool IsNew { get; init; }
    }
}

public sealed class ListingsApiFactory : WebApplicationFactory<Program>
{
    private readonly string _databaseName = $"listings-api-test-{Guid.NewGuid()}";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureAppConfiguration(
            (_, configurationBuilder) =>
            {
                configurationBuilder.AddInMemoryCollection(
                [
                    new KeyValuePair<string, string?>(
                        "ConnectionStrings:LacunaAuto",
                        "Host=localhost;Port=5432;Database=lacunaauto;Username=lacunaauto;Password=lacunaauto_dev_password")
                ]);
            });

        builder.ConfigureServices(
            services =>
            {
                services.RemoveAll(typeof(DbContextOptions<LacunaAutoDbContext>));
                services.AddDbContext<LacunaAutoDbContext>(
                    options =>
                    {
                        options.UseInMemoryDatabase(_databaseName);
                    });
            });
    }

    public async Task SeedAsync(IEnumerable<VehicleListing> listings, CancellationToken cancellationToken = default)
    {
        await using var scope = Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<LacunaAutoDbContext>();

        await dbContext.Database.EnsureDeletedAsync(cancellationToken);
        await dbContext.Database.EnsureCreatedAsync(cancellationToken);

        dbContext.Listings.AddRange(listings);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
