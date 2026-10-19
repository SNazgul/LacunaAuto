# LacunaAuto UI automation (E2E)

Playwright-driven browser tests for the implemented browsing UI (Milestone 1).
These tests live outside the .NET solution and do not affect `dotnet build` /
`dotnet test`.

Verified with: Node `v26.7.0`, npm `11.19.0`, Playwright `1.64.1`
(`@playwright/test` + `playwright`), .NET SDK `10.0.401`.

## Setup from a fresh checkout

```powershell
cd e2e
npm install
npx playwright install chromium      # downloads the pinned Chromium build
```

Then start the app under test from the repository root:

```powershell
dotnet run --project src/LacunaAuto.Web/LacunaAuto.Web
```

The default `http` launch profile serves plain HTTP on
`http://localhost:5032/`, which is the suite's default base URL.

## Running the tests

```powershell
cd e2e
npm test            # full suite (specs/)
npm run test:negative   # fault-injection checks, EXPECTED to fail (see below)
```

Override the target origin with `E2E_BASE_URL` (read by both config files):

```powershell
$env:E2E_BASE_URL = "http://localhost:5032/"
npm test
```

`npm test` writes a machine-readable report to `e2e/e2e-report.json`
(`reporter: [["json", { outputFile: "e2e-report.json" }]`). It contains one
entry per test with status, duration and worker index, plus run `stats`
(`expected`, `skipped`, `unexpected`, `flaky`, `duration`).

## Configuration (`playwright.config.ts`)

The config uses the typed `defineConfig` API of the installed Playwright
version. Options are verified effective by `specs/runner-config.spec.ts`, which
reads the runner's own injectable option fixtures (`actionTimeout`,
`navigationTimeout`, `locale`, `baseURL`) and asserts their values:

| Option | Verified value |
| --- | --- |
| `use.baseURL` | `E2E_BASE_URL` when set, otherwise `http://localhost:5032/` |
| `use.locale` | `en-US` |
| `use.actionTimeout` | `15000` ms |
| `use.navigationTimeout` | `30000` ms |
| default `use.viewport` | `1280x720` (tests set it explicitly) |
| `timeout` | `60000` ms |
| `workers` / `retries` | `2` / `1` |
| `screenshot` / `video` / `trace` | `"off"` (these take mode strings, not booleans) |

`actionTimeout`, `navigationTimeout`, `locale` and `baseURL` must be declared
inside `use`: a top-level placement is **not** picked up by the runner in this
version (a probe with `actionTimeout`/`navigationTimeout` at the top level read
back as `0`/`0`, and a top-level `baseURL` read back as `undefined`). The
`runner-config.spec.ts` assertions fail if that placement regresses.

The expected `baseURL` in that test is derived from `E2E_BASE_URL` through the
shared `APP_BASE_URL` helper constant (same fallback as the config), so an
origin override such as `E2E_BASE_URL=http://localhost:5132/` keeps the
assertion meaningful instead of failing. The timeouts and `locale` assertions
stay hardcoded.

## Suites

| Spec | What the assertions establish |
| --- | --- |
| `runner-config.spec.ts` | The runner's option fixtures report `actionTimeout = 15000`, `navigationTimeout = 30000`, `locale = en-US`, and `baseURL` equal to the effective origin (`E2E_BASE_URL` when set, otherwise `http://localhost:5032/`) |
| `navigation.spec.ts` | Home seed feed (8 cards); bottom-nav link clicks change the URL; Sell button carries `disabled`; unknown listing id renders the `.not-found` block and its back button returns home; direct request to an unknown route returns HTTP 404 and renders the localized `.not-found-page` inside the app layout (`main` + `nav.bottom-nav`), and its Home button returns to `/`; client-side navigation performed by `window.Blazor.navigateTo` (the call `NavigationManager.NavigateTo` compiles to) starts from rendered Home, sets a marker on the current JS window, tracks main-frame document requests, and asserts the localized not-found heading and URL, that the marker still survives, that **no** new main-frame document request was issued, and that the Home control returns to the rendered 8-card Home page in the same document |
| `home.spec.ts` | Search-bar click navigates to `/listings`; 6 category chips with exactly one active `All Vehicles`, inactive chips carry `disabled`, and the Filter button is `disabled`; 8 cards with 4 `Just Added` badges; first card click opens `/listings/1` with the matching model heading |
| `listings.spec.ts` | `8 results` / `Newest` labels; back button returns home; 8 `<img>` elements whose `src` points at `/images/vehicles/` **and that all finish decoding** (`complete` + non-zero intrinsic size, polled); Filter button `disabled` |
| `listing-details.spec.ts` | Make/model/price/location for listing 1; 4 quick facts with exact values; description section; 2 contact buttons with `disabled`; back button returns to `/listings` |
| `preferences-localization.spec.ts` | First run persists detected preferences (`en`/`en-US`/`USD` from the pinned browser locale); seeded preferences survive a plain reload and still drive the rendered strings; UK/RU string sets including the localized 404 heading; UK separators (`45 000 USD`) with the price currency unchanged; EN strings with UK separators; RU strings with EN separators (`45,000 USD`); clearing storage re-detects from the browser locale |
| `preference-readiness.spec.ts` | After a preference-triggered reload the helper returns only when the app is rendered (8 cards + localized strings), including a run where every response is delayed by 400 ms |
| `responsive.spec.ts` | Home and Listings grids at 599/600/899/900/1199/1200 px produce 1/2/2/3/3/4 columns; bottom nav visible at 1023 px and hidden at 1024 px; Listing Details at 390 and 1440 px has no horizontal document overflow and the shell/price stay inside the viewport |
| `pwa-assets.spec.ts` | `/manifest.webmanifest` returns 200 with the expected name/short name/start URL/display and 192/512 icons; both icon files return 200; the root HTML references Blazor |

## Negative checks (`negative/`)

`npm run test:negative` runs the same assertions against a deliberately broken
app. Both tests are **expected to fail**; a green run here would mean the suite
is not actually checking what it claims:

- `image-load.spec.ts` aborts every `**/images/vehicles/**` request. The element
  count is still 8, but the decoded-image poll times out with
  `Expected: 8 / Received: 0` - proving the image assertion detects broken
  loading.
- `preference-readiness.spec.ts` seeds different preferences, reloads with
  `waitUntil: "load"` only (no readiness wait) and asserts the feed: the probe
  logs `cards right after bare reload = 0`, proving the race that
  `seedPreferences`/`ensurePreferences` must close.

The fault injection lives only in `negative/`; normal runs are unaffected.

## Isolation and locale assumptions

- Playwright creates a **fresh BrowserContext per test**, so `localStorage` does
  not leak between tests. The suite still calls `ensurePreferences` so each test
  states its preference precondition explicitly rather than depending on
  detection order.
- The browser locale is pinned to `en-US` (`use.locale`), which is what makes
  first-run language detection deterministic. Tests that need another language
  seed preferences explicitly.
- Number formatting is asserted through `normalizeSpaces`, which normalizes the
  NBSP/narrow-NBSP separators the app emits for UK/RU locales.

## Not covered by these tests

- There is **no preferences UI in the app** (no Preferences window in
  `LacunaAuto.Web.Client`; `UI_SPEC.md` has no such window; preferences are only
  detected/stored by `LocalStorageUserPreferencesService`). Preferences are
  therefore exercised through the same `localStorage` key the app itself writes,
  and persistence is verified across reloads. A UI-driven check becomes
  possible when that window is implemented.
- PWA **installability** is not verified (no manifest parsing beyond the served
  JSON, no install prompt, no service worker). Only manifest/icon asset checks are
  covered.
- Visual appearance is asserted through layout/geometry checks (grid columns,
  overflow, element positions) plus one manual screenshot of the 404 page; no
  screenshot-diffing is used.
- The `/listings` empty state, filters, sort, sell, chat and the API-backed
  listing data are out of scope for Milestone 1.
