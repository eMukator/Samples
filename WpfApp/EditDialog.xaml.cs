using System.Windows;

namespace WpfApp
{
  public partial class EditDialog : Window
  {
    public ItemModel EditedItem { get; private set; }

    public EditDialog(ItemModel item)
    {
      InitializeComponent();

      EditedItem = new ItemModel
      {
        Title = item.Title,
        Description = item.Description
      };

      DataContext = EditedItem;
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
      DialogResult = true;
      Close();
    }
  }

}
