Implemented a minimal production service-worker strategy for the hosted Blazor Web App (Interactive WebAssembly) to stop runtime/SRI breakage from standalone-WASM-style offline precaching.

What changed in this task
- Updated service worker registration in src/LacunaAuto.Web/LacunaAuto.Web/Components/App.razor:
  - navigator.serviceWorker.register('service-worker.js', { updateViaCache: 'none' });
- Replaced published worker logic in src/LacunaAuto.Web/LacunaAuto.Web.Client/wwwroot/service-worker.published.js with a minimal install/activate-only worker:
  - removed import of service-worker-assets.js
  - removed offline precache graph logic
  - removed framework/runtime fetch interception and cache-first behavior
  - kept successful install/activate lifecycle only (skipWaiting + clients.claim)
- Updated docs/Development/CURRENT_WORK.md to reflect verified completion of the build rerun and the new minimal-worker decision.

What was preserved
- manifest.webmanifest
- PWA icons
- manifest linkage in host document
- service worker registration
- installable standalone app shell direction (without offline runtime precache)

Why offline support is intentionally deferred
- This project is a hosted .NET 10 Blazor Web App using Interactive WebAssembly, not a standalone Blazor WebAssembly app.
- The standard standalone-WASM offline precache model can conflict with hosted static asset fingerprinting/SRI/runtime resource loading (_framework/resource-collection.*.js and related assets).
- For the current milestone (PWA installability only), the safer and narrower approach is a minimal network-based worker that does not alter framework asset loading.
- Offline behavior will be handled as a separate future feature with hosted-architecture-specific design and validation.

Validation commands and results
1) dotnet build LacunaAuto.sln --nologo
- Result: succeeded (warnings exist in MAUI/Hybrid projects, unrelated to this change).

2) dotnet publish src/LacunaAuto.Web/LacunaAuto.Web/LacunaAuto.Web.csproj -c Release
- Result: succeeded.

Published output verification
- Verified published service worker at:
  src/LacunaAuto.Web/LacunaAuto.Web/bin/Release/net10.0/publish/wwwroot/service-worker.js
- Verified it does NOT reference:
  - service-worker-assets.js
  - asset hashes/assetsManifest/integrity mapping logic
  - CacheStorage precache logic
- Verified published PWA artifacts are still present:
  - manifest.webmanifest
  - service-worker.js
  - service-worker-assets.js (still emitted; not used by the minimal worker)
  - icon-192.png
  - icon-512.png

Scope boundaries respected
- No UI redesign or behavior change work.
- No localization/backend/API/database/auth/Hybrid changes.
- No generated service-worker-assets.js edits.
- PWA installability is not marked user/browser accepted yet.
- No commit performed.

Exact files changed in this task
- src/LacunaAuto.Web/LacunaAuto.Web/Components/App.razor
- src/LacunaAuto.Web/LacunaAuto.Web.Client/wwwroot/service-worker.published.js
- docs/Development/CURRENT_WORK.md
- docs/CURRENT_WORK/Agent_Response.md
