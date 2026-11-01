using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace LacunaAuto.Data.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialListingsBrowsing : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "vehicle_listings",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    make = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    model = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    trim_or_variant = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    year = table.Column<int>(type: "integer", nullable: false),
                    price = table.Column<decimal>(type: "numeric(12,2)", nullable: false),
                    original_currency_code = table.Column<string>(type: "char(3)", nullable: false),
                    mileage = table.Column<int>(type: "integer", nullable: false),
                    fuel_type = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    transmission = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    location = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    image_url = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    is_new = table.Column<bool>(type: "boolean", nullable: false),
                    is_published = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_vehicle_listings", x => x.id);
                    table.CheckConstraint("ck_vehicle_listings_currency_code", "char_length(original_currency_code) = 3");
                    table.CheckConstraint("ck_vehicle_listings_mileage", "mileage >= 0");
                    table.CheckConstraint("ck_vehicle_listings_price", "price >= 0");
                    table.CheckConstraint("ck_vehicle_listings_year", "year >= 1886 AND year <= 9999");
                });

            migrationBuilder.CreateIndex(
                name: "ix_vehicle_listings_is_published_id",
                table: "vehicle_listings",
                columns: new[] { "is_published", "id" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "vehicle_listings");
        }
    }
}
