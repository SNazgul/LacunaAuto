# LacunaAuto Roadmap

## Goal

Deliver a working web classifieds service where application users register with email, verify their accounts through Telegram or Viber during registration, and create and manage vehicle listings with photos. Visitors can browse, filter, and view details; signed-in users can contact sellers through private listing chats with photo attachments.

[PRODUCT_SCOPE.md](PRODUCT_SCOPE.md) defines product scope. [UI_SPEC.md](UI/UI_SPEC.md) defines the UI direction. This roadmap defines the delivery sequence and can be expanded as decisions are made.

All production listings are added by application users. Mock/seed data supports development; the browsing prototype is an intermediate milestone toward the complete service.

## Main Milestones

- [ ] 1. Implement the Web UI.
- [ ] 2. Implement the domain model and database structure.
- [ ] 3. Implement email registration and account verification through Telegram or Viber.
- [ ] 4. Choose and implement storage for listing photos and private chat photos.
- [ ] 5. Implement the listing-management and browsing API.
- [ ] 6. Implement private listing chats with photos and last-message editing.
- [ ] 7. Connect the Web UI, API, database, and external services.
- [ ] 8. Validate and stabilize the complete product.
- [ ] 9. Deploy the service and onboard initial users.

These checkboxes track verified outcomes. Existing implementation has not yet been assessed against every milestone; an unchecked item does not mean all of its code is missing.

## 1. Implement the Web UI

Complete the main layout, Home, Listings, and Listing Details screens, then the registration/sign-in/verification flow, basic account area, My Listings, listing creation/editing with photo management, and private listing chats. Include conversation access, text/photo sending, and editing of an eligible last message. Use mock/seed data initially. Include responsive layouts, agreed filters, seller contacts, and loading, empty, validation, and error states.

**Done when:** the buyer and seller flows can be exercised on mobile and desktop, with any simulated behavior clearly identified until integration is complete.

Reuse existing pages and components. Integrate one working flow at a time instead of waiting for every screen to be polished.

## 2. Implement the Domain Model and Database Structure

Use PostgreSQL with EF Core configuration and migrations. Define the Core models and Data persistence for users, email identity, account-verification state/channel, listing ownership, publication state, vehicle attributes, price/currency, contact information, and image metadata/external storage references. Include listing conversations, their two participants, ordered messages, edit timestamps, and private attachment references. Include relationships, constraints, query indexes, and development seed data.

**Done when:** a fresh database can be created reproducibly and persists users, verification state, listings, conversations/messages, and photo references with the relationships and constraints needed by the application.

Coordinate image metadata with milestone 4. All listing photos, chat attachments, thumbnails, and generated image variants are stored outside PostgreSQL in the chosen storage service. PostgreSQL contains structured data and text plus metadata and external references such as storage keys; it must not contain image bytes, BLOBs, or Base64-encoded images. Evaluate managed object storage or already-included server disk within the initial budget.

## 3. Implement Email Registration and Account Verification

Keep email-based registration, sign-in, and account recovery. Add a Telegram or Viber verification step during registration, linked to the same account. Define email confirmation separately: receiving a messenger code does not confirm ownership of the supplied email address.

Evaluate the verification providers and implement the supported channel(s), verification expiry, retry limits, failure handling, and account activation rules. Decide the behavior for users without a supported messenger and for provider outages before implementation; do not silently bypass verification.

**Done when:** a user can register with email, complete the agreed messenger-verification flow, sign in, and recover access; account permissions follow the defined verification state.

Provider research starting points: [Telegram Gateway](https://core.telegram.org/gateway) delivers verification codes to a voluntarily supplied Telegram-linked phone number; [Viber Business Messages](https://www.forbusiness.viber.com/en/business-messages/) supports registration OTP messages. Confirm onboarding, recipient coverage, costs, and whether both channels are available at launch. Provider selection and first-release channel coverage remain open decisions.

## 4. Choose and Implement Listing and Chat Photo Storage

Plan for tens of thousands of listing photos, private chat photos, image variants, and future growth.

**Budget constraint:** target no additional recurring storage cost for the initial pilot. Evaluate small legitimate free allowances, compression, quotas, and any disk already included in hosting. Record storage, delivery, request, processing, and backup costs separately; do not assume free storage means free operation at unlimited scale.

**Storage boundary:** every original image and derived image file is external to the application database. The database stores data, text, metadata, and external object references only. Enforce this boundary for listing uploads, chat attachments, thumbnails, and future generated variants.

**Recommended approach, provider pending:** evaluate one Backblaze B2 account for a limited pilot, Cloudflare R2 for image-delivery needs, or existing persistent server disk when available. Store metadata/references in PostgreSQL. Published listing images may use public delivery/caching; drafts and chat photos require protected access. The comparison of these choices, many free accounts, Telegram storage, and low fixed-price plans is in [PhotoStorage.md](Features/PhotoStorage.md).

Before implementing uploads, measure compressed-photo sizes and record the selected provider/region, verified cost controls, global/per-user quotas, photos-per-listing and per-message limits, upload-size limits, and original-image retention policy. Include thumbnail/detail-image generation, upload validation, failed-upload cleanup, deletion/cache invalidation, and backup/recovery. Define how new uploads and image delivery behave when free limits are reached, without deleting existing content or silently enabling paid usage.

**Done when:** the storage decision covers budget/capacity limits, public listing images, and private chat attachments; users can manage their own listing photos, and only conversation participants can upload/retrieve chat photos. Draft access, failed uploads, quota exhaustion, lifecycle cleanup, expected-volume behavior, and restore have been verified.

## 5. Implement the Listing-Management and Browsing API

Implement creation, editing, publication, unpublishing, and deletion for a user's own listings and photo associations. Provide public listing search/list and details endpoints with the agreed filters, pagination, and sorting. Public queries must expose only published listings. Include input validation, ownership checks, consistent errors, and logging according to repository rules.

**Done when:** users can manage only their own listings, and visitors can query published listings with usable seller contact information and image references.

## 6. Implement Private Listing Chats

Provide a private conversation between the listing owner and an interested user for each listing/participant pair. Support text, photo attachments, and reopening conversation history. Only the sender of the newest successfully sent message can edit that message; a subsequent text or photo message locks it. Enforce participant access and the edit rule on the server, including concurrent replies and edits.

**Done when:** two users can exchange text/photos about a listing, other users cannot access their conversation or attachments, and last-message editing passes the scenarios in [ListingChat.md](Features/ListingChat.md).

## 7. Connect the Web UI, API, Database, and External Services

Replace simulated behavior with API calls and connect registration verification, photo storage, listing management, public browsing, and private chats. Handle authentication, verification, upload, message send/edit, empty-result, unavailable-listing, and service-failure states consistently.

**Done when:** a user can register with email, complete verification, create a listing with photos, publish it, and manage it later; another signed-in user can find the listing and exchange private messages/photos with its seller, including an eligible edit before a reply.

## 8. Validate and Stabilize the Complete Product

Verify the complete buyer and seller journeys, ownership boundaries, verification failures, photo lifecycle, chat/attachment privacy, last-message editing races, and important error cases. Add focused automated coverage, check mobile/desktop usability and accessibility, and verify performance at the agreed listing/photo/message volume. Check configuration, operational logging, and database/photo recovery.

**Done when:** the agreed release scenarios and required restore/build/test checks pass, release-blocking defects are resolved, and the minimum support/moderation process for user-submitted listings has been decided.

Validation accompanies every milestone; this phase verifies the assembled product before release.

## 9. Deploy the Service and Onboard Initial Users

Deploy the Web application, API, database, public/private photo delivery, registration-verification integrations, and any services required by the chosen chat-delivery design. Configure HTTPS, secrets, migrations, logs, service/cost monitoring, backups, and recovery procedures. Verify the PWA experience if included in the release. Invite initial sellers to create real listings and buyers to try the service.

**Done when:** users can complete the seller-to-buyer journey in the deployed service, deployment and recovery procedures are checked, and feedback is recorded for the next iteration.

## Future Product Expansion

After the first usable service, select the next capabilities based on feedback and update product scope before implementation. These are candidates, not an approved delivery order:

- Favorites and saved listings.
- Dealer/public profiles and advanced moderation/administration tooling.
- Blazor Hybrid mobile and desktop applications.
- General notifications and paid features when justified.

## Decisions to Resolve as the Roadmap Is Expanded

- Which filters and sorting options are required for the first release?
- Which storage provider or included server disk, region, verified free-usage controls, upload limits, backup plan, and retention rules meet the initial budget? See [PhotoStorage.md](Features/PhotoStorage.md).
- What chat-photo limits, attachment-editing rules, retention behavior, and message-delivery mechanism will be used? See [ListingChat.md](Features/ListingChat.md).
- Will both Telegram and Viber be available at launch, and what are the email-confirmation, activation, fallback, and recovery rules?
- What minimum support/moderation process is needed for user-submitted listings before public launch?
- What is the release requirement for PWA installation and offline behavior?
- Reconcile localization scope: `PRODUCT_SCOPE.md` lists multi-language UI outside the initial MVP, while `UI_SPEC.md` section 18 requires localization infrastructure and language, regional-format, and currency preferences. Record the agreed release scope before expanding those tasks.

## How to Maintain This Roadmap

- Keep the main milestones broad; add detailed task checklists under a milestone when that work is ready to be planned.
- Review and reuse existing implementation before creating new tasks.
- For each detailed task, state the expected result, dependencies, and how completion will be checked.
- Mark a milestone complete only after its outcome has been verified.
- Keep detailed screen, feature, and domain specifications in their respective product documents and link them here as they are created.
- Update this roadmap when priorities change; update `PRODUCT_SCOPE.md` first when feature scope changes.
