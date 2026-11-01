Applied Slice 2.1A review fixes on `feature/listings-browsing-2-1` without broadening scope.

## What was fixed

### 1) PostgreSQL identity sequence handling after Development seed IDs 1..8

- Updated `DevelopmentListingsSeeder` to keep explicit IDs `1..8` and remain idempotent.
- Added PostgreSQL-specific sequence synchronization after seed upsert/save:
  - runs only when provider is Npgsql (`Database.IsNpgsql()`),
  - executes `setval(pg_get_serial_sequence(...), max(id), true)` logic so the next generated ID is greater than current max.
- No `SortOrder` introduced.
- Normal DB-generated identity IDs remain enabled.

File:
- `src/LacunaAuto.Data/Initialization/DevelopmentListingsSeeder.cs`

### 2) Nullability aligned with accepted UI listing contract

Optional (nullable) across domain/persistence/API:
- `TrimOrVariant`
- `FuelType`
- `Transmission`
- `Location`
- `ImageUrl`

Still required:
- `Id`, `Make`, `Model`, `Year`, `Price`, `OriginalCurrencyCode`, `Mileage`, `IsNew`, `IsPublished`

Updated:
- Core model:
  - `src/LacunaAuto.Core/Listings/VehicleListing.cs`
- EF configuration:
  - `src/LacunaAuto.Data/Persistence/Configurations/VehicleListingEntityConfiguration.cs`
- API DTO/projection:
  - `src/LacunaAuto.Api/Contracts/Listings/PublicListingCardDto.cs`
  - `src/LacunaAuto.Api/Endpoints/PublicListingsEndpoints.cs`

### 3) Initial migration regenerated (not a second corrective migration)

- Removed previous initial migration and regenerated the initial migration after the nullability fixes.
- Current initial migration files:
  - `src/LacunaAuto.Data/Persistence/Migrations/20261101155639_InitialListingsBrowsing.cs`
  - `src/LacunaAuto.Data/Persistence/Migrations/20261101155639_InitialListingsBrowsing.Designer.cs`
  - `src/LacunaAuto.Data/Persistence/Migrations/LacunaAutoDbContextModelSnapshot.cs`

### 4) Focused test coverage additions

- Added API test to verify null optional fields serialize as `null` in `GET /api/listings`.
- Added focused seeder idempotency test to verify running `DevelopmentListingsSeeder` twice does not duplicate rows.
- Existing published/unpublished/order tests retained.

File:
- `test/LacunaAuto.Api.Test/PublicListingsEndpointTests.cs`

## Documentation correction

Updated `docs/Development/CURRENT_WORK.md` to distinguish:
- implemented + focused tests passed, versus
- live PostgreSQL migration/seed/API verification still pending.

The `Now` checklist now keeps PostgreSQL persistence/data and minimal public API items unverified until exercised against running PostgreSQL.

## Validation run for this fix

Focused validation executed:
- `dotnet build src/LacunaAuto.Api/LacunaAuto.Api.csproj` ✅
- `dotnet test test/LacunaAuto.Api.Test/LacunaAuto.Api.Test.csproj` ✅

Test summary:
- 4/4 passed.

## PostgreSQL live verification status (important)

- Sequence fix is implemented in code but **not live-verified** against a running PostgreSQL instance in this environment.
- Docker CLI remains unavailable here, so local PostgreSQL container startup/verification is still blocked.

## Scope confirmation

- No PR opened.
- No merge performed.
- Changes remain strictly inside Slice 2.1A backend/persistence/test/docs scope.
