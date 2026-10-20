---
name: strata-qwen-coder-ui-automation
description: Plan, write, run, and review browser UI automation for implemented LacunaAuto features. Use only in GitHub Copilot sessions explicitly using Strata Qwen Coder when UI testing is requested. Leave inactive for other models or an unknown active model.
---

# UI automation for Strata Qwen Coder

## Applicability and responsibility

Use this skill only when the assistant is GitHub Copilot and the active model is explicitly identified as **Strata Qwen Coder** by session metadata or the user. Otherwise leave it inactive; do not infer model identity from behavior.

Own UI test planning, test maintenance, browser execution, failure investigation, and evidence-based reporting for the requested functionality. A testing assignment authorizes relevant test changes, not application fixes, redesigns, or new product features. Report product defects with reproduction steps; change application code only when the user also authorizes fixes.

Keep this skill inside its Copilot skill directory. Do not advertise or register it in general project documents or shared instructions unless explicitly requested. Follow the task's repository rules for ordinary implementation documentation and handoffs without adding skill-adoption notes.

## 1. Establish the test scope

- Read the repository instructions, [current work](../../../docs/Development/CURRENT_WORK.md), and the product criteria relevant to the requested screens. Distinguish implemented behavior from planned features and explicitly disabled controls.
- Inspect [the existing E2E workflow](../../../e2e/README.md), `e2e/package.json`, the lockfile, configuration, and relevant existing helpers/specs. Extend those tests rather than creating a competing suite. Avoid unrelated .NET test projects.
- Write a short coverage plan: user journey, starting state, user action, expected visible result, and relevant failure or boundary case. Select supported variants according to the task; do not multiply every test across every viewport and language without a reason.
- Confirm whether required controls actually exist. Do not invent a settings UI or claim a UI interaction when the test only seeds storage or calls an API.

## 2. Establish a trustworthy environment

- Use the installed Playwright version and locked dependencies. Check unfamiliar APIs and CLI options in local types, help, source, or official documentation before using them. Do not upgrade dependencies just to run a test.
- Use an authorized development/test environment and isolated fixture data. Confirm the checkout, revision, build, browser, and origin being tested. Reusing a server is valid only when it serves the source under review.
- Follow the documented app startup. Confirm readiness with a successful request and rendered application state; a failed `HEAD` request alone does not establish that `GET` is unavailable. Record processes you start so cleanup can target only those processes.
- Respect `E2E_BASE_URL`. Derive configurable expectations from the documented input contract; retain independent expectations for fixed requirements. Never hardcode the default origin into a test that supports an override.
- When configuring the runner, verify effective options through fixtures or behavior. Check [Playwright configuration](https://playwright.dev/docs/test-configuration) for option placement. A loaded configuration file or a green run does not prove every property took effect.

## 3. Exercise the real UI reliably

- Follow [Playwright's guidance](https://playwright.dev/docs/best-practices) on isolated tests and resilient locators: prefer roles, accessible names, labels, and existing test IDs. Reuse stable scoped selectors where appropriate; avoid incidental DOM order and unnecessarily elaborate page objects.
- Perform the interaction through the existing visible control when that interaction is under test. Storage seeding or API setup may establish preconditions, but must not replace the behavior being claimed.
- Keep tests independent through fresh contexts and controlled data. Pin relevant locale, timezone, and viewport assumptions. Do not depend on another test to create preferences or navigate first.
- After navigation, reload, or preference changes, wait for the Blazor state needed by the next action. Browser `load` can finish before components render. Use [retrying locator assertions](https://playwright.dev/docs/test-assertions), such as `toHaveCount` and `toHaveText`, instead of immediately asserting a sampled count or string during a transition.
- Avoid arbitrary sleeps and increasing retries to hide races. Deliberate response delays are appropriate for a focused slow-startup scenario.
- Capture unexpected browser errors and failed application requests. Distinguish them from deliberate faults, expected missing-resource responses, and known warnings; do not silently suppress them all.

## 4. Make each coverage claim observable

Apply only the rows relevant to the requested functionality:

| Claim | Evidence required |
| --- | --- |
| A user journey works | Real control interactions followed by the expected rendered state and relevant URL/data checks. |
| A missing route returns HTTP 404 | Inspect the direct navigation or HTTP response status as well as rendered content. |
| Navigation stays inside the SPA | Use actual application routing. Preserve a unique window marker and track main-frame document requests after startup; assert continuity and no new document request. Filter requests by the page's main frame. `page.goto` and `window.location` assignment do not establish SPA routing. |
| Images load | Verify successful loading/decoding and nonzero intrinsic dimensions. An image element or plausible `src` alone is insufficient. |
| Preferences persist | Change them through existing controls, reload without reseeding, and assert the retained visible effect. If no controls exist, label storage-driven coverage explicitly. |
| Localization and formatting work | Assert supported translated strings and number/date formatting independently. Verify money against the implemented currency contract; do not assume a preference converts or relabels prices. |
| Layout is responsive | Check agreed breakpoints on both sides, affected screens, overflow/clipping, and important controls. Use viewport-specific failure messages and inspect screenshots when appearance matters. |
| An unavailable control is disabled | Assert its presence and disabled state. Do not enable an unfinished feature to make a journey testable. |
| A PWA feature works | Separate manifest/icon availability, service-worker behavior, installation, and offline behavior. Development asset checks do not prove Release installability or offline support. |

For a version-sensitive programmatic navigation probe, verify the framework API in the served runtime before using it. Prefer ordinary user navigation when that is the requested behavior.

## 5. Challenge false positives

When adding or repairing a regression check, reproduce the defect or inject a narrow relevant fault. Examples include blocking the tested image request or delaying bootstrap responses. Confirm that the intended assertion detects it and that normal behavior passes.

Keep injection isolated. Do not accept any nonzero exit as a successful negative check: inspect the exact failure and exclude setup errors, unavailable servers, and unrelated assertions. The existing `npm run test:negative` suite intentionally fails; interpret its individual failures separately from the normal suite. Do not add unconditional skips or update expected output solely to hide a product regression.

## 6. Execute and investigate

- Run focused scenarios first, then the relevant normal suite. Follow repository-required build/test commands when applicable to the changes. Do not perform application builds for edits limited to skill instructions.
- Save full command output and exit status before filtering summaries. Preserve the full-run report when executing later probes; use separate report/output paths or a suitable reporter override.
- For failures, record the test, exact assertion, expected/actual result, environment, and shortest reproduction. Inspect screenshots, DOM, relevant requests, or a [Playwright trace](https://playwright.dev/docs/trace-viewer) as appropriate. Do not claim visual inspection from a screenshot that was merely captured.
- Classify the result as a product defect, test defect, environment blocker, or unresolved. Fix authorized test defects; report product defects unless application fixes were requested. After a justified correction, rerun affected tests and the relevant regression scope.

## 7. Report and clean up

Inspect the final generated report for passed, failed, skipped, and flaky outcomes. Confirm that it belongs to the final tested revision. Distinguish your execution from previously saved reports, and automated assertions from visual inspection.

Give a concise result with commands, environment, actual counts, covered journeys, evidence/artifact paths, uncovered behavior, and actionable defects. For each defect include the trigger, expected behavior, actual behavior, and reproduction. State precisely what remains unverified if execution is blocked; never substitute source inspection for a claimed browser run.

Stop only servers you started, restore temporary environment changes, and remove your temporary probes. Preserve user work and useful failure artifacts in the existing ignored artifact directories. Do not commit or publish results unless requested.
