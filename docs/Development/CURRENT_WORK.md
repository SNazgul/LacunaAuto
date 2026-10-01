# LacunaAuto Current Work

## Current Goal

Start the listings browsing vertical slice while keeping the accepted browsing UI stable and the app runnable after every change.

## Now

- [ ] Define the minimum listing/domain data required by the accepted Home, Listings, and Listing Details UI.
- [ ] Add the first PostgreSQL / EF Core persistence slice and reproducible development data for browsing.
- [ ] Add the minimal public browsing API needed by the existing UI.
- [ ] Connect the existing browsing UI to the real API without redesigning accepted screens.

Keep this as a small end-to-end slice. Do not start authentication, listing management, photo upload/storage implementation, chat, or Hybrid work in this task.

## Next

After the first real listings browsing slice is accepted:

- continue milestone 2 with the first agreed filtering / pagination / sorting behavior only as needed;
- keep prices in each listing's original currency until real conversion is explicitly implemented.

## Recently Completed

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

`dotnet test LacunaAuto.sln` has a pre-existing .NET 10 / Microsoft.Testing.Platform VSTest configuration issue. Do not fix it unless the task explicitly includes test infrastructure.

## Agent Rule

Update this file only with work that was actually implemented and verified. Keep **Now** to one small active slice. Do not change product scope or Roadmap order without explicit user approval.
