# UI_SPEC.md — LacunaAuto UI / UX Specification

> **Canonical UI specification for AI agents and developers.**
>
> This document defines the approved visual direction, interaction principles, responsive behavior, navigation model, and reusable UI patterns for LacunaAuto.
>
> Product scope and feature availability are defined by `docs/Product/PRODUCT_SCOPE.md`.  
> Repository and implementation rules are defined by `AGENTS.md`.
>
> **Important:** this document may describe UI affordances for future features. Their presence in this specification does **not** move those features into implementation scope. If `PRODUCT_SCOPE.md` marks a feature as future/out of scope, do not implement its behavior unless the relevant product specification explicitly moves it into scope.

---

## 1. Design Direction

LacunaAuto uses a clean, modern, premium automotive marketplace style.

The approved direction is based on:

- a light, spacious interface;
- large high-quality vehicle photography;
- strong black typography;
- a vivid orange-red accent;
- rounded cards and controls;
- minimal visual noise;
- touch-friendly mobile navigation;
- simple, clear information hierarchy;
- a premium but approachable automotive character.

The UI should feel closer to a modern consumer mobile marketplace than to a traditional dense classifieds website.

### 1.1 Visual Keywords

- Modern
- Premium
- Automotive
- Minimal
- Clean
- Photo-first
- Fast
- Friendly
- High contrast
- Mobile-first

---

## 2. Platform Priority

### 2.1 Primary Target — Mobile

The UI is designed **mobile-first**.

Primary design width range:

- 360–430 CSS px for common phone layouts;
- content must remain usable below and above this range;
- no screen should depend on one exact device resolution.

Primary mobile target:

- **.NET MAUI Blazor Hybrid** for Android/iOS/Windows when the Hybrid application is introduced;
- Razor UI should be shared with the web implementation where practical;
- mobile UI runs inside `BlazorWebView` in the Hybrid application.

### 2.2 Web / PWA

The web application uses **Blazor WebAssembly / PWA**.

Desktop web is a secondary presentation of the same product and design system, not a separate product.

Desktop layouts may reorganize the same components:

- bottom navigation → top navigation / sidebar where appropriate;
- single-column content → grid / split layout;
- full-screen mobile filters → persistent desktop filter sidebar;
- mobile detail stack → image/content split layout.

Do not create completely unrelated desktop and mobile visual languages.

---

## 3. Design Tokens

Exact values may be tuned during implementation, but the visual relationship between tokens must remain consistent.

### 3.1 Colors

| Token | Suggested Value | Purpose |
|---|---:|---|
| `--color-accent` | `#FF5A00` | Primary CTA, active navigation, selected filters |
| `--color-accent-hover` | `#E84F00` | Hover/pressed accent state |
| `--color-text-primary` | `#111111` | Headings, prices, high-priority text |
| `--color-text-secondary` | `#687280` | Metadata and supporting information |
| `--color-text-muted` | `#9AA2B1` | Placeholder / low-priority metadata |
| `--color-background` | `#FFFFFF` | Main surfaces |
| `--color-background-soft` | `#F5F6F8` | App/page background and secondary surfaces |
| `--color-border` | `#E5E7EB` | Dividers and neutral borders |
| `--color-dark-action` | `#101012` | Dark primary buttons / floating action button |
| `--color-error` | implementation token | Error state |
| `--color-success` | implementation token | Success state |

Avoid adding multiple competing brand colors.

### 3.2 Typography

Preferred visual behavior:

- clean modern sans-serif;
- system/native font stack is acceptable and preferred initially;
- strong hierarchy via weight and size rather than decorative fonts.

Suggested scale:

| Role | Weight | Approx. Size |
|---|---:|---:|
| Display / hero | 700 | 32–40 px |
| H1 | 700 | 28–32 px |
| H2 | 600–700 | 22–26 px |
| H3 | 600 | 18–20 px |
| Body | 400 | 14–16 px |
| Metadata | 400–500 | 12–14 px |
| Label / chip | 500–600 | 12–14 px |
| Price | 700 | 24–34 px |

Never reduce important mobile body text below comfortable readability.

### 3.3 Radius

Use rounded geometry consistently.

Suggested values:

- small controls/chips: 12–16 px;
- cards: 20–24 px;
- large image containers: 24–28 px;
- pill buttons: 999 px;
- bottom sheets: 28–36 px top corners;
- floating action button: circular.

### 3.4 Spacing

Use an 8 px-oriented spacing rhythm.

Common values:

- 4 px — micro spacing;
- 8 px — tight spacing;
- 12 px — icon/text spacing;
- 16 px — standard mobile spacing;
- 20–24 px — section spacing;
- 32 px — major section separation.

Default mobile page horizontal padding: approximately **16–20 px**.

### 3.5 Shadows

Use shadows sparingly.

Allowed uses:

- floating CTA;
- elevated price badge;
- floating bottom bar;
- overlays / sheets;
- selected elevated cards.

Cards should not all have strong shadows.

---

## 4. Application Shell — Mobile

### 4.1 Top Area

Home/feed top area contains:

- LacunaAuto logo / wordmark;
- search entry point;
- filter entry point;
- optional profile/avatar entry only when account functionality is in scope.

Search and filter must be easy to reach with one hand.

### 4.2 Primary Search

The primary search should be more discoverable than a standalone search icon.

Preferred mobile form:

- compact search field or expandable search control;
- placeholder such as `Search make, model, keyword…`;
- filter button immediately adjacent;
- filter button may show a badge with the number of active filters.

Search may initially navigate to the listings/search screen rather than providing full instant search.

### 4.3 Quick Category Chips

Below search/header, provide horizontally scrollable category chips such as:

- All Vehicles
- Electric
- SUVs
- Sports
- Sedans
- Trucks

Rules:

- active chip uses the accent color;
- inactive chips use a light/white background with subtle border;
- categories are shortcuts, not a replacement for full filters;
- do not hard-code categories that are not supported by product data.

### 4.4 Bottom Navigation

Approved long-term mobile navigation model:

1. **Feed / Home**
2. **Search**
3. **Sell** — central floating `+`
4. **Saved**
5. **Profile**

For the initial MVP, only destinations explicitly in `PRODUCT_SCOPE.md` should be active.

Future destinations may be visually omitted, disabled, or represented only when a feature specification explicitly asks for a placeholder.

`Inbox` should not occupy a permanent navigation position until messaging is actually part of product scope.

### 4.5 Central Sell Action

The central circular `+` is the strongest mobile action and represents **Sell Vehicle**.

Visual rules:

- dark/black circle;
- white plus icon;
- slightly elevated above the bottom navigation;
- clear touch target;
- subtle shadow.

Behavior must **not** be implemented until listing creation is in product scope.

### 4.6 Safe Areas

Hybrid/mobile implementation must respect:

- Android system navigation areas;
- iOS safe-area insets;
- notches and status bars;
- keyboard appearance.

The bottom navigation must never be hidden behind the OS navigation area.

---

## 5. Home / Feed Screen

### 5.1 Purpose

The Home/Feed screen is the discovery-first entry point into LacunaAuto.

Primary goals:

1. allow the user to search quickly;
2. allow quick category browsing;
3. show attractive vehicle listings;
4. lead the user into listing details.

### 5.2 Mobile Structure

Recommended order:

1. header/logo;
2. search + filters;
3. quick category chips;
4. optional section title / result context;
5. listing cards;
6. bottom navigation.

### 5.3 Listing Feed Mode

The approved discovery presentation uses **large photo-first cards**.

A secondary compact/grid mode may be added later for users who want faster scanning.

Large cards should not force the user to inspect decorative content before seeing:

- vehicle identity;
- price;
- key metadata.

### 5.4 Listing Card

Minimum card information:

- primary vehicle photo;
- make;
- model / trim;
- price;
- year;
- mileage;
- location when useful;
- one additional high-value property if space permits (fuel/powertrain or transmission);
- save/favorite control only when that feature is in scope.

Recommended hierarchy:

1. Photo
2. Make (small uppercase / secondary)
3. Model/trim (strong)
4. Price (high contrast/accent area)
5. Year + mileage + optional third characteristic

Optional badges:

- Just Added
- Electric
- Featured
- Dealer

Only show a badge when the underlying state is real.

### 5.5 Price Treatment

Price should be immediately visible.

The approved visual direction allows an orange price block/badge, but:

- it must not cover important vehicle image content;
- it must remain legible at narrow widths;
- it should not visually overpower the vehicle title.

### 5.6 Listing Interaction

Tapping/clicking the card opens vehicle details.

Do not require the user to tap only the title.

If a save icon exists, its touch target must be independent from the card navigation.

---

## 6. Search / Listings Screen

### 6.1 Purpose

The Listings screen is optimized for **intentional search**, while Home/Feed is optimized for discovery.

### 6.2 Mobile Layout

Recommended:

- compact top bar with back/navigation;
- current result count;
- sort control;
- search query field;
- filter entry/button;
- results displayed as 1-column compact cards or 2-column cards where content remains readable.

For very narrow devices, prefer a single-column compact layout.

### 6.3 Sorting

Initial sorting UI may include:

- Newest
- Price low to high
- Price high to low
- Mileage
- Year

Only expose sorts supported by the backend/API.

### 6.4 Loading / Pagination

The UI must support large result sets without rendering all listings at once.

Preferred behavior:

- incremental loading / pagination;
- skeleton loading state;
- avoid disruptive full-page spinners;
- preserve scroll/filter state when returning from details where practical.

---

## 7. Filters

### 7.1 Mobile Presentation

Filters open as:

- a full-screen mobile panel, or
- a large bottom sheet if the number of controls is small.

For the expected vehicle filter set, a **full-screen filter panel** is preferred.

### 7.2 Initial Filter Set

Product scope currently identifies likely filters:

- Location
- Make
- Model
- Price range
- Year range
- Mileage range
- Fuel type
- Transmission
- Body type

Filters should be introduced gradually according to backend support.

### 7.3 Filter Controls

Preferred controls:

- dropdown/search selector for make/model/location;
- min/max fields or dual-handle range controls for numeric ranges;
- segmented chips for fuel/transmission/body type when option count is small;
- searchable selection list for long option sets.

### 7.4 Actions

Filter panel must provide:

- Close/back;
- Reset/Clear;
- primary action: `Show N vehicles`.

`N` should reflect the current filter result count only when backend support exists. Otherwise use `Show results`.

### 7.5 Active Filter Visibility

After filters are applied:

- filter button displays active count badge where useful;
- selected important filters may appear as removable chips above results.

---

## 8. Vehicle Details Screen

### 8.1 Purpose

Vehicle Details must give the user confidence and enough information to decide whether to contact the seller.

### 8.2 Mobile Structure

Recommended order:

1. large image gallery;
2. back action;
3. save/share actions when supported;
4. make;
5. model/trim;
6. price;
7. location / listing age;
8. quick facts;
9. description;
10. extended specifications;
11. seller/contact section;
12. persistent or near-bottom contact CTA.

### 8.3 Image Gallery

Photo-first presentation is approved.

Requirements:

- horizontal swipe on mobile;
- image position indicator;
- tap image to open full-screen gallery when gallery support is implemented;
- preserve aspect ratio;
- avoid uncontrolled layout jumps while images load;
- use placeholder/skeleton during loading.

### 8.4 Quick Facts

A compact row/grid may show:

- Year
- Mileage
- Transmission
- Fuel / powertrain

Use icons only when the meaning remains obvious.

### 8.5 Description

Show a readable preview.

If description is long:

- show `Read more`;
- expand inline or open a details section;
- do not truncate information permanently.

### 8.6 Contact CTA

Preferred future actions:

- **Contact Seller / Message Seller**
- **Call Seller**

`Make Offer` is not a default MVP action and should only appear if explicitly introduced as a product feature.

For the current MVP, a static/placeholder contact block is acceptable according to `PRODUCT_SCOPE.md`.

---

## 9. Sell Vehicle Flow — Future Feature

The approved visual direction includes a listing creation flow, but **listing creation is outside the initial MVP**.

When this feature is moved into scope, use a **multi-step flow**, not one extremely long form.

Suggested steps:

1. Vehicle / VIN
2. Basic details
3. Specifications / condition
4. Photos
5. Price
6. Seller/contact details
7. Preview
8. Publish

### 9.1 Mobile Form Principles

- one primary action per screen;
- large touch targets;
- autosave draft where possible;
- clear progress indication;
- camera/gallery integration may be Hybrid-specific;
- validation appears close to the relevant field;
- never lose entered data due to navigation.

---

## 10. Saved / Profile / Messaging — Future Features

These UI destinations belong to the long-term design system but are not automatically in implementation scope.

### 10.1 Saved

When implemented:

- vehicle cards reuse the same listing-card components;
- empty state should explain how to save vehicles;
- save action should update immediately and handle backend failure gracefully.

### 10.2 Profile

When implemented:

- account basics;
- own listings;
- settings;
- sign-in/sign-out state;
- seller identity information where relevant.

### 10.3 Messaging

Messaging should not be added to primary navigation until a real messaging feature exists.

---

## 11. States

Every data-driven screen must define the following states.

### 11.1 Loading

Use skeleton placeholders shaped similarly to final content.

Avoid blocking spinners for normal list loading.

### 11.2 Empty

Example intent:

- no matching vehicles;
- explain briefly;
- provide a useful action such as `Clear filters`.

### 11.3 Error

Show:

- concise explanation;
- retry action;
- no raw exception text.

### 11.4 Offline / Weak Connection

Because the web client may be used as a PWA and Hybrid clients may be mobile:

- degrade gracefully;
- show a clear connectivity message where required;
- do not pretend actions succeeded while offline.

---

## 12. Interaction Rules

### 12.1 Touch Targets

Interactive mobile controls should generally provide a touch target of at least **44×44 CSS px**.

### 12.2 Feedback

Buttons and controls need visible:

- pressed state;
- disabled state;
- selected state;
- loading state when an action takes time.

### 12.3 Navigation

- back navigation should preserve user context where practical;
- do not surprise the user with navigation after minor controls;
- opening a listing should be fast and predictable;
- mobile transitions should feel simple, not over-animated.

### 12.4 Pull to Refresh

May be used for mobile feed/results when technically appropriate.

Do not rely on pull-to-refresh as the only refresh mechanism.

---

## 13. Responsive Desktop / Tablet Behavior

Desktop is secondary but must remain polished.

### 13.1 Breakpoint Strategy

Exact breakpoints may be tuned during implementation.

Suggested conceptual ranges:

- Mobile: `< 768 px`
- Tablet: `768–1023 px`
- Desktop: `>= 1024 px`

Do not hard-code behavior to one device model.

### 13.2 Desktop Home

Recommended:

- top navigation;
- larger hero/search area;
- search field visible directly;
- popular categories;
- featured/recent vehicles as a grid.

### 13.3 Desktop Listings

Recommended:

- filter sidebar on the left;
- result count + sort at top;
- 2–4 column listing grid depending on width;
- stable content width;
- no mobile bottom navigation.

### 13.4 Desktop Details

Recommended:

- large gallery left/main area;
- price/key facts/contact summary alongside;
- details and description below;
- CTA remains visible but not intrusive.

---

## 14. Component Model

Prefer reusable Razor components that can work in both Web and Hybrid where practical.

Likely reusable components:

- `AppLogo`
- `SearchBar`
- `FilterButton`
- `CategoryChip`
- `CategoryChipList`
- `VehicleListingCard`
- `VehiclePrice`
- `VehicleBadge`
- `VehicleQuickFacts`
- `VehicleGallery`
- `FilterPanel`
- `RangeFilter`
- `EmptyState`
- `ErrorState`
- `LoadingSkeleton`
- `MobileBottomNavigation`
- `PrimaryButton`
- `SecondaryButton`

Do not create components solely to abstract a few lines of markup.

---

## 15. Web vs Hybrid Implementation Boundaries

### Shared where possible

Keep these platform-neutral:

- layout markup;
- cards;
- typography;
- colors;
- filters;
- listing details;
- form controls;
- navigation concepts;
- loading/empty/error visuals.

### Hybrid-specific only when needed

Examples:

- native camera/photo picker;
- native share sheet;
- device permissions;
- push notifications;
- native deep-link integration;
- Android/iOS safe-area adjustments not solvable by shared CSS.

### Web/PWA-specific only when needed

Examples:

- browser install/PWA prompts;
- SEO/public URL behavior;
- browser-only sharing fallback;
- desktop hover behavior.

Do not duplicate complete screens for Web and Hybrid without a real platform reason.

---

## 16. Accessibility

Minimum expectations:

- sufficient contrast for text and controls;
- keyboard navigation on web;
- visible focus states;
- semantic buttons/links;
- meaningful image alt text;
- form labels associated with fields;
- icons must not be the only carrier of important meaning;
- support browser text scaling reasonably;
- do not encode state by color alone.

---

## 17. Performance / Perceived Performance

Vehicle photography is the largest visual performance risk.

Requirements:

- responsive image sizes;
- lazy-load off-screen listing images;
- reserve image dimensions/aspect ratio to prevent layout shift;
- use thumbnails for listing cards;
- load full-resolution images only when needed;
- use skeletons while loading;
- virtualize or paginate long result lists where appropriate.

On mobile, visual smoothness is more important than decorative effects.

---

## 18. Localization, Regional Formatting, and Currency

LacunaAuto must support internationalization from the beginning.

The application must treat the following as **three independent user preferences**:

1. **Application Language** — controls the language of the UI.
2. **Regional Format** — controls formatting conventions for dates, times, numbers, decimal/group separators, etc.
3. **Currency** — controls the user's preferred currency for monetary values where currency display/conversion is supported.

Changing one preference must **not automatically change either of the other two**.

Examples of valid combinations:

- Russian UI + Polish regional format + PLN
- Ukrainian UI + Ukrainian regional format + USD
- English UI + Polish regional format + EUR

### 18.1 Application Language

Supported UI languages:

- English (`en`) — default and fallback language
- Ukrainian (`uk`)
- Russian (`ru`)

Requirements:

- All user-visible application text must be localizable.
- Do not hard-code user-visible strings directly in Razor components, pages, dialogs, validation messages, navigation items, buttons, placeholders, empty states, error states, or notifications.
- Use the standard .NET localization approach (`IStringLocalizer` / `.resx`) unless the project later defines another localization mechanism.
- Shared Razor components should use the same localization resources in Web/PWA and .NET MAUI Blazor Hybrid where practical.
- If a translation is missing, fall back to English.
- UI layouts must tolerate different translated text lengths without clipping, overlap, or broken alignment.
- Do not design controls around one fixed English label width.
- Vehicle descriptions and other user-entered content must not be translated automatically unless a dedicated product feature explicitly requires it.

### 18.2 Regional Format

Regional Format is **one user setting** controlling culture-dependent presentation such as:

- date format;
- time format;
- decimal separator;
- digit grouping / thousands separator;
- numeric formatting;
- other culture-sensitive presentation where appropriate.

The user does not need separate settings for date format, number format, separators, etc. They are selected together by choosing one regional-format culture/preset.

Examples of regional-format presets may include:

- English (United States)
- English (United Kingdom)
- Ukrainian (Ukraine)
- Russian
- Polish (Poland)

The exact supported preset list may grow independently from the list of application languages.

Requirements:

- Regional Format must be independent from Application Language.
- Changing Application Language must not overwrite a Regional Format explicitly selected by the user.
- Dates and numbers must be formatted using the selected Regional Format.
- Do not manually format culture-sensitive values when standard .NET culture-aware formatting can be used.
- Regional Format must not determine the application's selected currency.

### 18.3 Currency

Currency is an independent user preference.

Requirements:

- The user must be able to select a preferred currency independently of Application Language and Regional Format.
- Use ISO 4217 currency codes internally where applicable, for example `USD`, `EUR`, `PLN`, `UAH`.
- The supported currency list must be configurable/extendable rather than inferred from the supported UI languages.
- Monetary display must respect both the selected currency and the selected Regional Format where appropriate.
- Changing Currency must not change Application Language or Regional Format.
- Changing Regional Format must not change Currency.
- Do not assume that a Russian-language user wants RUB, a Ukrainian-language user wants UAH, or an English-language user wants USD.

Currency conversion, exchange-rate providers, original listing currency, rounding rules, and the presentation of original versus converted prices are separate product/business concerns and must be specified before implementation. The UI architecture must not make those future decisions difficult.

### 18.4 Preference Initialization

On first launch, the application may use device/browser settings to choose sensible initial values:

- Application Language: use a supported device/browser language when available; otherwise English.
- Regional Format: use the device/browser regional culture when supported; otherwise a configured default.
- Currency: use a configured region-aware default when possible; otherwise the product default currency.

These are **initial defaults only**.

Once the user explicitly changes a preference, their selection takes precedence over automatic detection.

### 18.5 Preference Persistence

All three preferences must persist independently between application sessions:

- Application Language
- Regional Format
- Currency

For anonymous users, local client storage is sufficient initially.

When authenticated-user synchronization is introduced, preferences may also be stored server-side and synchronized across devices, but this is not required for the initial anonymous experience.

### 18.6 Settings UI

The settings/profile area should expose three separate controls:

- **Language**
- **Regional format**
- **Currency**

Each control must show the currently selected value and may be changed without modifying either of the other two.

When practical, changes should take effect immediately without requiring an application restart.

### 18.7 Implementation Guidance

Internationalization infrastructure should be introduced with the first implemented UI components rather than added retroactively.

AI agents implementing UI must:

1. use localization resources for all new user-visible strings;
2. add translations for English, Ukrainian, and Russian when introducing new UI text;
3. preserve English as the translation fallback;
4. keep Application Language, Regional Format, and Currency as three separate preference values;
5. never permanently derive one preference from another;
6. avoid duplicating localized strings across components when a shared resource is appropriate;
7. verify that the UI remains usable with longer translated text;
8. use culture-aware .NET formatting APIs for dates and numbers;
9. avoid hard-coded currency assumptions in reusable UI components.

---
## 19. Design Consistency Rules for AI Agents

When implementing UI:

1. Read `AGENTS.md`.
2. Read `docs/Product/PRODUCT_SCOPE.md`.
3. Read this `UI_SPEC.md`.
4. Read the relevant screen/feature specification if one exists.
5. Do not infer that a visually documented future feature is currently in scope.
6. Reuse existing components and design tokens.
7. Do not introduce a new color/style system for a single screen.
8. Prefer CSS isolation and shared design tokens according to project conventions.
9. Keep UI logic out of markup when it becomes complex.
10. Implement loading, empty, and error states for data-driven screens.
11. Preserve mobile-first behavior.
12. Do not broadly refactor unrelated UI while implementing one screen.

---

## 20. Screen-Specific Specifications

As the product grows, detailed screen behavior should be split into separate files rather than making this document indefinitely larger.

Recommended structure:

```text
docs/Product/UI/UI_SPEC.md
docs/Product/Screens/MainLayout.md
docs/Product/Screens/HomePage.md
docs/Product/Screens/ListingsPage.md
docs/Product/Screens/Filters.md
docs/Product/Screens/ListingDetailsPage.md
docs/Product/Screens/SellVehicle.md        # when feature enters scope
docs/Product/Features/*.md
```

This file remains the **global design system and cross-screen UX contract**.

Screen files define exact content and behavior for one screen.

Feature files define behavior spanning multiple screens or backend/UI boundaries.

---

## 21. Approved Design Summary

The approved LacunaAuto direction is:

- mobile-first;
- large vehicle photography;
- white / soft-gray surfaces;
- black typography;
- orange-red accent;
- rounded cards and controls;
- prominent search and filters;
- quick vehicle category chips;
- photo-first discovery feed;
- efficient search/list view;
- large detail gallery;
- simple quick facts;
- strong seller-contact CTA;
- floating central Sell action when listing creation is in scope;
- responsive reuse between Blazor WebAssembly/PWA and .NET MAUI Blazor Hybrid;
- desktop UI derived from the same design system rather than designed independently.

This direction should be treated as the baseline for future LacunaAuto screen specifications and implementation.
