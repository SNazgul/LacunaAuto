# GitHub Copilot Instructions — LacunaAuto

These are repository-wide instructions for GitHub Copilot when working in this repository.

## Canonical Instructions

Follow the canonical AI agent instructions in `AGENTS.md`.

Use this file as a Copilot-specific bridge, not as a replacement for `AGENTS.md`.
If there is a conflict between this file and `AGENTS.md`, prefer `AGENTS.md` unless this file explicitly defines Copilot-specific behavior.

## Project Context

LacunaAuto is a .NET 10 solution for an automotive classifieds platform.

Main technologies:

- ASP.NET Core Web API
- Blazor WebAssembly / PWA
- Planned Blazor Hybrid with .NET MAUI
- Entity Framework Core
- PostgreSQL
- Docker-based local development
- Clean / layered architecture

Respect the existing solution structure and naming conventions.
Do not introduce unrelated architectural changes while implementing a task.

## General Copilot Behavior

- Keep changes minimal, focused, and directly related to the requested task.
- Prefer modifying existing patterns over inventing new ones.
- Inspect similar existing code before creating new files or abstractions.
- Do not perform broad refactoring unless explicitly requested.
- Do not modify test projects unless the task explicitly asks for tests or the build requires a test fix.
- Do not add new dependencies unless they are clearly justified by the task and consistent with the project.
- Do not hardcode secrets, connection strings, tokens, passwords, or environment-specific values in code.

## Architecture Rules

- Keep UI logic out of business/domain logic.
- Keep business logic in the appropriate Core/Application layer.
- Keep EF Core and PostgreSQL-specific implementation details in the Data layer.
- Keep API endpoints thin: validate input, call services/use cases, return proper results.
- Use dependency injection consistently.
- Prefer `ILogger<T>` abstractions in application code.
- Do not reference Serilog directly from Core/Application code unless there is a strong reason.

## Logging and Observability

Logging is mandatory to consider for any task that touches:

- REST API endpoints
- Controllers or Minimal APIs
- Services / use cases / business logic
- Repositories or database access
- EF Core configuration
- External I/O or integrations
- Background jobs or long-running workflows
- Authentication, authorization, configuration, or infrastructure code

For such tasks, follow `docs/AI/Rules/LOGGING.md`.

Prefer infrastructure-level logging first:

- HTTP request/response metadata should be handled by ASP.NET Core / Serilog request logging.
- EF Core query timing and tracing should be handled by OpenTelemetry / EF instrumentation.
- Unhandled exceptions should be handled by centralized exception handling or middleware.
- `TraceId` and `SpanId` should be included through the configured logging/tracing pipeline.

Add manual `ILogger<T>` logs only when they add real operational value, for example:

- Important business operations
- Business rule violations
- Handled exceptions
- Suspicious actions
- External integration failures
- Long-running or unusual workflows

Do not add noisy method-entry or method-exit logs.
Do not log passwords, tokens, secrets, full personal data, large request/response bodies, or unnecessary internal implementation details.

If a logging-related decision is unclear, read `docs/AI/Rules/LOGGING.md` before coding.
For major logging or observability tasks, first summarize the relevant acceptance criteria from `docs/AI/Rules/LOGGING.md`.

## API Rules

When adding or changing API endpoints:

- Follow the existing Minimal API / controller style used by the project.
- Use async APIs where appropriate.
- Validate input using the project’s existing validation approach.
- Return correct HTTP status codes.
- Use ProblemDetails for errors when applicable.
- Add or update OpenAPI metadata where appropriate.
- Consider whether manual business logging is needed according to `docs/AI/Rules/LOGGING.md`.

## EF Core / PostgreSQL Rules

When adding or changing entities, DbContext configuration, migrations, repositories, or queries:

- Follow existing EF Core patterns in the solution.
- Keep database-specific code in the Data layer.
- Add indexes, constraints, and configuration intentionally.
- Avoid unnecessary eager loading and inefficient queries.
- Do not log raw SQL or sensitive query parameters in production.
- Rely on configured EF Core/OpenTelemetry tracing for query timing unless a specific manual log adds value.

## Blazor / UI Rules

When working on Blazor WebAssembly, shared Razor components, or future MAUI Blazor Hybrid:

- Prefer shared Razor components when UI should be reused.
- Keep components small and focused.
- Use `[Parameter]`, `EventCallback<T>`, and `RenderFragment` appropriately.
- Prefer CSS isolation where appropriate.
- Handle loading and error states explicitly.
- Do not place business logic directly in UI components.

## Prompt Files

Reusable prompt files live in `.github/prompts/`.
Use them when the user explicitly references a workflow such as adding an API endpoint, creating a feature, adding an EF entity, refactoring code, or writing tests.

Prompt files do not replace `AGENTS.md` or this file.
They are task templates and must still follow repository-wide instructions.

## Validation

Before finishing a task, run applicable validation commands when possible:

- `dotnet restore LacunaAuto.sln`
- `dotnet build LacunaAuto.sln`
- `dotnet test LacunaAuto.sln`

If a command cannot be run, explain why.

At the end of a task, summarize:

- What changed
- Which files were changed
- How to verify the change locally
- Any commands that were run or could not be run
