# Implement Logging and Observability

---
description: "One-time setup for Serilog, OpenTelemetry, Seq, tracing, and structured logging"
agent: "agent"
---

# One-time infrastructure setup

This prompt is intended to be used once when setting up logging and observability infrastructure.

Before implementing, read and follow:
- ../../AGENTS.md
- ../../docs/AI/Rules/LOGGING.md

You are implementing production-ready logging and observability for the LacunaAuto .NET solution.

## Mandatory context

Before making any changes, read and follow:

- `AGENTS.md`
- `CLAUDE.md` if present
- `docs/AI/Rules/LOGGING.md`

These files are authoritative. Do not ignore them.

## Goal

Implement structured logging and observability for the ASP.NET Core API using:

- `ILogger<T>` in application code
- Serilog as the structured logging provider
- Serilog request logging for HTTP request summaries
- OpenTelemetry for traces and metrics
- Seq + Console for local Development observability

## Important scope rules

Keep the task focused. Do not refactor unrelated application code.

Inspect only the files needed for logging/observability setup, especially:

- `Program.cs`
- `appsettings*.json`
- `docker-compose*.yml`
- `Directory.Packages.props`
- relevant `*.csproj` files
- existing extension methods or infrastructure folders if they already exist

Do not modify `test/`, `tests/`, `Test`, `Tests`, or `spec` folders unless tests are explicitly requested.

## Implementation requirements

### 1. Packages

Add the required NuGet packages using the project’s existing package management style.

Expected package categories:

- Serilog ASP.NET Core integration
- Serilog configuration from appsettings
- Serilog Console sink
- Serilog Seq sink
- Serilog enrichers for environment/process/thread/log context
- OpenTelemetry hosting integration
- OpenTelemetry ASP.NET Core instrumentation
- OpenTelemetry HttpClient instrumentation
- OpenTelemetry Entity Framework Core instrumentation, if EF Core is used
- OpenTelemetry OTLP exporter if traces are sent to Seq through OTLP

Use package versions consistent with the solution.
If central package management is used, update `Directory.Packages.props` instead of hard-coding versions inside individual `.csproj` files.

### 2. Serilog startup configuration

Configure Serilog during API startup.

Requirements:

- use Serilog as the logging provider
- read configuration from `appsettings*.json`
- enrich logs from `LogContext`
- include environment/machine/process/thread information
- include trace/span correlation where available
- write logs to Console in Development
- write logs to Seq in Development when configured
- preserve safe default logging if Seq is not running

Application code should continue to use `ILogger<T>`.
Do not replace application logging with direct Serilog static API usage.

### 3. HTTP request logging

Add Serilog request logging middleware.

Requirements:

- log one summary event per HTTP request
- include method, path, status code, elapsed milliseconds
- include `TraceId` and `SpanId` when available
- do not log full request/response bodies
- do not log headers containing tokens, cookies, or secrets

Place the middleware in the correct order in the ASP.NET Core pipeline.

### 4. OpenTelemetry

Configure OpenTelemetry for the API.

Required tracing instrumentation:

- ASP.NET Core incoming requests
- outgoing `HttpClient` calls
- Entity Framework Core database operations, if EF Core is used

Set resource attributes such as:

- service name: `LacunaAuto.Api`
- deployment environment
- application version when available

Configure OTLP export to Seq if an endpoint is present in configuration.
For local Seq over HTTP/protobuf, the trace endpoint is typically:

```text
http://localhost:5341/ingest/otlp/v1/traces
```

Do not hard-code this endpoint in Production configuration.

### 5. Configuration files

Update `appsettings.json` and/or `appsettings.Development.json` as appropriate.

The configuration should include:

- Serilog minimum levels
- Microsoft/System logging overrides
- Console sink
- Seq sink for Development
- Observability service name
- optional OTLP endpoint for traces

Do not add secrets to configuration.

### 6. Docker Compose

If Docker Compose exists, add Seq for local Development unless it is already present.

Recommended local access:

```text
http://localhost:5341
```

Seq should not be required for Production startup.

### 7. Safety rules

Do not log:

- passwords
- password hashes
- access tokens
- refresh tokens
- API keys
- authorization headers
- cookies
- full request/response bodies
- full personal data
- uploaded file contents

Do not enable EF Core sensitive data logging in Production.

### 8. Validation

After implementation, run when applicable:

```bash
dotnet restore LacunaAuto.sln
dotnet build LacunaAuto.sln
dotnet test LacunaAuto.sln
```

If a command cannot be run, explain why.

## Final response requirements

When done, provide:

1. Summary of changed files.
2. Added packages.
3. How to start Seq locally.
4. How to verify logs in Seq.
5. How to verify traces/correlation.
6. Any commands that failed and why.
7. Any manual follow-up steps.

Do not stage or commit changes unless explicitly requested.
