# LacunaAuto Roadmap

## Goal

Deliver a working web classifieds service where application users register with email, verify their accounts through Telegram or Viber during registration, and create and manage vehicle listings with photos. Visitors can browse, filter, and view details; signed-in users can contact sellers through private listing chats with photo attachments.

[PRODUCT_SCOPE.md](PRODUCT_SCOPE.md) defines product scope and MVP boundaries. [UI_SPEC.md](UI/UI_SPEC.md) defines the UI direction. [CURRENT_WORK.md](../Development/CURRENT_WORK.md) tracks the active implementation slice, verified progress, known issues, and open implementation questions.

All production listings are added by application users. Mock/seed data supports development; the browsing prototype is an intermediate milestone toward the complete service.

## Delivery Principle

Keep the product runnable and usable after every accepted implementation slice, even when the available feature set is still small.

Prefer small vertical slices that connect the UI, API, persistence, and external services as soon as those layers are introduced. Do not build large disconnected layers and postpone integration to a later milestone unless there is a specific reason to do so.

For each slice:

1. define the smallest useful outcome;
2. implement only what that outcome needs;
3. connect the layers immediately;
4. validate the result, including visual validation when UI is involved;
5. record verified progress in `CURRENT_WORK.md`;
6. only then expand to the next slice.

Automated tests should be added alongside the behavior they protect. The final stabilization milestone closes remaining coverage, integration, regression, performance, and operational gaps rather than postponing all testing until the end.

## Main Milestones

- [ ] 1. Complete the browsing UI foundation.
- [ ] 2. Deliver the listings browsing vertical slice.
- [ ] 3. Deliver the authentication and basic account vertical slice.
- [ ] 4. Finalize the Backblaze B2 photo-storage design and operational limits.
- [ ] 5. Deliver the listing-management vertical slice with photos.
- [ ] 6. Deliver the private listing-chat vertical slice with photos.
- [ ] 7. Validate and stabilize the complete product.
- [ ] 8. Deploy the service and onboard initial users.

Roadmap checkboxes represent verified milestone outcomes, not individual commits or partially implemented code. Detailed active status belongs in `CURRENT_WORK.md`.

## 1. Complete the Browsing UI Foundation

Complete and visually validate the current browsing experience using development seed/mock data where necessary.

This milestone includes:

- application shell, routing, and navigation;
- Home;
- Listings;
- Listing Details;
- reusable listing UI components and shared design tokens;
- responsive mobile and desktop behavior;
- the current localization/preferences foundation;
- loading, empty, and error-state patterns needed by the browsing flow;
- PWA installability for the Web client.

Do not expand this milestone into registration, listing creation, account management, photo upload, or chat screens. Those UI flows belong to the vertical slices that introduce their real behavior.

**Done when:** Home, Listings, and Listing Details can be exercised on representative mobile and desktop widths, the current localization/preferences behavior has been verified, PWA installation works, and the browsing prototype is visually accepted as a stable foundation for the first real backend slice.

Reuse existing pages and components. Do not rewrite accepted UI merely because the backend is still simulated.

## 2. Deliver the Listings Browsing Vertical Slice

Replace the seed-only browsing path with the smallest real end-to-end listings implementation.

Define only the listing/domain data needed by the current browsing UI. Add the required Core models, PostgreSQL/EF Core persistence, migrations, development data, and minimal public API endpoints for listing search/list and details. Public queries must expose only published listings.

Connect the existing Home, Listings, and Listing Details UI to the real API as part of this milestone rather than deferring integration. Introduce the first agreed filters, pagination, and sorting only as needed for the usable browsing slice.

Keep listing prices in their original currency until real conversion is explicitly implemented. Image records may contain metadata/external references, but image file storage itself is handled by later photo-storage/listing-management work.

Include input validation, consistent errors, and logging according to repository rules. Add focused tests for the introduced domain, persistence, and API behavior.

**Done when:** a fresh development database can be created reproducibly, published listings can be queried through the API, and the existing Home / Listings / Listing Details flow works against real persisted data without relying on seed-only client data.

## 3. Deliver the Authentication and Basic Account Vertical Slice

Implement email-based registration, sign-in, sign-out, account recovery, and the minimum account area required by the MVP. Add the required user/account persistence, API behavior, authentication/authorization, and corresponding Web UI in the same slice.

Add the Telegram or Viber verification step during registration, linked to the same account. Messenger verification does not prove ownership of the supplied email address; email confirmation behavior must be defined separately.

Before implementation, resolve supported verification provider(s), expiry, retry limits, failure handling, account activation rules, provider outage behavior, and fallback/recovery behavior. Do not silently bypass required verification.

Provider research starting points:

- [Telegram Gateway](https://core.telegram.org/gateway)
- [Viber Business Messages](https://www.forbusiness.viber.com/en/business-messages/)

Confirm onboarding, recipient coverage, costs, and first-release channel coverage before relying on a provider.

Add focused tests for account state, authentication boundaries, verification state transitions, and authorization rules.

**Done when:** a user can register with email, complete the agreed messenger-verification flow, sign in, sign out, recover access, and reach the basic account area; permissions follow the authoritative account/verification state.

## 4. Finalize the Backblaze B2 Photo-Storage Design and Operational Limits

The primary data store is PostgreSQL on the application/server infrastructure. All listing and chat image files will be stored separately in Backblaze B2; PostgreSQL stores only structured data, image metadata, and external object references.

Plan for tens of thousands of listing photos, private chat photos, derived variants, and future growth.

**Budget constraint:** target no additional recurring storage cost for the initial pilot where practical. Evaluate legitimate free allowances, compression, quotas, and any persistent disk already included in hosting. Record storage, delivery, request, processing, and backup costs separately.

**Storage boundary:** every original image and derived image file is external to PostgreSQL. The database stores structured data, text, image metadata, and external object references only. Do not store image bytes, BLOBs, or Base64-encoded image content in database records.

Backblaze B2 is the selected image-storage provider. The current pricing assumption is the first 10 GB free and then $6.95/TB/month; download/egress limits and costs must be accounted for before launch. The earlier provider comparison and implementation notes remain in [PhotoStorage.md](Features/PhotoStorage.md).

Before this milestone is complete, decide and record:

- Backblaze B2 account/bucket layout and selected region;
- public-listing versus private-chat delivery model;
- cost controls and quota behavior;
- global, per-user, per-listing, and per-message limits;
- upload-size and accepted-format limits;
- image dimensions/quality and generated variants;
- original-file retention;
- failed-upload cleanup and lifecycle reconciliation;
- backup/restore approach;
- behavior when free/capacity limits are reached;
- abstraction/migration boundary for moving to another provider later.

This milestone chooses and validates the architecture. Actual user-facing listing-photo upload and private chat-photo upload are implemented in milestones 5 and 6 so they can be integrated with real ownership and authorization.

**Done when:** the Backblaze B2 bucket/access model, quotas, cost controls, processing rules, cleanup, backup/restore, and migration boundary are documented well enough to implement listing and chat uploads without redesigning the persistence boundary.

## 5. Deliver the Listing-Management Vertical Slice with Photos

Implement the complete seller flow for a user's own listings.

In the same vertical slice, add:

- My Listings;
- listing creation;
- listing editing;
- publication and unpublishing;
- deletion;
- seller contact information;
- listing photo upload, ordering, replacement/removal, and display;
- ownership and authorization checks;
- required Core/Data/API changes;
- the corresponding Web UI;
- integration with the selected photo-storage design from milestone 4.

Only the owning user may manage a listing or its listing photos. Public browsing must continue to expose only published listings. Failed uploads, validation failures, storage quota exhaustion, and partial operations must leave the listing in an accurate recoverable state.

Add focused tests for ownership boundaries, state transitions, validation, photo metadata/storage coordination, and public/private visibility.

**Done when:** a signed-in verified user can create a listing with photos, publish it, see it through the real public browsing flow, later edit/unpublish/delete it, and cannot manage another user's listing or photos.

## 6. Deliver the Private Listing-Chat Vertical Slice with Photos

Implement a private conversation between the listing owner and an interested signed-in user for each listing/participant pair.

Deliver the full slice together:

- conversation persistence and participant relationships;
- ordered message history;
- text messages;
- private photo attachments using the milestone 4 storage design;
- conversation access from the listing/account UI;
- API authorization for every conversation/message/attachment operation;
- message send/upload progress and recoverable failure states;
- the last-message editing rule defined in [ListingChat.md](Features/ListingChat.md);
- the corresponding Web UI and API integration.

Only the sender of the newest successfully sent message may edit that message. A subsequent successfully accepted text or photo message locks the earlier message. Enforce the rule authoritatively and atomically on the server, including stale clients and concurrent replies/edits.

Add focused tests for participant privacy, direct attachment access, conversation separation, message ordering, edit races, and retry/duplicate behavior.

**Done when:** two authorized users can open a conversation from a listing, exchange text/photos, reopen history, and use the allowed last-message edit behavior; a third user cannot read or modify the conversation or retrieve its private attachments.

## 7. Validate and Stabilize the Complete Product

By this point the main product flows should already be integrated because each earlier milestone delivered a runnable vertical slice.

Validate the assembled buyer and seller journeys end to end:

- browse/search/details;
- registration, verification, sign-in, and recovery;
- listing creation/management and photo lifecycle;
- publication visibility and ownership boundaries;
- private chat and attachment privacy;
- last-message editing races;
- important validation and service-failure cases;
- responsive mobile/desktop usability and accessibility;
- PWA installation;
- performance at the agreed listing/photo/message volume;
- configuration, operational logging, secrets, and recovery procedures.

Automated tests should already exist for introduced behavior. Add missing regression/integration coverage here rather than treating this milestone as the first testing phase.

Offline behavior beyond normal browser/PWA caching remains an explicit release decision; PWA installability itself is part of the Web product direction.

**Done when:** agreed release scenarios, required build/test checks, recovery checks, performance expectations, and operational checks pass; release-blocking defects are resolved; and the minimum support/moderation process for user-submitted listings has been decided.

## 8. Deploy the Service and Onboard Initial Users

Deploy the Web application, API, PostgreSQL database, public/private photo delivery, registration-verification integrations, and any services required by the selected chat-delivery design.

Configure:

- HTTPS;
- secrets;
- migrations;
- production logging/observability;
- service and cost monitoring;
- backups and restore procedures;
- storage quota/capacity monitoring;
- PWA installability in the deployed environment.

Invite initial sellers to create real listings and buyers to try the service. Record operational issues and user feedback for the next iteration.

**Done when:** users can complete the seller-to-buyer journey in the deployed service, deployment and recovery procedures have been verified, and initial feedback is captured for the next roadmap update.

## Future Product Expansion

After the first usable service, select the next capabilities based on feedback and update product scope before implementation. These are candidates, not an approved delivery order:

- Favorites and saved listings.
- Dealer/public profiles and advanced moderation/administration tooling.
- Blazor Hybrid mobile and desktop applications.
- General notifications and paid features when justified.

## Decisions to Resolve as the Roadmap Is Expanded

- Which filters and sorting options are required for the first real listings slice and the first public release?
- Which Backblaze B2 region/bucket layout, verified free-usage controls, upload limits, backup plan, and retention rules meet the initial budget? See [PhotoStorage.md](Features/PhotoStorage.md).
- What chat-photo limits, attachment-editing rules, retention behavior, and message-delivery mechanism will be used? See [ListingChat.md](Features/ListingChat.md).
- Will both Telegram and Viber be available at launch, and what are the email-confirmation, activation, fallback, and recovery rules?
- What minimum support/moderation process is needed for user-submitted listings before public launch?
- What offline behavior, if any, is required beyond normal PWA/browser caching?
- Reconcile localization scope: `PRODUCT_SCOPE.md` lists multi-language UI outside the initial MVP, while `UI_SPEC.md` section 18 and the current UI foundation include localization infrastructure and independent language, regional-format, and currency preferences. Record the agreed release scope before expanding localization work.

## How to Maintain This Roadmap

- Keep the main milestones outcome-oriented and broad enough to survive implementation details.
- Prefer small vertical slices inside a milestone; the application should remain runnable and usable after each accepted slice.
- Track active tasks, verified implementation progress, known issues, and immediate next work in [CURRENT_WORK.md](../Development/CURRENT_WORK.md), not by turning this roadmap into a commit log.
- Review and reuse existing implementation before creating new tasks.
- For each detailed task, state the expected result, dependencies, and how completion will be checked.
- Integrate new UI, API, persistence, and external-service behavior as soon as the slice introduces them; do not create a separate late "connect everything" phase.
- Add focused automated tests alongside the behavior they protect.
- Mark a milestone complete only after its `Done when` outcome has been verified.
- Keep detailed screen, feature, and domain specifications in their respective product documents and link them here as they are created.
- Update this roadmap when priorities change; update `PRODUCT_SCOPE.md` first when feature scope changes.
