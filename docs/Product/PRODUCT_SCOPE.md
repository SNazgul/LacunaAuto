# PRODUCT_SCOPE.md — LacunaAuto Product Scope

> Canonical product document for AI agents and developers.  
> This file describes what LacunaAuto is as a product. It does **not** describe repository structure, coding style, logging rules, or technical agent behavior. Those rules belong in `AGENTS.md` and `docs/AI/Rules/*.md`.

---

## 1. Product Vision

LacunaAuto is a modern vehicle classifieds and search platform.

The product should allow visitors to browse, search, filter, and view vehicle advertisements in a clean web interface. All production listings are created by application users, who register with email and create and manage their own listings with photos and contact information. Account verification through Telegram or Viber is part of the registration flow.

The first usable service must support the complete seller-to-buyer journey: register, verify an account, publish a vehicle listing with photos, find that listing, and contact its seller, including through a private listing chat. A browsing prototype with mock data remains an intermediate development milestone. Favorites, dealer profiles, advanced moderation, mobile applications, and additional services may follow later.

---

## 2. Product Type

LacunaAuto is a vehicle classifieds platform with listings submitted by application users. External aggregation/import is not the initial source of production listings.

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

### 3.2 Private Sellers — Initial MVP

Users who want to create and manage their own vehicle listings.

### 3.3 Dealers — Future Scope

Professional sellers who may manage multiple listings and have public dealer profiles.

### 3.4 Administrators / Moderators — Future Scope

Internal users who review listings, handle reports, and manage platform quality.

---

## 4. Initial MVP Scope

The initial MVP is a working classifieds service with user-created listings and public browsing.

A user should be able to:

1. Open the application home page.
2. See a clear entry point to vehicle search.
3. Open a vehicle listings page.
4. Browse a list/grid of vehicle listings.
5. See basic vehicle information in listing cards.
6. Filter listings by the most important criteria.
7. Open a listing details page.
8. View detailed information about a selected vehicle.
9. See usable seller contact information.
10. Register with an email address, sign in, and recover account access.
11. Verify the account during registration through Telegram or Viber using the supported verification flow.
12. Create, edit, publish, unpublish, and delete their own listings.
13. Upload and manage listing photos.
14. Exchange private text messages and photos with another user in a conversation tied to a specific listing.
15. Edit their own message only while it is the newest message in that conversation; a message from the other participant ends that editing opportunity.

Seed/mock listings and placeholder contacts are acceptable during development. Production listings must be created and maintained by application users, persisted through the API and database, and include usable seller contact information.

Email remains the registration identity. Messenger verification must be linked to that same account; it does not replace email registration or automatically establish email ownership. Provider coverage, email-confirmation behavior, account activation rules, and fallback/recovery behavior must be specified before implementation.

Photo storage and delivery must support tens of thousands of listing photos. Select the storage architecture and provider before implementing uploads, including image processing, access rules, removal, retention, and operating costs. Provider choices belong in the roadmap and technical specifications.

The initial photo-storage plan must target no additional recurring storage cost for a limited pilot. Define capacity and upload/delivery limits explicitly; a free allowance is not a promise of unlimited service. Compare compression, provider free allowances, and any storage included in hosting before considering a paid plan. See [PhotoStorage.md](Features/PhotoStorage.md) for the proposed options and budget controls.

All listing photos, chat attachments, thumbnails, and other generated image variants must be stored outside the application database. PostgreSQL stores structured data and text only, plus image metadata and external storage references such as an object key, provider identifier, content type, dimensions, size, checksum, and access state. Do not store image bytes, binary large objects, or Base64-encoded images in database columns.

Listing chats are private conversations between the listing owner and an interested user. Each listing and participant pair has its own conversation; messages and attached photos are accessible only to those participants through application features. Chat photos require private storage and delivery. Detailed behavior is defined in [ListingChat.md](Features/ListingChat.md).

---

## 5. Out of Initial MVP Scope

The following features are intentionally outside the first MVP unless explicitly moved into scope:

- favorites / saved listings;
- public user profile pages beyond the basic account and own-listings area;
- dealer profile pages;
- payments or promoted listings;
- advanced moderation;
- admin panel;
- general notifications beyond registration and account recovery;
- multi-language UI;
- full native mobile application;
- complex SEO and public indexing strategy;
- vehicle history integration;
- financing, insurance, or leasing integrations.

Authentication, basic account navigation, listing management, photo upload, registration verification, and private listing chats are now in scope. The minimum moderation/support process for user-submitted content must be decided before public launch; a full admin panel remains outside the initial MVP.

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

The first usable service must show contact information supplied by the seller and provide a private listing-chat entry point. Placeholder contacts are limited to development/demo data. Sending messages requires a signed-in account.

### 6.5 Register and Verify an Account

As a seller, I want to register with email and verify my account during registration through Telegram or Viber so that I can use the application's account and listing features. Verification state belongs to the registered account and must be handled separately from email ownership.

### 6.6 Create and Manage Listings

As a seller, I want to enter vehicle details, upload photos, publish a listing, and later edit, unpublish, or delete it. Only the owning user may manage the listing and its photos through seller features.

### 6.7 Discuss a Listing Privately

As a signed-in user, I want to exchange text and photos with the listing owner in a private conversation about that listing. Both participants can edit their own newest message only while it remains the last message in the conversation. A sent reply locks the earlier message; merely typing does not. See [ListingChat.md](Features/ListingChat.md) for examples and acceptance criteria.

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
   - shows usable seller/contact information;
   - allows returning to listings.

4. **Main Layout / Navigation**
   - provides stable navigation structure;
   - should stay simple in the first MVP;
   - includes sign-in, account, and listing-creation entry points.

5. **Registration / Sign-in / Verification / Recovery**
   - keeps email registration;
   - supports Telegram or Viber account verification during registration;
   - explains pending, failed, expired, and completed verification states.

6. **My Listings / Create and Edit Listing**
   - allows users to manage their own listings and photos;
   - supports preview, publication, and removal from public browsing.

7. **Conversations / Listing Chat**
   - allows participants to reopen their listing conversations;
   - shows the relevant listing, messages, and private photo attachments;
   - offers editing only for the current user's editable last message.

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
- vehicle photos and their storage references;
- user account and email identity;
- registration verification state and channel;
- listing ownership and publication state;
- listing conversation and its two participants;
- message sender, content, ordering, and edit timestamp;
- private chat-photo references and their message association.

For the first implementation, the model should remain simple and should not over-engineer future use cases.

---

## 9. Product Modules

### 9.1 Browse Listings — Initial MVP

Core browsing, list/grid presentation, basic filtering, and listing details.

### 9.2 Listing Management and Photos — Initial MVP

Creating, editing, publishing, unpublishing, and deleting user-owned listings, including photo upload and management.

### 9.3 Authentication and Accounts — Initial MVP

Email registration, login, recovery, basic account management, ownership-based access control, and Telegram or Viber account verification during registration.

### 9.4 Favorites — Future Scope

Saving listings for later comparison.

### 9.5 Dealer Profiles — Future Scope

Dealer pages, dealer listing management, and dealer metadata.

### 9.6 Moderation / Administration — Future Scope

Reviewing, approving, rejecting, hiding, or reporting listings.

### 9.7 Mobile / Hybrid Application — Future Scope

Blazor Hybrid / MAUI application using shared UI components where possible.

### 9.8 Private Listing Chat — Initial MVP

Private conversations between two users about a listing, with text, photo attachments, and editing restricted to the sender's message while it remains the newest message in the conversation. See [ListingChat.md](Features/ListingChat.md).

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

- All production listings will be added and maintained by application users.
- Email registration remains; Telegram or Viber verifies the account during registration.
- The first usable service includes accounts, listing management, photo uploads, browsing, usable seller contacts, and private listing chats with photo attachments.
- A message may be edited only by its sender while it is the newest message in that conversation. A reply ends that editing opportunity.
- Build the basic UI with mock/seed data if needed, then connect it to the API and database through complete user journeys.
- Decide how to store and deliver tens of thousands of listing photos and private chat attachments before implementing the upload workflows.
- Target no additional recurring photo-storage cost initially, with explicit capacity limits and a plan for growth beyond free allowances.
- Store all image files externally; keep only structured data, text, image metadata, and external storage references in PostgreSQL.
- Keep favorites, payments, dealer profiles, full admin tooling, and native applications outside the first MVP unless separately moved into scope.
