Updated the two Strata Qwen Coder skills so they can be used by the planned LacunaAuto local Qwen UI Tester runner/API as well as by authorized GitHub Copilot sessions.

What changed
- `.github/skills/strata-qwen-coder-ui-automation/SKILL.md`
  - broadened applicability from GitHub Copilot-only to either GitHub Copilot or the LacunaAuto local Qwen UI Tester runner/API;
  - still requires the active model to be explicitly identified as Strata Qwen Coder;
  - model identity may come from session metadata, runner metadata, or the user;
  - kept the skill inactive for other/unknown models;
  - changed the wording from “Copilot skill directory” to the neutral “current skill directory”.

- `.github/skills/strata-qwen-coder-verification/SKILL.md`
  - applied the same execution-context broadening and explicit model-identity requirement;
  - kept the skill inactive if model or execution context cannot be confirmed.

Intentionally not changed
- No changes to `AGENTS.md`, `CURRENT_WORK.md`, E2E tests, application code, or general project documentation.
- No runner/API implementation yet.

Validation
- Reviewed the branch diff against `main`.
- No build/test run: this task only changes skill instruction Markdown and the UI automation skill explicitly says not to run application builds for skill-instruction-only edits.

Branch
- `agents/qwen-ui-tester-skills`
