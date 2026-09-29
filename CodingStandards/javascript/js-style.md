# JavaScript — Code Style

## Základní pravidla

- Indentace: **2 mezery** (ne 4, ne taby)
- Středníky: **vždy** (ASI je nevyzpytatelný)
- Uvozovky: **jednoduché** `'` pro řetězce, template literals kde jsou interpolace
- Maximální délka řádku: **100 znaků**
- Trailing comma: **vždy** v multi-line (usnadňuje diff)

## Proměnné

```javascript
// ✓ const jako výchozí
const maxRetries = 3;
const user = { name: 'Jan', email: 'jan@example.com' };

// ✓ let pouze pokud se hodnota mění
let retryCount = 0;
while (retryCount < maxRetries) {
  retryCount++;
}

// ✗ Nikdy var
var x = 1;  // funkční scope, hoisting → problémy

// ✓ Destructuring místo opakovaného přístupu
const { name, email, role = 'user' } = userData;
const [first, second, ...rest] = items;

// ✓ Computed property names
const field = 'email';
const update = { [field]: newEmail };
```

## Funkce

```javascript
// ✓ Arrow funkce pro krátké callbacky a anonymous funkce
const doubled = numbers.map(n => n * 2);
const evens = numbers.filter(n => n % 2 === 0);

// ✓ Named funkce pro pojmenované operace (lepší stack traces)
function calculateOrderTotal(items) {
  return items.reduce((sum, item) => sum + item.price * item.quantity, 0);
}

// ✓ Default parametry
function createUser(name, role = 'user', active = true) {
  return { name, role, active };
}

// ✓ Rest parametry místo arguments
function logMessages(level, ...messages) {
  messages.forEach(msg => console.log(`[${level}] ${msg}`));
}

// ✗ Modifikace vstupních parametrů
function processUser(user) {
  user.processed = true;  // MUTACE VSTUPU — špatně
  return user;
}

// ✓ Vrať nový objekt
function processUser(user) {
  return { ...user, processed: true };
}
```

## Objekty a pole

```javascript
// ✓ Shorthand property names
const name = 'Jan';
const email = 'jan@example.com';
const user = { name, email };  // ne { name: name, email: email }

// ✓ Spread pro kopírování a merge (immutability)
const updated = { ...original, email: newEmail };
const extended = [...existingItems, newItem];

// ✓ Optional chaining
const city = user?.address?.city;
const firstItem = items?.[0]?.name;

// ✓ Nullish coalescing (ne ||, kvůli falsy hodnotám jako 0 nebo '')
const count = data.count ?? 0;
const label = config.label ?? 'Výchozí';

// ✗ || pro default může spolknout 0 nebo false
const count = data.count || 0;  // pokud count === 0, vrátí 0 správně, ale záměr není jasný
```

## Třídy

```javascript
// ✓ Třídy pro entity, services a komponenty
class OrderService {
  #repository;  // private field (ES2022)
  #logger;

  constructor(repository, logger) {
    this.#repository = repository;
    this.#logger = logger;
  }

  async getById(id) {
    const order = await this.#repository.findById(id);
    if (!order) return null;
    return this.#mapToDto(order);
  }

  #mapToDto(order) {  // private method
    return { id: order.id, customerName: order.customerName, total: order.total };
  }
}
```

## Formátování — Prettier config

```json
// .prettierrc
{
  "semi": true,
  "singleQuote": true,
  "tabWidth": 2,
  "printWidth": 100,
  "trailingComma": "all",
  "arrowParens": "avoid",
  "endOfLine": "lf"
}
```

## ESLint config (flat config — ESLint 9+)

```javascript
// eslint.config.js
import js from '@eslint/js';

export default [
  js.configs.recommended,
  {
    rules: {
      'no-var': 'error',
      'prefer-const': 'error',
      'no-console': 'warn',
      'eqeqeq': ['error', 'always'],
      'no-unused-vars': ['error', { argsIgnorePattern: '^_' }],
      'no-implicit-coercion': 'error',
      'prefer-template': 'error',
      'object-shorthand': 'error',
    },
  },
];
```

## Zakázané praktiky

```javascript
// ✗ == místo === (implicitní type coercion)
if (userId == '123') { }   // CHYBA
if (userId === '123') { }  // ✓

// ✗ Implicitní type coercion
const num = +'42';       // použij Number('42') nebo parseInt('42', 10)
const bool = !!value;    // použij Boolean(value)

// ✗ eval() — bezpečnostní riziko
eval('const x = 1');

// ✗ with statement
with (obj) { }

// ✗ delete na proměnné
delete obj.property;  // OK pro objekty
delete variable;      // NIKDY
```
