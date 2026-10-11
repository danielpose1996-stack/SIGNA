using Avalonia.Controls;
using Avalonia.Interactivity;
using GestorDocente.Desktop.ViewModels;
namespace GestorDocente.Desktop.Views;
public partial class CatalogEditorWindow:Window
{
    public CatalogEditorWindow(){InitializeComponent();Controls.WindowLayout.Configure(this);}
    private void Cancel(object? sender,RoutedEventArgs e)=>Close();
    private async void Save(object? sender,RoutedEventArgs e){if(DataContext is CatalogEditorViewModel model && await model.SaveAsync())Close();}
}
