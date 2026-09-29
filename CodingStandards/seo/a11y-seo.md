# Přístupnost (a11y) jako součást SEO — ASP.NET Core

Google používá přístupnost jako ranking signal. Dodržování těchto pravidel zároveň zlepšuje SEO i uživatelský zážitek.

## Obrázky a alt texty

### Pravidla

- **Každý `<img>` musí mít `alt` atribut** — bez výjimek
- Dekorativní obrázky: `alt=""` (prázdný string, nikoli vynechaný atribut)
- Informační obrázky: stručný, výstižný popis obsahu obrázku
- Obrázky s textem: alt musí obsahovat přesný text z obrázku
- Délka alt textu: max 125 znaků

```cshtml
@* Informační obrázek *@
<img src="/img/graf-prodeje.webp"
     alt="Graf prodeje 2024: meziroční nárůst 23 %"
     width="800" height="400" loading="lazy">

@* Dekorativní obrázek *@
<img src="/img/dekorativni-oddelovac.webp" alt="" width="1200" height="4">

@* Logo v odkazu — alt popisuje cíl odkazu *@
<a href="/">
    <img src="/img/logo.webp" alt="Název webu — přejít na homepage" width="150" height="50">
</a>
```

---

## Sémantické HTML

Správné sémantické tagy pomáhají Googlu pochopit strukturu stránky.

```cshtml
@* Správná struktura stránky *@
<header>
    <nav aria-label="Hlavní navigace">
        <ul>
            <li><a href="/">Domů</a></li>
            <li><a href="/produkty">Produkty</a></li>
        </ul>
    </nav>
</header>

<main>
    <article>
        <h1>Název článku</h1>
        <p>Obsah...</p>
    </article>

    <aside aria-label="Související články">
        <h2>Mohlo by vás zajímat</h2>
    </aside>
</main>

<footer>
    <nav aria-label="Patičkové menu">...</nav>
</footer>
```

### Zakázané praktiky

- Nepoužívej `<div>` nebo `<span>` jako tlačítka bez `role="button"` a `tabindex`
- Nepoužívej `<table>` pro layout
- Nadpisy nejsou stylové prvky — nepoužívej `<h3>` jen proto, že se hodí velikostí

---

## Odkazy

- Každý odkaz musí mít smysluplný anchor text (viz on-page-seo.md)
- Slepé odkazy (`<a href="#">`) pouze s JavaScriptovým handlerm a `role="button"`
- Vnější odkazy na nedůvěryhodné weby: `rel="noopener noreferrer"`
- Vnější komerční/placené odkazy: `rel="nofollow sponsored"`

```cshtml
@* Vnější odkaz *@
<a href="https://external.com" rel="noopener noreferrer" target="_blank">
    Odkaz na externí zdroj
    <span class="visually-hidden">(otevře se v novém okně)</span>
</a>
```

---

## Formuláře

- Každý `<input>` musí mít přidružený `<label>` (pomocí `for`/`id` nebo wrappingu)
- Placeholder není náhrada za label
- Chybové hlášky musí být programově asociovány s polem (`aria-describedby`)

```cshtml
@* Správně *@
<div class="form-group">
    <label for="email">E-mailová adresa</label>
    <input type="email" id="email" name="email"
           aria-describedby="email-error"
           autocomplete="email">
    <span id="email-error" class="error" role="alert">
        @Html.ValidationMessageFor(m => m.Email)
    </span>
</div>

@* Špatně *@
<input type="email" placeholder="E-mail">
```

---

## Barvy a kontrast

- Minimální kontrastní poměr textu: **4,5:1** (normální text), **3:1** (velký text ≥18px bold nebo ≥24px)
- Informace nesmí být předávána pouze barvou (přidej icon nebo text)
- Testuj kontrast na: [WebAIM Contrast Checker](https://webaim.org/resources/contrastchecker/)

---

## Klávesová navigace a focus

- Celý web musí být ovladatelný pouze klávesnicí
- Viditelný focus indicator na všech interaktivních prvcích (ne `outline: none` bez náhrady)
- Logické pořadí tabování odpovídá vizuálnímu pořadí

```css
/* Nikdy nemazat focus bez náhrady */
:focus {
    outline: 2px solid #005fcc;
    outline-offset: 2px;
}
```

---

## Jazyk stránky

```cshtml
@* _Layout.cshtml — vždy uváděj lang atribut *@
<html lang="cs">

@* Vícejazyčný web *@
<html lang="@(ViewData["Lang"] ?? "cs")">
```

---

## Skip navigation

Povinné pro weby s opakující se navigací:

```cshtml
@* Úplně první element v <body> *@
<a href="#main-content" class="skip-link visually-hidden-focusable">
    Přejít na hlavní obsah
</a>

@* ... navigace ... *@

<main id="main-content">
    @RenderBody()
</main>
```

```css
.skip-link.visually-hidden-focusable:focus {
    position: fixed;
    top: 0;
    left: 0;
    z-index: 9999;
    padding: 8px 16px;
    background: #000;
    color: #fff;
}
```

---

## ARIA — obecná pravidla

- Nepoužívej ARIA pokud existuje nativní HTML ekvivalent
- `aria-label` a `aria-labelledby` pro elementy bez viditelného textu
- `aria-hidden="true"` pro dekorativní SVG ikony

```cshtml
@* Ikona tlačítka bez textu *@
<button aria-label="Zavřít dialog">
    <svg aria-hidden="true" focusable="false">...</svg>
</button>
```

---

## Audit nástroje

- [axe DevTools](https://www.deque.com/axe/) — Chrome extension pro rychlý audit
- [WAVE](https://wave.webaim.org/) — online accessibility checker
- Lighthouse → Accessibility score cíl: **≥ 90**
