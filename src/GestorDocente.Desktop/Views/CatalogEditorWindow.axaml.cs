using Avalonia.Controls;
using Avalonia.Interactivity;
using GestorDocente.Desktop.ViewModels;
namespace GestorDocente.Desktop.Views;
public partial class CatalogEditorWindow:Window
{
    public CatalogEditorWindow(){InitializeComponent();Controls.WindowLayout.Configure(this);}
    private void Cancel(object? sender,RoutedEventArgs e)=>Close();
    private void Save(object? sender,RoutedEventArgs e){if((DataContext as CatalogEditorViewModel)?.Save()==true)Close();}
}
