using Avalonia.Controls;
using Avalonia.Interactivity;
using GestorDocente.Desktop.ViewModels;
namespace GestorDocente.Desktop.Views;
public partial class RecoveryWindow : Window
{
    public RecoveryWindow(){InitializeComponent();Controls.WindowLayout.Configure(this);}
    private async void Restore(object? sender,RoutedEventArgs e){if(DataContext is RecoveryViewModel model)await model.RestoreAsync();}
    private void CloseDialog(object? sender,RoutedEventArgs e)=>Close();
}
