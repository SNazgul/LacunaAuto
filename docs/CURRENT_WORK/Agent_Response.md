Implemented Milestone 2, Slice 2.1A backend/persistence foundation only (no UI integration).

## What changed

### Core (`LacunaAuto.Core`)
- Replaced placeholder class with minimal listing domain types:
  - `Listings/VehicleListing.cs`
  - `Listings/FuelType.cs`
  - `Listings/TransmissionType.cs`
- Domain includes the card fields plus `IsPublished`; `IsNew` remains explicitly persisted (no time-based behavior).
- Core remains independent from `LacunaAuto.UI.Shared`.

### Data (`LacunaAuto.Data`)
- Added EF Core + PostgreSQL wiring and Core reference:
  - `LacunaAuto.Data.csproj`
  - `Extensions/ServiceCollectionExtensions.cs`
  - `Persistence/LacunaAutoDbContext.cs`
  - `Persistence/Configurations/VehicleListingEntityConfiguration.cs`
- Added Development-only idempotent seeding:
  - `Initialization/DevelopmentDatabaseInitializer.cs`
  - `Initialization/DevelopmentListingsSeeder.cs`
- Added first migration:
  - `Persistence/Migrations/20261101135601_InitialListingsBrowsing.cs`
  - `Persistence/Migrations/20261101135601_InitialListingsBrowsing.Designer.cs`
  - `Persistence/Migrations/LacunaAutoDbContextModelSnapshot.cs`

### API (`LacunaAuto.Api`)
- Wired Data/Core references and EF design package in `LacunaAuto.Api.csproj`.
- Added data registration and development initializer in `Program.cs`.
- Added public listings endpoint:
  - `Endpoints/PublicListingsEndpoints.cs`
  - DTO: `Contracts/Listings/PublicListingCardDto.cs`
- Removed weather sample endpoint and updated HTTP file:
  - `LacunaAuto.Api.http` now targets `GET /api/listings`.
- Added development connection string configuration in `appsettings.Development.json`.

### Tests (`LacunaAuto.Api.Test`)
- Replaced placeholder test with focused endpoint tests:
  - `PublicListingsEndpointTests.cs`
    - verifies unpublished listings are excluded;
    - verifies published listings are returned in deterministic `OrderBy(Id)` order with expected DTO values.
- Added test packages in `LacunaAuto.Api.Test.csproj`.

### Shared package management / docs
- Added required package versions in `Directory.Packages.props` (EF Core 10.0.14 train, Npgsql EF Core provider 10.0.3, API test package).
- Updated `docs/Development/CURRENT_WORK.md` with implemented/verified slice progress and current blockers.

## Migration name

- `InitialListingsBrowsing` (`20261101135601_InitialListingsBrowsing`)

## API route and response shape

- Route: `GET /api/listings`
- Behavior:
  - anonymous/public;
  - only `IsPublished == true`;
  - async EF query with `AsNoTracking`;
  - deterministic `OrderBy(Id)`;
  - DTO output only.
- Response item shape:
  - `id`, `make`, `model`, `trimOrVariant`, `year`, `price`, `originalCurrencyCode`,
    `mileage`, `fuelType`, `transmission`, `location`, `imageUrl`, `isNew`.

## Validation results

### Required solution-level commands
- `dotnet restore LacunaAuto.sln` ❌ failed (`NETSDK1147` missing `maui-android` workload for `LacunaAuto.Hybrid` / `LacunaAuto.Frontend`)
- `dotnet build LacunaAuto.sln` ❌ failed (same `NETSDK1147` workload blocker)
- `dotnet test LacunaAuto.sln` ❌ failed (same `NETSDK1147` workload blocker)

### Focused validation for this slice
- `dotnet build src/LacunaAuto.Api/LacunaAuto.Api.csproj` ✅ passed
- `dotnet test test/LacunaAuto.Api.Test/LacunaAuto.Api.Test.csproj` ✅ passed (2/2)
- `dotnet-ef migrations list ...` ✅ shows `20261101135601_InitialListingsBrowsing`

## PostgreSQL migration + seed verification status

- `dotnet-ef database update ...` was attempted but could not connect to PostgreSQL (`Failed to connect to 127.0.0.1:5432`).
- Local container startup could not be performed because Docker tooling is unavailable in this environment (`docker` / `docker-compose` commands not found).
- Therefore, live verification of migration application + development seed + runtime `GET /api/listings` against a running local PostgreSQL container is **not completed in this run**.

## Intentionally deferred to Slice 2.1B

- UI wiring (Home/Listings/Details consuming the new API).
- Any behavior beyond this minimal read-only listings slice (filters, pagination, sorting UI, auth, listing management, photo storage, chat, MAUI).
- Port conflict handling between API (`:5132`) and Qwen UI tester (`127.0.0.1:5132`) unless required for local API execution.
