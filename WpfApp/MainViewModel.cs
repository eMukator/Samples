using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Text.Json;
using System.Windows;
using WpfApp.Behaviors;

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
      Items.Add(new ItemModel { Id = 1, Title = "Blip", Description = "Something to almost notice" });
      Items.Add(new ItemModel { Id = 2, Title = "Flarp", Description = "Something to pretend you use" });
      Items.Add(new ItemModel { Id = 3, Title = "Zindle", Description = "Something to keep around for no reason" });
    }

    [RelayCommand]
    void Add(object parameter)
    {
      var id = Items.Any()
        ? Items.Max(x => x.Id) + 1
        : 1;
      var item = new ItemModel { Id = id, Title = $"<title_{id}>", Description = $"<description_{id}>" };
      Items.Add(item);
      Edit(item);
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
    void SaveToJson()
    {
      var options = new JsonSerializerOptions { WriteIndented = true };
      var json = JsonSerializer.Serialize(Items, options);
      System.IO.File.WriteAllText("items.json", json);
    }

    [RelayCommand]
    void LoadFromJson() {
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

    [RelayCommand]
    void Reorder(ReorderRequest req)
    {
      var source = (ItemModel)req.Source;
      var target = (ItemModel)req.Target;

      int oldIndex = Items.IndexOf(source);
      int targetIndex = Items.IndexOf(target);

      int newIndex = req.DropAbove ? targetIndex : targetIndex + 1;

      if (newIndex > oldIndex)
        newIndex--;

      if (newIndex != oldIndex)
        Items.Move(oldIndex, newIndex);
    }


    [RelayCommand]
    void Help()
    {
      MessageBox.Show("Help yourself", "Help", MessageBoxButton.OK);
    }

  }
}
