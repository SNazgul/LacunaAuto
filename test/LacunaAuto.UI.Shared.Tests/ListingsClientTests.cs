using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using LacunaAuto.UI.Shared.Models;
using LacunaAuto.Web.Client.Services;

namespace LacunaAuto.UI.Shared.Tests;

// Preserves the client-contract coverage from feature/listings-ui-api-2-1b,
// adapted to the dedicated details endpoint and JsonException error contract.
public sealed class ListingsClientTests
{
    private const string _cardJson = """
        {
          "id": 8, "make": "Tesla", "model": "Model 3", "trimOrVariant": "Long Range",
          "year": 2023, "price": 45000, "originalCurrencyCode": "USD", "mileage": 12000,
          "fuelType": "Electric", "transmission": "Automatic", "location": "Kyiv, Ukraine",
          "imageUrl": "/images/vehicles/tesla-model-3.svg", "isNew": true
        }
        """;

    [Fact]
    public async Task GetListings_ShouldPreserveAllPublicCardValues()
    {
        using var http = CreateHttpClient(request =>
        {
            Assert.Equal("/api/listings", request.RequestUri!.AbsolutePath);
            return JsonResponse($"[{_cardJson}]");
        });
        var service = new ListingsClient(http);
        var listing = Assert.Single(await service.GetListingsAsync(TestContext.Current.CancellationToken));

        Assert.Equal(8, listing.Id);
        Assert.Equal("Tesla", listing.Make);
        Assert.Equal("Model 3", listing.Model);
        Assert.Equal("Long Range", listing.TrimOrVariant);
        Assert.Equal(2023, listing.Year);
        Assert.Equal(45000m, listing.Price);
        Assert.Equal("USD", listing.OriginalCurrencyCode);
        Assert.Equal(12000, listing.Mileage);
        Assert.Equal(FuelType.Electric, listing.FuelType);
        Assert.Equal(TransmissionType.Automatic, listing.Transmission);
        Assert.Equal("Kyiv, Ukraine", listing.Location);
        Assert.Equal("/images/vehicles/tesla-model-3.svg", listing.ImageUrl);
        Assert.True(listing.IsNew);
    }

    [Fact]
    public async Task GetListings_ShouldAppendEncodedMakeQueryWhenProvided()
    {
        using var http = CreateHttpClient(request =>
        {
            Assert.Equal("/api/listings", request.RequestUri!.AbsolutePath);
            Assert.Equal("?make=Auto%25_%20Group", request.RequestUri.Query);
            return JsonResponse($"[{_cardJson}]");
        });
        var service = new ListingsClient(http);

        var listing = Assert.Single(await service.GetListingsAsync("Auto%_ Group", TestContext.Current.CancellationToken));
        Assert.Equal("Tesla", listing.Make);
    }

    [Fact]
    public async Task GetListings_ShouldAppendSortQueryWhenSupported()
    {
        using var http = CreateHttpClient(request =>
        {
            Assert.Equal("/api/listings", request.RequestUri!.AbsolutePath);
            Assert.Equal("?sort=year-desc", request.RequestUri.Query);
            return JsonResponse($"[{_cardJson}]");
        });
        var service = new ListingsClient(http);

        var listing = Assert.Single(await service.GetListingsAsync(null, "year-desc", TestContext.Current.CancellationToken));
        Assert.Equal(8, listing.Id);
    }

    [Fact]
    public async Task GetListings_ShouldAppendMakeAndSortQueriesWhenBothProvided()
    {
        using var http = CreateHttpClient(request =>
        {
            Assert.Equal("/api/listings", request.RequestUri!.AbsolutePath);
            Assert.Equal("?make=Auto%25_%20Group&sort=year-asc", request.RequestUri.Query);
            return JsonResponse($"[{_cardJson}]");
        });
        var service = new ListingsClient(http);

        var listing = Assert.Single(await service.GetListingsAsync("Auto%_ Group", "year-asc", TestContext.Current.CancellationToken));
        Assert.Equal(8, listing.Id);
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    [InlineData("newest")]
    public async Task GetListings_ShouldOmitSortQueryWhenValueIsBlankOrUnsupported(string sort)
    {
        using var http = CreateHttpClient(request =>
        {
            Assert.Equal("/api/listings", request.RequestUri!.AbsolutePath);
            Assert.Equal(string.Empty, request.RequestUri.Query);
            return JsonResponse($"[{_cardJson}]");
        });
        var service = new ListingsClient(http);

        var listing = Assert.Single(await service.GetListingsAsync(null, sort, TestContext.Current.CancellationToken));
        Assert.Equal(8, listing.Id);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task GetListings_ShouldOmitMakeQueryWhenValueIsBlank(string make)
    {
        using var http = CreateHttpClient(request =>
        {
            Assert.Equal("/api/listings", request.RequestUri!.AbsolutePath);
            Assert.Equal(string.Empty, request.RequestUri.Query);
            return JsonResponse($"[{_cardJson}]");
        });
        var service = new ListingsClient(http);

        var listing = Assert.Single(await service.GetListingsAsync(make, TestContext.Current.CancellationToken));
        Assert.Equal(8, listing.Id);
    }

    [Fact]
    public async Task GetListingsPage_ShouldAppendPagingMakeModelAndSortQueries()
    {
        using var http = CreateHttpClient(request =>
        {
            Assert.Equal("/api/listings/page", request.RequestUri!.AbsolutePath);
            Assert.Equal("?make=Auto%25_%20Group&model=Model%25_%203&sort=year-desc&page=2&pageSize=8", request.RequestUri.Query);
            return JsonResponse($$"""
                {
                  "items": [{{_cardJson}}],
                  "page": 2,
                  "pageSize": 8,
                  "totalCount": 9,
                  "totalPages": 2
                }
                """);
        });
        var service = new ListingsClient(http);

        var page = await service.GetListingsPageAsync("Auto%_ Group", "Model%_ 3", "year-desc", 2, 8, TestContext.Current.CancellationToken);
        Assert.Equal(2, page.Page);
        Assert.Equal(8, page.PageSize);
        Assert.Equal(9, page.TotalCount);
        Assert.Equal(2, page.TotalPages);
        Assert.Equal(8, Assert.Single(page.Items).Id);
    }

    [Fact]
    public async Task GetListingsPage_ShouldOmitBlankModelQuery()
    {
        using var http = CreateHttpClient(request =>
        {
            Assert.Equal("/api/listings/page", request.RequestUri!.AbsolutePath);
            Assert.Equal("?make=Auto%25_%20Group&sort=year-desc&page=2&pageSize=8", request.RequestUri.Query);
            return JsonResponse($$"""
                {
                  "items": [{{_cardJson}}],
                  "page": 2,
                  "pageSize": 8,
                  "totalCount": 9,
                  "totalPages": 2
                }
                """);
        });
        var service = new ListingsClient(http);

        var page = await service.GetListingsPageAsync("Auto%_ Group", "   ", "year-desc", 2, 8, TestContext.Current.CancellationToken);
        Assert.Equal(8, Assert.Single(page.Items).Id);
    }

    [Fact]
    public async Task GetListingMakes_ShouldReadAndNormalizeDistinctValues()
    {
        using var http = CreateHttpClient(request =>
        {
            Assert.Equal("/api/listings/makes", request.RequestUri!.AbsolutePath);
            return JsonResponse("""["Tesla", "TESLA", "BMW"]""");
        });
        var service = new ListingsClient(http);

        var makes = await service.GetListingMakesAsync(TestContext.Current.CancellationToken);
        Assert.Equal(["BMW", "Tesla"], makes);
    }

    [Fact]
    public async Task GetListingModels_ShouldRequestMakeScopedRouteAndNormalizeDistinctValues()
    {
        using var http = CreateHttpClient(request =>
        {
            Assert.Equal("/api/listings/models", request.RequestUri!.AbsolutePath);
            Assert.Equal("?make=Auto%25_%20Group", request.RequestUri.Query);
            return JsonResponse("""["Model Y", "MODEL Y", "Model 3"]""");
        });
        var service = new ListingsClient(http);

        var models = await service.GetListingModelsAsync("Auto%_ Group", TestContext.Current.CancellationToken);
        Assert.Equal(["Model 3", "Model Y"], models);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task GetListingModels_ShouldRequireMake(string make)
    {
        using var http = CreateHttpClient(_ => JsonResponse("[]"));
        var service = new ListingsClient(http);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            service.GetListingModelsAsync(make, TestContext.Current.CancellationToken));
    }

    [Theory]
    [InlineData(0, 8)]
    [InlineData(1, 0)]
    [InlineData(1, 25)]
    public async Task GetListingsPage_ShouldValidatePagingArguments(int page, int pageSize)
    {
        using var http = CreateHttpClient(_ => JsonResponse("{}"));
        var service = new ListingsClient(http);

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            service.GetListingsPageAsync(null, null, null, page, pageSize, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task GetListingsPage_ShouldRejectNullPayload()
    {
        using var http = CreateHttpClient(_ => JsonResponse("null"));
        await Assert.ThrowsAsync<JsonException>(() =>
            new ListingsClient(http).GetListingsPageAsync(null, null, null, 1, 8, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task GetListing_ShouldRequestOnlyTheSelectedId()
    {
        var requests = 0;
        using var http = CreateHttpClient(request =>
        {
            requests++;
            Assert.Equal(HttpMethod.Get, request.Method);
            Assert.Equal("/api/listings/8", request.RequestUri!.AbsolutePath);
            return JsonResponse(_cardJson);
        });
        var listing = await new ListingsClient(http).GetListingAsync(8, TestContext.Current.CancellationToken);

        Assert.NotNull(listing);
        Assert.Equal(8, listing.Id);
        Assert.Equal("Tesla", listing.Make);
        Assert.Equal(1, requests);
    }

    [Fact]
    public async Task GetListing_ShouldReturnNullFor404()
    {
        using var http = CreateHttpClient(_ => new HttpResponseMessage(HttpStatusCode.NotFound));
        Assert.Null(await new ListingsClient(http).GetListingAsync(999, TestContext.Current.CancellationToken));
    }

    [Theory]
    [InlineData("fuelType")]
    [InlineData("transmission")]
    public async Task GetListings_ShouldRejectUnsupportedEnumValues(string field)
    {
        var card = JsonNode.Parse(_cardJson)!.AsObject();
        card[field] = "UnknownValue";
        using var http = CreateHttpClient(_ => JsonResponse($"[{card.ToJsonString()}]"));
        await Assert.ThrowsAsync<JsonException>(() =>
            new ListingsClient(http).GetListingsAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task GetListings_ShouldRejectNullPayload()
    {
        using var http = CreateHttpClient(_ => JsonResponse("null"));
        await Assert.ThrowsAsync<JsonException>(() =>
            new ListingsClient(http).GetListingsAsync(TestContext.Current.CancellationToken));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ApiErrors_ShouldPropagateInsteadOfBecomingEmptyOrNotFound(bool details)
    {
        using var http = CreateHttpClient(_ => new HttpResponseMessage(HttpStatusCode.InternalServerError));
        var service = new ListingsClient(http);
        await Assert.ThrowsAsync<HttpRequestException>(async () =>
        {
            if (details)
            {
                await service.GetListingAsync(8, TestContext.Current.CancellationToken);
            }
            else
            {
                await service.GetListingsAsync(TestContext.Current.CancellationToken);
            }
        });
    }

    [Fact]
    public async Task GetListing_ShouldPreserveNullableFields()
    {
        var card = JsonNode.Parse(_cardJson)!.AsObject();
        foreach (var field in new[] { "trimOrVariant", "fuelType", "transmission", "location", "imageUrl" })
        {
            card[field] = null;
        }
        using var http = CreateHttpClient(_ => JsonResponse(card.ToJsonString()));
        var listing = await new ListingsClient(http).GetListingAsync(8, TestContext.Current.CancellationToken);

        Assert.NotNull(listing);
        Assert.Null(listing.TrimOrVariant);
        Assert.Null(listing.FuelType);
        Assert.Null(listing.Transmission);
        Assert.Null(listing.Location);
        Assert.Null(listing.ImageUrl);
    }

    private static HttpResponseMessage JsonResponse(string json)
    {
        return new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };
    }

    private static HttpClient CreateHttpClient(Func<HttpRequestMessage, HttpResponseMessage> responseFactory)
    {
        return new HttpClient(new StubHttpMessageHandler(responseFactory))
        {
            BaseAddress = new Uri("http://localhost:5133/")
        };
    }

    private sealed class StubHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> responseFactory) : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _responseFactory = responseFactory;

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return Task.FromResult(_responseFactory(request));
        }
    }
}
