# LacunaAuto Current Work

## Current Goal

Finish the browsing UI foundation while keeping the app runnable after every accepted change.

## Now

- [x] Implement and verify the Web client PWA foundation for hosted Blazor (manifest, icons, service worker wiring, publish artifacts).
- [ ] Confirm the app can be installed from a supported desktop/mobile browser.
- [ ] Confirm the installed app launches into the current browsing experience.
- [x] Re-run `dotnet build LacunaAuto.sln --nologo` after stopping any running `LacunaAuto.Web` process that locks Debug output.

Do not start backend/API/database/auth/photo-storage/Hybrid work in this task.

## Next

After PWA installability is accepted:

- mark Roadmap milestone 1 — Complete the Browsing UI Foundation — complete;
- start Roadmap milestone 2 as a small vertical slice: listing domain + PostgreSQL/EF Core + minimal browsing API + connect the existing browsing UI.

## Recently Completed

- Replaced published service-worker offline precache logic with a minimal install/activate worker to avoid hosted Blazor Web App Interactive WASM framework/SRI conflicts; offline support is intentionally deferred to a separate future task.
- Fixed published service-worker installation failure caused by non-served `LacunaAuto.Web.Client.styles.css`: removed explicit host link, preserved styling through `LacunaAuto.Web.styles.css` scoped-bundle imports, and added targeted published service-worker precache exclusion for that single generated path.
- Hosted Blazor PWA foundation implemented in `LacunaAuto.Web` + `LacunaAuto.Web.Client`: manifest metadata, 192/512 icons, service worker registration from host document, dev vs published service-worker behavior, and Release publish artifact validation (`manifest.webmanifest`, `service-worker.js`, `service-worker-assets.js`, icons).
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

`dotnet test LacunaAuto.sln` has a pre-existing .NET 10 / Microsoft.Testing.Platform VSTest configuration issue. Do not fix it unless the task explicitly includes test infrastructure.

## Agent Rule

Update this file only with work that was actually implemented and verified. Keep **Now** to one small active slice. Do not change product scope or Roadmap order without explicit user approval.
