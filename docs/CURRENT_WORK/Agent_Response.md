Prepared the first local LacunaAuto Qwen UI Tester integration stage on branch `agents/qwen-ui-tester-skills`.

What changed

1. Updated the two Strata Qwen Coder skills
- `.github/skills/strata-qwen-coder-ui-automation/SKILL.md`
- `.github/skills/strata-qwen-coder-verification/SKILL.md`

They now apply when Strata Qwen Coder is explicitly identified either in an authorized GitHub Copilot session or through the LacunaAuto local Qwen UI Tester runner/API. They remain inactive for other/unknown models.

2. Added a standalone .NET 10 local runner under:
- `tools/LacunaAuto.QwenUiTester/`

The runner:
- locks review to the checked-out Git SHA and refuses a mismatched `--head`;
- refuses a dirty working tree by default;
- starts its own LacunaAuto.Web instance on `http://127.0.0.1:5132/`;
- refuses to trust an already-running app unless `--reuse-running-app` is explicit;
- runs the normal Playwright regression suite before contacting Qwen;
- preserves the full JSON regression report before focused reruns;
- calls STRATA through the OpenAI-compatible `/v1/chat/completions` endpoint;
- explicitly identifies the active model/context so the two Qwen skills apply;
- runs a tool-call loop with no arbitrary shell/PowerShell access;
- writes structured review artifacts under ignored `e2e/e2e-artifacts/`;
- returns process exit 0/2/3 for PASS/FAIL/BLOCKED and 10 for runner/integration failure.

Tools exposed to Qwen are deliberately constrained:
- `read_file`
- `search_text`
- `git_diff`
- `run_playwright` for existing `e2e/specs/*.ts` only
- `read_e2e_report`
- `http_get`

The current runner cannot create adaptive Playwright probes yet. The model is required to report that behavior as Not verified rather than invent coverage.

3. Added:
- `tools/LacunaAuto.QwenUiTester/README.md`

It documents prerequisites, default endpoints, CLI/environment configuration, evidence files, exit codes, and the next GitHub self-hosted-runner stage.

Intentionally not changed
- No GitHub Actions/self-hosted runner workflow yet.
- No PR-comment automation yet.
- No arbitrary command execution tool for Qwen.
- No application/backend/UI behavior changes.
- No changes to `AGENTS.md` or canonical product/current-work docs.
- The runner project has not yet been added to `LacunaAuto.sln`; first it should be built and exercised against the user's actual STRATA API.

Validation status
- Repository diff and implementation structure reviewed.
- Runtime validation is still required on the user's Windows/.NET/STRATA environment because this environment does not have the .NET SDK or access to the user's local STRATA instance.
- Do not merge this branch until the local build and smoke run succeed.

Suggested first validation:
```powershell
git switch agents/qwen-ui-tester-skills
git pull
dotnet build tools/LacunaAuto.QwenUiTester/LacunaAuto.QwenUiTester.csproj
```

Then, with STRATA/model running and the E2E dependencies installed:
```powershell
dotnet run --project tools/LacunaAuto.QwenUiTester -- `
  --base main `
  --head HEAD `
  --scope "Smoke-test the local Qwen UI review integration without modifying application code." `
  --acceptance "The normal Playwright regression suite runs, Qwen can use the constrained tools, and the runner produces a structured PASS/FAIL/BLOCKED review."
```
