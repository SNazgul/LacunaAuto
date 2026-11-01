Implemented the next Qwen GitHub integration slice: publish the review result as a GitHub Check Run on the exact tested PR head SHA.

Changes:
- Added `checks: write` only to the isolated publish job.
- Added a `Publish Qwen check run` step using the workflow `GITHUB_TOKEN`.
- Check name: `Qwen UI Review`.
- PASS maps to `success`.
- FAIL maps to `failure`.
- BLOCKED maps to `neutral`.
- ERROR/integration failure maps to `failure`.
- Check `head_sha` is the resolved PR head SHA from the prepare job, not the default-branch commit that hosts the `issue_comment` workflow.
- Check details link to the GitHub Actions run.
- Existing detailed PR comment publishing remains unchanged.
- Updated the Qwen UI Tester README with the exact-SHA Check Run behavior and corrected stale self-hosted wording.

Security:
- The review job remains `contents: read`.
- STRATA secret exposure is unchanged.
- Only the isolated publish job receives `checks: write`.

Runtime verification is intentionally pending until this workflow revision is merged to `main`, because the `issue_comment` workflow executes from the default branch. After merge, reopen the temporary PR #9, trigger `/qwen-ui-review`, and verify that the `Qwen UI Review` check appears on the tested PR SHA.

Do not merge `docs/CURRENT_WORK/Agent_Response.md` into `main`.
