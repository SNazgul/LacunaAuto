# Slice 2.1B reconciliation

## Context

This records the comparison of the earlier stacked integration branch with PR #19 after the backend and Qwen infrastructure squash merges. The agreed sequence is to finish/merge 2.1B, then separately plan Qwen Vision responsive review infrastructure.

## Compared revisions

- Earlier branch: `feature/listings-ui-api-2-1b`, tip `e7756cc`; the actual integration commit is `76ea1f5`.
- Current integration: PR #19, `feature/listings-browsing-2-1b`, based on merged `main` commit `d28d4c6`.
- The earlier branch is stacked on the pre-squash 2.1A history. Comparing its full tree to current `main` also shows old Qwen infrastructure, so the integration commit was inspected separately. Core/Data and package versions match the merged backend foundation.

## Behavior and decision

| Area | Earlier 2.1B | PR #19 retained behavior |
| --- | --- | --- |
| Home/Listings | Public API list, no runtime seed fallback | Same; unused client seed file removed |
| Details | Fetch the list and select an ID in the browser | Published-only details endpoint; list/detail contract parity tested |
| Errors | Distinct errors; Details retry reloads the page | Distinct loading/empty/error/404, in-place retry, cancellation of stale details requests |
| Connectivity | Browser calls configured API directly; CORS | Browser calls Web; two fixed public GET routes forward to API |
| Client contracts | Six focused service tests | Coverage adapted to `ListingsClient`, including direct-ID requests, null fields, invalid enums and HTTP errors |
| Loading readiness | `data-lacuna-loading` marker | Marker restored alongside accessible `aria-busy`, shared with E2E readiness |
| Local API port | 5133 | API launch profiles, Web Development upstream and HTTP examples aligned to 5133 |

Keep PR #19 as the integration branch. Its Web forwarding path is deliberate: Web is already part of the authoritative topology, the API address stays in server configuration, and browser requests use one origin. This replaces the earlier prototype's CORS setup. Do not add unused CORS configuration or a second listings service. No product capability depends on direct browser-to-API access in this slice.

Preserve the earlier branch for reference; do not merge its older infrastructure or temporary `docs/CURRENT_WORK/Agent_Response.md`. The retained implementation includes real PostgreSQL-backed browsing, original currencies, optional fields, publication visibility, localization/preferences, and the accepted screen composition. Descriptions and seller actions retain the same Development placeholders.

## Validation

After reconciliation, Web build and Release publish passed, the UI/client project passed 10/10 tests, and the full browser suite passed 49/49 with no retries. A smoke check of the published Release verified eight live API cards, a cleared loading marker, and navigation to details. The focused client project is also included as a required step in the GitHub review job before Qwen; cloud verification runs on the updated PR head.

## Next task

After 2.1B is merged, plan the Qwen Vision infrastructure separately. It includes constrained layout inspection, fixed mobile/tablet/desktop presets, actual screenshot pixels sent to STRATA, a Vision smoke check, and BLOCKED results for unavailable Vision or missing required evidence. Product redesign and implementation of that infrastructure are outside this reconciliation.

Current screenshot spot checks and existing Qwen text/HTTP/E2E review do not constitute the future screenshot-aware Qwen capability.
