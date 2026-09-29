# PWA — Manifest & Instalovatelnost

## Web App Manifest

Soubor `wwwroot/manifest.webmanifest`:

```json
{
  "name": "Název Aplikace",
  "short_name": "Zkratka",
  "description": "Stručný popis aplikace (max 100 znaků)",
  "start_url": "/",
  "scope": "/",
  "display": "standalone",
  "display_override": ["window-controls-overlay", "standalone", "minimal-ui"],
  "orientation": "any",
  "theme_color": "#1a56db",
  "background_color": "#ffffff",
  "lang": "cs",
  "dir": "ltr",

  "icons": [
    { "src": "/icons/icon-72.png",   "sizes": "72x72",   "type": "image/png", "purpose": "any" },
    { "src": "/icons/icon-96.png",   "sizes": "96x96",   "type": "image/png", "purpose": "any" },
    { "src": "/icons/icon-128.png",  "sizes": "128x128", "type": "image/png", "purpose": "any" },
    { "src": "/icons/icon-192.png",  "sizes": "192x192", "type": "image/png", "purpose": "any maskable" },
    { "src": "/icons/icon-512.png",  "sizes": "512x512", "type": "image/png", "purpose": "any maskable" },
    { "src": "/icons/icon.svg",      "sizes": "any",     "type": "image/svg+xml", "purpose": "any" }
  ],

  "screenshots": [
    {
      "src": "/screenshots/desktop.webp",
      "sizes": "1280x720",
      "type": "image/webp",
      "form_factor": "wide",
      "label": "Přehled aplikace na desktopu"
    },
    {
      "src": "/screenshots/mobile.webp",
      "sizes": "390x844",
      "type": "image/webp",
      "form_factor": "narrow",
      "label": "Přehled aplikace na mobilu"
    }
  ],

  "shortcuts": [
    {
      "name": "Nová objednávka",
      "short_name": "Objednávka",
      "url": "/orders/new",
      "icons": [{ "src": "/icons/shortcut-order.png", "sizes": "96x96" }]
    }
  ],

  "categories": ["business", "productivity"],

  "share_target": {
    "action": "/share",
    "method": "POST",
    "enctype": "multipart/form-data",
    "params": {
      "title": "title",
      "text": "text",
      "url": "url"
    }
  }
}
```

## Odkaz z HTML

```cshtml
@* _Layout.cshtml — v <head> *@
<link rel="manifest" href="/manifest.webmanifest">
<meta name="theme-color" content="#1a56db">
<meta name="theme-color" content="#0f172a" media="(prefers-color-scheme: dark)">

@* iOS specifické — Safari nepodporuje manifest plně *@
<meta name="apple-mobile-web-app-capable" content="yes">
<meta name="apple-mobile-web-app-status-bar-style" content="black-translucent">
<meta name="apple-mobile-web-app-title" content="Zkratka">
<link rel="apple-touch-icon" href="/icons/icon-192.png">

@* Windows tiles *@
<meta name="msapplication-TileImage" content="/icons/icon-144.png">
<meta name="msapplication-TileColor" content="#1a56db">
```

## Ikony — požadavky

- Formáty: PNG (povinné) + SVG (doporučené)
- **Maskable ikona**: obsah musí být v 80% kruhu středu (safe zone)
- Testuj maskable ikonu na: [maskable.app](https://maskable.app)
- Minimálně: 192×192 a 512×512 PNG

## Install prompt — vlastní UI

```javascript
// pwa-install.js
let deferredPrompt = null;
const installBtn = document.getElementById('install-btn');

window.addEventListener('beforeinstallprompt', event => {
  event.preventDefault();
  deferredPrompt = event;

  // Zobraz vlastní tlačítko instalace
  installBtn?.removeAttribute('hidden');
});

installBtn?.addEventListener('click', async () => {
  if (!deferredPrompt) return;

  deferredPrompt.prompt();
  const { outcome } = await deferredPrompt.userChoice;

  console.log(`Install prompt outcome: ${outcome}`);
  deferredPrompt = null;
  installBtn.setAttribute('hidden', '');
});

// Skryj tlačítko po instalaci
window.addEventListener('appinstalled', () => {
  installBtn?.setAttribute('hidden', '');
  deferredPrompt = null;
});

// Detekce — je aplikace nainstalována?
function isInstalledPwa() {
  return window.matchMedia('(display-mode: standalone)').matches
    || window.navigator.standalone === true;
}
```

```cshtml
@* Tlačítko instalace — skryté dokud není k dispozici *@
<button id="install-btn" hidden aria-label="Nainstalovat aplikaci">
  <svg aria-hidden="true">...</svg>
  Přidat na plochu
</button>
```

## Serving manifest z ASP.NET Core

```csharp
// Program.cs — správný MIME type pro .webmanifest
app.UseStaticFiles(new StaticFileOptions
{
    ContentTypeProvider = new FileExtensionContentTypeProvider
    {
        Mappings =
        {
            [".webmanifest"] = "application/manifest+json"
        }
    }
});
```
