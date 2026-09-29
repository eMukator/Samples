# JavaScript — Async/Await & Promises

## Základní pravidla

- Vždy `async/await` místo `.then()` řetězení (čitelnější, lepší stack traces)
- Každá async operace musí mít ošetřeny chyby
- Nikdy nevracet Promise bez `await` ve funkci obalené try/catch

## Správné vzory

```javascript
// ✓ async/await s try/catch
async function fetchOrder(id) {
  try {
    const response = await fetch(`/api/orders/${id}`);
    if (!response.ok) throw new Error(`HTTP ${response.status}`);
    return await response.json();
  } catch (error) {
    console.error('Failed to fetch order:', error);
    throw error;  // re-throw pokud caller musí reagovat
  }
}

// ✓ Paralelní operace — Promise.all
async function fetchDashboardData(userId) {
  const [orders, customer, stats] = await Promise.all([
    fetchOrders(userId),
    fetchCustomer(userId),
    fetchStats(userId),
  ]);
  return { orders, customer, stats };
}

// ✓ Promise.allSettled pokud chceš výsledky i při částečném selhání
async function fetchAllWithFallback(ids) {
  const results = await Promise.allSettled(ids.map(id => fetchOrder(id)));

  return results.map((result, index) => ({
    id: ids[index],
    order: result.status === 'fulfilled' ? result.value : null,
    error: result.status === 'rejected' ? result.reason.message : null,
  }));
}
```

## Zakázané vzory

```javascript
// ✗ Floating promises — nezachycené chyby
fetchOrder(id);  // výsledek ignorován, chyby spolknuty

// ✓ Explicitně ignorovat nebo ošetřit
void fetchOrder(id).catch(err => logger.error('Background fetch failed', err));

// ✗ .then() míchané s async/await
async function loadData() {
  return fetch('/api/data')
    .then(r => r.json())  // míchání stylů
    .then(data => process(data));
}

// ✓ Konzistentně async/await
async function loadData() {
  const response = await fetch('/api/data');
  const data = await response.json();
  return process(data);
}

// ✗ Await v smyčce kde jde o sekvenční zpracování zbytečně
for (const id of ids) {
  const order = await fetchOrder(id);  // sekvenční — pomalé
  orders.push(order);
}

// ✓ Paralelně
const orders = await Promise.all(ids.map(id => fetchOrder(id)));

// ✗ new Promise() wrapper kolem async funkce (Promise constructor anti-pattern)
return new Promise(async (resolve, reject) => {  // NIKDY
  try {
    const data = await fetchData();
    resolve(data);
  } catch (err) {
    reject(err);
  }
});

// ✓ Prostě vrať async funkci
return fetchData();
```

## Fetch API — standardní vzor

```javascript
// ✓ Centralizovaná fetch utility
async function apiFetch(url, options = {}) {
  const response = await fetch(url, {
    headers: {
      'Content-Type': 'application/json',
      ...options.headers,
    },
    ...options,
  });

  if (!response.ok) {
    const errorBody = await response.json().catch(() => ({}));
    const error = new Error(errorBody.message ?? `HTTP error ${response.status}`);
    error.status = response.status;
    throw error;
  }

  if (response.status === 204) return null;
  return response.json();
}

// Použití
const order = await apiFetch(`/api/orders/${id}`);

const created = await apiFetch('/api/orders', {
  method: 'POST',
  body: JSON.stringify(orderData),
});
```

## AbortController — zrušení požadavků

```javascript
// ✓ Vždy umožni zrušení dlouhých operací (komponenty, search)
class SearchService {
  #abortController = null;

  async search(query) {
    // Zruš předchozí požadavek
    this.#abortController?.abort();
    this.#abortController = new AbortController();

    try {
      const results = await apiFetch(
        `/api/search?q=${encodeURIComponent(query)}`,
        { signal: this.#abortController.signal }
      );
      return results;
    } catch (error) {
      if (error.name === 'AbortError') return null;  // zrušeno — OK
      throw error;
    }
  }
}
```

## Error handling vzory

```javascript
// ✓ Typed errors pro různé situace
class ApiError extends Error {
  constructor(message, status, body) {
    super(message);
    this.name = 'ApiError';
    this.status = status;
    this.body = body;
  }
}

class NetworkError extends Error {
  constructor(message, cause) {
    super(message, { cause });
    this.name = 'NetworkError';
  }
}

// ✓ Specifické zachytávání
try {
  const order = await fetchOrder(id);
} catch (error) {
  if (error instanceof ApiError && error.status === 404) {
    return null;  // 404 je očekávaný stav
  }
  if (error instanceof ApiError && error.status === 401) {
    redirectToLogin();
    return;
  }
  throw error;  // ostatní chyby přeposílej výš
}
```
