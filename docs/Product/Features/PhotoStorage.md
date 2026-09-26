# Listing and Chat Photo Storage

Status: proposed storage approach; no provider selected or provisioned.

Pricing and provider documentation checked on 2026-09-24. Recheck before choosing a service. This document expands milestone 4 in [Roadmap.md](../Roadmap.md) and the private-attachment requirements in [ListingChat.md](ListingChat.md).

## Budget and Requirements

- Target no additional recurring storage cost for the initial pilot. Very low fixed-price alternatives may be considered; a paid provider commitment has not been made.
- Support public listing photos and private chat photos. Store every original and derived image file outside PostgreSQL.
- Keep only structured data, text, image metadata, and external storage references in PostgreSQL. Image metadata may include the provider, bucket/container, object key, content type, byte size, dimensions, checksum, ownership, ordering, processing state, and access state.
- Never store image bytes, BLOBs, or Base64-encoded images in database columns. Temporary upload buffering also belongs outside the database.
- Size the pilot using measured compressed-image sizes, photo counts, delivery traffic, and backups. Tens of thousands of photos do not automatically require 1 TB.
- Define a capacity limit and behavior when it is reached. A zero-budget pilot cannot promise unlimited uploads or unlimited traffic.
- Preserve a migration path to another provider as the service grows.

## Options Considered

### Many Free Storage Accounts

Not recommended for the initial architecture. Distributing files across many accounts adds credential management, quota tracking, account recovery, broken-link handling, migration, and deletion work. Capacity split across accounts is not a backup.

Provider rules differ; permission to create multiple accounts does not establish permission to pool free allowances for one application. Check the specific provider's rules before considering this approach. No universal prohibition is assumed, but a large account-orchestration system is poor value for this MVP.

### Telegram Account or Bot as Storage

Not recommended as the production photo store. A personal account archive is a different service model from application object storage, and this review has not established suitable durability, delivery, or recovery guarantees for it.

For a bot-based design specifically, [Telegram's Bot Developer Terms](https://telegram.org/tos/bot-developers) restrict external uses that significantly diverge from the platform's purpose, explicitly including cloud-storage sites. This restriction must not be generalized into a claim about every personal-account API use, but it makes the proposed bot-backed storage approach unsuitable to assume is permitted.

The hosted [Bot API getFile method](https://core.telegram.org/bots/api#getfile) currently limits downloads to 20 MB and returns temporary download links containing the bot token. Those links cannot be exposed as public image URLs. A proxy/cache and protected credentials would still be needed. Telegram registration verification remains a separate planned integration.

### Application Storage Providers

| Option | Published pricing / allowance | Implication for LacunaAuto |
|---|---|---|
| [Backblaze B2](https://www.backblaze.com/cloud-storage/pricing) | First 10 GB free; then $6.95/TB/month. Direct egress is free up to 3 times average monthly stored data, then $0.01/GB; listed delivery partners have separate free-egress arrangements. | Candidate for a tightly limited pilot; account for reads/delivery as well as stored bytes. |
| [Cloudflare R2 Standard](https://developers.cloudflare.com/r2/pricing/) | 10 GB-month, 1 million Class A requests, and 10 million Class B requests free monthly. Additional storage is $0.015/GB-month; direct egress is free. | Candidate for image delivery, but storage and request overages are billable; processing and other services are separate. |
| [IDrive e2 Standard](https://www.idrive.com/s3-storage-e2/pricing) | 1 TB for $29.75 for the first year, paid yearly; listed standard renewal $59.50/year. Monthly pay-as-you-go has a $6 minimum for 1 TB. | A low fixed-price alternative if money becomes available. The annual promotion is about $2.48/month equivalent, not a monthly payment or a permanent price. Free egress has a 3-times-storage condition. |
| Existing server disk | No additional storage subscription if sufficient disk is already included in hosting. Hosting, transfer, and backup costs still apply. | Useful alternative if hosting includes spare persistent disk. It needs capacity monitoring, separate backups, and a later migration path. |

No ongoing $1/TB/month offer suitable for this live application was verified among the providers reviewed. Cheap archive or personal-backup capacity should not be treated as equivalent to frequently accessed application storage.

## Recommended Pilot Approach

1. Evaluate one Backblaze B2 account for a limited pilot first; keep R2 as the delivery-oriented alternative. [B2 signup](https://www.backblaze.com/sign-up/cloud-storage) does not require a credit card. Its [bucket documentation](https://www.backblaze.com/docs/cloud-storage-create-and-manage-buckets) requires payment history or a small credited payment before enabling public buckets. For a no-payment pilot, evaluate private buckets with images delivered through the application API, including the API's hosting/transfer cost.
2. Keep the database/storage boundary explicit: PostgreSQL stores the image record and external object key; the selected external store contains the actual file and all generated variants. Deleting or replacing an image must coordinate both records without placing file content in the database.
3. Keep public-listing and private-chat access separate. A listing-image endpoint may serve a published listing to visitors; chat-image delivery must authorize the requesting participant. Never make a chat bucket public.
4. Resize and compress uploads, generate only the image variants actually used, and remove temporary/failed uploads. Decide original-file retention explicitly; do not silently discard originals or existing chat attachments.
5. Use an application quota below the provider's free allowance, reserving capacity for in-progress uploads and variants. An initial 8 GB working limit within a 10 GB allowance is a proposal, not a measured capacity guarantee. Include all stored versions in accounting.
6. Stop accepting new photos before the quota is exhausted and show a clear message. Limit per-user upload usage and track delivery/request usage too. Reaching a limit must not delete existing content or silently start paid usage.
7. Configure and verify available provider caps before launch. [B2 caps](https://www.backblaze.com/docs/cloud-storage-create-and-manage-caps-and-alerts) cover daily charge categories and changes can take up to ten minutes; [Class D transactions cannot be capped](https://www.backblaze.com/docs/en/cloud-storage-data-caps-and-alerts). Disable unnecessary billable features. Alerts alone are not spending caps. [R2 signup](https://developers.cloudflare.com/r2/get-started/) uses a subscription checkout and usage billing; do not treat its free allowance as a guaranteed hard stop.
8. Choose and verify an independent backup/restore arrangement within the total budget. Define what happens at download limits; a strictly free pilot may have to temporarily refuse image delivery. Keep storage behind a small service interface so a later move does not change listing/chat behavior.

Provider selection depends on the hosting arrangement, acceptable signup/payment requirements, expected reads, and available backup capacity. Existing server disk can be preferable when those resources are already paid for. These are recommendations for evaluation, not deployed infrastructure.

## Illustrative Capacity Estimate

Assumption: one optimized image plus its thumbnail averages 300 KB combined, with no retained original. This is a sizing example to validate with representative photos, not a guaranteed image-quality or file-size target. Decimal units are used.

| Source photos, across listings and chats | Storage under this assumption |
|---|---:|
| 20,000 | 6 GB |
| 50,000 | 15 GB |
| 100,000 | 30 GB |

Extra variants, retained originals, temporary uploads, and backups increase these figures. For example, 10,000 listings with 8 photos each means 80,000 photos, or about 24 GB before chat photos and backups under the same assumption.

At 30 GB stored for a full month, R2 Standard storage alone would be about $0.30 after its 10 GB allowance. B2 storage alone would be about $0.14 after its 10 GB allowance at the listed rate. These estimates exclude delivery overages, requests where charged, image processing, backups, hosting, and taxes; they are not total-service cost promises.

## Decisions Before Implementation

- Confirm hosting and any already-included persistent disk/backup space.
- Select one provider and verify signup requirements, cost controls, and public/private delivery.
- Measure representative photos and choose dimensions, quality, thumbnail variants, and original retention.
- Define reconciliation and cleanup for database records whose external objects are missing, and external objects with no corresponding database record.
- Set global, per-user, per-listing, and per-message limits without changing the agreed chat privacy or editing rules.
- Define retention, user-visible quota behavior, backups, and the migration trigger when the free capacity is no longer sufficient.
