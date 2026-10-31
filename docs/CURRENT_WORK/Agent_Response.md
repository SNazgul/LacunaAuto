Implemented the first GitHub automation layer for the validated LacunaAuto Strata Qwen UI Tester.

What changed:
- Added `.github/workflows/qwen-ui-review.yml`.
- Trigger: repository-owner PR comment whose first line is exactly `/qwen-ui-review`, plus a manual `workflow_dispatch` path.
- The prepare job resolves the open PR through GitHub, rejects fork heads, and captures exact base/head SHAs.
- The review job requires the dedicated Windows self-hosted runner labels `self-hosted, Windows, X64, lacuna-qwen`.
- Checkout is pinned to the exact PR head SHA with `fetch-depth: 0` and `persist-credentials: false`.
- The self-hosted review job has only `contents: read`; the write-capable token used for the final PR comment exists only in a separate GitHub-hosted publish job.
- E2E dependencies are installed with `npm ci`; Chromium is installed through the locked Playwright package.
- The Qwen runner is invoked with exact base/head SHAs, `reasoning-effort=medium`, `max-tokens=12000`, and `max-tool-turns=12`.
- Qwen review artifacts are uploaded for 14 days and the report is posted back to the PR.
- FAIL, BLOCKED, and integration failures cause the review job to fail after artifacts are preserved.
- Updated `tools/LacunaAuto.QwenUiTester/README.md` with runner registration, GitHub variable/secret setup, security boundaries, and slash-command usage.

Security boundary:
- Slash-trigger is owner-only.
- Only same-repository PR heads are allowed.
- Fork PRs are rejected before the self-hosted job starts.
- PR checkout credentials are not persisted.
- STRATA credentials are exposed only to the Qwen execution step.
- The self-hosted runner should be dedicated/isolated because reviewed PR code is built and executed.

Not yet verified:
- The GitHub workflow cannot be exercised from this branch through `issue_comment`; GitHub loads that trigger from the default branch.
- A dedicated self-hosted runner with label `lacuna-qwen` is not yet registered/configured in this task.
- Repository variable `STRATA_CHAT_COMPLETIONS_URL` and repository secret `STRATA_API_KEY` still need to be configured for Actions.
- The first real slash-triggered PR review and PR comment round trip remain to be verified after the bootstrap workflow is merged.

Intentionally deferred:
- A formal GitHub Check Run tied to the tested SHA. Add it only after the first real self-hosted PR review is proven.
- Automatic review on every PR/push.
- Adaptive temporary Playwright probes.

Draft PR: #8 `ci: add self-hosted Qwen PR review workflow`.

Do not merge `docs/CURRENT_WORK/Agent_Response.md` into `main`.
