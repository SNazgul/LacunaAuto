Applied the requested review fixes on branch `fix/qwen-web-api-review` without redesigning the implementation.

## What was fixed

1. **PostgreSQL Windows service startup hardening**
   - Updated [.github/scripts/bootstrap-postgres-windows.ps1](../../.github/scripts/bootstrap-postgres-windows.ps1):
     - explicit, clear error if expected service is missing;
     - reads service startup mode via `Win32_Service`;
     - if startup mode is `Disabled`, switches to `Manual` before `Start-Service`.

2. **GitHub runner superuser password for bootstrap**
   - Updated workflow bootstrap invocation in [.github/workflows/qwen-ui-review.yml](../../.github/workflows/qwen-ui-review.yml) to pass:
     - `-SuperuserPassword "root"`
   - Kept script default `SuperuserPassword = ""` for local-friendly usage.

3. **API connection-string override key**
   - Updated workflow env var key from:
     - `ConnectionStrings__DefaultConnection`
   - to:
     - `ConnectionStrings__LacunaAuto`
   - Kept this as review-job-only workflow environment override (no appsettings/product code changes).

4. **Backend-relevant detection broadened (focused scope only)**
   - Updated backend relevance logic in [.github/workflows/qwen-ui-review.yml](../../.github/workflows/qwen-ui-review.yml) to include:
     - prefix: `test/LacunaAuto.Api.Test/`
     - exact file: `Directory.Packages.props`
   - Existing prefixes (`src/LacunaAuto.Api/`, `src/LacunaAuto.Core/`, `src/LacunaAuto.Data/`) retained.

## Validation executed

- `dotnet build tools\LacunaAuto.QwenUiTester\LacunaAuto.QwenUiTester.csproj` ✅
- PowerShell parser check for bootstrap script ✅
- Missing-service defensive path check:
  - `Expected PostgreSQL service '__missing_postgres_service__' was not found on this machine.` ✅
- Verified Disabled → Manual logic exists in script (`StartMode == Disabled` + `Set-Service ... -StartupType Manual`) ✅
- Verified workflow contains:
  - `ConnectionStrings__LacunaAuto` ✅
  - `bootstrap-postgres-windows.ps1 -SuperuserPassword "root"` ✅

## Files updated

- [.github/scripts/bootstrap-postgres-windows.ps1](../../.github/scripts/bootstrap-postgres-windows.ps1)
- [.github/workflows/qwen-ui-review.yml](../../.github/workflows/qwen-ui-review.yml)
- [docs/CURRENT_WORK/Agent_Response.md](./Agent_Response.md)
