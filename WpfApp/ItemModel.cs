using CommunityToolkit.Mvvm.ComponentModel;

namespace WpfApp
{
  public partial class ItemModel : ObservableObject
  {
    public int Id { get; set; }

    [ObservableProperty]
    string title;

    [ObservableProperty]
    string description;

  }
}
