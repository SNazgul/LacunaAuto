# LacunaAuto

Vehicle aggregator.

A modern platform for posting and searching vehicle advertisements.

Built with .NET 10 using clean architecture principles and maximum code sharing between web and mobile applications.

---

## 🎯 Project Goals

- Web version with Progressive Web App (PWA) support, installable directly from the browser
- Full native mobile and desktop applications for Android, iOS and Windows
- High level of code reuse across all platforms
- Clean, scalable and maintainable architecture
- AI-friendly project structure for assisted development with GitHub Copilot, Claude Code and similar tools

---

## 🛠 Tech Stack

| Layer             | Technology                         |
|-------------------|------------------------------------|
| Backend           | ASP.NET Core Web API (.NET 10)     |
| ORM               | Entity Framework Core              |
| Database          | PostgreSQL                         |
| Web Frontend      | Blazor WebAssembly Hosted + PWA    |
| Mobile / Desktop  | Blazor Hybrid (.NET MAUI)          |
| Shared UI         | Blazor Razor Components            |
| Logging           | Serilog + OpenTelemetry + Seq      |
| Local Development | Docker Compose                     |

---

## 📁 Solution Structure

```text
LacunaAuto/
├── src/
│   ├── LacunaAuto.Api/              # ASP.NET Core Web API
│   ├── LacunaAuto.Core/             # Models, DTOs, business logic
│   ├── LacunaAuto.Data/             # EF Core, migrations, repositories
│   ├── LacunaAuto.Web/              # Blazor WebAssembly hosting
│   ├── LacunaAuto.Web.Client/       # Blazor WebAssembly client (PWA)
│   ├── LacunaAuto.Hybrid/           # Blazor Hybrid MAUI (planned)
│   └── LacunaAuto.Frontend/         # Legacy MAUI project (optional)
├── test/
│   └── LacunaAuto.Api.Test/         # API tests
├── docs/
│   └── AI/
│       └── Rules/
│           └── LOGGING.md           # Logging and observability rules
├── .github/
│   └── prompts/                     # Reusable AI prompts
├── docker-compose.yml
├── global.json
├── README.md
├── AGENTS.md
├── CLAUDE.md
└── LacunaAuto.sln
```

---

## 🧱 Project Responsibilities

### `LacunaAuto.Api`

ASP.NET Core Web API backend.

Responsible for:

- HTTP endpoints
- API configuration
- Authentication and authorization
- Request validation
- OpenAPI documentation
- Dependency injection composition
- Logging and observability setup

---

### `LacunaAuto.Core`

Core business layer.

Responsible for:

- Domain models
- DTOs used by the application
- Business logic
- Interfaces
- Business services
- Shared validation rules

> Note: If the solution grows, public API/client DTOs may later be moved into a separate `LacunaAuto.Contracts` project.

---

### `LacunaAuto.Data`

Data access layer.

Responsible for:

- Entity Framework Core `DbContext`
- Entity configurations
- PostgreSQL integration
- Database migrations
- Repository implementations
- Seed data

---

### `LacunaAuto.Web`

Blazor WebAssembly hosted server project.

Responsible for:

- Hosting the Blazor WebAssembly client
- Serving static frontend files
- Web-specific startup and configuration

---

### `LacunaAuto.Web.Client`

Main web frontend.

Responsible for:

- Blazor WebAssembly UI
- PWA support
- Pages and components
- Client-side routing
- Browser-specific frontend logic

---

### `LacunaAuto.Hybrid`

Planned Blazor Hybrid MAUI application.

Responsible for:

- Android application
- iOS application
- Windows desktop application
- Reusing Blazor components where possible

---

### `LacunaAuto.Frontend`

Legacy MAUI project.

This project is optional and may be removed or archived later if `LacunaAuto.Hybrid` becomes the final mobile and desktop application.

---

## 🚀 Local Development

### Requirements

- .NET 10 SDK
- Docker Desktop
- Visual Studio 2026, Visual Studio Code or Rider
- PostgreSQL client is optional

---

### Start local infrastructure

```bash
docker-compose up -d
```

This starts local development services such as:

- PostgreSQL
- Seq

---

### Restore packages

```bash
dotnet restore LacunaAuto.sln
```

---

### Build solution

```bash
dotnet build LacunaAuto.sln
```

---

### Run tests

```bash
dotnet test LacunaAuto.sln
```

---

### Run backend API

```bash
dotnet run --project src/LacunaAuto.Api
```

---

### Run Blazor web application

```bash
dotnet run --project src/LacunaAuto.Web
```

---

## 🐘 Local PostgreSQL

Default local PostgreSQL settings:

```text
Host: localhost
Port: 5432
Database: lacunaauto
Username: lacunaauto
Password: lacunaauto_dev_password
```

Connection string example:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=lacunaauto;Username=lacunaauto;Password=lacunaauto_dev_password"
  }
}
```

---

## 📊 Local Seq

Seq is used for local structured log viewing.

Default local URL:

```text
http://localhost:5341
```

---

## 🧠 AI Agent Instructions

Repository-level AI instructions are stored in:

```text
AGENTS.md
```

Claude Code specific instructions are stored in:

```text
CLAUDE.md
```

Logging and observability rules are stored in:

```text
docs/AI/Rules/LOGGING.md
```

Reusable prompt files are stored in:

```text
.github/prompts/
```

---

## ✅ Validation Commands

Before finishing development tasks, run when applicable:

```bash
dotnet restore LacunaAuto.sln
dotnet build LacunaAuto.sln
dotnet test LacunaAuto.sln
```

If a command cannot be run, explain why.

---

## 📝 Development Notes

- Keep changes minimal and focused.
- Do not mix UI, business logic and data access responsibilities.
- Prefer code reuse between Blazor WebAssembly and Blazor Hybrid.
- Keep AI instructions and prompt files up to date as the project structure evolves.
- Do not modify test projects unless the task explicitly requires writing or fixing tests.
