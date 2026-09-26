# LacunaAuto Current Work

## Purpose

This file tracks the active implementation slice, recently completed work, known issues, and open implementation questions.

It is intentionally more detailed and more frequently updated than [Roadmap.md](../Product/Roadmap.md).

Document roles:

- [PRODUCT_SCOPE.md](../Product/PRODUCT_SCOPE.md) defines approved product scope and MVP boundaries.
- [Roadmap.md](../Product/Roadmap.md) defines the delivery sequence and milestone outcomes.
- [UI_SPEC.md](../Product/UI/UI_SPEC.md) defines the visual and interaction direction.
- `CURRENT_WORK.md` records what is being worked on now and what has been verified.

Do not use this file to silently expand product scope or reorder roadmap milestones.

## Current Goal

Complete and visually validate the browsing UI foundation while keeping the application runnable and usable after every accepted change.

Current milestone: **Roadmap milestone 1 — Complete the Browsing UI Foundation**.

## In Progress

- [x] Review the Home page on a mobile viewport.
- [x] Fix the mobile bottom navigation to a stable `Home | Sell (+) | Search` layout.
- [ ] Review and refine the Listings page on the same mobile viewport.
- [ ] Review and refine the Listing Details page on the same mobile viewport.
- [ ] Verify the three browsing screens on desktop/responsive widths.
- [ ] Verify EN / UK / RU UI localization on the browsing flow.
- [ ] Verify that Language, Regional Format, and Currency remain independent persisted preferences.
- [ ] Verify PWA installability for the Web client.

Visual-review items are complete only after the result has been inspected and accepted, not merely because code was written.

## Completed Foundation

- [x] Added `LacunaAuto.UI.Shared` as the shared Razor Class Library.
- [x] Configured the Web client for the current Interactive WebAssembly/PWA architecture.
- [x] Added client-side routes, layout, and navigation.
- [x] Added shared design tokens and reusable vehicle-listing UI components.
- [x] Added Home, Listings, and Listing Details pages backed by development seed data.
- [x] Added EN / UK / RU localization resources and localization infrastructure.
- [x] Added independent Language, Regional Format, and Currency preferences.
- [x] Persisted anonymous-user preferences in browser local storage.
- [x] Added regional formatting support for prices and mileage.
- [x] Added typed and localized fuel/transmission display values.
- [x] Removed artificial loading delays from the seed-data browsing pages.
- [x] Kept listing prices in their original currency; preferred currency does not relabel or fake-convert prices.

## Next After the Current Goal

Start Roadmap milestone 2 as a small end-to-end listings slice:

1. define only the listing/domain data needed for browsing;
2. persist it in PostgreSQL with EF Core;
3. expose the minimal browsing API;
4. replace seed-data reads in the existing browsing UI with real API calls;
5. verify that the same Home / Listings / Details flow still works before expanding scope.

Do not build a large disconnected backend and postpone integration until later.

## Open Questions

- Replace the current local demo vehicle illustrations with realistic local demo photos, or keep them temporarily as explicit placeholders?
- Which exact filters and sorting options belong in the first real Listings slice?
- What should the first Settings/preferences UI look like and where should it be reachable from?
- What offline behavior, if any, is required beyond normal PWA/browser caching?

## Known Issues / Follow-up

- `dotnet build LacunaAuto.sln` succeeds for the current UI work.
- `dotnet test LacunaAuto.sln` currently hits a pre-existing .NET 10 / Microsoft.Testing.Platform VSTest configuration problem in the test projects. Do not treat this as a failure introduced by the browsing UI work. Fix it only in a task that explicitly includes test infrastructure.

## Last Implementation Notes

- The Home page mobile layout has been visually reviewed and is acceptable for the current MVP foundation.
- The mobile bottom navigation now has three stable positions: Home, central Sell (+), and Search.
- The top-right Home action is a filter/sliders affordance and is intentionally non-functional until the filtering slice is implemented.
- Current vehicle images are local demo assets, not production photo-storage integration.
- `SkeletonCard` remains available for future genuine asynchronous API loading states.
- `LacunaAuto.Hybrid` is not part of the current implementation slice.

## How Agents Must Update This File

After an implementation task that changes the current slice:

1. update only the status that was actually implemented and verified;
2. move verified work to the appropriate completed section;
3. record new known issues or unresolved implementation questions;
4. keep the next step small and runnable;
5. do not change product scope, roadmap order, or product decisions without explicit user approval;
6. do not mark a Roadmap milestone complete merely because code exists — its `Done when` outcome must be verified.
