# Sitemap & Robots.txt — ASP.NET Core

## Sitemap

### Povinná pravidla

- Sitemap musí být dostupná na `/sitemap.xml`
- Maximálně 50 000 URL na soubor (při více použij sitemap index)
- Vždy odkazuj na sitemap z `robots.txt`
- Zahrnuj pouze indexovatelné URL (bez `noindex` stránek)
- Nezahrnuj: admin stránky, thank-you stránky, search results, paginated URL (kromě první stránky)
- Vždy používej absolutní URL s preferovanou doménou (www nebo bez www — konzistentně)

### Implementace pomocí NuGet balíčku

Doporučený balíček: `AspNetCore.SitemapMiddleware` nebo vlastní implementace.

**Vlastní implementace (doporučeno pro kontrolu):**

```csharp
// SitemapController.cs
[Route("sitemap.xml")]
public class SitemapController : Controller
{
    private readonly ISitemapService _sitemapService;

    public SitemapController(ISitemapService sitemapService)
        => _sitemapService = sitemapService;

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var urls = await _sitemapService.GetUrlsAsync();
        var xml = BuildSitemapXml(urls);
        return Content(xml, "application/xml");
    }

    private static string BuildSitemapXml(IEnumerable<SitemapUrl> urls)
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
        sb.AppendLine("<urlset xmlns=\"http://www.sitemaps.org/schemas/sitemap/0.9\">");
        foreach (var url in urls)
        {
            sb.AppendLine("  <url>");
            sb.AppendLine($"    <loc>{System.Security.SecurityElement.Escape(url.Loc)}</loc>");
            if (url.LastMod.HasValue)
                sb.AppendLine($"    <lastmod>{url.LastMod:yyyy-MM-dd}</lastmod>");
            sb.AppendLine($"    <changefreq>{url.ChangeFreq ?? "weekly"}</changefreq>");
            sb.AppendLine($"    <priority>{url.Priority ?? 0.8:F1}</priority>");
            sb.AppendLine("  </url>");
        }
        sb.AppendLine("</urlset>");
        return sb.ToString();
    }
}

public record SitemapUrl(string Loc, DateTime? LastMod, string? ChangeFreq, double? Priority);
```

### Priority hodnoty (vodítko)

| Typ stránky        | Priority |
|--------------------|----------|
| Homepage           | 1.0      |
| Hlavní kategorie   | 0.8      |
| Podkategorie       | 0.6      |
| Produkty / články  | 0.6      |
| Ostatní stránky    | 0.4      |

### Changefreq hodnoty

| Typ obsahu         | Changefreq |
|--------------------|------------|
| Homepage           | daily      |
| Blog / novinky     | weekly     |
| Produkty           | weekly     |
| Statické stránky   | monthly    |

---

## Robots.txt

- Musí být dostupný na `/robots.txt`
- Musí obsahovat odkaz na sitemap
- Blokuj neindexovatelné sekce explicitně

### Minimální šablona

```
User-agent: *
Disallow: /admin/
Disallow: /account/
Disallow: /cart/
Disallow: /checkout/
Disallow: /search
Disallow: /thank-you
Allow: /

Sitemap: https://example.com/sitemap.xml
```

### Implementace jako statický soubor

Umísti `robots.txt` do `wwwroot/robots.txt`. Ověř, že je middleware pro statické soubory aktivní:

```csharp
// Program.cs
app.UseStaticFiles(); // musí být před UseRouting
```

### Dynamický robots.txt (pokud potřebuješ prostředí-specifické chování)

```csharp
// RobotsController.cs
[Route("robots.txt")]
public class RobotsController : Controller
{
    private readonly IWebHostEnvironment _env;

    public RobotsController(IWebHostEnvironment env) => _env = env;

    [HttpGet]
    public IActionResult Index()
    {
        var content = _env.IsProduction()
            ? "User-agent: *\nDisallow: /admin/\nSitemap: https://example.com/sitemap.xml"
            : "User-agent: *\nDisallow: /";  // blokuj vše na staging/dev

        return Content(content, "text/plain");
    }
}
```

## Ověření po nasazení

- [ ] `https://example.com/sitemap.xml` vrací validní XML
- [ ] `https://example.com/robots.txt` obsahuje `Sitemap:` direktivu
- [ ] Google Search Console → Sitemaps → přidat sitemap URL
- [ ] Na staging/dev prostředí je celý web v `Disallow: /`
