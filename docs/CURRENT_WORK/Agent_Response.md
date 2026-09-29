Implemented focused localization fixes for remaining hardcoded browsing UI text, preserving independent preference behavior (Language, RegionalFormat, CurrencyCode).

What changed
- Localized remaining hardcoded text in Listing Details:
  - Mileage label now uses localization key.
  - Description section title now uses localization key.
  - Placeholder description replaced with localized format string and parameters (Year, Make, Model).
- Localized hardcoded accessibility labels in browsing UI:
  - Home filter button aria-label localized.
  - Listings filter button aria-label localized.
  - Bottom navigation aria-label and Sell button aria-label localized.
- Added new resource keys in EN/UK/RU resource files:
  - Mileage
  - Description
  - ListingPlaceholderDescription
  - Filter
  - Sell
  - PrimaryNavigation

Scope constraints respected
- Kept "km" unchanged.
- Did not translate listing data (make/model/trim/location).
- Did not change RegionalFormat behavior.
- Did not use CurrencyCode to relabel or convert prices.
- Did not redesign layouts or add settings UI.
- No Home/Listings layout or behavior redesign changes.

Files changed
- src/LacunaAuto.Web/LacunaAuto.Web.Client/Pages/ListingDetails.razor
- src/LacunaAuto.Web/LacunaAuto.Web.Client/Pages/Home.razor
- src/LacunaAuto.Web/LacunaAuto.Web.Client/Pages/Listings.razor
- src/LacunaAuto.Web/LacunaAuto.Web.Client/Components/Layout/NavMenu.razor
- src/LacunaAuto.UI.Shared/Resources/SharedResources.resx
- src/LacunaAuto.UI.Shared/Resources/SharedResources.uk.resx
- src/LacunaAuto.UI.Shared/Resources/SharedResources.ru.resx

Validation
- Ran: dotnet build LacunaAuto.sln --nologo
- Initial attempt hit file-lock errors from running LacunaAuto.Web process.
- Terminated locking process and reran build.
- Final result: Build succeeded.

Intentionally not changed
- docs/Development/CURRENT_WORK.md was not updated to mark localization/preferences verification as accepted.
- No commits were made.