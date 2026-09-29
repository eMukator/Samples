# SEO Web Standards — ASP.NET Core

Tato sada pravidel definuje standardy pro SEO implementaci v ASP.NET Core (Razor Pages / MVC).
Vždy aplikuj VŠECHNA pravidla níže, pokud není explicitně řečeno jinak.

## Moduly

@meta-tags.md
@structured-data.md
@sitemap-robots.md
@core-web-vitals.md
@on-page-seo.md
@a11y-seo.md

## Rychlá kontrola (před každým PR)

- [ ] Každá stránka má unikátní `<title>` (50–60 znaků)
- [ ] Každá stránka má unikátní `<meta name="description">` (120–158 znaků)
- [ ] Canonical URL je nastavena
- [ ] Obrázky mají `alt` atributy
- [ ] Je přítomen právě jeden `<h1>` na stránku
- [ ] Sitemap je aktuální a odkazuje na ni `robots.txt`
- [ ] Stránka se načte do 2,5 s (LCP)
- [ ] Nedochází k layout shiftu (CLS < 0,1)
