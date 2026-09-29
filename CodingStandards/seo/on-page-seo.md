# On-Page SEO — ASP.NET Core

## Struktura nadpisů (Headings)

### Povinná pravidla

- **Přesně jeden `<h1>` na stránku** — vždy, bez výjimek
- `<h1>` musí obsahovat primární klíčové slovo stránky
- `<h1>` nesmí být shodný s `<title>` (ale může být podobný)
- Hierarchie nadpisů musí být logická: h1 → h2 → h3 (nikdy nepřeskakuj úrovně)
- Nadpisy popisují obsah sekce — nejsou jen vizuální prvky

```cshtml
@* Správně *@
<h1>Průvodce výběrem notebooku pro studenty</h1>
  <h2>Podle rozpočtu</h2>
    <h3>Do 15 000 Kč</h3>
    <h3>15 000–25 000 Kč</h3>
  <h2>Podle použití</h2>

@* Špatně — h1 v _Layout.cshtml (logo/název webu jako h1 na každé stránce) *@
<h1><img src="/logo.png" alt="Název webu"></h1>
```

---

## URL struktura

- Krátké, popisné, klíčové slovo v URL
- Pouze malá písmena, slova oddělená pomlčkou `-` (nikoli podtržítkem `_`)
- Bez diakritiky v URL (slug normalizuj při vytváření)
- Maximální délka: 75 znaků (bez domény)
- Konzistentní trailing slash (buď vždy nebo nikdy — canonical to řeší)

```
✓ /navody/vybir-notebook-pro-studenty
✓ /produkty/kancelarsky-stul-bily-180cm
✗ /Navody/VybirNotebookProStudenty
✗ /navody/vyb%C3%ADr-notebook-pro-studenty
✗ /p?id=123
```

### Normalizace slugů v ASP.NET Core

```csharp
public static string ToSlug(string text)
{
    // Normalizace diakritiky
    var normalized = text.Normalize(System.Text.NormalizationForm.FormD);
    var sb = new System.Text.StringBuilder();
    foreach (var c in normalized)
    {
        if (System.Globalization.CharUnicodeInfo.GetUnicodeCategory(c)
            != System.Globalization.UnicodeCategory.NonSpacingMark)
            sb.Append(c);
    }

    return sb.ToString()
        .Normalize(System.Text.NormalizationForm.FormC)
        .ToLowerInvariant()
        .Replace(" ", "-")
        .Replace("_", "-")
        .Replace("--", "-")                            // eliminuj dvojité pomlčky
        .Trim('-');
}
```

---

## Interní linking

- Každá stránka musí být dosažitelná maximálně **3 kliknutími** od homepage
- Používej popisný anchor text (ne "klikněte zde", "více informací")
- Anchor text má obsahovat klíčové slovo cílové stránky
- Orphan pages (stránky bez příchozích interních odkazů) jsou zakázány
- Breadcrumbs jsou povinné na všech stránkách kromě homepage

```cshtml
@* Správně *@
<a href="/navody/vybir-notebook">Průvodce výběrem notebooku</a>

@* Špatně *@
<a href="/navody/vybir-notebook">Klikněte zde</a>
<a href="/navody/vybir-notebook">Více informací</a>
```

### Breadcrumbs — Razor Pages / MVC

```cshtml
@* Partial _Breadcrumbs.cshtml *@
@model IEnumerable<BreadcrumbItem>

<nav aria-label="Breadcrumb">
    <ol class="breadcrumb">
        @foreach (var (item, index) in Model.Select((x, i) => (x, i)))
        {
            <li class="breadcrumb-item @(index == Model.Count() - 1 ? "active" : "")">
                @if (index < Model.Count() - 1)
                {
                    <a href="@item.Url">@item.Title</a>
                }
                else
                {
                    <span aria-current="page">@item.Title</span>
                }
            </li>
        }
    </ol>
</nav>
```

---

## Obsah stránky

- Minimální délka textu na stránce: 300 slov (pro indexovatelné stránky)
- Primární klíčové slovo se vyskytuje: v `<h1>`, v prvním odstavci, přirozeně v textu
- Hustota klíčového slova: 1–2 % (nepřesahuj — keyword stuffing je penalizován)
- Thin content (stránky s méně než 200 slovy bez zřejmého důvodu) označit jako `noindex`

---

## Paginace

- Nepoužívej `rel="next"` / `rel="prev"` (Google již nepodporuje)
- Canonical na paginated stránkách: každá stránka má vlastní canonical (ne na první stránku)
- `?page=1` nebo `/strana/1` — přesměruj na základní URL (301)
- Paginated stránky zahrnuj do sitemaps (s nižší priority hodnotou)

---

## 301 Redirecty

- Při změně URL vždy nastav 301 redirect ze staré URL
- Redirecty zaznamenávej do centrálního souboru nebo databáze
- Maximální délka redirect řetězce: 2 přesměrování

```csharp
// Program.cs — statické redirecty
app.UseRewriter(new RewriteOptions()
    .AddRedirectToHttpsPermanent()
    .AddRedirect("stare-url/(.*)", "nova-url/$1", 301));
```

---

## Zakázané praktiky

- Skrytý text (bílý text na bílém pozadí, `display:none` pro obsah)
- Cloaking (různý obsah pro Googleboty a uživatele)
- Keyword stuffing
- Duplicate content bez canonical
- Automaticky generované stránky bez hodnoty pro uživatele
