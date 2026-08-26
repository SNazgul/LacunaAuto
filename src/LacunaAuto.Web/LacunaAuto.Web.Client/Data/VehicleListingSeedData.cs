using LacunaAuto.UI.Shared.Models;

namespace LacunaAuto.Web.Client.Data;

/// <summary>
/// Static seed data for MVP UI implementation.
/// </summary>
public static class VehicleListingSeedData
{
    public static IReadOnlyList<VehicleListingCardModel> GetAll()
    {
        return new List<VehicleListingCardModel>
        {
            new()
            {
                Id = 1,
                Make = "Tesla",
                Model = "Model 3",
                TrimOrVariant = "Long Range",
                Year = 2023,
                Price = 45000m,
                OriginalCurrencyCode = "USD",
                Mileage = 12000,
                FuelType = FuelType.Electric,
                Transmission = TransmissionType.Automatic,
                Location = "Kyiv, Ukraine",
                ImageUrl = null,
                IsNew = true
            },
            new()
            {
                Id = 2,
                Make = "BMW",
                Model = "X5",
                TrimOrVariant = "xDrive40i",
                Year = 2022,
                Price = 62000m,
                OriginalCurrencyCode = "USD",
                Mileage = 25000,
                FuelType = FuelType.Petrol,
                Transmission = TransmissionType.Automatic,
                Location = "Lviv, Ukraine",
                ImageUrl = null,
                IsNew = false
            },
            new()
            {
                Id = 3,
                Make = "Toyota",
                Model = "RAV4",
                TrimOrVariant = "Hybrid",
                Year = 2023,
                Price = 38000m,
                OriginalCurrencyCode = "USD",
                Mileage = 8000,
                FuelType = FuelType.Hybrid,
                Transmission = TransmissionType.Automatic,
                Location = "Odesa, Ukraine",
                ImageUrl = null,
                IsNew = true
            },
            new()
            {
                Id = 4,
                Make = "Mercedes-Benz",
                Model = "C-Class",
                TrimOrVariant = "C 200",
                Year = 2021,
                Price = 42000m,
                OriginalCurrencyCode = "USD",
                Mileage = 35000,
                FuelType = FuelType.Petrol,
                Transmission = TransmissionType.Automatic,
                Location = "Dnipro, Ukraine",
                ImageUrl = null,
                IsNew = false
            },
            new()
            {
                Id = 5,
                Make = "Volkswagen",
                Model = "Tiguan",
                TrimOrVariant = "R-Line",
                Year = 2022,
                Price = 35000m,
                OriginalCurrencyCode = "USD",
                Mileage = 18000,
                FuelType = FuelType.Diesel,
                Transmission = TransmissionType.Automatic,
                Location = "Kharkiv, Ukraine",
                ImageUrl = null,
                IsNew = false
            },
            new()
            {
                Id = 6,
                Make = "Audi",
                Model = "Q5",
                TrimOrVariant = "Premium Plus",
                Year = 2023,
                Price = 55000m,
                OriginalCurrencyCode = "USD",
                Mileage = 5000,
                FuelType = FuelType.Petrol,
                Transmission = TransmissionType.Automatic,
                Location = "Kyiv, Ukraine",
                ImageUrl = null,
                IsNew = true
            },
            new()
            {
                Id = 7,
                Make = "Ford",
                Model = "Mustang",
                TrimOrVariant = "GT",
                Year = 2022,
                Price = 48000m,
                OriginalCurrencyCode = "USD",
                Mileage = 15000,
                FuelType = FuelType.Petrol,
                Transmission = TransmissionType.Manual,
                Location = "Lviv, Ukraine",
                ImageUrl = null,
                IsNew = false
            },
            new()
            {
                Id = 8,
                Make = "Honda",
                Model = "CR-V",
                TrimOrVariant = "EX-L",
                Year = 2023,
                Price = 36000m,
                OriginalCurrencyCode = "USD",
                Mileage = 6000,
                FuelType = FuelType.Petrol,
                Transmission = TransmissionType.Automatic,
                Location = "Odesa, Ukraine",
                ImageUrl = null,
                IsNew = true
            }
        };
    }
}
