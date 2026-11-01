using System.Net;
using System.Text;
using LacunaAuto.UI.Shared.Models;
using LacunaAuto.Web.Client.Services;

namespace LacunaAuto.UI.Shared.Tests;

public sealed class PublicListingsApiServiceTests
{
    [Fact]
    public async Task GetAllAsync_ShouldMapApiPayloadToVehicleListingCardModel()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var service = CreateService(
            _ =>
                new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = BuildJsonContent(
                        """
                        [
                          {
                            "id": 1,
                            "make": "Tesla",
                            "model": "Model 3",
                            "trimOrVariant": "Long Range",
                            "year": 2023,
                            "price": 45000,
                            "originalCurrencyCode": "USD",
                            "mileage": 12000,
                            "fuelType": "Electric",
                            "transmission": "Automatic",
                            "location": "Kyiv, Ukraine",
                            "imageUrl": "/images/vehicles/tesla-model-3.svg",
                            "isNew": true
                          }
                        ]
                        """)
                });

        var listings = await service.GetAllAsync(cancellationToken);

        var listing = Assert.Single(listings);
        Assert.Equal(1, listing.Id);
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
    public async Task GetByIdAsync_ShouldReturnMatchingListing_WhenListingExists()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var requestCount = 0;
        var service = CreateService(
            _ =>
            {
                requestCount++;
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = BuildJsonContent(
                        """
                        [
                          {
                            "id": 1,
                            "make": "Tesla",
                            "model": "Model 3",
                            "year": 2023,
                            "price": 45000,
                            "originalCurrencyCode": "USD",
                            "mileage": 12000,
                            "isNew": true
                          },
                          {
                            "id": 8,
                            "make": "Honda",
                            "model": "CR-V",
                            "year": 2023,
                            "price": 36000,
                            "originalCurrencyCode": "USD",
                            "mileage": 6000,
                            "isNew": true
                          }
                        ]
                        """)
                };
            });

        var listing = await service.GetByIdAsync(8, cancellationToken);

        Assert.NotNull(listing);
        Assert.Equal(8, listing.Id);
        Assert.Equal("Honda", listing.Make);
        Assert.Equal(1, requestCount);
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnNull_WhenListingDoesNotExist()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var service = CreateService(
            _ =>
                new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = BuildJsonContent(
                        """
                        [
                          {
                            "id": 1,
                            "make": "Tesla",
                            "model": "Model 3",
                            "year": 2023,
                            "price": 45000,
                            "originalCurrencyCode": "USD",
                            "mileage": 12000,
                            "isNew": true
                          }
                        ]
                        """)
                });

        var listing = await service.GetByIdAsync(999, cancellationToken);

        Assert.Null(listing);
    }

    [Fact]
    public async Task GetAllAsync_ShouldThrowInvalidOperationException_WhenFuelTypeIsUnsupported()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var service = CreateService(
            _ =>
                new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = BuildJsonContent(
                        """
                        [
                          {
                            "id": 1,
                            "make": "Tesla",
                            "model": "Model 3",
                            "year": 2023,
                            "price": 45000,
                            "originalCurrencyCode": "USD",
                            "mileage": 12000,
                            "fuelType": "UnknownFuel",
                            "isNew": true
                          }
                        ]
                        """)
                });

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => service.GetAllAsync(cancellationToken));
        Assert.Contains("Unsupported fuel type value 'UnknownFuel'", exception.Message);
    }

    [Fact]
    public async Task GetAllAsync_ShouldThrowInvalidOperationException_WhenPayloadIsNull()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var service = CreateService(
            _ =>
                new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = BuildJsonContent("null")
                });

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => service.GetAllAsync(cancellationToken));
        Assert.Equal("Listings API returned an empty payload.", exception.Message);
    }

    [Fact]
    public async Task GetAllAsync_ShouldPropagateHttpRequestException_WhenApiReturnsErrorStatusCode()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var service = CreateService(
            _ => new HttpResponseMessage(HttpStatusCode.InternalServerError));

        await Assert.ThrowsAsync<HttpRequestException>(() => service.GetAllAsync(cancellationToken));
    }

    private static PublicListingsApiService CreateService(Func<HttpRequestMessage, HttpResponseMessage> responseFactory)
    {
        var httpClient = new HttpClient(new StubHttpMessageHandler(responseFactory))
        {
            BaseAddress = new Uri("http://localhost:5133/")
        };

        return new PublicListingsApiService(httpClient);
    }

    private static StringContent BuildJsonContent(string json)
    {
        return new StringContent(json, Encoding.UTF8, "application/json");
    }

    private sealed class StubHttpMessageHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _responseFactory;

        public StubHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> responseFactory)
        {
            _responseFactory = responseFactory;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var response = _responseFactory(request);
            return Task.FromResult(response);
        }
    }
}
