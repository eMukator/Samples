# TypeScript — Typování

## tsconfig.json — povinná nastavení

```json
{
  "compilerOptions": {
    "strict": true,
    "noImplicitAny": true,
    "strictNullChecks": true,
    "noUncheckedIndexedAccess": true,
    "exactOptionalPropertyTypes": true,
    "noImplicitReturns": true,
    "noFallthroughCasesInSwitch": true,
    "forceConsistentCasingInFileNames": true,
    "esModuleInterop": true,
    "skipLibCheck": true,
    "target": "ES2022",
    "module": "ESNext",
    "moduleResolution": "bundler"
  }
}
```

## Typy vs. Interfaces

```typescript
// ✓ type pro unions, intersections, aliasy primitivních typů
type OrderStatus = 'pending' | 'confirmed' | 'shipped' | 'delivered' | 'cancelled';
type UserId = string;
type OrderId = number;
type CreateOrderResult = Order | ValidationError;

// ✓ interface pro objektové struktury (rozšiřitelné)
interface Order {
  id: number;
  customerName: string;
  total: number;
  status: OrderStatus;
  createdAt: Date;
  notes?: string;  // optional property
}

// ✓ Rozšiřování interfaces
interface OrderWithItems extends Order {
  items: OrderLineItem[];
}
```

## Generics

```typescript
// ✓ Pojmenované generic parametry (ne jen T)
interface Repository<TEntity extends { id: number }> {
  findById(id: number): Promise<TEntity | null>;
  findAll(): Promise<TEntity[]>;
  save(entity: TEntity): Promise<TEntity>;
  delete(id: number): Promise<void>;
}

// ✓ Utility types — používej je aktivně
type CreateOrderDto = Omit<Order, 'id' | 'createdAt'>;
type UpdateOrderDto = Partial<Pick<Order, 'customerName' | 'notes'>>;
type ReadonlyOrder = Readonly<Order>;

// ✓ Conditional types
type NonNullable<T> = T extends null | undefined ? never : T;
type ApiResponse<T> = T extends void ? { success: boolean } : { success: boolean; data: T };
```

## Enums vs. const objects

```typescript
// ✓ Preferuj const objects před enum (tree-shakeable, lépe kompatibilní)
const OrderStatus = {
  Pending: 'pending',
  Confirmed: 'confirmed',
  Shipped: 'shipped',
  Delivered: 'delivered',
  Cancelled: 'cancelled',
} as const;

type OrderStatus = typeof OrderStatus[keyof typeof OrderStatus];
// = 'pending' | 'confirmed' | 'shipped' | 'delivered' | 'cancelled'

// ✓ Enum je OK pro numerické hodnoty s bitovými maskami
const enum Permission {
  Read = 1 << 0,
  Write = 1 << 1,
  Delete = 1 << 2,
  Admin = Read | Write | Delete,
}
```

## Type guards a narrowing

```typescript
// ✓ Type guard funkce
function isOrder(value: unknown): value is Order {
  return (
    typeof value === 'object' &&
    value !== null &&
    'id' in value &&
    'customerName' in value
  );
}

// ✓ Discriminated unions
type ApiResult<T> =
  | { success: true; data: T }
  | { success: false; error: string };

function handleResult<T>(result: ApiResult<T>) {
  if (result.success) {
    console.log(result.data);   // TypeScript ví, že data existuje
  } else {
    console.error(result.error); // TypeScript ví, že error existuje
  }
}

// ✓ satisfies operátor (TS 4.9+) — ověř typ bez ztráty inference
const config = {
  host: 'localhost',
  port: 5432,
} satisfies Record<string, string | number>;

config.port.toFixed(0);  // TypeScript ví, že port je number (ne string | number)
```

## Zakázané praktiky

```typescript
// ✗ any bez komentáře
const data: any = fetchData();  // ztráta typové bezpečnosti

// ✓ Pokud musíš, okomentuj proč
// eslint-disable-next-line @typescript-eslint/no-explicit-any
const legacyData: any = legacyLibrary.getData();  // TODO: typovat až bude čas

// ✗ Non-null assertion bez zdůvodnění
const element = document.getElementById('app')!;

// ✓ S kontrolou nebo komentářem
const element = document.getElementById('app');
if (!element) throw new Error('Element #app not found in DOM');

// ✗ Type casting přes as tam kde lze použít narrowing
const order = data as Order;

// ✓ Type guard
if (isOrder(data)) {
  // data je zde Order
}
```
