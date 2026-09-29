# UI Komponenty

## Navigace — mobilní hamburger menu

```cshtml
@* _Layout.cshtml *@
<header class="site-header">
  <nav class="navbar" aria-label="Hlavní navigace">
    <a href="/" class="navbar__logo" aria-label="Název webu — domů">
      <img src="/img/logo.svg" alt="" width="120" height="40" aria-hidden="true">
    </a>

    <button class="navbar__toggle"
            aria-expanded="false"
            aria-controls="main-menu"
            aria-label="Otevřít menu">
      <span class="hamburger" aria-hidden="true"></span>
    </button>

    <ul id="main-menu" class="navbar__menu" role="list">
      <li><a href="/products" @(ViewContext.RouteData.Values["controller"]?.ToString() == "Products" ? "aria-current=page" : "")>Produkty</a></li>
      <li><a href="/orders">Objednávky</a></li>
      <li><a href="/contact">Kontakt</a></li>
    </ul>
  </nav>
</header>
```

```css
.navbar {
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 0 var(--space-4);
  height: 64px;
  gap: var(--space-4);
}

.navbar__toggle {
  display: flex;           /* viditelné na mobilu */
  background: none;
  border: none;
  cursor: pointer;
  min-width: 44px;
  min-height: 44px;
  align-items: center;
  justify-content: center;
}

.navbar__menu {
  position: fixed;
  inset: 64px 0 0 0;
  background: var(--color-surface);
  flex-direction: column;
  padding: var(--space-4);
  list-style: none;
  margin: 0;

  /* Defaultně skryté na mobilu */
  display: none;
}

.navbar__menu.is-open {
  display: flex;
}

@media (min-width: 768px) {
  .navbar__toggle { display: none; }

  .navbar__menu {
    position: static;
    display: flex;
    flex-direction: row;
    background: none;
    padding: 0;
  }
}
```

```javascript
// navbar.js
const toggle = document.querySelector('.navbar__toggle');
const menu   = document.querySelector('.navbar__menu');

toggle?.addEventListener('click', () => {
  const isOpen = toggle.getAttribute('aria-expanded') === 'true';
  toggle.setAttribute('aria-expanded', String(!isOpen));
  menu?.classList.toggle('is-open', !isOpen);
});

// Zavři menu při kliknutí mimo
document.addEventListener('click', event => {
  if (!event.target.closest('.navbar') && menu?.classList.contains('is-open')) {
    toggle?.setAttribute('aria-expanded', 'false');
    menu.classList.remove('is-open');
  }
});

// Zavři menu klávesou Escape
document.addEventListener('keydown', event => {
  if (event.key === 'Escape' && menu?.classList.contains('is-open')) {
    toggle?.setAttribute('aria-expanded', 'false');
    menu.classList.remove('is-open');
    toggle?.focus();  // vrať focus na toggle
  }
});
```

---

## Modal / Dialog

```cshtml
@* Použij nativní <dialog> — má vestavěnou přístupnost *@
<dialog id="confirm-dialog"
        class="modal"
        aria-labelledby="dialog-title"
        aria-describedby="dialog-desc">
  <div class="modal__content">
    <h2 id="dialog-title">Potvrzení smazání</h2>
    <p id="dialog-desc">Opravdu chcete smazat objednávku #@Model.OrderId? Tuto akci nelze vrátit.</p>
    <div class="modal__actions">
      <button class="btn btn--ghost" data-action="close-dialog">Zrušit</button>
      <button class="btn btn--danger" id="confirm-delete">Smazat</button>
    </div>
  </div>
</dialog>
```

```css
.modal {
  max-width: min(480px, 90vw);
  width: 100%;
  border: none;
  border-radius: 0.75rem;
  padding: var(--space-6);
  box-shadow: 0 20px 60px rgba(0,0,0,0.3);
}

/* Backdrop */
.modal::backdrop {
  background: rgba(0, 0, 0, 0.5);
  backdrop-filter: blur(4px);
}

/* Animace otevření */
@keyframes modal-in {
  from { opacity: 0; transform: translateY(-1rem) scale(0.95); }
  to   { opacity: 1; transform: translateY(0) scale(1); }
}

.modal[open] {
  animation: modal-in 200ms ease-out;
}

.modal__actions {
  display: flex;
  gap: var(--space-3);
  justify-content: flex-end;
  flex-wrap: wrap;
  margin-top: var(--space-6);
}
```

```javascript
// modal.js
const dialog = document.getElementById('confirm-dialog');

// Otevření
document.querySelectorAll('[data-open-dialog]').forEach(btn => {
  btn.addEventListener('click', () => {
    dialog?.showModal();
  });
});

// Zavření tlačítkem
document.querySelectorAll('[data-action="close-dialog"]').forEach(btn => {
  btn.addEventListener('click', () => dialog?.close());
});

// Zavření kliknutím na backdrop
dialog?.addEventListener('click', event => {
  const rect = dialog.getBoundingClientRect();
  const isOutside =
    event.clientX < rect.left || event.clientX > rect.right ||
    event.clientY < rect.top  || event.clientY > rect.bottom;

  if (isOutside) dialog.close();
});
// Escape klávesa je nativně podporována <dialog>
```

---

## Toast notifikace

```javascript
// toast.js
class ToastManager {
  #container;

  constructor() {
    this.#container = document.createElement('div');
    this.#container.setAttribute('aria-live', 'polite');
    this.#container.setAttribute('aria-atomic', 'false');
    this.#container.className = 'toast-container';
    document.body.appendChild(this.#container);
  }

  show(message, type = 'info', duration = 4000) {
    const toast = document.createElement('div');
    toast.className = `toast toast--${type}`;
    toast.textContent = message;
    toast.setAttribute('role', type === 'error' ? 'alert' : 'status');

    this.#container.appendChild(toast);

    // Odstranění po uplynutí doby
    setTimeout(() => {
      toast.classList.add('toast--hiding');
      toast.addEventListener('animationend', () => toast.remove(), { once: true });
    }, duration);
  }
}

export const toast = new ToastManager();
// Použití: toast.show('Objednávka uložena', 'success');
```

```css
.toast-container {
  position: fixed;
  bottom: var(--space-4);
  right: var(--space-4);
  left: var(--space-4);
  display: flex;
  flex-direction: column;
  gap: var(--space-2);
  z-index: 9999;
  pointer-events: none;

  @media (min-width: 480px) {
    left: auto;
    max-width: 360px;
  }
}

.toast {
  padding: var(--space-3) var(--space-4);
  border-radius: 0.5rem;
  background: var(--color-surface-raised);
  box-shadow: 0 4px 12px rgba(0,0,0,0.15);
  pointer-events: auto;
  animation: toast-in 200ms ease-out;
}

@keyframes toast-in {
  from { opacity: 0; transform: translateY(1rem); }
  to   { opacity: 1; transform: translateY(0); }
}
```

---

## Bottom navigation (mobilní vzor)

```cshtml
@* Pro mobilní aplikace — navigace dole na obrazovce *@
<nav class="bottom-nav" aria-label="Hlavní navigace">
  <a href="/" class="bottom-nav__item" aria-current="page">
    <svg class="bottom-nav__icon" aria-hidden="true">...</svg>
    <span class="bottom-nav__label">Domů</span>
  </a>
  <a href="/orders" class="bottom-nav__item">
    <svg class="bottom-nav__icon" aria-hidden="true">...</svg>
    <span class="bottom-nav__label">Objednávky</span>
  </a>
  <a href="/profile" class="bottom-nav__item">
    <svg class="bottom-nav__icon" aria-hidden="true">...</svg>
    <span class="bottom-nav__label">Profil</span>
  </a>
</nav>
```

```css
.bottom-nav {
  display: flex;
  position: fixed;
  bottom: 0;
  left: 0;
  right: 0;
  background: var(--color-surface);
  border-top: 1px solid var(--color-border);
  /* Bezpečná zóna pro iPhone s notchem */
  padding-bottom: env(safe-area-inset-bottom);
  z-index: 100;
}

.bottom-nav__item {
  flex: 1;
  display: flex;
  flex-direction: column;
  align-items: center;
  padding: var(--space-2) var(--space-1);
  min-height: 56px;
  text-decoration: none;
  color: var(--color-text-muted);
  font-size: 0.75rem;
  gap: var(--space-1);
}

.bottom-nav__item[aria-current="page"] {
  color: var(--color-primary);
}

/* Skryj na desktopu */
@media (min-width: 768px) {
  .bottom-nav { display: none; }
  /* Přidej padding-bottom hlavnímu obsahu jen na mobilu */
}
```
