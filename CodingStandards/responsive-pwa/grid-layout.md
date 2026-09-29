# Grid & Layout

## CSS Grid — primární nástroj pro layout

```css
/* ✓ Základní responzivní grid — bez breakpointů */
.grid {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(min(280px, 100%), 1fr));
  gap: var(--space-6);
}
/* auto-fit + minmax = automaticky responzivní bez media queries */
```

```css
/* ✓ Fixní sloupcový grid s breakpointy */
.grid-cols {
  display: grid;
  grid-template-columns: 1fr;           /* mobil: 1 sloupec */
  gap: var(--space-4);
}

@media (min-width: 640px) {
  .grid-cols { grid-template-columns: repeat(2, 1fr); }
}

@media (min-width: 1024px) {
  .grid-cols { grid-template-columns: repeat(3, 1fr); }
}
```

```css
/* ✓ Holy Grail layout — header, main+sidebar, footer */
.page-layout {
  display: grid;
  grid-template-rows: auto 1fr auto;
  grid-template-areas:
    "header"
    "main"
    "footer";
  min-height: 100dvh;
}

@media (min-width: 1024px) {
  .page-layout {
    grid-template-columns: 280px 1fr;
    grid-template-areas:
      "header  header"
      "sidebar main"
      "footer  footer";
  }
}

header  { grid-area: header; }
.sidebar { grid-area: sidebar; }
main    { grid-area: main; }
footer  { grid-area: footer; }
```

## Flexbox — pro komponenty a jednorozměrné layouty

```css
/* ✓ Flex pro navigaci, karty, toolbary */
.navbar {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: var(--space-4);
  flex-wrap: wrap;  /* na mobilu se zalamuje */
}

/* ✓ Flex pro centrování */
.card {
  display: flex;
  flex-direction: column;
}

.card__content {
  flex: 1;  /* roztáhne obsah — všechny karty stejně vysoké */
}

.card__footer {
  margin-top: auto;  /* tlačítko vždy dole */
}
```

## Container queries — layout závislý na rodiči

```css
/* ✓ Container queries — modernější alternativa k media queries pro komponenty */
.card-wrapper {
  container-type: inline-size;
  container-name: card;
}

.card {
  display: block;
}

@container card (min-width: 400px) {
  .card {
    display: grid;
    grid-template-columns: 200px 1fr;
  }
}
/* Karta se přizpůsobí podle svého kontejneru, ne podle okna prohlížeče */
```

## Obrázky v layoutu

```cshtml
@* ✓ Obrázek přizpůsobený kontejneru — vždy *@
<img src="/img/foto.webp"
     alt="Popis"
     width="800" height="600"
     loading="lazy"
     style="max-width: 100%; height: auto; display: block;">
```

```css
/* ✓ Globální reset pro obrázky */
img, video, svg {
  max-width: 100%;
  height: auto;
  display: block;
}

/* ✓ Object-fit pro pevné rozměry (galerie, product thumbnails) */
.product-image {
  width: 100%;
  aspect-ratio: 4 / 3;
  object-fit: cover;
  object-position: center;
}
```

## Tabulky — responzivní vzory

```css
/* ✓ Varianta 1 — horizontální scroll */
.table-wrapper {
  overflow-x: auto;
  -webkit-overflow-scrolling: touch;  /* plynulý scroll na iOS */
}

/* ✓ Varianta 2 — přeskupení do karet na mobilu */
@media (max-width: 640px) {
  table, thead, tbody, tr, th, td {
    display: block;
  }

  thead { display: none; }  /* skryj hlavičku */

  td::before {
    content: attr(data-label);  /* HTML: <td data-label="Jméno"> */
    font-weight: bold;
    display: inline-block;
    width: 40%;
  }
}
```

```cshtml
@* HTML pro card variantu *@
<td data-label="Zákazník">@order.CustomerName</td>
<td data-label="Celkem">@order.Total.ToString("C")</td>
```

## Sticky prvky

```css
/* ✓ Sticky header */
.site-header {
  position: sticky;
  top: 0;
  z-index: 100;
  background: var(--color-surface);
  /* Zabraň layout jumpu při sticky */
  backdrop-filter: blur(8px);
  background: color-mix(in srgb, var(--color-surface) 90%, transparent);
}

/* ✓ Sticky sidebar — jen na desktopu */
@media (min-width: 1024px) {
  .sidebar {
    position: sticky;
    top: calc(var(--header-height) + var(--space-4));
    max-height: calc(100dvh - var(--header-height) - var(--space-8));
    overflow-y: auto;
  }
}
```
