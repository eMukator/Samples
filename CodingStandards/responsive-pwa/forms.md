# Formuláře — mobilní optimalizace

## Správné input atributy

Mobilní klávesnice se přizpůsobí podle `type` a `inputmode`. Vždy je nastav.

```cshtml
@* Telefonní číslo *@
<input type="tel"
       inputmode="tel"
       autocomplete="tel"
       placeholder="+420 xxx xxx xxx">

@* E-mail *@
<input type="email"
       inputmode="email"
       autocomplete="email"
       spellcheck="false"
       autocapitalize="none">

@* Číslo (bez spinnerů) *@
<input type="text"
       inputmode="numeric"
       pattern="[0-9]*"
       autocomplete="off">

@* Celé číslo se spinnery *@
<input type="number"
       inputmode="numeric"
       min="1"
       max="999"
       step="1">

@* Cena / desetinné číslo *@
<input type="text"
       inputmode="decimal"
       pattern="[0-9]+([,.][0-9]{1,2})?">

@* Vyhledávání *@
<input type="search"
       inputmode="search"
       autocomplete="off"
       autocorrect="off"
       spellcheck="false">

@* URL *@
<input type="url"
       inputmode="url"
       autocomplete="url"
       autocapitalize="none">
```

## Autocomplete — vždy nastav

```cshtml
@* Přihlašovací formulář *@
<input type="text"     autocomplete="username"         name="username">
<input type="password" autocomplete="current-password" name="password">

@* Registrační formulář *@
<input type="text"     autocomplete="given-name"  name="firstName">
<input type="text"     autocomplete="family-name" name="lastName">
<input type="email"    autocomplete="email"        name="email">
<input type="password" autocomplete="new-password" name="password">

@* Adresa *@
<input type="text"    autocomplete="street-address"   name="street">
<input type="text"    autocomplete="address-level2"   name="city">
<input type="text"    autocomplete="postal-code"      name="zip">
<select               autocomplete="country"          name="country">

@* Platební karta *@
<input type="text"  autocomplete="cc-number"   inputmode="numeric">
<input type="text"  autocomplete="cc-name">
<input type="text"  autocomplete="cc-exp"      inputmode="numeric">
<input type="text"  autocomplete="cc-csc"      inputmode="numeric">
```

## Responzivní layout formuláře

```css
.form {
  display: grid;
  gap: var(--space-4);
}

/* Dvousloupcový layout na větších obrazovkách */
.form-row {
  display: grid;
  grid-template-columns: 1fr;
  gap: var(--space-4);
}

@media (min-width: 640px) {
  .form-row--2col {
    grid-template-columns: repeat(2, 1fr);
  }
}

/* Formulářové pole */
.form-field {
  display: flex;
  flex-direction: column;
  gap: var(--space-1);
}

.form-field label {
  font-weight: 500;
  font-size: 0.875rem;
}

.form-field input,
.form-field select,
.form-field textarea {
  width: 100%;
  padding: 0.75rem 1rem;
  border: 1.5px solid var(--color-border);
  border-radius: 0.5rem;
  font-size: 1rem;         /* min 16px — zabrání zoom na iOS */
  line-height: 1.5;
  background: var(--color-surface);
  transition: border-color 150ms;
  min-height: 44px;        /* touch target */
}

/* ✓ Viditelný focus — nikdy nemazat */
.form-field input:focus,
.form-field select:focus,
.form-field textarea:focus {
  outline: none;
  border-color: var(--color-primary);
  box-shadow: 0 0 0 3px color-mix(in srgb, var(--color-primary) 20%, transparent);
}

/* Validační stavy */
.form-field input:invalid:not(:placeholder-shown) {
  border-color: var(--color-error);
}

.form-field__error {
  color: var(--color-error);
  font-size: 0.8125rem;
  display: none;
}

.form-field--error .form-field__error {
  display: block;
}
```

## Font-size minimum — zabrání zoom na iOS

```css
/* iOS přiblíží stránku pokud je font-size inputu < 16px */
/* ✓ Vždy nastav font-size alespoň 1rem (16px) na input elementech */
input, select, textarea {
  font-size: max(1rem, 16px);
}
```

## Razor Pages / MVC validace s přístupností

```cshtml
@* Správná ASP.NET Core validace s ARIA *@
<div class="form-field @(ViewData.ModelState["Email"]?.Errors.Any() == true ? "form-field--error" : "")">
  <label asp-for="Email">E-mailová adresa</label>
  <input asp-for="Email"
         type="email"
         inputmode="email"
         autocomplete="email"
         autocapitalize="none"
         aria-describedby="email-error"
         aria-invalid="@(ViewData.ModelState["Email"]?.Errors.Any() == true ? "true" : "false")">
  <span id="email-error"
        class="form-field__error"
        role="alert"
        asp-validation-for="Email"></span>
</div>
```

## Submit tlačítko — stavy

```cshtml
<button type="submit"
        class="btn btn--primary btn--full-width"
        id="submit-btn">
  <span class="btn__text">Odeslat objednávku</span>
  <span class="btn__loading" aria-hidden="true" hidden>Odesílám...</span>
</button>
```

```javascript
document.querySelector('form')?.addEventListener('submit', event => {
  const btn = document.getElementById('submit-btn');
  if (!btn) return;

  btn.disabled = true;
  btn.querySelector('.btn__text')?.setAttribute('hidden', '');
  btn.querySelector('.btn__loading')?.removeAttribute('hidden');
  btn.setAttribute('aria-busy', 'true');
});
```

## Gesture-friendly prvky

```css
/* Velké checkboxy a radiobuttony pro touch */
.checkbox-wrapper {
  display: flex;
  align-items: center;
  gap: var(--space-3);
  min-height: 44px;
  cursor: pointer;
}

.checkbox-wrapper input[type="checkbox"] {
  width: 20px;
  height: 20px;
  flex-shrink: 0;
  cursor: pointer;
  accent-color: var(--color-primary);
}
```
