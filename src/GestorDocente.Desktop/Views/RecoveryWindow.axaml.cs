using Avalonia.Controls;
using Avalonia.Interactivity;
using GestorDocente.Desktop.ViewModels;
namespace GestorDocente.Desktop.Views;
public partial class RecoveryWindow : Window
{
    public RecoveryWindow(){InitializeComponent();Controls.WindowLayout.Configure(this);}
    private void Restore(object? sender,RoutedEventArgs e)=>(DataContext as RecoveryViewModel)?.Restore();
    private void CloseDialog(object? sender,RoutedEventArgs e)=>Close();
}
