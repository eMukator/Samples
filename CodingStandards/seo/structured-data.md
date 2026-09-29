# Strukturovaná Data — Schema.org

## Formát

Vždy používej **JSON-LD** (nikoli Microdata nebo RDFa). Vkládej do `<head>` nebo těsně před `</body>`.

```cshtml
@* _Layout.cshtml nebo konkrétní View *@
@if (ViewData["SchemaOrg"] != null)
{
    <script type="application/ld+json">
        @Html.Raw(ViewData["SchemaOrg"])
    </script>
}
```

## Povinné typy podle kontextu stránky

### Organization / WebSite (homepage)

```json
{
  "@context": "https://schema.org",
  "@graph": [
    {
      "@type": "Organization",
      "@id": "https://example.com/#organization",
      "name": "Název firmy",
      "url": "https://example.com",
      "logo": {
        "@type": "ImageObject",
        "url": "https://example.com/img/logo.png",
        "width": 200,
        "height": 60
      },
      "contactPoint": {
        "@type": "ContactPoint",
        "telephone": "+420-XXX-XXX-XXX",
        "contactType": "customer service"
      }
    },
    {
      "@type": "WebSite",
      "@id": "https://example.com/#website",
      "url": "https://example.com",
      "name": "Název webu",
      "publisher": { "@id": "https://example.com/#organization" },
      "potentialAction": {
        "@type": "SearchAction",
        "target": "https://example.com/search?q={search_term_string}",
        "query-input": "required name=search_term_string"
      }
    }
  ]
}
```

### BreadcrumbList (všechny podstránky)

```json
{
  "@context": "https://schema.org",
  "@type": "BreadcrumbList",
  "itemListElement": [
    { "@type": "ListItem", "position": 1, "name": "Domů", "item": "https://example.com" },
    { "@type": "ListItem", "position": 2, "name": "Kategorie", "item": "https://example.com/kategorie" },
    { "@type": "ListItem", "position": 3, "name": "Název stránky" }
  ]
}
```

### Article / BlogPosting (články)

```json
{
  "@context": "https://schema.org",
  "@type": "Article",
  "headline": "Nadpis článku",
  "description": "Popis článku",
  "image": "https://example.com/img/clanek.jpg",
  "datePublished": "2025-01-01T00:00:00+01:00",
  "dateModified": "2025-06-01T00:00:00+01:00",
  "author": {
    "@type": "Person",
    "name": "Jméno autora"
  },
  "publisher": { "@id": "https://example.com/#organization" }
}
```

### Product (e-shop, produktové stránky)

```json
{
  "@context": "https://schema.org",
  "@type": "Product",
  "name": "Název produktu",
  "image": ["https://example.com/img/produkt.jpg"],
  "description": "Popis produktu",
  "sku": "SKU123",
  "brand": { "@type": "Brand", "name": "Značka" },
  "offers": {
    "@type": "Offer",
    "url": "https://example.com/produkt",
    "priceCurrency": "CZK",
    "price": "999.00",
    "availability": "https://schema.org/InStock"
  },
  "aggregateRating": {
    "@type": "AggregateRating",
    "ratingValue": "4.5",
    "reviewCount": "89"
  }
}
```

### FAQ (stránky s otázkami)

```json
{
  "@context": "https://schema.org",
  "@type": "FAQPage",
  "mainEntity": [
    {
      "@type": "Question",
      "name": "Otázka?",
      "acceptedAnswer": {
        "@type": "Answer",
        "text": "Odpověď."
      }
    }
  ]
}
```

## Validace

- Vždy testuj pomocí [Google Rich Results Test](https://search.google.com/test/rich-results)
- Testuj pomocí [Schema.org Validator](https://validator.schema.org/)
- JSON-LD musí být validní JSON — escapuj speciální znaky v C# před vložením do ViewData

## ASP.NET Core helper — bezpečné escapování

```csharp
// V Controlleru nebo PageModel
ViewData["SchemaOrg"] = System.Text.Json.JsonSerializer.Serialize(schemaObject, new JsonSerializerOptions
{
    Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    WriteIndented = false
});
```
