# JavaScript — Patterns & Anti-patterns

## Immutability

```javascript
// ✓ Nikdy nemodifikuj vstupní parametry — vracej nové hodnoty
function addItem(cart, item) {
  return {
    ...cart,
    items: [...cart.items, item],
    total: cart.total + item.price,
  };
}

function updateStatus(order, newStatus) {
  return { ...order, status: newStatus, updatedAt: new Date() };
}

// ✗ Mutace vstupu
function addItem(cart, item) {
  cart.items.push(item);     // MUTACE
  cart.total += item.price;  // MUTACE
  return cart;
}

// ✓ Array operace které nevytváří mutace
const sorted = [...items].sort((a, b) => a.name.localeCompare(b.name));
const reversed = [...items].reverse();
const withoutFirst = items.slice(1);
const without = items.filter(item => item.id !== removeId);
```

## Móduly — importy

```javascript
// ✓ Named exports preferovány před default exports (lépe refactorovatelné)
// orders.js
export function getOrder(id) { }
export function createOrder(data) { }
export class OrderService { }

// ✓ Pořadí importů: external → internal → relative
import { useState, useEffect } from 'react';  // 1. external/npm balíčky
import { apiFetch } from '@/lib/api';          // 2. internal aliasy
import { formatCurrency } from '../utils';      // 3. relative importy
import type { Order } from './types';           // 4. type-only importy

// ✓ Barrel exports pro veřejné API modulů
// orders/index.js
export { OrderService } from './OrderService';
export { OrderValidator } from './OrderValidator';
export type { Order, OrderDto } from './types';

// ✗ Přepisování globálního prostoru
window.myApp = {};  // vyhni se globálním proměnným
```

## DOM manipulace (vanilla JS v ASP.NET MVC views)

```javascript
// ✓ Vždy kontroluj existenci elementu
const button = document.getElementById('submit-btn');
button?.addEventListener('click', handleSubmit);

// ✓ Event delegation místo listenerů na každém elementu
document.querySelector('.order-list')?.addEventListener('click', event => {
  const row = event.target.closest('[data-order-id]');
  if (!row) return;
  handleOrderClick(row.dataset.orderId);
});

// ✓ Dokumentované data atributy místo tříd pro JS hooks
// HTML: <button data-action="delete-order" data-order-id="42">
// JS:
document.addEventListener('click', event => {
  const action = event.target.dataset.action;
  if (action === 'delete-order') {
    deleteOrder(event.target.dataset.orderId);
  }
});

// ✗ Třídy pro JS hooks (mixing stylů a chování)
document.querySelectorAll('.js-delete-order').forEach(btn => { });
```

## State management (bez frameworku)

```javascript
// ✓ Jednoduchý observable store pro sdílený stav
function createStore(initialState) {
  let state = { ...initialState };
  const listeners = new Set();

  return {
    getState: () => ({ ...state }),
    setState: updater => {
      state = { ...state, ...(typeof updater === 'function' ? updater(state) : updater) };
      listeners.forEach(fn => fn(state));
    },
    subscribe: fn => {
      listeners.add(fn);
      return () => listeners.delete(fn);  // unsubscribe
    },
  };
}

const cartStore = createStore({ items: [], total: 0 });
const unsubscribe = cartStore.subscribe(state => renderCart(state));
```

## Výkon

```javascript
// ✓ Debounce pro search/resize handlery
function debounce(fn, delay) {
  let timeoutId;
  return (...args) => {
    clearTimeout(timeoutId);
    timeoutId = setTimeout(() => fn(...args), delay);
  };
}

const handleSearch = debounce(async query => {
  const results = await searchOrders(query);
  renderResults(results);
}, 300);

// ✓ IntersectionObserver pro lazy loading (místo scroll event)
const observer = new IntersectionObserver(entries => {
  entries.forEach(entry => {
    if (entry.isIntersecting) {
      loadMoreItems();
      observer.unobserve(entry.target);
    }
  });
});

observer.observe(document.querySelector('.load-more-trigger'));

// ✓ DocumentFragment pro hromadné DOM operace
const fragment = document.createDocumentFragment();
items.forEach(item => {
  const li = document.createElement('li');
  li.textContent = item.name;
  fragment.appendChild(li);
});
document.querySelector('.item-list').appendChild(fragment);  // jeden reflow
```

## Bezpečnost

```javascript
// ✗ innerHTML s uživatelským obsahem — XSS!
element.innerHTML = `<div>${userInput}</div>`;

// ✓ textContent pro text, nebo DOMPurify pro HTML
element.textContent = userInput;

// Pro HTML od uživatele (např. rich text editor):
import DOMPurify from 'dompurify';
element.innerHTML = DOMPurify.sanitize(userHtml);

// ✓ Bezpečná URL validace
function isSafeUrl(url) {
  try {
    const parsed = new URL(url);
    return ['http:', 'https:'].includes(parsed.protocol);
  } catch {
    return false;
  }
}

// ✗ Přímé vkládání URL bez validace
link.href = userProvidedUrl;  // může být javascript:alert(1)

// ✓
if (isSafeUrl(userProvidedUrl)) link.href = userProvidedUrl;
```
