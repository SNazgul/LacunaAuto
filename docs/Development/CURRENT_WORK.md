# LacunaAuto Current Work

## Current Goal

Start the listings browsing vertical slice while keeping the accepted browsing UI stable and the app runnable after every change.

## Now

- [x] Define the minimum listing/domain data required by the accepted Home, Listings, and Listing Details UI.
- [x] Add the first PostgreSQL / EF Core persistence slice and reproducible development data for browsing.
- [x] Add the minimal public browsing API needed by the existing UI.
- [ ] Connect the existing browsing UI to the real API without redesigning accepted screens.

Keep this as a small end-to-end slice. Do not start authentication, listing management, photo upload/storage implementation, chat, or Hybrid work in this task.

## Next

After the first real listings browsing slice is accepted:

- continue milestone 2 with the first agreed filtering / pagination / sorting behavior only as needed;
- keep prices in each listing's original currency until real conversion is explicitly implemented.

## Recently Completed

- Milestone 2, Slice 2.1A backend/persistence foundation implemented (backend only, no UI wiring):
  - Added Core listing domain types independent from `LacunaAuto.UI.Shared`, including minimal card fields plus `IsPublished` and explicit persisted `IsNew` behavior.
  - Added EF Core + PostgreSQL persistence in `LacunaAuto.Data` with explicit mapping (`vehicle_listings` table), constraints, and index for published browsing reads; first migration created: `InitialListingsBrowsing`.
  - Added Development-only idempotent seeding aligned with the current 8 `VehicleListingSeedData` records (same IDs/order, image URLs, and `IsNew` values; all published for browsing).
  - Added public `GET /api/listings` (anonymous, published-only, `AsNoTracking`, deterministic `OrderBy(Id)`, DTO output for current listing cards, OpenAPI metadata); removed `/weatherforecast` sample endpoint.
  - Added focused API tests for published/unpublished behavior, deterministic order/value checks, and null-optional-field serialization (`LacunaAuto.Api.Test`): **4/4 passed**.
  - Added focused Development seeder idempotency coverage (run seeder twice without duplicate rows); this test does not claim PostgreSQL identity-sequence verification.
  - Live PostgreSQL verification completed manually on Windows against the real PostgreSQL 17 Docker container:
    - `docker compose up -d postgres` succeeded.
    - PostgreSQL was reachable on `localhost:5432`.
    - `dotnet ef database update` succeeded for `InitialListingsBrowsing`.
    - API started in Development and the Development seeder executed successfully.
    - `GET http://localhost:5132/api/listings` returned HTTP 200 with exactly 8 seeded listings (IDs 1 through 8) in deterministic order.
    - PostgreSQL query confirmed `MAX(id) = 8`.
    - `vehicle_listings_id_seq` confirmed `last_value = 8` and `is_called = true`; therefore the next normal identity-generated listing ID is `9`.
- E2E config-test URL-override fix, verified green at **37/37** browser tests plus 2 expected-failure fault-injection checks:
  - `specs/runner-config.spec.ts` hardcoded `expect(baseURL).toBe("http://localhost:5032/")`, which failed for a valid `E2E_BASE_URL` override. The expectation now derives from `E2E_BASE_URL` through the shared `APP_BASE_URL` helper constant (same default as the config), so `E2E_BASE_URL=http://localhost:5132/` passes and reports that origin. `actionTimeout = 15000`, `navigationTimeout = 30000` and `locale = en-US` remain hardcoded.
- E2E follow-up review pass (two findings), verified green at **37/37** browser tests plus 2 expected-failure fault-injection checks:
  - `actionTimeout` (`15000`) and `navigationTimeout` (`30000`) moved inside the `use` block; an independent probe had measured `0`/`0` with the previous top-level placement. Verified effective through the runner's own injectable option fixtures by the new permanent test `specs/runner-config.spec.ts`, which asserts `actionTimeout = 15000`, `navigationTimeout = 30000`, `locale = en-US`, and `baseURL` equal to the effective origin.
  - The falsely labeled "in-app navigation" test in `specs/navigation.spec.ts` (which assigned `window.location.href`, a full document navigation) was replaced with real Blazor client-side routing via `window.Blazor.navigateTo(path, false, false)` — the call `NavigationManager.NavigateTo` compiles to in the served `blazor.web.<hash>.js`. Evidence from the run: window marker `lacuna-spa-1792435238711` identical before and after the SPA navigation (and after returning Home), and the tracked main-frame document requests during the navigation were `[]`. The separate direct-request test still asserts HTTP 404.
  - `e2e/README.md` and this document corrected for the timeout placement and the SPA-navigation claim.
- E2E review pass on the UI automation suite (`e2e/`), verified green at **36/36** browser tests plus 2 expected-failure fault-injection checks:
  - `playwright.config.ts` rewritten with the typed `defineConfig` API of Playwright 1.64.1; invalid options removed (`reporters`, `browser`, `contextName`, `contextFlags`, boolean `screenshot`/`video`), and `locale`, `baseURL`, `actionTimeout` and `navigationTimeout` all declared inside `use` (a top-level placement is ignored by this version). Verified effective through the runner's own option fixtures in `specs/runner-config.spec.ts`: `actionTimeout = 15000`, `navigationTimeout = 30000`, `locale = en-US`, and `baseURL` equal to the effective origin. The `json` reporter writes `e2e/e2e-report.json`.
  - Image assertions now require every `<img>` to finish decoding (`complete` + intrinsic size, polled with `expect.poll`); `negative/image-load.spec.ts` blocks all vehicle image requests and fails with `Expected: 8 / Received: 0`.
  - `seedPreferences`/`clearPreferences`/`ensurePreferences` take a ready selector and wait for the rendered selector after each reload; `negative/preference-readiness.spec.ts` shows a bare reload observing `0` cards, while `specs/preference-readiness.spec.ts` (including a 400 ms-per-response slow-startup run) passes.
  - Deprecated `<NotFound>` router element replaced with the supported `NotFoundPage="typeof(NotFoundPage)"` attribute plus `@page "/not-found"` / `@layout MainLayout` on `NotFoundPage.razor`; the duplicate host `NotFound.razor` was deleted so the client page is the single `/not-found` re-execute target (removes the AmbiguousMatchException). Verified: HTTP 404 for direct unknown routes, client-side navigation via `window.Blazor.navigateTo` renders the same page in the same document, EN/UK/RU headings, Home button returns to `/`.
  - Responsive coverage extended to 599/600/899/900/1199/1200 px grid probes on Home and Listings, the 1023/1024 px bottom-nav boundary, and Listing Details overflow/geometry at 390 and 1440 px.
  - Preferences coverage extended (persistence across a plain reload, two independence combinations, clearing storage re-detects from the pinned browser locale); the redundant `disabled-controls.spec.ts` was removed after moving its only unique assertion (Home Filter button) into `home.spec.ts`.
  - `e2e/README.md` rewritten: fresh-checkout setup, effective config table, accurate per-suite coverage, negative-check evidence, real BrowserContext isolation behavior, and an explicit "Not covered" list.
- Unknown routes now render a real localized 404 page inside the app shell, driven by the client router (`LacunaAuto.Web.Client/Routes.razor`) while the host keeps returning HTTP 404 via `UseStatusCodePagesWithReExecute("/not-found")`. Verified in Chrome (EN/UK/RU) and by the E2E suite.
- Playwright UI automation suite scaffolded and verified green against the running app in `e2e/` (outside the .NET solution): navigation/routing, Home, Listings, Listing Details, EN/UK/RU localization with persisted preferences, responsive 1/2/3/4-column grid, PWA manifest/assets.
- Roadmap milestone 1 — Complete the Browsing UI Foundation — accepted.
- Web PWA installability verified in Chrome from a Release publish; the installed app launches in standalone mode.
- Published service worker uses a minimal install/activate strategy so it does not interfere with hosted .NET 10 Blazor Web App Interactive WebAssembly static-asset fingerprinting/SRI.
- Offline precaching/offline runtime behavior is intentionally deferred to a separate future decision/task.
- Home mobile layout reviewed and accepted.
- Bottom navigation fixed to `Home | Sell (+) | Search`.
- Mobile `/listings` reviewed and accepted.
- Mobile `/listings/{id}` reviewed and accepted.
- Home, Listings, and Listing Details responsive layouts reviewed and accepted across mobile, tablet, desktop, and wide desktop widths.
- Responsive listing grids use 1 / 2 / 3 / 4 columns at the agreed breakpoints, with mobile bottom navigation hidden from 1024px upward.
- EN / UK / RU browsing localization reviewed and accepted.
- Language, Regional Format, and Currency preferences verified as independent and persisted.
- Listing prices remain in their original currency; Currency preference does not relabel or fake-convert prices.
- Shared Razor UI foundation, seed-data Home/Listings/Details, localization, and persisted preferences are in place.
- Production data decision: PostgreSQL on the server; all image files will be stored separately in Backblaze B2.

## Known Issue

`dotnet restore LacunaAuto.sln`, `dotnet build LacunaAuto.sln`, and `dotnet test LacunaAuto.sln` currently fail in this environment with `NETSDK1147` because required MAUI Android workloads are not installed for `LacunaAuto.Hybrid` / `LacunaAuto.Frontend` target frameworks.

Local PostgreSQL container verification is blocked in this environment because Docker/Docker Compose CLI is unavailable (`docker` and `docker-compose` commands not found). As a result, migration application to a running local PostgreSQL container and live `GET /api/listings` verification against that container were not completed in this run.

The previously documented `dotnet test LacunaAuto.sln` VSTest blocker did **not** reproduce: on 2026-10-19 with .NET SDK `10.0.401`, `dotnet test LacunaAuto.sln` reported `Test run summary: Passed!` for both `LacunaAuto.Api.Test` and `LacunaAuto.UI.Shared.Tests` (0 failed). No test-infrastructure change was made.

The host `Components/Pages/NotFound.razor` (`src/LacunaAuto.Web/LacunaAuto.Web/`) was **removed**: with the supported `NotFoundPage="typeof(NotFoundPage)"` router attribute, the client page `LacunaAuto.Web.Client/Components/Pages/NotFoundPage.razor` owns `@page "/not-found"` and is the endpoint that `UseStatusCodePagesWithReExecute("/not-found")` re-executes. Keeping both pages registered the same route and produced an AmbiguousMatchException. Any future 404 content change belongs in the client `NotFoundPage.razor`.

## Agent Rule

Update this file only with work that was actually implemented and verified. Keep **Now** to one small active slice. Do not change product scope or Roadmap order without explicit user approval.
