using Avalonia.Controls;
using Avalonia.Interactivity;
using GestorDocente.Desktop.ViewModels;
namespace GestorDocente.Desktop.Views;
public partial class CutWeightsWindow : Window
{
    public CutWeightsWindow(){InitializeComponent();Closing+=(_,e)=>{if(DataContext is CutWeightsViewModel{IsSaving:true})e.Cancel=true;};}
    private void Cancel(object? sender,RoutedEventArgs e)=>Close();
    private async void Save(object? sender,RoutedEventArgs e){if(DataContext is CutWeightsViewModel model && await model.SaveAsync())Close();}
}
