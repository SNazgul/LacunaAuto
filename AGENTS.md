# AGENTS.md — Project Guidelines for Lacuna

## Project Overview
Lacuna is a modern .NET solution using clean layered architecture.  
It consists of an ASP.NET Core API backend and a Blazor WebAssembly frontend (PWA), with plans to add Blazor Hybrid (MAUI).

## High-Level Solution Structure
LacunaAuto/
├── src/
│   ├── LacunaAuto.Api/              # ASP.NET Core Web API
│   ├── LacunaAuto.Core/             # Models, DTOs, business logic, services
│   ├── LacunaAuto.Data/             # EF Core, repositories, migrations
│   ├── LacunaAuto.Web/              # Blazor WebAssembly hosting
│   ├── LacunaAuto.Web.Client/       # Blazor WebAssembly client (main UI - PWA)
│   ├── LacunaAuto.Hybrid/           # Blazor Hybrid MAUI (planned)
│   └── LacunaAuto.Frontend/         # Legacy MAUI project (optional)
├── test/                            # All test projects
|   └── LacunaAuto.Api.Test/         # Tests for LacunaAuto.Api project
└── LacunaAuto.sln


## AI Agent Behavior Rules (Important for efficiency)

### General Rules
- Always read this `AGENTS.md` file first before exploring the codebase or starting a task.
- **Never** read or modify anything inside the `test/` folder (or any folder/project containing `Test`, `Tests`, or `spec`) unless the task explicitly requires writing or fixing tests.
- Keep all changes **minimal and focused**. Do not add extra functionality or "improvements" without explicit permission.
- For complex tasks, first propose a short plan, then proceed after confirmation.

### Logging Rules

Logging is mandatory for API, service, database, external I/O, background job, and important business logic changes.

Follow `docs/AI/Rules/LOGGING.md`.

Prefer infrastructure-level logging first:
- HTTP request/response metadata must be handled by ASP.NET Core/Serilog request logging.
- EF Core query timing/tracing must be handled by OpenTelemetry/EF instrumentation.
- Unhandled exceptions must be handled by centralized exception handling/middleware.
- TraceId and SpanId must be included through the configured logging/tracing pipeline.

Add manual `ILogger<T>` logs only when they add real operational value:
- important business operations;
- business rule violations;
- handled exceptions;
- suspicious actions;
- external integration failures;
- long-running or unusual workflows.

Do not add noisy method-entry/method-exit logs.
Do not log sensitive data, secrets, tokens, full personal data, or large request/response bodies.


### UI Implementation Rules (especially from mockup images)
When the task is to implement a window or component based on a mockup image:

1. First, carefully analyze the provided image and describe what needs to be implemented.
2. Search and edit code **only** in the following projects:
   - `LacunaAuto.Web.Client` (primary)
   - `LacunaAuto.Web`
   - `LacunaAuto.Hybrid` (when added)
3. Strictly avoid searching or modifying other projects when working on UI.
4. Prefer modifying existing similar components rather than creating new ones from scratch.
5. Implement **only** what is clearly shown in the mockup.

### Search and Context Rules
- Use targeted and precise searches (specific folders/files) instead of broad solution-wide searches.
- When in doubt about scope, ask for clarification instead of exploring the entire solution.

## Coding Style and Conventions

### Naming Conventions
- Use **CamelCase** (PascalCase) for class names, method names, and public properties.
- All declared variables and fields **must** have explicit access modifiers (`private`, `public`, `protected`, `internal`).
- Private/internal fields and variables **must** start with an underscore `_` followed by a lowercase letter (e.g., `_userName`, `_isLoading`).

### Braces and Code Formatting
- Opening and closing curly braces `{` and `}` in methods, functions, classes, and control statements **must always be placed on a new line**.
- If a control statement (`if`, `for`, `while`, etc.) contains only **one line** of code, curly braces are optional.

### General Code Quality
- Keep methods and classes focused and reasonably small.
- Use meaningful and descriptive names.
- Follow the layered architecture: UI should not contain business logic (move it to Core services when appropriate).

## Blazor Specific Guidelines

Since this project uses Blazor WebAssembly (and will use Hybrid), follow these recommendations:

### Component Structure
- Keep components small and focused on a single responsibility.
- Prefer using `@code` blocks inside `.razor` files for simple logic. Use code-behind (`.razor.cs`) only when the logic becomes complex.
- Use `[Parameter]` for component inputs and `EventCallback<T>` (not `Action`) for parent-child communication.

### State and Lifecycle
- Use lifecycle methods correctly: `OnInitializedAsync`, `OnParametersSetAsync`, `OnAfterRenderAsync`.
- Avoid heavy computations directly in the markup or in `OnInitialized`.
- For shared state across components, prefer dependency injection (scoped services) over cascading parameters when possible.

### Performance (especially important for WebAssembly)
- Minimize unnecessary re-renders. Use `@key` when rendering lists.
- Move expensive logic to services in `LacunaAuto.Core` instead of keeping it in components.
- Use `Virtualize` component for long lists when applicable.
- Keep JavaScript interop to a minimum.

### Styling and UI
- Prefer CSS isolation (`.razor.css` files) or global styles in a structured way.
- For MIUI-style windows/components, keep the markup clean and match the design system consistently.

### General Blazor Best Practices
- Inject services using `@inject` or constructor injection in code-behind.
- Use `NavigationManager` for routing instead of direct URL manipulation.
- Handle loading and error states explicitly in components (use flags like `_isLoading`).
- Validate forms using `EditForm` and `DataAnnotations` where possible.

## Running the Project

- Use `docker-compose up` for local development.
- Main frontend: `LacunaAuto.Web.Client` (Blazor WebAssembly PWA).
- Backend: `LacunaAuto.Api`.
- Database and migrations are handled in `LacunaAuto.Data`.

## Additional Notes

- If something is unclear, ask clarifying questions (max 2–3) instead of making assumptions.
- Always respect the layered architecture (UI → Core → Data).
- These rules exist to keep the codebase clean, consistent, and token-efficient when working with AI agents.


## Mandatory Build / Validation Commands

Before finishing a task, run when applicable:

- dotnet restore LacunaAuto.sln
- dotnet build LacunaAuto.sln
- dotnet test LacunaAuto.sln

If a command cannot be run, explain why.


For cross-cutting infrastructure tasks such as logging, authentication, configuration, observability, or dependency injection, first inspect all relevant startup/composition files:
- Program.cs
- appsettings*.json
- docker-compose*.yml
- Directory.Packages.props
- *.csproj


For product behavior, MVP boundaries, and feature scope, read `docs/Product/PRODUCT_SCOPE.md`.


### Translated Documentation

- Files located under `Translations/` are human-readable translations only.
- AI agents must not read or use translated documentation unless explicitly requested by the user.
- English documentation outside `Translations/` is canonical.
- If a translation conflicts with the canonical English document, always follow the English document.