Prepared the first LacunaAuto Qwen UI Tester integration stage on branch `agents/qwen-ui-tester-skills`.

Latest addition: dedicated STRATA smoke mode
- Added `--strata-smoke`.
- This mode does not start LacunaAuto.Web or Playwright.
- It validates the remote HTTPS endpoint, Bearer authentication, model selection, configured reasoning effort, a forced function/tool call, the tool-result round trip, and the final model response.
- Expected success marker: `LACUNA_QWEN_SMOKE_OK`.
- `STRATA_API_KEY` remains environment-only and is never written to repository files.

Runner configuration now matches the supplied custom-endpoint model:
- full configurable Chat Completions URL via `STRATA_CHAT_COMPLETIONS_URL` / `--strata-url`;
- model `qwen3.8-flash-next-coder-iq1_m`;
- `reasoning_effort=high` by default;
- `max_tokens=48000` by default;
- Bearer authorization when `STRATA_API_KEY` is present.

The full UI-review runner still:
- locks review to the checked-out Git SHA;
- refuses a dirty working tree by default;
- starts a dedicated LacunaAuto.Web instance;
- runs the normal Playwright regression suite first;
- preserves the full regression JSON report;
- gives Qwen only constrained read/verification tools;
- writes PASS / FAIL / BLOCKED review artifacts under ignored `e2e/e2e-artifacts/`.

Runtime validation is still required on the user's Windows/.NET/STRATA environment. Do not merge until build and STRATA smoke mode succeed.

First validation steps:

```powershell
git switch agents/qwen-ui-tester-skills
git pull

dotnet build tools\LacunaAuto.QwenUiTester\LacunaAuto.QwenUiTester.csproj
```

Then configure the secret only in the current PowerShell process:

```powershell
$env:STRATA_CHAT_COMPLETIONS_URL = "https://194.53.196.226/v1/chat/completions"
$env:STRATA_API_KEY = [System.Net.NetworkCredential]::new(
    "",
    (Read-Host "STRATA API key" -AsSecureString)
).Password
```

Run only the transport/auth/tool-calling smoke test:

```powershell
dotnet run --project tools\LacunaAuto.QwenUiTester -- --strata-smoke
```

Expected successful tail:

```text
Transport  : OK
Auth       : OK
Tool call  : OK
Tool result: OK
Final      : LACUNA_QWEN_SMOKE_OK
```
