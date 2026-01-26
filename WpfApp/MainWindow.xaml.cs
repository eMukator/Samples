using System.Windows;
using System.Windows.Input;

namespace WpfApp
{
  public partial class MainWindow : Window
  {
    public MainWindow()
    {
      InitializeComponent();
      DataContext = new MainViewModel();
    }

    void ListView_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
      if (DataContext is MainViewModel vm && vm.SelectedItem is not null)
        vm.EditCommand.Execute(vm.SelectedItem);
    }

  }
}