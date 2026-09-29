# WpfApp

Šablona WPF aplikace (.NET 9) postavená na MVVM s [CommunityToolkit.Mvvm](https://learn.microsoft.com/dotnet/communitytoolkit/mvvm/).

## Co ukazuje

- `ObservableObject`, `[ObservableProperty]` a `[RelayCommand]` včetně `CanExecute`
- Seznam položek s přidáním, úpravou v modálním dialogu (`EditDialog`) a odebráním
- Uložení a načtení seznamu do/z `items.json` (`System.Text.Json`)
- Přeřazení položek přetažením myší přes attached behavior (`Behaviors/DragDropBehavior.cs`)
  s indikátorem místa vložení

## Spuštění

```
dotnet run
```
