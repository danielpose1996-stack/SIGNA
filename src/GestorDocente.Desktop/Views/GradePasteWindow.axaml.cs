using Avalonia.Controls;
using Avalonia.Interactivity;
using GestorDocente.Desktop.ViewModels;
namespace GestorDocente.Desktop.Views;
public partial class GradePasteWindow:Window
{
    public GradePasteWindow(){InitializeComponent();Closing+=(_,e)=>{if(DataContext is GradePasteViewModel{IsSaving:true})e.Cancel=true;};}
    private void Cancel(object? sender,RoutedEventArgs e)=>Close();
    private async void Save(object? sender,RoutedEventArgs e){if(DataContext is GradePasteViewModel model && await model.SaveAsync())Close();}
}
