# LacunaAuto Current Work

## Current Goal

Integrate the verified responsive Qwen reviewer and continue the listings browsing slice.

## Now

- [x] Define the minimum listing/domain data required by the accepted Home, Listings, and Listing Details UI.
- [x] Add the first PostgreSQL / EF Core persistence slice and reproducible development data for browsing.
- [x] Add the minimal public browsing API needed by the existing UI.
- [x] Connect the existing browsing UI to the real API without redesigning accepted screens.
- [x] Merge Slice 2.1B (PR #19, squash commit `0865ea5`).
- [x] Add bounded layout and screenshot tools for 390×844, 768×1024, and 1440×900.
- [x] Add the two-image Vision smoke, PNG transport after tool responses, and the responsive evidence requirement for UI `PASS`.
- [x] Verify pixel interpretation on the configured STRATA endpoint and the exact-SHA GitHub artifact/check path.

Keep this as a small end-to-end slice. Do not start authentication, listing management, photo upload/storage implementation, chat, or Hybrid work in this task.

## Next

Continue the product roadmap with the first agreed browsing behavior.

- continue milestone 2 with the first agreed filtering / pagination / sorting behavior only as needed;
- keep prices in each listing's original currency until real conversion is explicitly implemented.

## Recently Completed

- Milestone 2 make-filter slice implemented on `feature/listings-make-filter`.
  - The Listings page loads available makes from the unfiltered API response, preserves the options through filtered and empty states, and clears the filter with “All makes.”
  - The Web proxy forwards the encoded optional `make` parameter. The PostgreSQL query applies exact case-insensitive matching to published listings and preserves the existing order.
  - The default Development seed retains its eight listings. Playwright covers a long make at 390 px, filter selection and clearing, and live proxy filtering with a seeded make.
  - Verification on 2026-11-03 included 17 API tests, 13 UI/client tests, and all 51 Playwright tests. An isolated PostgreSQL record matched both `ß` and `ẞ` forms through the Web proxy.
- Responsive review pilot [run #17, attempt 2](https://github.com/SNazgul/LacunaAuto/actions/runs/41611745283/attempts/2) passed on `20d750d` using reviewer code from `cca8346`. STRATA read both smoke-test PNGs. Qwen received six viewport PNGs and layout measurements for `/listings` and `/listings/1`. The workflow published the PNGs, the Markdown and JSON reports, and a successful `Qwen UI Review` check on the tested SHA. Playwright passed 49/49 on PostgreSQL, API, and Web.
- Local review tests confirmed `BLOCKED` for disabled Vision or a model `PASS` with missing evidence. They also verified that a model `FAIL` is preserved and non-UI reviews skip Vision.
- Milestone 2, Slice 2.1B implemented and merged in PR #19:
  - Home, Listings, and Listing Details now read the public API through a shared browser client; removed the client-only `VehicleListingSeedData` and seed fallback.
  - Added anonymous, published-only `GET /api/listings/{id}` using the current card fields. Missing, unpublished, and non-positive IDs return 404. Descriptions and seller actions retain their existing Development placeholders; no new seller/domain fields were introduced.
  - Web forwards only the two public listing GET routes to configured `ListingsApi:BaseUrl`. Browser requests stay on the Web origin. Production requires an explicit HTTP(S) API origin; Development and the API launch profile use `http://localhost:5133/`.
  - Qwen's Web process now receives the runner's API origin, including custom-port overrides.
  - Added localized loading / empty / error states and retry. Details refresh on route-parameter changes and cancel obsolete requests; errors do not fall back to seed records or masquerade as 404.
  - Verification on 2026-11-02 with .NET SDK 10.0.402:
    - Web build and Release publish passed; Qwen runner build passed.
    - Focused API tests: **10/10 passed**, including detail/list contract parity, null optionals, and publication visibility.
    - UI/client tests: **10/10 passed** (one existing component test plus nine client-contract cases adapted from the earlier 2.1B branch). The GitHub review job runs this project before Qwen so its coverage is enforced in CI as well.
    - Playwright full suite: **49/49 passed**, with 12 new API-browsing cases and 0 retries/failures. Deliberately changed browser API payloads prove that the rendered records/IDs/currency come from API responses.
    - An isolated fresh PostgreSQL 17 test container applied `InitialListingsBrowsing`, seeded IDs 1..8, and reported identity sequence `8 / is_called=true` (next ID 9).
    - Live API and published Production Web host returned 200 for list/details; a controlled unpublished fixture and missing IDs returned 404 through both origins, with JSON 404 responses from Web.
    - A separate Web process pointed at an unavailable API returned 502 `application/problem+json` with a trace ID; upstream exception details were not exposed in the response.
    - Published Release screens exercised at 390px and 1440px: no browser page errors or horizontal overflow. Spot-checked Home, Listings, Details, empty/error and missing-listing screenshots; this is not the larger visual acceptance review.
  - Full-solution restore remains blocked by the existing `NETSDK1147` MAUI Android workload issue; no workload or Hybrid changes made.
  - Reconciled with the earlier stacked `feature/listings-ui-api-2-1b` integration (`76ea1f5`): preserved its client-contract coverage, restored the `data-lacuna-loading` marker alongside `aria-busy`, and aligned API launch profiles/Web configuration/HTTP examples on port 5133. Retained the verified same-origin Web forwarding design in PR #19 instead of adding unused browser CORS. The older branch remains untouched for reference.
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
- Qwen review infrastructure now uses explicit dual origins and authoritative runtime topology for the review job:
  - Runner updates (`tools/LacunaAuto.QwenUiTester`) now distinguish Web (`http://127.0.0.1:5132/`) and API (`http://127.0.0.1:5133/`) origins, expose target-aware `http_get(target, route)` constraints, run focused API tests only when backend-relevant, and fail closed to `BLOCKED` when required topology is unavailable.
  - Workflow updates (`.github/workflows/qwen-ui-review.yml`) now start PostgreSQL via the preinstalled Windows service `postgresql-x64-17` (no Docker Compose), keep backend-diff classification for focused backend test scope only, and still enforce exact-SHA/owner/same-repo/secret isolation gates.
  - The review job now fails closed before model execution when PostgreSQL bootstrap fails: it emits a structured `BLOCKED` review artifact (`qwen-ui-review.md` + `qwen-ui-review.json` with exact tested/base SHA), sets `exit_code=3` / `model_result=BLOCKED`, and preserves the existing artifact + PR/check-run publish path.
  - Added `.github/scripts/bootstrap-postgres-windows.ps1` for review-only PostgreSQL service bootstrap + `lacunaauto` role/database provisioning.
  - Verified locally:
    - `dotnet build tools/LacunaAuto.QwenUiTester/LacunaAuto.QwenUiTester.csproj` — passed.
    - `dotnet test test/LacunaAuto.Api.Test/LacunaAuto.Api.Test.csproj` — passed (1/1).
    - `dotnet run ... --skip-regression --regression-only --api-required` with normal ports — exited `0` with topology ready.
    - `dotnet run ... --skip-regression --regression-only --api-required --api-base-url http://127.0.0.1:5132/` (forced API/Web port collision) — exited `3` with required-topology-unavailable behavior.
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

The previously documented `dotnet test LacunaAuto.sln` VSTest blocker did **not** reproduce: on 2026-10-19 with .NET SDK `10.0.401`, `dotnet test LacunaAuto.sln` reported `Test run summary: Passed!` for both `LacunaAuto.Api.Test` and `LacunaAuto.UI.Shared.Tests` (0 failed). No test-infrastructure change was made.

The host `Components/Pages/NotFound.razor` (`src/LacunaAuto.Web/LacunaAuto.Web/`) was **removed**: with the supported `NotFoundPage="typeof(NotFoundPage)"` router attribute, the client page `LacunaAuto.Web.Client/Components/Pages/NotFoundPage.razor` owns `@page "/not-found"` and is the endpoint that `UseStatusCodePagesWithReExecute("/not-found")` re-executes. Keeping both pages registered the same route and produced an AmbiguousMatchException. Any future 404 content change belongs in the client `NotFoundPage.razor`.

## Agent Rule

Update this file only with work that was actually implemented and verified. Keep **Now** to one small active slice. Do not change product scope or Roadmap order without explicit user approval.
