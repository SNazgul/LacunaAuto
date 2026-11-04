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
