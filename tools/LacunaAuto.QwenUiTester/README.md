# LacunaAuto Qwen UI Tester

Local read-only UI review runner for the LacunaAuto project.

It connects the existing Playwright E2E suite with the local STRATA-hosted
**Strata Qwen Coder** model. The runner is intentionally not an application
implementation agent: it can inspect repository text/diffs, execute existing
Playwright specs, inspect the preserved JSON report, and probe HTTP routes, but
it cannot edit application or test files.

## Current stage

This is the first local integration stage. It proves the path:

```text
exact Git revision
    -> runner-owned PostgreSQL + LacunaAuto.Api + LacunaAuto.Web topology
    -> normal Playwright regression suite
    -> STRATA /v1/chat/completions
    -> Qwen tool-call loop
    -> structured Markdown review
```

The local loop and GitHub integration are verified. The
`.github/workflows/qwen-ui-review.yml` workflow automatically reviews
OWNER-authored same-repository PRs on GitHub-hosted Windows, checks out the exact
PR head SHA, executes this tester, uploads the review artifacts, publishes the
detailed report back to the PR, and creates a `Qwen UI Review` Check Run on the
exact tested SHA.

If the workflow cannot bootstrap PostgreSQL on the GitHub runner, it does not
invoke STRATA for a normal model review. It emits a structured `BLOCKED` review
artifact for the exact SHA instead, preserving fail-closed check behavior and
artifact/check-run publishing.

The slash-command path remains available for an explicit rerun or custom
acceptance criteria.

The runner also does **not** create adaptive Playwright probes yet. Qwen may
rerun existing focused specs and must explicitly report new behavior that
remains unverified.

## Requirements

- .NET SDK 10
- Node/npm and the existing `e2e/` dependencies installed
- Playwright Chromium installed
- access to a STRATA-hosted OpenAI-compatible chat-completions endpoint
- model available as `qwen3.8-flash-next-coder-iq1_m` (or override it)

The existing E2E setup remains authoritative:

```powershell
cd e2e
npm install
npx playwright install chromium
```

## Endpoints

The Qwen UI Tester uses explicit, separate local origins for Web and API so the
model cannot silently probe API routes on the Web process:

```text
LacunaAuto.Web: http://127.0.0.1:5132/
LacunaAuto.Api: http://127.0.0.1:5133/
```

The STRATA endpoint is remote/configurable and is **not committed to the repository**.
Provide the full Chat Completions URL (including `/v1/chat/completions`) through
`STRATA_CHAT_COMPLETIONS_URL` or `--strata-url`.

Authoritative review mode requires API + Web runtime topology. If something is
already responding on either origin, the runner stops rather than trusting it.
`--reuse-running-app` and `--reuse-running-api` exist only for explicit,
non-default cases where the caller has already verified exact revision match.

## First smoke run

Before starting LacunaAuto.Api/LacunaAuto.Web or Playwright, validate only the remote STRATA
transport, Caddy Bearer authentication, model selection, reasoning setting, and
the complete tool-call round trip:

```powershell
$env:STRATA_CHAT_COMPLETIONS_URL = "https://<your-strata-host>/v1/chat/completions"
$env:STRATA_API_KEY = [System.Net.NetworkCredential]::new("", (Read-Host "STRATA API key" -AsSecureString)).Password

dotnet run --project tools/LacunaAuto.QwenUiTester -- --strata-smoke
```

A successful smoke test prints:

```text
Transport  : OK
Auth       : OK
Tool call  : OK
Tool result: OK
Final      : LACUNA_QWEN_SMOKE_OK
```

This mode deliberately does not start the API app, Web app, or Playwright.

## First full UI review run

After the STRATA smoke test passes, run from anywhere inside a **clean**
checkout of the branch/revision being reviewed:

```powershell
dotnet run --project tools/LacunaAuto.QwenUiTester -- `
  --base main `
  --head HEAD `
  --scope "Review the current browsing UI without changing application code." `
  --acceptance "Regression suite passes; changed UI behavior has evidence; report anything not covered."
```

The runner verifies that `--head` resolves to the current checkout. By default
it also refuses a dirty working tree because an uncommitted tree cannot be
identified reliably by the reported SHA.

Authoritative mode also requires runtime topology availability:
PostgreSQL + `LacunaAuto.Api` + `LacunaAuto.Web`. The runner starts API/Web,
but PostgreSQL must already be available (or configured via environment
override connection string for the API process).

## Configuration

CLI options:

| Option | Default |
| --- | --- |
| `--base` | `main` |
| `--head` | `HEAD` |
| `--scope` | generic current-UI review |
| `--acceptance` | evidence-backed regression/review |
| `--base-url` | `http://127.0.0.1:5132/` |
| `--web-base-url` | `http://127.0.0.1:5132/` |
| `--api-base-url` | `http://127.0.0.1:5133/` |
| `--api-required` | on by default unless `--api-optional` is set |
| `--api-optional` | off |
| `--backend-relevant` | off |
| `--postgres-bootstrap-status` | `unknown` |
| `--strata-url` | required full Chat Completions endpoint URL |
| `--model` | `qwen3.8-flash-next-coder-iq1_m` |
| `--reasoning-effort` | `high` |
| `--max-tokens` | `48000` |
| `--max-tool-turns` | `16` |
| `--strata-smoke` | off; test only STRATA auth/tool calling |
| `--regression-only` | off; run Web + Playwright only, without STRATA |
| `--skip-regression` | off |
| `--allow-dirty` | off |
| `--reuse-running-app` | off |
| `--reuse-running-api` | off |

Environment-variable alternatives:

```text
E2E_BASE_URL
QWEN_API_BASE_URL
QWEN_API_REQUIRED
QWEN_BACKEND_RELEVANT
QWEN_POSTGRES_BOOTSTRAP_STATUS
STRATA_CHAT_COMPLETIONS_URL
STRATA_MODEL
STRATA_API_KEY
STRATA_REASONING_EFFORT
QWEN_UI_MAX_TOKENS
QWEN_UI_MAX_TOOL_TURNS
```

`STRATA_API_KEY` is never written to repository files. When present, the runner sends it as `Authorization: Bearer <key>`, matching VS Code Custom Endpoint behavior for Chat Completions endpoints unless custom request headers are configured.

For an interactive local run, avoid putting the secret itself in PowerShell history:

```powershell
$env:STRATA_API_KEY = [System.Net.NetworkCredential]::new(
  "",
  (Read-Host "STRATA API key" -AsSecureString)
).Password
```

After the run:

```powershell
Remove-Item Env:STRATA_API_KEY
```

The runner sends top-level `reasoning_effort` for this Chat Completions model. The default is `high`, matching the current Strata Qwen Coder configuration.

## Tools exposed to Qwen

The model does **not** receive an arbitrary PowerShell/shell tool.

It receives only:

- `read_file` — bounded UTF-8 repository text files;
- `search_text` — exact `git grep` search;
- `git_diff` — the locked `base...head` review diff;
- `run_playwright` — existing files under `e2e/specs/*.ts` only; optional `grep` is a test-title regex only, duplicate runs are rejected, and each review has a bounded focused-run budget;
- `read_e2e_report` — the preserved full regression JSON report;
- `http_get` — GET routes under an explicit `target` (`web` or `api`) and a relative route only.

The full regression report is copied before any focused rerun so a later
targeted Playwright execution cannot overwrite the evidence from the initial
full suite.

## Output

Generated Qwen review files live under the ignored `e2e/qwen-reviews/` folder, which is deliberately separate from Playwright's cleaned `e2e/e2e-artifacts/` output directory:

```text
qwen-full-regression-report.json
qwen-focused-<timestamp>.json
qwen-ui-review.md
qwen-ui-review.json
```

If Qwen consumes the configured tool-turn budget without returning a final answer, the runner issues one final no-tools, low-reasoning request using the evidence already collected instead of failing the whole review.

If a non-empty final response violates the required first-line verdict contract,
the runner makes one additional no-tools, low-reasoning **format-repair** request
that must preserve the existing findings and verdict. If that repair still does
not produce a valid first line, classification remains fail-safe `BLOCKED`.

The model report must return one of:

```text
Result: PASS
Result: FAIL
Result: BLOCKED
```

A normal regression failure cannot be promoted to PASS by the model.

Process exit codes:

| Exit | Meaning |
| ---: | --- |
| 0 | PASS |
| 2 | FAIL |
| 3 | BLOCKED |
| 10 | runner/integration failure |

## GitHub PR review workflow

After `.github/workflows/qwen-ui-review.yml` is merged to the default branch,
configure GitHub before the first real run.

### GitHub-hosted Windows runner

The review job runs on GitHub-hosted `windows-latest`, not on a self-hosted
machine. This is intentional because the repository is public and reviewed PR
code is built and executed.

Each review therefore runs in a disposable GitHub-hosted VM rather than on the
developer workstation. No local GitHub Actions runner needs to be installed or
registered.

The workflow still accepts only repository-owner-triggered reviews of
same-repository PR heads. Fork PRs are rejected before the review job, because
the STRATA secret is exposed only to the Qwen execution step and must never be
made available to untrusted fork code.

The workflow does **not** use Docker Compose for PostgreSQL on `windows-latest`.
It starts the preinstalled Windows service `postgresql-x64-17`, then provisions
the review database/user (or applies an explicit review-only connection-string
override) before starting the authoritative API+Web review topology.

Checkout uses `persist-credentials: false`, and the review job itself has only
`contents: read`. The write-capable token used to post the final PR comment is
kept in a separate GitHub-hosted publish job.

### Repository configuration

In **Settings -> Secrets and variables -> Actions**, add:

- repository variable `STRATA_CHAT_COMPLETIONS_URL` containing the full
  `/v1/chat/completions` endpoint;
- repository secret `STRATA_API_KEY`.

Do not commit either value to the repository.

The GitHub-hosted runner needs outbound access to NuGet/npm/Playwright setup sources and the configured STRATA endpoint.

### Trigger behavior

For a same-repository PR authored by the repository owner, the workflow runs
automatically on:

- PR opened;
- PR reopened;
- PR head updated (`synchronize`).

Fork PRs and non-owner PRs are rejected before the review job and therefore
never receive the STRATA secret.

The owner can still request a manual rerun or override acceptance criteria by
commenting:

```text
/qwen-ui-review
Regression passes. Verify the exact PR diff and the acceptance criteria for this change.
```

The first line must be exactly `/qwen-ui-review`. Everything after the first
line becomes the acceptance criteria. If omitted, the generic evidence-backed
criteria are used.

A manual `workflow_dispatch` path is also available.

The workflow:

1. resolves the open PR and rejects fork heads;
2. captures the exact base and head SHAs;
3. checks out the exact head SHA with full Git history;
4. installs the locked E2E dependencies and Chromium;
5. starts `postgresql-x64-17` and provisions review DB access for the API;
6. runs the tester with explicit Web/API origins in authoritative topology mode
   (`API required`), `reasoning-effort=medium`, `max-tokens=12000`, and
   `max-tool-turns=12`;
7. preserves `e2e/qwen-reviews/` as a 14-day workflow artifact;
8. posts the complete or bounded review back to the PR;
9. fails the review job for FAIL, BLOCKED, or integration failure.

Backend-diff classification is still computed, but only to decide whether
focused `LacunaAuto.Api.Test` execution is included in evidence; it does not
control API runtime availability.

The workflow publishes two GitHub surfaces for each completed review:

- a detailed PR comment containing the bounded Markdown report;
- a `Qwen UI Review` Check Run attached to the **exact tested PR head SHA**.

Check Run conclusions are mapped as follows. `BLOCKED` deliberately fails closed because GitHub treats a neutral required check as satisfying the requirement:

| Qwen result | Check conclusion |
| --- | --- |
| `PASS` | `success` |
| `FAIL` | `failure` |
| `BLOCKED` | `failure` |
| integration/error | `failure` |

This exact-SHA binding is important because an `issue_comment` workflow itself
runs from the default-branch workflow revision, while the Qwen runner checks out
and reviews the PR head SHA explicitly.

## Next stage

After the exact-SHA Check Run is verified on a real PR:

1. decide whether branch protection should require the `Qwen UI Review` check
   for `main`;
2. decide whether automatic review should later expand beyond OWNER-authored
   same-repository PRs;
3. only then consider controlled temporary adaptive Playwright probes.
