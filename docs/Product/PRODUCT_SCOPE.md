# PRODUCT_SCOPE.md — LacunaAuto Product Scope

> Canonical product document for AI agents and developers.  
> This file describes what LacunaAuto is as a product. It does **not** describe repository structure, coding style, logging rules, or technical agent behavior. Those rules belong in `AGENTS.md` and `docs/AI/Rules/*.md`.

---

## 1. Product Vision

LacunaAuto is a modern vehicle classifieds and search platform.

The product should allow users to browse, search, filter, and view vehicle advertisements in a clean web interface. Over time, the platform may support posting listings, user accounts, favorites, dealer profiles, moderation, mobile applications, and additional vehicle-related services.

The first goal is to build a small but real working product core: a user can open the application, find relevant vehicles, and view vehicle listing details.

---

## 2. Product Type

LacunaAuto is a vehicle advertisement aggregator / classifieds platform.

The product is not initially intended to be:

- a full CRM for car dealers;
- a payment platform;
- a vehicle history provider;
- a financing or insurance marketplace;
- a chat-first marketplace;
- a complex auction platform.

These areas may be added later only when explicitly moved into scope.

---

## 3. Target Users

### 3.1 Vehicle Buyers

Users who want to search for vehicles, compare listings, filter results, and open listing details.

### 3.2 Private Sellers — Future Scope

Users who want to create and manage their own vehicle listings.

### 3.3 Dealers — Future Scope

Professional sellers who may manage multiple listings and have public dealer profiles.

### 3.4 Administrators / Moderators — Future Scope

Internal users who review listings, handle reports, and manage platform quality.

---

## 4. Initial MVP Scope

The initial MVP focuses on browsing and discovering vehicle listings.

A user should be able to:

1. Open the application home page.
2. See a clear entry point to vehicle search.
3. Open a vehicle listings page.
4. Browse a list/grid of vehicle listings.
5. See basic vehicle information in listing cards.
6. Filter listings by the most important criteria.
7. Open a listing details page.
8. View detailed information about a selected vehicle.
9. See seller contact information or a placeholder contact block.

The MVP may use seed/mock data at the beginning, but the implementation should evolve toward real backend data through API endpoints and database entities.

---

## 5. Out of Initial MVP Scope

The following features are intentionally outside the first MVP unless explicitly moved into scope:

- user registration and login;
- creating, editing, or deleting listings from the UI;
- photo upload;
- favorites / saved listings;
- user profile pages;
- dealer profile pages;
- messaging / chat;
- payments or promoted listings;
- advanced moderation;
- admin panel;
- notifications;
- multi-language UI;
- full native mobile application;
- complex SEO and public indexing strategy;
- vehicle history integration;
- financing, insurance, or leasing integrations.

Authentication may appear later as a navigation entry or placeholder only if a feature specification explicitly requires it.

---

## 6. Core User Scenarios

### 6.1 Browse Listings

As a vehicle buyer, I want to open the listings page and see available vehicle advertisements so that I can start comparing vehicles.

### 6.2 Filter Listings

As a vehicle buyer, I want to filter listings by common vehicle attributes so that I can find relevant vehicles faster.

Initial filters may include:

- make;
- model;
- price range;
- year range;
- mileage range;
- fuel type;
- transmission;
- body type;
- location.

Filters should be introduced gradually. The first implementation may contain only a subset.

### 6.3 View Listing Details

As a vehicle buyer, I want to open a listing and see detailed vehicle information so that I can decide whether the vehicle is interesting.

### 6.4 Contact Seller

As a vehicle buyer, I want to see seller contact information or a clear contact section so that I know how to proceed.

In the first MVP this may be a simple placeholder or static contact data.

---

## 7. Initial Product Screens

Detailed behavior of each screen should live in separate files under `docs/Product/Screens/` when needed.

Initial screens:

1. **Home Page**
   - introduces the product;
   - provides search/browse entry point;
   - may show featured or recent listings later.

2. **Listings Page**
   - displays vehicle listing cards;
   - supports basic filters;
   - handles loading, empty, and error states.

3. **Listing Details Page**
   - displays full vehicle details;
   - shows images or image placeholders;
   - shows seller/contact block;
   - allows returning to listings.

4. **Main Layout / Navigation**
   - provides stable navigation structure;
   - should stay simple in the first MVP;
   - login/account navigation is not part of the first MVP unless explicitly added.

---

## 8. Initial Product Data Concepts

Detailed domain modeling should live in `docs/Product/DomainModel.md` when needed.

The product will likely need the following concepts:

- vehicle listing;
- vehicle make;
- vehicle model;
- price;
- production year;
- mileage;
- fuel type;
- transmission type;
- body type;
- engine information;
- location;
- seller/contact information;
- vehicle photos.

For the first implementation, the model should remain simple and should not over-engineer future use cases.

---

## 9. Product Modules

### 9.1 Browse Listings — Initial MVP

Core browsing, list/grid presentation, basic filtering, and listing details.

### 9.2 Listing Management — Future Scope

Creating, editing, publishing, and deleting listings.

### 9.3 Authentication and Accounts — Future Scope

User login, registration, account management, seller identity, and access control.

### 9.4 Favorites — Future Scope

Saving listings for later comparison.

### 9.5 Dealer Profiles — Future Scope

Dealer pages, dealer listing management, and dealer metadata.

### 9.6 Moderation / Administration — Future Scope

Reviewing, approving, rejecting, hiding, or reporting listings.

### 9.7 Mobile / Hybrid Application — Future Scope

Blazor Hybrid / MAUI application using shared UI components where possible.

---

## 10. Product Behavior Principles

- Keep the first product experience simple and fast.
- Prefer a working vertical slice over a large unfinished architecture.
- Do not add product features just because the architecture supports them.
- Do not implement future modules unless they are explicitly moved into scope.
- UI should expose only behavior that is actually supported or clearly marked as placeholder.
- Every new feature should update the relevant product documentation before implementation.

---

## 11. Documentation Rules

This file is the top-level product scope document.

Use additional files for details:

```text
/docs/Product/PRODUCT_SCOPE.md           # Product-level scope and MVP boundaries
/docs/Product/DomainModel.md             # Product/domain concepts and entities
/docs/Product/Screens/*.md               # Screen-level behavior
/docs/Product/Features/*.md              # Feature-level behavior
/docs/Product/Roadmap.md                 # Optional future planning
```

Do not put detailed UI behavior, API contracts, database schema, or implementation details into this file unless they define product scope.

---

## 12. Change Management

When adding a new feature:

1. Decide whether it changes product scope.
2. If yes, update this file.
3. Add or update a feature specification under `docs/Product/Features/`.
4. Add or update screen specifications under `docs/Product/Screens/` if UI behavior changes.
5. Only then implement the feature.

AI agents should not silently expand product scope while implementing code.

---

## 13. Current Product Decision Snapshot

Current agreed direction:

- Start with browsing vehicle listings.
- Build a basic UI first using mock/seed data if needed.
- Then add a minimal backend vertical slice for listings.
- Then connect the Blazor UI to the real API.
- Keep authentication, listing creation, favorites, chat, payments, and admin features out of the first MVP.
