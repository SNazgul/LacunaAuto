Implemented the Qwen review infrastructure fix for explicit Web/API topology and constrained origin-aware verification on branch `fix/qwen-web-api-review`.

## What changed

- Updated the Qwen workflow to:
  - compute backend relevance only for focused backend test evidence scope;
  - bootstrap PostgreSQL on `windows-latest` via preinstalled Windows service (`postgresql-x64-17`) without Docker Compose;
  - always run the runner with explicit Web/API origins and required API topology in authoritative mode.
- Added a Windows PostgreSQL bootstrap script:
  - `.github/scripts/bootstrap-postgres-windows.ps1`
  - starts service, waits for availability, provisions/updates `lacunaauto` role and database.
- Updated runner options/runtime orchestration to distinguish Web/API:
  - explicit Web/API URLs and API-required mode;
  - backend-relevant switch used only for conditional focused API test execution;
  - API + Web startup status captured as explicit runtime evidence.
- Updated constrained HTTP tool design:
  - `http_get` now requires explicit `target: web|api`;
  - absolute URLs remain forbidden;
  - `/api/*` on `target=web` is explicitly rejected;
  - unavailable API target is explicitly surfaced as a tool/runtime unavailability.
- Updated prompt/report metadata and fail-closed classification:
  - review context now includes Web/API origins, runtime availability, backend test evidence, and what remained unverified;
  - required-topology unavailability is classified `BLOCKED` (fail-closed) instead of allowing wrong-origin product `FAIL`.
- Updated related docs:
  - `tools/LacunaAuto.QwenUiTester/README.md`
  - `docs/Development/CURRENT_WORK.md` (recently completed entry with validation evidence).

## Files changed

- `.github/workflows/qwen-ui-review.yml`
- `.github/scripts/bootstrap-postgres-windows.ps1` (new)
- `tools/LacunaAuto.QwenUiTester/RunnerOptions.cs`
- `tools/LacunaAuto.QwenUiTester/AppHost.cs`
- `tools/LacunaAuto.QwenUiTester/ToolRegistry.cs`
- `tools/LacunaAuto.QwenUiTester/Program.cs`
- `tools/LacunaAuto.QwenUiTester/README.md`
- `docs/Development/CURRENT_WORK.md`

## Validation run

- `dotnet build tools/LacunaAuto.QwenUiTester/LacunaAuto.QwenUiTester.csproj` ✅
- `dotnet test test/LacunaAuto.Api.Test/LacunaAuto.Api.Test.csproj` ✅ (1 passed)
- `dotnet run --project tools\LacunaAuto.QwenUiTester -- --strata-url http://127.0.0.1:1/v1/chat/completions --base main --head HEAD --allow-dirty --skip-regression --regression-only --api-required` ✅ (exit 0, topology ready path)
- `dotnet run --project tools\LacunaAuto.QwenUiTester -- --strata-url http://127.0.0.1:1/v1/chat/completions --base main --head HEAD --allow-dirty --skip-regression --regression-only --api-required --api-base-url http://127.0.0.1:5132/` ✅ (exit 3, forced topology-unavailable/BLOCKED path)
- PowerShell parse check for `.github/scripts/bootstrap-postgres-windows.ps1` ✅

## Intentionally not changed

- No application/product behavior code under `src/` was modified.
- No arbitrary external URL access was added to model tools.
- No broad tester framework redesign was introduced.

## Notes

- YAML parser modules were not available in this environment (`python` lacked `yaml`, PowerShell lacked `ConvertFrom-Yaml`), so workflow validation was done by targeted diff inspection and runner/build execution evidence.
