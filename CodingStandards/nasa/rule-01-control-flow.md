# Pravidlo 1 — Omez řízení toku

> *"Nepoužívej goto, setjmp nebo longjmp. Každý cyklus musí mít pevnou horní hranici iterací."*

## C# adaptace

V C# neexistuje `goto` jako hrozba (přesto je zakázán), ale ekvivalentní rizika jsou:
- neomezené smyčky (`while(true)`, `for(;;)`)
- rekurze bez omezení hloubky
- `Thread.Sleep` v produkčním kódu místo správné synchronizace

### Zakázané konstrukce

```csharp
// ✗ goto — zakázáno bez výjimky
goto cleanup;

// ✗ Neomezená smyčka bez escape podmínky a timeoutu
while (true)
{
    var message = await queue.ReceiveAsync();
    Process(message);
}

// ✗ Rekurze bez omezení hloubky
public TreeNode Search(TreeNode node, int value)
{
    if (node.Value == value) return node;
    return Search(node.Left, value);  // může přetéct zásobník
}
```

### Správné vzory

```csharp
// ✓ Smyčka s explicitní horní hranicí
const int MaxIterations = 10_000;
var iteration = 0;

while (condition && iteration < MaxIterations)
{
    Process();
    iteration++;
}

if (iteration >= MaxIterations)
    _logger.LogError("Překročen limit iterací — možná nekonečná smyčka");

// ✓ Smyčka s CancellationToken (pro long-running background services)
while (!ct.IsCancellationRequested)
{
    var message = await queue.ReceiveAsync(ct);
    await ProcessAsync(message, ct);
}

// ✓ Rekurze s omezením hloubky
public TreeNode? Search(TreeNode? node, int value, int maxDepth = 100)
{
    if (node is null || maxDepth <= 0) return null;
    if (node.Value == value) return node;
    return Search(node.Left, value, maxDepth - 1)
        ?? Search(node.Right, value, maxDepth - 1);
}

// ✓ Iterativní varianta jako preferovaná alternativa ke rekurzi
public TreeNode? SearchIterative(TreeNode root, int value)
{
    var stack = new Stack<TreeNode>();
    stack.Push(root);

    while (stack.TryPop(out var current))
    {
        if (current.Value == value) return current;
        if (current.Right is not null) stack.Push(current.Right);
        if (current.Left is not null) stack.Push(current.Left);
    }

    return null;
}
```

### Timeouty pro všechny I/O operace

```csharp
// ✓ Vždy nastav timeout pro externí volání
using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
cts.CancelAfter(TimeSpan.FromSeconds(30));

try
{
    var result = await _httpClient.GetAsync(url, cts.Token);
}
catch (OperationCanceledException) when (!ct.IsCancellationRequested)
{
    throw new TimeoutException($"Volání {url} vypršelo po 30 sekundách");
}
```
