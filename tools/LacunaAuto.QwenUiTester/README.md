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
    -> runner-owned LacunaAuto.Web process
    -> normal Playwright regression suite
    -> STRATA /v1/chat/completions
    -> Qwen tool-call loop
    -> structured Markdown review
```

GitHub PR triggering and PR comments are intentionally not part of this stage.
They should be added only after the local loop is verified.

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

The Qwen UI Tester deliberately uses a dedicated local application port so it does
not silently test an unrelated Visual Studio/dev-server process:

```text
LacunaAuto under test: http://127.0.0.1:5132/
```

The STRATA endpoint is remote/configurable and is **not committed to the repository**.
Provide the full Chat Completions URL (including `/v1/chat/completions`) through
`STRATA_CHAT_COMPLETIONS_URL` or `--strata-url`.

If something is already responding on the app port, the runner stops rather
than trusting it. `--reuse-running-app` exists only for an explicit,
non-default case where the caller has already verified that the server is
serving the exact revision under review.

## First smoke run

Before starting LacunaAuto.Web or Playwright, validate only the remote STRATA
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

This mode deliberately does not start the Web app or Playwright.

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

## Configuration

CLI options:

| Option | Default |
| --- | --- |
| `--base` | `main` |
| `--head` | `HEAD` |
| `--scope` | generic current-UI review |
| `--acceptance` | evidence-backed regression/review |
| `--base-url` | `http://127.0.0.1:5132/` |
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

Environment-variable alternatives:

```text
E2E_BASE_URL
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
- `http_get` — GET routes under the configured application origin.

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

## Next stage after local verification

After this local loop is proven against the real STRATA installation:

1. add an isolated GitHub self-hosted runner;
2. checkout the exact PR head SHA;
3. pass PR scope/acceptance criteria to this tool;
4. publish `qwen-ui-review.md` as a PR comment;
5. expose PASS / FAIL / BLOCKED as a GitHub check;
6. only then consider controlled temporary adaptive Playwright probes.
