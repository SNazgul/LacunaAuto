# LacunaAuto
Vehicle agregator

A modern platform for posting and searching vehicle advertisements.

Built with .NET 10 using clean architecture and maximum code sharing between web and mobile applications.

---

## 🎯 Project Goals

- Web version with Progressive Web App (PWA) support (installable directly from the browser)
- Full native mobile and desktop applications (Android, iOS, Windows)
- High level of code reuse across all platforms
- Clean, scalable and maintainable architecture

---

## 🛠 Tech Stack

| Layer             | Technology                                      |
|-------------------|-------------------------------------------------|
| Backend           | ASP.NET Core Web API (.NET 10)                  |
| ORM               | Entity Framework Core                           |
| Database          | PostgreSQL                                      |
| Web Frontend      | Blazor WebAssembly Hosted + PWA                 |
| Mobile / Desktop  | Blazor Hybrid (.NET MAUI)                       |
| Shared Components | Blazor Razor Components                         |

---

## 📁 Solution Structure

```bash
AutoRiaClone/
├── src/
│   ├── AutoRiaClone.Api/              # ASP.NET Core Web API
│   ├── AutoRiaClone.Core/             # Models, DTOs, business logic
│   ├── AutoRiaClone.Data/             # EF Core, migrations, repositories
│   ├── AutoRiaClone.Web/              # Blazor WebAssembly hosting
│   ├── AutoRiaClone.Web.Client/       # Blazor WebAssembly client (PWA)
│   ├── AutoRiaClone.Hybrid/           # Blazor Hybrid MAUI (to be added)
│   └── AutoRiaClone.Frontend/         # Legacy MAUI project (optional)
├── test/
├── docker-compose.yml
├── global.json
├── README.md
└── LacunaAuto.sln