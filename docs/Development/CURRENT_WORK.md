# LacunaAuto Current Work

## Current Goal

Finish the browsing UI foundation while keeping the app runnable after every accepted change.

## Now

- [ ] Verify EN / UK / RU across the browsing flow.
- [ ] Verify that Language, Regional Format, and Currency remain independent persisted preferences.
- [ ] Build after any focused localization/preferences fixes.
- [ ] Visually verify the result before moving to PWA installability.

Do not start backend/API/database/auth/photo-storage/Hybrid work in this task.

## Next

After localization/preferences verification is accepted:

- verify PWA installability.

After the browsing UI foundation is accepted, start Roadmap milestone 2 as a small vertical slice: listing domain + PostgreSQL/EF Core + minimal browsing API + connect the existing browsing UI.

## Recently Completed

- Home mobile layout reviewed and accepted.
- Bottom navigation fixed to `Home | Sell (+) | Search`.
- Mobile `/listings` reviewed and accepted.
- Mobile `/listings/{id}` reviewed and accepted.
- Home, Listings, and Listing Details responsive layouts reviewed and accepted across mobile, tablet, desktop, and wide desktop widths.
- Responsive listing grids use 1 / 2 / 3 / 4 columns at the agreed breakpoints, with mobile bottom navigation hidden from 1024px upward.
- Shared Razor UI foundation, seed-data Home/Listings/Details, localization, and persisted preferences are in place.
- Production data decision: PostgreSQL on the server; all image files will be stored separately in Backblaze B2.

## Known Issue

`dotnet test LacunaAuto.sln` has a pre-existing .NET 10 / Microsoft.Testing.Platform VSTest configuration issue. Do not fix it unless the task explicitly includes test infrastructure.

## Agent Rule

Update this file only with work that was actually implemented and verified. Keep **Now** to one small active slice. Do not change product scope or Roadmap order without explicit user approval.
