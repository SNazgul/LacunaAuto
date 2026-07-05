# LOGGING.md — Logging and Observability Rules for LacunaAuto

## Purpose

This document defines the mandatory logging and observability rules for LacunaAuto.
It is intended for human developers and AI coding agents such as GitHub Copilot Agent, Claude Code, and similar tools.

The goal is to make backend behavior easy to debug, monitor, and troubleshoot without producing noisy, unsafe, or low-value logs.

## Scope

These rules apply to:

- `LacunaAuto.Api`
- backend services in `LacunaAuto.Core`
- data access code in `LacunaAuto.Data`
- background jobs, hosted services, integrations, and infrastructure code
- future API-related code added to the solution

Frontend logging is out of scope unless explicitly requested.

## Required Technology Stack

Use the following observability stack unless the project owner explicitly changes it:

- **ILogger<T>** as the logging abstraction used by application code.
- **Serilog** as the structured logging provider.
- **Serilog request logging** for HTTP request/response summaries.
- **OpenTelemetry** for traces and metrics.
- **Seq** as the local Development log and trace viewer.
- **Console** logging for Development and container logs.

Do not introduce a different logging framework unless explicitly requested.
Do not bypass `ILogger<T>` in application services. The only acceptable direct usage of Serilog static APIs is early bootstrap logging in `Program.cs`.

## Core Principles

- Logs must be structured, queryable, and useful.
- Prefer structured properties over string interpolation.
- Every backend log produced during an HTTP request must be correlatable through `TraceId` and `SpanId`.
- Log important business operations, not every method call.
- Do not log secrets, tokens, passwords, full personal data, or large payloads.
- Log enough information to diagnose production issues without exposing sensitive data.
- Keep logging implementation centralized and consistent.
- Do not create broad refactors only to add logging.

## Required Correlation Properties

Every HTTP request log and business operation log should include, when available:

- `TraceId`
- `SpanId`
- `RequestId`
- `UserId` only if authentication exists and the value is safe to log
- domain identifiers such as `CarId`, `ListingId`, `SellerId`, `ImageId` when useful

Use PascalCase for all custom log property names.

Good property names:

- `TraceId`
- `SpanId`
- `ElapsedMilliseconds`
- `StatusCode`
- `ListingId`
- `UserId`
- `PageNumber`
- `SearchQueryHash`

Bad property names:

- `trace_id`
- `span_id`
- `elapsed_ms`
- `status_code`
- `payload`
- `data`

## Logging Levels

Use levels consistently.

| Level | Use For | Examples |
|---|---|---|
| `Trace` | Extremely detailed diagnostics, usually disabled | Rare low-level diagnostics |
| `Debug` | Development-only diagnostic details | SQL command details, decision branches during local debugging |
| `Information` | Normal important application events | HTTP request completed, listing created, image uploaded, user login succeeded |
| `Warning` | Suspicious, unexpected, or business-rule-related situations that do not crash the app | Invalid state transition, forbidden ownership attempt, external API slow response |
| `Error` | Operation failed but application continues | Unhandled exception, database save failure, external API failure |
| `Critical` | Application or service is unusable | Startup failure, required infrastructure unavailable |

Do not log expected validation errors as `Error`.
Use `Warning` only when the event is useful for monitoring or security analysis.
Use `Information` for normal business flow.

## HTTP Request Logging

API projects must log one structured summary event per HTTP request.

The HTTP request log should include at least:

- HTTP method
- request path
- status code
- elapsed time in milliseconds
- `TraceId`
- `SpanId`

Do not log full request or response bodies by default.
Do not log uploaded file contents.
Do not log authorization headers, cookies, refresh tokens, access tokens, or API keys.

Recommended event message template:

```csharp
"HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {ElapsedMilliseconds} ms"
```

## Business Operation Logging

Important business operations must be logged at their boundaries.

Examples:

- listing created
- listing updated
- listing deleted or deactivated
- image upload started/completed/failed
- user registration or login result
- search executed with safe summarized parameters
- payment or subscription action, if such features are added later
- external API request completed or failed

Business logs should include stable identifiers and safe summary data.

Good:

```csharp
_logger.LogInformation(
    "Listing {ListingId} was created by user {UserId}",
    listingId,
    userId);
```

Bad:

```csharp
_logger.LogInformation($"Listing was created: {JsonSerializer.Serialize(request)}");
```

## Database Logging and Tracing

Use OpenTelemetry instrumentation for Entity Framework Core where applicable.

Database telemetry should capture:

- query duration
- database operation type
- failure information
- correlation with the current HTTP request trace

Detailed SQL text should be enabled only in Development and only when needed.
Do not enable sensitive data logging in Production.
Do not log full SQL parameter values if they may contain personal data.

EF Core warnings and errors must remain visible.
Noisy EF Core informational logs may be filtered unless actively needed.

## Exceptions

Unhandled exceptions must be logged once by centralized middleware or the ASP.NET Core pipeline.

Do not log the same exception repeatedly in every layer.
If an exception is caught and handled locally, log it only when the event is useful and include the business context.

Good:

```csharp
_logger.LogError(
    exception,
    "Failed to create listing for user {UserId}",
    userId);
```

Bad:

```csharp
_logger.LogError(exception, "Error");
```

## ProblemDetails

API errors should use `ProblemDetails` where appropriate.

Error responses must not expose internal exception details in Production.
Use `TraceId` in error responses when possible so users, logs, and traces can be correlated.

## Sensitive Data Rules

Never log:

- passwords
- password hashes
- access tokens
- refresh tokens
- API keys
- authorization headers
- cookies
- full personal documents
- full phone numbers unless explicitly required and masked
- full email addresses unless explicitly required and safe for the scenario
- full request/response bodies containing personal data
- uploaded image/file contents

Prefer masking or hashing when identifiers are needed for diagnostics.

Examples:

- `EmailHash` instead of `Email`
- `PhoneMasked` instead of `Phone`
- `SearchQueryHash` or safe normalized search facets instead of raw free-text search

## Serilog Requirements

The API must configure Serilog during application startup.

Minimum requirements:

- read logging configuration from `appsettings*.json`
- enrich logs from log context
- enrich logs with environment and machine/process information
- write to Console in Development
- write to Seq in Development when Seq is available
- keep Microsoft/System framework logs at reasonable levels
- include trace/span correlation in log events

Application code must use `ILogger<T>`, not Serilog-specific logger types.

## OpenTelemetry Requirements

Configure OpenTelemetry for the API project.

Minimum tracing instrumentation:

- ASP.NET Core incoming requests
- outgoing `HttpClient` calls
- Entity Framework Core database operations, if EF Core is used by the API

Recommended resource attributes:

- `service.name`: `LacunaAuto.Api`
- `service.version`: application version when available
- `deployment.environment`: current environment

For local Development, export traces to Seq through OTLP when configured.
Console exporter may be used temporarily for diagnostics, but it should not be the main long-term observability target.

## Seq Requirements for Development

Local development should provide Seq through Docker Compose unless the project owner chooses another setup.

Recommended local URL:

```text
http://localhost:5341
```

Serilog should send logs to Seq.
OpenTelemetry should send traces to Seq via OTLP when configured.

Do not require Seq for the application to start in Production.
If Seq is unavailable in Development, the API should still start and write logs to Console.

## Configuration Guidelines

Prefer configuration over hard-coded endpoints.

Recommended configuration sections:

```json
{
  "Serilog": {
    "MinimumLevel": {
      "Default": "Information",
      "Override": {
        "Microsoft": "Warning",
        "Microsoft.AspNetCore": "Warning",
        "Microsoft.EntityFrameworkCore.Database.Command": "Warning",
        "System": "Warning"
      }
    },
    "WriteTo": [
      {
        "Name": "Console"
      },
      {
        "Name": "Seq",
        "Args": {
          "serverUrl": "http://localhost:5341"
        }
      }
    ],
    "Enrich": [
      "FromLogContext",
      "WithMachineName",
      "WithEnvironmentName",
      "WithProcessId",
      "WithThreadId"
    ]
  },
  "Observability": {
    "ServiceName": "LacunaAuto.Api",
    "Otlp": {
      "Endpoint": "http://localhost:5341/ingest/otlp/v1/traces"
    }
  }
}
```

Adjust exact package names and configuration syntax to the versions used by the project.
Do not hard-code Development endpoints in Production configuration.

## Minimal API / Controller Logging Rules

For Minimal APIs and controllers:

- keep endpoint handlers thin
- log business events in services whenever possible
- use endpoint-level logs only for endpoint-specific context
- avoid duplicate logs if Serilog request logging already logs request completion
- include domain identifiers, not full DTO payloads

Endpoint handlers should not contain verbose logging logic.

## Service Layer Logging Rules

Services should log important business decisions and operation outcomes.

Services should not log every private method call.
Services should not serialize entire entities or DTOs into logs.
Services should include useful identifiers and safe summaries.

## Data Layer Logging Rules

Repositories and DbContext-related code should not manually log every query.
Use OpenTelemetry and EF Core diagnostics for query telemetry.

Manual data-layer logs are acceptable for:

- migration/bootstrap operations
- unusual data repair operations
- important data consistency warnings
- failed persistence operations when not already logged higher up

## Health Checks

If health checks are added, logging should avoid noisy repeated success logs.
Failures should be visible and correlated with infrastructure status.

## AI Agent Implementation Rules

When an AI agent implements logging or observability, it must:

1. Read `AGENTS.md` first.
2. Read `CLAUDE.md` if present.
3. Read this `docs/AI/Rules/LOGGING.md` file.
4. Inspect only relevant infrastructure files before editing:
   - `Program.cs`
   - `appsettings*.json`
   - `docker-compose*.yml`
   - `Directory.Packages.props`
   - relevant `*.csproj` files
5. Keep the implementation minimal and focused.
6. Prefer extension methods for clean startup configuration when useful.
7. Avoid changing unrelated application behavior.
8. Run validation commands when applicable:
   - `dotnet restore LacunaAuto.sln`
   - `dotnet build LacunaAuto.sln`
   - `dotnet test LacunaAuto.sln`
9. If a command cannot be run, explain why.
10. Summarize changed files and how to view logs/traces in Seq.

## Acceptance Criteria

A logging/observability implementation is acceptable when:

- the API starts successfully
- logs are structured
- logs appear in Console during Development
- logs appear in Seq during Development when Seq is running
- HTTP requests are logged once with method, path, status code, and elapsed time
- exceptions are logged with stack traces and useful context
- `TraceId` and `SpanId` are present or available for correlation
- OpenTelemetry traces are configured for ASP.NET Core requests
- EF Core operations are traced when EF Core is present
- no sensitive data is logged by default
- the solution builds successfully

## Anti-Patterns

Avoid:

- logging full DTOs/entities/request bodies
- logging the same exception multiple times in different layers
- using string interpolation instead of structured logging templates
- adding logging to every method
- hard-coding local Seq URLs in Production settings
- enabling sensitive EF Core data logging in Production
- replacing `ILogger<T>` with direct Serilog usage throughout the codebase
- introducing unrelated refactoring during logging setup
