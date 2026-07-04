# Logging Guidelines

## General Principles

- Use **Serilog** as the main structured logging library.
- Use **OpenTelemetry** for automatic tracing of HTTP requests and Entity Framework Core queries.
- All logs must be structured (JSON).
- Always include `TraceId` and `SpanId` for request correlation.
- In Development: send logs to **Seq** + Console.
- In Production: plan to export logs to Seq / Azure Monitor / Grafana.
- Follow the principle: **"Log what is useful for debugging and monitoring, not everything"**.

## What Must Be Logged

| Level       | What to Log                                      | Required? |
|-------------|--------------------------------------------------|---------|
| Information | HTTP requests and responses (method, path, status, duration) | Yes |
| Information | Database query execution time (via OpenTelemetry) | Yes |
| Error       | All unhandled exceptions                         | Yes |
| Warning     | Business rule violations and suspicious actions  | Yes |
| Debug       | Detailed SQL queries (only in Development)       | No  |
| Information | Important business operations                    | Recommended |

## Property Naming

Use **PascalCase** for log properties:
- `UserId`, `CarId`, `ElapsedMilliseconds`, `StatusCode`, `TraceId`, `SpanId`

## What Should NOT Be Logged

- Sensitive data (passwords, tokens, full personal data)
- Verbose internal method calls in production
- Large request/response bodies unless explicitly needed

## Tools

| Environment  | Destination          | Tool     |
|--------------|----------------------|----------|
| Development  | Local machine        | **Seq**  |
| Production   | Centralized system   | Seq / Grafana / Azure Monitor |