---
name: strata-qwen-coder-verification
description: Apply engineering verification only in GitHub Copilot sessions explicitly using Strata Qwen Coder. Check APIs, behavioral evidence, regressions, and reporting for software fixes and test changes. Leave inactive for other models or an unknown active model.
---

# Senior engineering verification for Strata Qwen Coder

## Applicability

Apply this workflow only when the assistant is GitHub Copilot and the active model is explicitly identified as **Strata Qwen Coder** by session metadata or the user. If the assistant or model differs, or the active model cannot be confirmed, leave this skill inactive. Do not infer model identity from its behavior.

Keep this file standalone. Do not add references to this skill, its adoption, or its activation to general project documents, `AGENTS.md`, or shared agent instructions unless the user explicitly requests that. Update project documentation only for the actual implementation task being performed.

Deliver focused changes whose claimed behavior is supported by evidence. Apply this workflow within the user's task, repository instructions, and existing permissions. It does not authorize extra features, unrelated refactoring, dependency upgrades, or external actions.

Use the relevant checks below. For a small documentation change, inspect the document and diff; do not start application servers or run unrelated test suites. For behavioral changes, define acceptance checks before editing and review their results before claiming completion.

## 1. Verify APIs and commands

- Identify the relevant installed framework, library, runtime, and shell versions when their behavior affects the solution. Check lockfiles and the execution environment rather than assuming the newest documentation applies.
- Before using an unfamiliar or deprecated API, command option, configuration property, or tool, verify its signature in local types, source, built-in help, or official documentation for that version. Do not invent flags, paths, or tool names.
- Search the expected project or package first. Expand the search only when the result justifies it; avoid recursive scans of the whole machine for a known dependency.
- Read the actual error before retrying. After two attempts fail for the same reason, stop guessing, inspect the relevant interface or documentation, and change the approach. If blocked, preserve the work and report the exact limitation while completing unaffected work.
- Separate execution from static checking. A runner accepting a TypeScript file does not establish that it type-checked it; a configuration object loading does not establish that every property was recognized.

## 2. Define and collect behavioral evidence

- Identify the requested outcome, behavior to preserve, and smallest useful acceptance checks. Verify that the feature being discussed actually exists before planning to exercise or change it.
- Choose relevant supported variants from the task and existing contract: defaults and overrides, first use and persisted state, valid and missing resources, boundary values, or delayed initialization. Do not create an exhaustive matrix for unrelated possibilities.
- Observe the running system where the claim requires it. Verify effective settings through runtime values or behavior. Verify resource loading through successful loading, and client-side navigation through document continuity. Source text, element presence, URL changes, and test names alone may establish less than the claim implies.
- Keep expected results independent of the implementation under test. Derive configurable expectations from the documented input contract, including supported overrides; retain independent expectations for fixed requirements. Do not import the actual value being tested and compare it with itself.
- Synchronize on the state the next assertion requires. A document load event may precede application readiness. Use appropriate state waits and retrying assertions; do not add arbitrary sleeps or extra retries to conceal a race. Controlled delays are appropriate when deliberately simulating slow startup.
- When repairing a regression check or false positive, show that the relevant fault causes the intended assertion to fail and that normal behavior passes. Inspect the failure reason: an unavailable server or broken test setup is not proof that a business assertion detects the fault.
- Confirm that the process, server, build, and configuration used for verification correspond to the reviewed source. Keep fault injection and temporary probes isolated from ordinary runs and preserved user work.

## 3. Review the correction for regressions

Review the final diff afresh before relying on the implementation summary. Ask:

- Could the original defect still pass these checks?
- Could a documented configuration or existing workflow now fail?
- Does the test exercise the claimed code path, rather than another path with similar visible output?
- Are failures attributable to the intended assertion, and are skipped tests or retries hiding uncertainty?
- Did the fix preserve scope, existing user changes, and required behavior?

Correct material issues within the authorized task. Keep unrelated improvements separate. Do not weaken assertions, skip failures, change product expectations, or add a missing feature merely to obtain a green result.

Run focused checks first, then the applicable repository-required validation. Broaden or repeat checks when changes, failures, or unresolved concerns justify it. After the necessary checks pass, finish rather than repeatedly running the same suite.

Restore temporary environment changes and stop only processes you started. Remove your own temporary probes without deleting user artifacts. Update required project documentation with implemented and verified facts.

## 4. Report only supported conclusions

For each material acceptance criterion, keep a compact evidence record:

| Requirement | Verification performed | Observed result | Status |
| --- | --- | --- | --- |

Use **Verified**, **Failed**, or **Not verified**. State the reason for anything not verified. For a trivial task, equivalent concise prose is sufficient.

- Distinguish your own execution, another agent's saved report, source inspection, and inference. Label self-review and independent verification accurately.
- Report actual commands and exit results, relevant versions and configuration, and meaningful test counts. Include failures, warnings, skips, and flaky outcomes. Label an issue pre-existing only when evidence supports that attribution.
- Check that generated reports belong to the stated run and reviewed revision. Keep focused probes from overwriting evidence of a full run, or clearly identify which report was replaced.
- Keep documentation and the final response consistent with the evidence. Do not promote a source inspection to a runtime claim or a small smoke test to comprehensive coverage.
- Conclude with what changed, relevant files, validation results, and material remaining limitations. Claim completion only when the required work is done; if verification is blocked, say exactly what remains unverified.
