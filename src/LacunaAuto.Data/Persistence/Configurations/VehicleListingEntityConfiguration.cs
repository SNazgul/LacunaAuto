using LacunaAuto.Core.Listings;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LacunaAuto.Data.Persistence.Configurations;

internal sealed class VehicleListingEntityConfiguration : IEntityTypeConfiguration<VehicleListing>
{
    public void Configure(EntityTypeBuilder<VehicleListing> builder)
    {
        builder.ToTable(
            "vehicle_listings",
            tableBuilder =>
            {
                tableBuilder.HasCheckConstraint("ck_vehicle_listings_year", "year >= 1886 AND year <= 9999");
                tableBuilder.HasCheckConstraint("ck_vehicle_listings_price", "price >= 0");
                tableBuilder.HasCheckConstraint("ck_vehicle_listings_mileage", "mileage >= 0");
                tableBuilder.HasCheckConstraint("ck_vehicle_listings_currency_code", "char_length(original_currency_code) = 3");
            });

        builder.HasKey(listing => listing.Id);

        builder.Property(listing => listing.Id)
            .HasColumnName("id");

        builder.Property(listing => listing.Make)
            .HasColumnName("make")
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(listing => listing.Model)
            .HasColumnName("model")
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(listing => listing.TrimOrVariant)
            .HasColumnName("trim_or_variant")
            .HasMaxLength(64);

        builder.Property(listing => listing.Year)
            .HasColumnName("year")
            .IsRequired();

        builder.Property(listing => listing.Price)
            .HasColumnName("price")
            .HasColumnType("numeric(12,2)")
            .IsRequired();

        builder.Property(listing => listing.OriginalCurrencyCode)
            .HasColumnName("original_currency_code")
            .HasColumnType("char(3)")
            .IsRequired();

        builder.Property(listing => listing.Mileage)
            .HasColumnName("mileage")
            .IsRequired();

        builder.Property(listing => listing.FuelType)
            .HasColumnName("fuel_type")
            .HasConversion<string>()
            .HasMaxLength(16);

        builder.Property(listing => listing.Transmission)
            .HasColumnName("transmission")
            .HasConversion<string>()
            .HasMaxLength(16);

        builder.Property(listing => listing.Location)
            .HasColumnName("location")
            .HasMaxLength(128);

        builder.Property(listing => listing.ImageUrl)
            .HasColumnName("image_url")
            .HasMaxLength(512);

        builder.Property(listing => listing.IsNew)
            .HasColumnName("is_new")
            .IsRequired();

        builder.Property(listing => listing.IsPublished)
            .HasColumnName("is_published")
            .HasDefaultValue(false)
            .IsRequired();

        builder.HasIndex(listing => new { listing.IsPublished, listing.Id })
            .HasDatabaseName("ix_vehicle_listings_is_published_id");
    }
}
