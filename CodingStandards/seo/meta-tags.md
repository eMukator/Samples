# Meta Tagy — ASP.NET Core

## Title tag

- Délka: 50–60 znaků (Google zobrazí max ~60)
- Formát: `{Název stránky} | {Název webu}`
- Musí být unikátní napříč celým webem
- Obsahuje primární klíčové slovo co nejblíže začátku
- Nikdy nepoužívej výchozí fallback jako "Untitled" nebo název projektu

```cshtml
@* _Layout.cshtml *@
<title>@(ViewData["Title"] != null ? $"{ViewData["Title"]} | NázevWebu" : "NázevWebu")</title>
```

```cshtml
@* Každý View musí nastavit: *@
@{
    ViewData["Title"] = "Konkrétní název stránky";
}
```

## Meta Description

- Délka: 120–158 znaků
- Unikátní pro každou stránku
- Obsahuje výzvu k akci (CTA) tam kde je to přirozené
- Nikdy neopakuj doslovně text z `<title>`

```cshtml
@* _Layout.cshtml *@
<meta name="description" content="@(ViewData["MetaDescription"] ?? "Výchozí popis webu — max 158 znaků.")">
```

## Canonical URL

- Každá veřejná stránka musí mít canonical
- Používej absolutní URL vždy (ne relativní)
- Canonical musí odpovídat preferované verzi URL (www vs. bez www, trailing slash)

```cshtml
@* _Layout.cshtml *@
<link rel="canonical" href="@(ViewData["CanonicalUrl"] ?? $"{Context.Request.Scheme}://{Context.Request.Host}{Context.Request.Path}")">
```

## Open Graph a Twitter Cards

Povinné pro všechny veřejné stránky:

```cshtml
@* _Layout.cshtml *@
<meta property="og:title" content="@(ViewData["Title"] ?? "NázevWebu")">
<meta property="og:description" content="@(ViewData["MetaDescription"] ?? "Výchozí popis")">
<meta property="og:url" content="@(ViewData["CanonicalUrl"] ?? $"{Context.Request.Scheme}://{Context.Request.Host}{Context.Request.Path}")">
<meta property="og:type" content="@(ViewData["OgType"] ?? "website")">
<meta property="og:image" content="@(ViewData["OgImage"] ?? $"{Context.Request.Scheme}://{Context.Request.Host}/img/og-default.jpg")">
<meta property="og:image:width" content="1200">
<meta property="og:image:height" content="630">
<meta property="og:locale" content="cs_CZ">

<meta name="twitter:card" content="summary_large_image">
<meta name="twitter:title" content="@(ViewData["Title"] ?? "NázevWebu")">
<meta name="twitter:description" content="@(ViewData["MetaDescription"] ?? "Výchozí popis")">
<meta name="twitter:image" content="@(ViewData["OgImage"] ?? $"{Context.Request.Scheme}://{Context.Request.Host}/img/og-default.jpg")">
```

## Robots meta tag

```cshtml
@* Výchozí — indexovatelné stránky *@
<meta name="robots" content="index, follow">

@* Stránky které NESMÍ být indexovány (admin, thank-you, search results): *@
@* ViewData["NoIndex"] = true; — nastav ve View nebo Controlleru *@
@if (ViewData["NoIndex"] is true)
{
    <meta name="robots" content="noindex, nofollow">
}
```

## Hreflang (vícejazyčné weby)

Pokud web existuje ve více jazycích:

```cshtml
<link rel="alternate" hreflang="cs" href="https://example.com/cs/@ViewData["Slug"]">
<link rel="alternate" hreflang="en" href="https://example.com/en/@ViewData["Slug"]">
<link rel="alternate" hreflang="x-default" href="https://example.com/@ViewData["Slug"]">
```

## Zakázané praktiky

- Neplnit `<title>` a `<meta description>` klíčovými slovy oddělené čárkami
- Nepoužívat stejný description na více stránkách
- Neskrývat meta description (display:none nebo visibility:hidden)
- Nepřesahovat 60 znaků v title (Google title ořízne)
