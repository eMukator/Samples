# Výkon & UX na mobilních zařízeních

## Prefers-reduced-motion — respektuj systémové nastavení

```css
/* ✓ Vždy obal animace do media query */
@media (prefers-reduced-motion: no-preference) {
  .card { transition: transform 200ms ease, box-shadow 200ms ease; }
  .card:hover { transform: translateY(-2px); }

  @keyframes fade-in {
    from { opacity: 0; transform: translateY(1rem); }
    to   { opacity: 1; transform: translateY(0); }
  }

  .fade-in { animation: fade-in 300ms ease-out; }
}

/* Minimální animace pro uživatele s reduced motion */
@media (prefers-reduced-motion: reduce) {
  * { animation-duration: 0.01ms !important; transition-duration: 0.01ms !important; }
}
```

## Dark mode — automatická podpora

```css
:root {
  --color-bg:      #ffffff;
  --color-surface: #f8fafc;
  --color-text:    #0f172a;
  --color-border:  #e2e8f0;
  --color-primary: #1a56db;
}

@media (prefers-color-scheme: dark) {
  :root {
    --color-bg:      #0f172a;
    --color-surface: #1e293b;
    --color-text:    #f1f5f9;
    --color-border:  #334155;
    --color-primary: #60a5fa;
  }
}

/* Obrázky — sniž jas v dark mode */
@media (prefers-color-scheme: dark) {
  img:not([src$=".svg"]) { filter: brightness(0.9); }
}
```

## Safe area insets — iPhone notch / Dynamic Island

```css
/* ✓ Vždy přidej safe area padding pro fixed/sticky prvky */
.site-header {
  padding-top: env(safe-area-inset-top);
}

.bottom-nav {
  padding-bottom: env(safe-area-inset-bottom);
}

/* Globální reset pro celou stránku */
body {
  padding-left:  env(safe-area-inset-left);
  padding-right: env(safe-area-inset-right);
}
```

```cshtml
@* Viewport meta tag pro podporu safe areas *@
<meta name="viewport" content="width=device-width, initial-scale=1, viewport-fit=cover">
```

## Scroll — plynulost a výkon

```css
/* ✓ Momentum scroll na iOS pro scrollovatelné kontejnery */
.scrollable {
  overflow-y: auto;
  -webkit-overflow-scrolling: touch;
  overscroll-behavior: contain;  /* zabraň scroll chain */
}

/* ✓ Scroll snap pro slidery / karusely */
.carousel {
  display: flex;
  overflow-x: auto;
  scroll-snap-type: x mandatory;
  scroll-behavior: smooth;
  gap: var(--space-4);
  padding-bottom: var(--space-2);  /* prostor pro scrollbar */
}

.carousel__item {
  scroll-snap-align: start;
  flex-shrink: 0;
  width: min(280px, 80vw);
}

/* ✓ Skryj scrollbar, zachovej funkčnost */
.carousel {
  scrollbar-width: none;
}
.carousel::-webkit-scrollbar {
  display: none;
}
```

## Touch události — správné vzory

```javascript
// ✓ Passive event listeners pro scroll výkon
document.addEventListener('touchstart', handleTouchStart, { passive: true });
document.addEventListener('touchmove', handleTouchMove, { passive: true });

// ✓ Swipe detekce
class SwipeDetector {
  #startX = 0;
  #startY = 0;
  #threshold = 50;

  constructor(element, { onLeft, onRight, onUp, onDown }) {
    element.addEventListener('touchstart', e => {
      this.#startX = e.touches[0].clientX;
      this.#startY = e.touches[0].clientY;
    }, { passive: true });

    element.addEventListener('touchend', e => {
      const dx = e.changedTouches[0].clientX - this.#startX;
      const dy = e.changedTouches[0].clientY - this.#startY;

      if (Math.abs(dx) > Math.abs(dy) && Math.abs(dx) > this.#threshold) {
        dx < 0 ? onLeft?.() : onRight?.();
      } else if (Math.abs(dy) > this.#threshold) {
        dy < 0 ? onUp?.() : onDown?.();
      }
    }, { passive: true });
  }
}
```

## Skeleton loading — místo spinnerů

```cshtml
@* Skeleton pro card — zobraz dokud se načítají data *@
<div class="card skeleton" aria-busy="true" aria-label="Načítám...">
  <div class="skeleton__line skeleton__line--title"></div>
  <div class="skeleton__line"></div>
  <div class="skeleton__line skeleton__line--short"></div>
</div>
```

```css
.skeleton__line {
  height: 1rem;
  border-radius: 0.25rem;
  background: linear-gradient(
    90deg,
    var(--color-border) 25%,
    color-mix(in srgb, var(--color-border) 50%, var(--color-surface)) 50%,
    var(--color-border) 75%
  );
  background-size: 200% 100%;
  animation: shimmer 1.5s infinite;
  margin-bottom: var(--space-2);
}

@keyframes shimmer {
  0%   { background-position: 200% 0; }
  100% { background-position: -200% 0; }
}

@media (prefers-reduced-motion: reduce) {
  .skeleton__line { animation: none; }
}
```

## Lighthouse PWA checklist — cílové hodnoty

| Kategorie | Cíl |
|-----------|-----|
| Performance | ≥ 90 |
| Accessibility | ≥ 95 |
| Best Practices | ≥ 95 |
| SEO | ≥ 90 |
| PWA | Splněno |

**Testuj na:**
- Chrome DevTools → Lighthouse (Mobile, throttled)
- [web.dev/measure](https://web.dev/measure/)
- Skutečné zařízení (Android + iOS) — emulátor neodhalí vše
