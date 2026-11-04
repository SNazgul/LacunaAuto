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

- continue milestone 2 with additional filters in separate slices;
- keep prices in each listing's original currency until real conversion is explicitly implemented.

## Recently Completed

- Increased the STRATA Vision smoke timeout from 60 to 240 seconds. All eight runner tests passed, and the two-PNG check accepted a loopback response delayed by 65 seconds.

- Listings combobox Escape→ArrowUp reopen fix completed on `feature/listings-model-filter`.
  - Updated [`Listings.razor`](../../src/LacunaAuto.Web/LacunaAuto.Web.Client/Pages/Listings.razor) so make/model active-option scrolling is queued and executed in `OnAfterRenderAsync`, ensuring the active option scrolls only after the listbox has rendered.
  - Fixed keyboard reopen behavior for both searchable dropdowns: after Escape closes the popup, ArrowUp reopens it on the last option and keeps that option visible.
  - Extended the existing 390px Playwright regression in [`listings.spec.ts`](../../e2e/specs/listings.spec.ts) with Escape → ArrowUp coverage for both make and model listboxes.
  - Verification on 2026-11-04:
    - `dotnet build src/LacunaAuto.Web/LacunaAuto.Web/LacunaAuto.Web.csproj` — passed.
    - `npm --prefix e2e test -- specs/listings.spec.ts --grep "390px combobox keyboard keeps active option visible and tabs past popup options"` — **1/1 passed**.
- Listings make/model combobox regression follow-up completed on `feature/listings-model-filter`.
  - Listings request timing on make changes fixed in [`Listings.razor`](../../src/LacunaAuto.Web/LacunaAuto.Web.Client/Pages/Listings.razor): selecting a make now starts the page-1 listings request immediately while model options load independently.
  - Preserved stale model-options response protection when switching makes during pending model-options requests.
  - Kept model-options retry scoped to model options only (no coupled listings refresh).
  - Improved combobox keyboard behavior:
    - active make/model option now scrolls into view during ArrowUp/ArrowDown navigation;
    - popup option buttons are removed from tab order (`tabindex="-1"`);
    - Tab closes make/model popups and advances focus naturally.
  - Added JS helper `scrollListboxOptionIntoView` in [`App.razor`](../../src/LacunaAuto.Web/LacunaAuto.Web/Components/App.razor) for listbox active-option visibility.
  - Added Playwright regressions in [`listings.spec.ts`](../../e2e/specs/listings.spec.ts):
    - delayed model-options + make-switch coverage ensuring immediate listings filtering, stale-response safety, and model-options-only retry;
    - 390px keyboard coverage for active-option visibility while scrolling and Tab behavior, with Enter/Escape/mouse interaction preservation.
  - Verification on 2026-11-04:
    - `dotnet build src/LacunaAuto.Web/LacunaAuto.Web/LacunaAuto.Web.csproj` — passed.
    - `npm --prefix e2e test -- specs/listings.spec.ts specs/api-browsing.spec.ts` — **26/26 passed**.
    - `npm --prefix e2e test` — **62/62 passed**.
- Listings make-dependent model filter + searchable combobox slice implemented on `feature/listings-model-filter` (spec commit `1e4340f` included).
  - Added `GET /api/listings/models?make=...` with contract behavior:
    - missing/blank make -> `400 ProblemDetails` with `traceId`;
    - unknown make -> `200 []`.
  - Extended `GET /api/listings/page` with optional `model` filter and contract behavior:
    - blank model leaves model filter unset;
    - nonblank model without make -> `400 ProblemDetails` with `traceId`.
  - Applied escaped exact case-insensitive matching for make/model in PostgreSQL (`ILIKE` with literal `%`, `_`, `\` handling) before count/paging; preserved sort semantics and `Id` tie-breaker.
  - Updated Web proxy forwarding to include `model` query and `/api/listings/models`, preserving upstream `400 ProblemDetails` passthrough for listings routes.
  - Replaced make/model selectors on `/listings` with accessible searchable combobox/listbox controls:
    - typed query is separate from applied filter;
    - typing narrows suggestions without triggering a listings request;
    - Enter applies highlighted option;
    - Escape/blur without selection restores applied value;
    - changing/clearing make clears selected model; make/model changes reset page to 1 while keeping sort;
    - preserved stale model-options response protection.
  - Added/updated coverage for API contract, client route composition, live Web-proxy + PostgreSQL behavior, and 390px keyboard/typeahead flows.
  - Verification on 2026-11-04:
    - `dotnet build src/LacunaAuto.Api/LacunaAuto.Api.csproj` — passed.
    - `dotnet build src/LacunaAuto.Web/LacunaAuto.Web/LacunaAuto.Web.csproj` — passed.
    - `dotnet test test/LacunaAuto.Api.Test/LacunaAuto.Api.Test.csproj` — **42/42 passed**.
    - `dotnet test test/LacunaAuto.UI.Shared.Tests/LacunaAuto.UI.Shared.Tests.csproj` — **28/28 passed**.
    - `npm --prefix e2e test -- specs/api-browsing.spec.ts` — **12/12 passed**.
    - `npm --prefix e2e test -- specs/listings.spec.ts` — **12/12 passed**.
    - `npm --prefix e2e test` — **60/60 passed**.
    - `dotnet restore/build/test LacunaAuto.sln` — blocked by existing `NETSDK1147` MAUI Android workload requirement (`LacunaAuto.Hybrid` / `LacunaAuto.Frontend`), unchanged by this slice.
- Listings pagination review follow-up completed on `feature/listings-pagination`.
  - Added post-pagination accessibility behavior on `/listings`: after successful Previous/Next actions, the app scrolls the next-page results start under the sticky header and moves focus to the results heading for keyboard/screen-reader continuity.
  - Limited upstream `400 ProblemDetails` passthrough in the Web proxy to `GET /api/listings/page` only; other listings routes keep existing non-success handling.
  - Extended the live Web-proxy + PostgreSQL Playwright contract check to request page 1 and page 2 with `pageSize=3`, assert exact ordered ID slices, and verify consistent totals across both pages.
  - Added mocked UI regressions for:
    - 390px keyboard-activated Next pagination focus/scroll behavior.
    - Catalog shrink invalidating the requested page, with a single recovery fetch to the last valid page.
  - Verification on 2026-11-04:
    - `dotnet build src/LacunaAuto.Web/LacunaAuto.Web/LacunaAuto.Web.csproj` — passed.
    - `dotnet test test/LacunaAuto.Api.Test/LacunaAuto.Api.Test.csproj` — **33/33 passed**.
    - `dotnet test test/LacunaAuto.UI.Shared.Tests/LacunaAuto.UI.Shared.Tests.csproj` — **24/24 passed**.
    - `npm --prefix e2e test -- specs/listings.spec.ts specs/api-browsing.spec.ts` — **23/23 passed**.
    - `npm --prefix e2e test` — **59/59 passed**.
    - `dotnet restore/build/test LacunaAuto.sln` — blocked by existing `NETSDK1147` MAUI Android workload requirement (`LacunaAuto.Hybrid` / `LacunaAuto.Frontend`), unchanged by this follow-up.
- Listings server-side pagination slice implemented on `feature/listings-pagination`.
  - Added paged public route `GET /api/listings/page` with 1-based paging (`page`, `pageSize`), default page size `8`, maximum `24`, and `400 ProblemDetails` + `traceId` for malformed/out-of-range pagination parameters.
  - Preserved Home contract by keeping `GET /api/listings` as the existing array response, and added `GET /api/listings/makes` for make options independent of current page.
  - Applied published-only and optional make filtering before counting and paging, preserved year sort semantics plus `Id` tie-breaker, and bounded offset math for large page values (including `page=2147483647`).
  - Updated Web proxy forwarding for `/api/listings/page` and `/api/listings/makes`; current behavior forwards upstream `400` payloads for `/api/listings/page` and preserves existing outage handling for other listings routes.
  - Updated `/listings` UI to use server-side pagination with localized Previous/Next controls and page indicator, filtered-total header count (including singular label behavior), page reset on make/sort changes, one-time fallback to the last valid page after shrink, retryable make-options errors, and retry preserving selected make/sort/page on page-request failures.
  - Updated mocks and tests for the new routes while preserving the normal eight-listing development seed.
  - Verification on 2026-11-03:
    - `dotnet test test/LacunaAuto.Api.Test/LacunaAuto.Api.Test.csproj` — **33/33 passed**.
    - `dotnet test test/LacunaAuto.UI.Shared.Tests/LacunaAuto.UI.Shared.Tests.csproj` — **24/24 passed**.
    - `npm --prefix e2e test -- specs/listings.spec.ts specs/api-browsing.spec.ts` — **21/21 passed**.
    - `npm --prefix e2e test` — **57/57 passed**.
    - `dotnet restore/build/test LacunaAuto.sln` — blocked by existing `NETSDK1147` MAUI Android workload requirement (`LacunaAuto.Hybrid` / `LacunaAuto.Frontend`), unchanged by this slice.
- Listings year-sort UX follow-up on `feature/listings-year-sort`: made the two model-year directions visually distinct in the closed selector at 390px for EN/UK/RU.
  - Shortened localized sort options so distinguishing words appear first (`Newest first` / `Oldest first`, `Новіші спочатку` / `Старіші спочатку`, `Новые сначала` / `Старые сначала`).
  - Kept model-year context in the sort control accessible name (`Sort by model year` and localized equivalents).
  - Extended Playwright coverage at 390px to verify both selected sort states and selector viewport bounds in English, Ukrainian, and Russian.
  - Verification on 2026-11-03:
    - `npm --prefix e2e test -- specs/listings.spec.ts specs/preferences-localization.spec.ts` — **17/17 passed**.
- Listings year-sort slice implemented on `feature/listings-year-sort`.
  - Replaced the static `Newest` text on `/listings` with a localized sort selector: `Default order`, `Newest first`, and `Oldest first` (EN/UK/RU localized).
  - Added optional `sort=year-desc|year-asc` flow through the browser client, Web proxy, and API. Omitted/blank/unsupported sort values now keep default ID ordering.
  - Applied sorting after published-only and make filtering in PostgreSQL-backed reads, with required tie-breaker `Id ASC` for equal years.
  - Preserved state composition: changing make keeps sort, changing sort keeps make, clearing make keeps sort, and returning to default order keeps make.
  - Added coverage for sorting directions, tie-break behavior, make+sort composition, 390px dual-control viewport safety, Home request query omissions, and live Web-proxy forwarding.
  - Verification on 2026-11-03:
    - `dotnet test test/LacunaAuto.Api.Test/LacunaAuto.Api.Test.csproj` — **24/24 passed**.
    - `dotnet test test/LacunaAuto.UI.Shared.Tests/LacunaAuto.UI.Shared.Tests.csproj` — **18/18 passed**.
    - `npm --prefix e2e test -- specs/listings.spec.ts specs/api-browsing.spec.ts specs/home.spec.ts specs/preferences-localization.spec.ts` — **32/32 passed**.
    - `npm --prefix e2e test` — **53/53 passed**.
    - `dotnet restore/build/test LacunaAuto.sln` — blocked by existing `NETSDK1147` MAUI Android workload requirement (`LacunaAuto.Hybrid` / `LacunaAuto.Frontend`), unchanged by this slice.
- Listings make-filter follow-up: localized singular result count on `/listings` when exactly one listing is shown.
  - Added `Result` localization entries for EN/UK/RU and updated Listings header text logic to keep existing loading/error/multi-result behavior unchanged.
  - Extended the live Web-proxy seeded `Volkswagen` filter test to assert `1 result` after selecting the make and `8 results` after clearing to `All makes`.
  - Verification on 2026-11-03:
    - `dotnet build src/LacunaAuto.Web/LacunaAuto.Web/LacunaAuto.Web.csproj` — passed (existing IDE0040 warnings only).
    - `dotnet test test/LacunaAuto.Api.Test/LacunaAuto.Api.Test.csproj` — **17/17 passed**.
    - `npx playwright test specs/listings.spec.ts` — **6/6 passed**.
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
