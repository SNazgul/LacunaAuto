// Minimal production service worker for installability only.
// Offline caching is intentionally deferred for the hosted Blazor Web App architecture.
self.addEventListener('install', function (event)
{
    event.waitUntil(self.skipWaiting());
});

self.addEventListener('activate', function (event)
{
    event.waitUntil(self.clients.claim());
});