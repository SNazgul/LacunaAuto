Implemented Milestone 2, Slice 2.1B on `feature/listings-ui-api-2-1b` by wiring the accepted browsing UI to the real listings API without redesigning accepted behavior.

## What changed

- Replaced runtime seed-data usage on:
  - Home (`src/LacunaAuto.Web/LacunaAuto.Web.Client/Pages/Home.razor`)
  - Listings (`src/LacunaAuto.Web/LacunaAuto.Web.Client/Pages/Listings.razor`)
  - Listing Details (`src/LacunaAuto.Web/LacunaAuto.Web.Client/Pages/ListingDetails.razor`)
- Added a minimal Web.Client listings API layer:
  - `src/LacunaAuto.Web/LacunaAuto.Web.Client/Services/IPublicListingsService.cs`
  - `src/LacunaAuto.Web/LacunaAuto.Web.Client/Services/PublicListingsApiService.cs`
  - Uses `GET /api/listings` for list and resolves details by ID from returned public data (no new backend details endpoint).
- Kept `VehicleListingCardModel` as the UI-facing model; added client-side DTO + mapping logic in the client service layer (no coupling to API/Core persistence types).
- Added minimal localized loading/error handling on affected pages:
  - no fallback to `VehicleListingSeedData` on API failure;
  - failure remains visible via localized minimal error states (`Loading` / `ErrorLoadingContent` / `TryAgain`).
- Added a readiness marker contract for API-backed page loading (`data-lacuna-loading="true"`) and updated Playwright readiness helper to wait for loading completion before assertions.
- Wired Development Web→API connectivity with minimal config:
  - Web client API base address in `src/LacunaAuto.Web/LacunaAuto.Web.Client/wwwroot/appsettings.Development.json` (`Api:BaseUrl = http://localhost:5133/`);
  - client startup registration in `src/LacunaAuto.Web/LacunaAuto.Web.Client/Program.cs`;
  - API CORS allowlist in `src/LacunaAuto.Api/appsettings.Development.json` + `src/LacunaAuto.Api/Program.cs`;
  - API launch HTTP port moved from `5132` to `5133` in `src/LacunaAuto.Api/Properties/launchSettings.json`;
  - API `.http` profile updated in `src/LacunaAuto.Api/LacunaAuto.Api.http`.
- Added focused tests for the new client/service/mapping behavior:
  - `test/LacunaAuto.UI.Shared.Tests/PublicListingsApiServiceTests.cs`
  - `test/LacunaAuto.UI.Shared.Tests/LacunaAuto.UI.Shared.Tests.csproj` updated to reference Web.Client.
- Updated progress doc:
  - `docs/Development/CURRENT_WORK.md`

## Architecture chosen for Web → API calls

- Browser UI uses `IPublicListingsService` (`PublicListingsApiService`) in Web.Client.
- Service fetches public listings from `api/listings` via `HttpClient` configured from `Api:BaseUrl` (fallback to host base address when not configured).
- Service maps API payload to `VehicleListingCardModel` and parses enum strings explicitly.
- Pages consume only the UI model and service abstraction.

## API/base-address configuration

- API development HTTP origin: `http://localhost:5133`.
- Web development origin remains `http://localhost:5032`.
- Web client dev config points to API `http://localhost:5133/`.
- API CORS allowlist includes local Web origins (including `127.0.0.1:5132` for current Qwen runner-hosted Web process).

## Validation and tests run

- Focused .NET tests/build:
  - `dotnet test test/LacunaAuto.UI.Shared.Tests/LacunaAuto.UI.Shared.Tests.csproj` → passed (7/7).
  - `dotnet test test/LacunaAuto.Api.Test/LacunaAuto.Api.Test.csproj` → passed (4/4).
  - `dotnet build src/LacunaAuto.Web/LacunaAuto.Web/LacunaAuto.Web.csproj` → passed.
  - `dotnet build src/LacunaAuto.Api/LacunaAuto.Api.csproj` → passed.
- Integrated slice validation (requested):
  - started PostgreSQL (`docker compose up -d postgres`);
  - ran `LacunaAuto.Api` + `LacunaAuto.Web` together;
  - verified `GET http://localhost:5133/api/listings` returns 200 and 8 listings;
  - ran existing Playwright suite against Web origin (`E2E_BASE_URL=http://localhost:5032/`) → passed (37/37).
- Full-solution commands:
  - `dotnet restore/build/test LacunaAuto.sln` fail with `NETSDK1147` due missing `maui-android` workload in `LacunaAuto.Hybrid`/`LacunaAuto.Frontend` (reported as environment/workload blocker, not a Slice 2.1B logic failure).

## Intentionally deferred / not changed

- No filtering/pagination/sorting/auth/listing-management/photo-upload/chat/hybrid/currency-conversion/API-versioning work.
- No backend listing-details endpoint added (not required for this slice).
- No caching or broader client state-management layer added.
- Qwen runner was not redesigned in this slice; documented follow-up remains: current Web-only hosting model needs separate infrastructure work to independently validate API-backed UI.
