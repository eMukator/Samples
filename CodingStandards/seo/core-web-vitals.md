# Core Web Vitals & Výkon — ASP.NET Core

## Cílové hodnoty (Google "Good" threshold)

| Metrika | Cíl       | Kritické |
|---------|-----------|----------|
| LCP     | ≤ 2,5 s   | > 4,0 s  |
| INP     | ≤ 200 ms  | > 500 ms |
| CLS     | ≤ 0,1     | > 0,25   |
| TTFB    | ≤ 800 ms  | > 1800 ms|
| FCP     | ≤ 1,8 s   | > 3,0 s  |

---

## LCP — Largest Contentful Paint

### Pravidla

- LCP element (obvykle hero obrázek nebo H1) musí být v HTML — ne lazy-loaded
- Hero obrázek: přidej `fetchpriority="high"` a **nikdy** `loading="lazy"`
- Přednahraj klíčové fonty a obrázky pomocí `<link rel="preload">`
- Maximalizuj TTFB: response time serveru < 200 ms

```cshtml
@* _Layout.cshtml — hero obrázek *@
<img src="/img/hero.webp"
     alt="Popis hero obrázku"
     width="1200" height="600"
     fetchpriority="high">

@* Preload pro LCP obrázek *@
<link rel="preload" as="image" href="/img/hero.webp" fetchpriority="high">

@* Preload pro kritické fonty *@
<link rel="preload" as="font" type="font/woff2" href="/fonts/inter.woff2" crossorigin>
```

### Response Cache v ASP.NET Core

```csharp
// Program.cs
builder.Services.AddResponseCaching();
builder.Services.AddOutputCache();

app.UseResponseCaching();
app.UseOutputCache();
```

```csharp
// Controller / PageModel
[OutputCache(Duration = 3600)] // 1 hodina
public IActionResult Index() { ... }
```

---

## CLS — Cumulative Layout Shift

### Pravidla

- **Vždy** uváděj `width` a `height` u `<img>`, `<video>`, `<iframe>`
- Rezervuj místo pro reklamy a dynamicky načítaný obsah (min-height)
- Fonty: používej `font-display: swap` nebo `font-display: optional`
- Nepřidávej obsah nad existující obsah (banners, cookies) bez rezervovaného místa

```cshtml
@* Správně — rozměry vždy uvedeny *@
<img src="/img/produkt.webp" alt="Produkt" width="400" height="300" loading="lazy">

@* Špatně — chybí rozměry → layout shift *@
<img src="/img/produkt.webp" alt="Produkt">
```

```css
/* Rezervace místa pro cookie banner */
.cookie-banner {
    min-height: 80px; /* rezervuj prostor i před načtením */
}

/* Font display */
@font-face {
    font-family: 'Inter';
    src: url('/fonts/inter.woff2') format('woff2');
    font-display: swap;
}
```

---

## INP — Interaction to Next Paint

### Pravidla

- Neprovádět synchronní operace na hlavním vláknu při interakci uživatele
- Debounce na input/search handlery (min 150 ms)
- Těžké JS operace přesuň do Web Workers nebo rozděl pomocí `scheduler.yield()`
- Minimalizuj velikost JavaScript bundlů — lazy load vše co není potřeba při prvním vykreslení

---

## Obrázky

- Formát: **WebP** jako primární, AVIF kde je podpora dostupná
- Fallback: JPEG/PNG pro starší prohlížeče pomocí `<picture>`
- Lazy loading: `loading="lazy"` na všechny obrázky **mimo** LCP element a above-the-fold obsah
- Komprese: max 200 KB pro hero obrázky, max 100 KB pro ostatní
- Responzivní obrázky: používej `srcset` a `sizes`

```cshtml
<picture>
    <source srcset="/img/hero.avif" type="image/avif">
    <source srcset="/img/hero.webp" type="image/webp">
    <img src="/img/hero.jpg"
         alt="Popis"
         width="1200" height="600"
         fetchpriority="high">
</picture>

<picture>
    <source srcset="/img/foto.avif" type="image/avif">
    <source srcset="/img/foto.webp" type="image/webp">
    <img src="/img/foto.jpg"
         alt="Popis"
         width="400" height="300"
         loading="lazy">
</picture>
```

---

## CSS & JavaScript

- Critical CSS inline v `<head>` (max 14 KB)
- Necritical CSS: `<link rel="stylesheet" media="print" onload="this.media='all'">` nebo async load
- JavaScript: `defer` nebo `async` na všechny non-critical skripty
- Nikdy neblokuj rendering synchronním JS v `<head>`

```cshtml
@* Správně *@
<script src="/js/main.js" defer></script>
<script src="/js/analytics.js" async></script>

@* Špatně — blokuje rendering *@
<script src="/js/main.js"></script>
```

---

## HTTP Headers pro výkon

```csharp
// Program.cs — přidat response headers
app.Use(async (context, next) =>
{
    context.Response.Headers.Append("X-Content-Type-Options", "nosniff");
    // Cache statických assetů
    if (context.Request.Path.StartsWithSegments("/lib") ||
        context.Request.Path.StartsWithSegments("/css") ||
        context.Request.Path.StartsWithSegments("/js") ||
        context.Request.Path.StartsWithSegments("/img"))
    {
        context.Response.Headers.CacheControl = "public, max-age=31536000, immutable";
    }
    await next();
});
```

## Měření a monitoring

- Pravidelně testuj na: [PageSpeed Insights](https://pagespeed.web.dev/)
- Monitoruj Real User Metrics přes Google Search Console → Core Web Vitals report
- Lokálně testuj přes Lighthouse v Chrome DevTools (throttled 4G, mobile)
