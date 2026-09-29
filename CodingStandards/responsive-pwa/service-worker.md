# Service Worker — Caching & Offline

## Registrace

```javascript
// wwwroot/js/sw-register.js — načíst v _Layout.cshtml
if ('serviceWorker' in navigator) {
  window.addEventListener('load', async () => {
    try {
      const registration = await navigator.serviceWorker.register(
        '/service-worker.js',
        { scope: '/' }
      );

      // Automatický update — nečekej na zavření tabu
      registration.addEventListener('updatefound', () => {
        const newWorker = registration.installing;
        newWorker?.addEventListener('statechange', () => {
          if (newWorker.state === 'installed' && navigator.serviceWorker.controller) {
            showUpdateNotification();  // informuj uživatele o dostupné aktualizaci
          }
        });
      });

      console.log('SW registered:', registration.scope);
    } catch (error) {
      console.error('SW registration failed:', error);
    }
  });
}

function showUpdateNotification() {
  const banner = document.getElementById('update-banner');
  banner?.removeAttribute('hidden');
}
```

```cshtml
@* _Layout.cshtml *@
<script src="/js/sw-register.js" defer></script>

@* Banner pro dostupnou aktualizaci *@
<div id="update-banner" hidden role="alert" aria-live="polite">
  <p>Je dostupná nová verze aplikace.</p>
  <button onclick="location.reload()">Aktualizovat</button>
</div>
```

## Service Worker — strategie cachování

```javascript
// wwwroot/service-worker.js
const CACHE_VERSION = 'v1.0.0';  // zvyšuj při každém deployi
const STATIC_CACHE  = `static-${CACHE_VERSION}`;
const DYNAMIC_CACHE = `dynamic-${CACHE_VERSION}`;

// Soubory cachované při instalaci (shell aplikace)
const PRECACHE_ASSETS = [
  '/',
  '/offline.html',
  '/css/site.css',
  '/js/site.js',
  '/icons/icon-192.png',
  '/icons/icon-512.png',
];

// Install — předcachuj shell
self.addEventListener('install', event => {
  event.waitUntil(
    caches.open(STATIC_CACHE)
      .then(cache => cache.addAll(PRECACHE_ASSETS))
      .then(() => self.skipWaiting())  // aktivuj okamžitě
  );
});

// Activate — vymaž staré cache
self.addEventListener('activate', event => {
  event.waitUntil(
    caches.keys()
      .then(keys => Promise.all(
        keys
          .filter(key => key !== STATIC_CACHE && key !== DYNAMIC_CACHE)
          .map(key => caches.delete(key))
      ))
      .then(() => self.clients.claim())
  );
});

// Fetch — strategie podle typu požadavku
self.addEventListener('fetch', event => {
  const { request } = event;
  const url = new URL(request.url);

  // Ignoruj non-GET a cross-origin requesty
  if (request.method !== 'GET' || url.origin !== location.origin) return;

  // API volání — Network First (čerstvá data, fallback na cache)
  if (url.pathname.startsWith('/api/')) {
    event.respondWith(networkFirst(request, DYNAMIC_CACHE, 5000));
    return;
  }

  // Statické assety (CSS, JS, obrázky) — Cache First
  if (url.pathname.match(/\.(css|js|webp|png|jpg|svg|woff2)$/)) {
    event.respondWith(cacheFirst(request, STATIC_CACHE));
    return;
  }

  // HTML stránky — Network First s offline fallback
  if (request.headers.get('Accept')?.includes('text/html')) {
    event.respondWith(networkFirstWithOffline(request));
    return;
  }
});

// Cache First — rychlé statické assety
async function cacheFirst(request, cacheName) {
  const cached = await caches.match(request);
  if (cached) return cached;

  const response = await fetch(request);
  if (response.ok) {
    const cache = await caches.open(cacheName);
    cache.put(request, response.clone());
  }
  return response;
}

// Network First — API data, s timeout
async function networkFirst(request, cacheName, timeoutMs) {
  const timeoutPromise = new Promise((_, reject) =>
    setTimeout(() => reject(new Error('timeout')), timeoutMs)
  );

  try {
    const response = await Promise.race([fetch(request), timeoutPromise]);
    if (response.ok) {
      const cache = await caches.open(cacheName);
      cache.put(request, response.clone());
    }
    return response;
  } catch {
    return caches.match(request) || new Response(
      JSON.stringify({ error: 'offline', cached: false }),
      { status: 503, headers: { 'Content-Type': 'application/json' } }
    );
  }
}

// Network First pro HTML — s offline fallback stránkou
async function networkFirstWithOffline(request) {
  try {
    const response = await fetch(request);
    if (response.ok) {
      const cache = await caches.open(DYNAMIC_CACHE);
      cache.put(request, response.clone());
    }
    return response;
  } catch {
    return caches.match(request)
      ?? caches.match('/offline.html');
  }
}
```

## Offline stránka

```cshtml
@* wwwroot/offline.html — jednoduchá statická stránka *@
<!DOCTYPE html>
<html lang="cs">
<head>
  <meta charset="UTF-8">
  <meta name="viewport" content="width=device-width, initial-scale=1">
  <title>Jste offline — Název Aplikace</title>
  <style>
    body {
      font-family: system-ui, sans-serif;
      display: grid;
      place-items: center;
      min-height: 100dvh;
      margin: 0;
      padding: 1rem;
      text-align: center;
      background: #f8fafc;
      color: #1e293b;
    }
    h1 { font-size: clamp(1.5rem, 4vw, 2.5rem); }
    button {
      margin-top: 1rem;
      padding: 0.75rem 1.5rem;
      background: #1a56db;
      color: white;
      border: none;
      border-radius: 0.5rem;
      font-size: 1rem;
      cursor: pointer;
      min-height: 44px;
    }
  </style>
</head>
<body>
  <div>
    <h1>Jste offline</h1>
    <p>Zkontrolujte připojení k internetu a zkuste to znovu.</p>
    <button onclick="location.reload()">Zkusit znovu</button>
  </div>
</body>
</html>
```

## Background Sync — odložené akce při offline

```javascript
// Uložení akce do IndexedDB při offline
async function queueAction(action) {
  const db = await openDb();
  await db.add('pending-actions', { ...action, timestamp: Date.now() });

  if ('sync' in self.registration) {
    await self.registration.sync.register('sync-pending-actions');
  }
}

// V Service Workeru
self.addEventListener('sync', event => {
  if (event.tag === 'sync-pending-actions') {
    event.waitUntil(syncPendingActions());
  }
});

async function syncPendingActions() {
  const db = await openDb();
  const actions = await db.getAll('pending-actions');

  for (const action of actions) {
    try {
      await fetch(action.url, { method: action.method, body: action.body });
      await db.delete('pending-actions', action.id);
    } catch {
      break;  // přeruš — stále offline
    }
  }
}
```

## Doporučené cache limity

```javascript
// Pravidelné čištění dynamické cache — max 50 položek nebo 7 dní
async function trimCache(cacheName, maxItems) {
  const cache = await caches.open(cacheName);
  const keys = await cache.keys();
  if (keys.length > maxItems) {
    await cache.delete(keys[0]);  // FIFO
  }
}
```
