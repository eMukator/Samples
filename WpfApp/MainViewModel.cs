using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Text.Json;
using System.Threading.Channels;
using System.Windows.Input;

namespace WpfApp
{
  public partial class MainViewModel : ObservableObject
  {
    public ObservableCollection<ItemModel> Items { get; set; } = new();

    [ObservableProperty]
    ItemModel selectedItem;

    public event Action<ItemModel> ItemSaved;

    public MainViewModel()
    {
      Items = new ObservableCollection<ItemModel>();
    }

    bool CanEditOrRemove(ItemModel item) => item is not null;

    [RelayCommand]
    void LoadDefaultData(object parameter)
    {
      Items.Clear();
      Items.Add(new ItemModel { Id = 1, Title = "Fruit", Description = "Something to eat" });
      Items.Add(new ItemModel { Id = 2, Title = "Hammer", Description = "Something to work with" });
      Items.Add(new ItemModel { Id = 3, Title = "Hunger", Description = "Something to feel it" });
    }

    [RelayCommand]
    void Add(object parameter)
    {
      var id = Items.Any()
        ? Items.Max(x => x.Id) + 1
        : 1;
      Items.Add(new ItemModel { Id = id, Title = "<new>", Description = "<description>" });
    }

    [RelayCommand(CanExecute = nameof(CanEditOrRemove))]
    void Edit(ItemModel item)
    {
      var dialog = new EditDialog(item)
      {
        Owner = System.Windows.Application.Current.MainWindow
      };

      if (dialog.ShowDialog() == true)
      {
        item.Title = dialog.EditedItem.Title;
        item.Description = dialog.EditedItem.Description;
        ItemSaved?.Invoke(item);
      }
    }

    [RelayCommand(CanExecute = nameof(CanEditOrRemove))]
    void Remove(ItemModel item)
    {
      Items.Remove(item);
    }

    [RelayCommand]
    void Exit()
    {
      System.Windows.Application.Current.Shutdown();
    }

    [RelayCommand]
    private void SaveToJson()
    {
      var options = new JsonSerializerOptions { WriteIndented = true };
      var json = JsonSerializer.Serialize(Items, options);
      System.IO.File.WriteAllText("items.json", json);
    }

    [RelayCommand]
    private void LoadFromJson() {
      if (!System.IO.File.Exists("items.json"))
        return;
      var json = System.IO.File.ReadAllText("items.json");
      var loaded = JsonSerializer.Deserialize<List<ItemModel>>(json);
      if (loaded is null)
        return;
      Items.Clear();
      foreach (var item in loaded)
        Items.Add(item);
    }

  }
}
