# Mobile-First Design

## Základní princip

Piš CSS pro mobil jako základ. Breakpointy **přidávají** komplexitu pro větší obrazovky — nikdy nepřepisují mobilní základ.

```css
/* ✗ Desktop-first — přepisování je zbytečná práce */
.card { display: flex; gap: 2rem; }
@media (max-width: 768px) { .card { display: block; } }

/* ✓ Mobile-first — rozšiřování */
.card { display: block; }
@media (min-width: 768px) { .card { display: flex; gap: 2rem; } }
```

## Viewport meta tag — povinný

```cshtml
@* _Layout.cshtml — musí být v každém <head> *@
<meta name="viewport" content="width=device-width, initial-scale=1">
```

Nikdy nepřidávej `user-scalable=no` — zabraňuje přiblížení pro slabozraké (přístupnost + SEO).

## Breakpointy — standardní sada

Definuj jako CSS custom properties nebo proměnné. Konzistentní sada napříč projektem.

```css
/* :root nebo _variables.css */
:root {
  --bp-sm:  480px;   /* velké telefony na šířku */
  --bp-md:  768px;   /* tablety */
  --bp-lg:  1024px;  /* malé laptopy */
  --bp-xl:  1280px;  /* standardní desktop */
  --bp-2xl: 1536px;  /* velké obrazovky */
}
```

```css
/* Použití — vždy min-width (mobile-first) */
.container {
  width: 100%;
  padding: 0 1rem;
}

@media (min-width: 768px) {
  .container {
    padding: 0 2rem;
  }
}

@media (min-width: 1280px) {
  .container {
    max-width: 1200px;
    margin: 0 auto;
    padding: 0 2rem;
  }
}
```

## Typografie — fluid scaling

```css
/* ✓ Fluid typography — plynulé škálování bez skoků */
:root {
  --text-base: clamp(1rem, 0.9rem + 0.5vw, 1.125rem);
  --text-lg:   clamp(1.125rem, 1rem + 0.6vw, 1.375rem);
  --text-xl:   clamp(1.25rem, 1.1rem + 0.75vw, 1.75rem);
  --text-2xl:  clamp(1.5rem, 1.25rem + 1.25vw, 2.25rem);
  --text-3xl:  clamp(1.875rem, 1.5rem + 1.875vw, 3rem);
}

body { font-size: var(--text-base); }
h1   { font-size: var(--text-3xl); }
h2   { font-size: var(--text-2xl); }
h3   { font-size: var(--text-xl); }
```

## Spacing — relativní jednotky

```css
:root {
  --space-1:  0.25rem;   /*  4px */
  --space-2:  0.5rem;    /*  8px */
  --space-3:  0.75rem;   /* 12px */
  --space-4:  1rem;      /* 16px */
  --space-6:  1.5rem;    /* 24px */
  --space-8:  2rem;      /* 32px */
  --space-12: 3rem;      /* 48px */
  --space-16: 4rem;      /* 64px */
}

/* ✗ Nikdy fixní px pro layout spacing */
.section { margin-bottom: 64px; }

/* ✓ Relativní + fluid */
.section { margin-bottom: clamp(var(--space-8), 5vw, var(--space-16)); }
```

## Touch targety

```css
/* ✓ Minimum 44×44px pro všechny klikatelné prvky (Apple HIG, WCAG 2.5.5) */
button,
a,
[role="button"],
input[type="checkbox"],
input[type="radio"] {
  min-height: 44px;
  min-width: 44px;
}

/* ✓ Pro inline odkazy — zvětši hit area bez vizuální změny */
nav a {
  display: inline-flex;
  align-items: center;
  padding: 0.75rem 1rem;  /* větší klikatelná oblast */
}
```

## Orientace displeje

```css
/* Přizpůsob layout pro landscape na mobilech */
@media (max-width: 768px) and (orientation: landscape) {
  .hero {
    min-height: 100svh;  /* svh = small viewport height — ignoruje browser chrome */
    padding: var(--space-4) 0;
  }

  nav { height: 48px; }  /* nižší navbar v landscape */
}
```

## Viewport jednotky — moderní přístup

```css
/* ✗ Stará 100vh — na mobilech zahrnuje browser chrome */
.hero { min-height: 100vh; }

/* ✓ Dynamické viewport jednotky (podporováno od 2023) */
.hero    { min-height: 100dvh; }  /* dynamic — mění se při scroll */
.modal   { max-height: 100svh; }  /* small — nejmenší viewport (s chrome) */
.sidebar { height: 100lvh; }      /* large — největší viewport (bez chrome) */
```
