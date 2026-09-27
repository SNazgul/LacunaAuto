# LacunaAuto Current Work

## Current Goal

Finish the browsing UI foundation while keeping the app runnable after every accepted change.

## Now

- [ ] Review and refine the mobile `/listings/{id}` page at approximately 390×844.
- [ ] Build after the focused UI change.
- [ ] Visually verify the result before starting another screen.

Do not change backend code, auth, photo storage, or Hybrid in this task.

## Next

After `/listings/{id}` is accepted:

- verify the three browsing screens on desktop/responsive widths;
- verify EN / UK / RU and independent Language / Regional Format / Currency;
- verify PWA installability.

After the browsing UI foundation is accepted, start Roadmap milestone 2 as a small vertical slice: listing domain + PostgreSQL/EF Core + minimal browsing API + connect the existing browsing UI.

## Recently Completed

- Home mobile layout reviewed and accepted.
- Bottom navigation fixed to `Home | Sell (+) | Search`.
- Mobile `/listings` reviewed and accepted.
- Shared Razor UI foundation, seed-data Home/Listings/Details, localization, and persisted preferences are in place.
- Production data decision: PostgreSQL on the server; all image files will be stored separately in Backblaze B2.

## Known Issue

`dotnet test LacunaAuto.sln` has a pre-existing .NET 10 / Microsoft.Testing.Platform VSTest configuration issue. Do not fix it unless the task explicitly includes test infrastructure.

## Agent Rule

Update this file only with work that was actually implemented and verified. Keep **Now** to one small active slice. Do not change product scope or Roadmap order without explicit user approval.
