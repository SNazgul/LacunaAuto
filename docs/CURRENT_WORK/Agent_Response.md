Implemented a desktop-only Listing Details image geometry fix to stop the left photo panel from stretching to full content-column height and over-cropping vehicle photos.

What changed
- In desktop breakpoint (>=1024px) for Listing Details image panel:
  - removed full-height stretching behavior
  - set natural landscape aspect ratio (4:3)
  - top-aligned the image panel within the desktop grid so it can be shorter than the right details column
- Kept object-fit: cover in place.
- Preserved rounded corners and floating back button.

Scope constraints respected
- Kept existing two-column desktop structure at >=1024px.
- Did not change accepted mobile/tablet layout.
- Did not change right-side details/content card.
- Did not change typography, spacing, navigation, buttons, data, or functionality.
- Did not modify Home or Listings.

Validation
- Ran: dotnet build LacunaAuto.sln --nologo
- Result: Build succeeded with warnings (existing IDE0040 style warnings in unrelated Hybrid/Frontend files).

Exactly which files changed
- src/LacunaAuto.Web/LacunaAuto.Web.Client/Pages/ListingDetails.razor.css

Intentionally not changed
- docs/Development/CURRENT_WORK.md was not updated to mark responsive review as accepted.
- No commits were made.
