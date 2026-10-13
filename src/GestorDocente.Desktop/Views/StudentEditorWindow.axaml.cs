using Avalonia.Controls;
using Avalonia.Interactivity;
using GestorDocente.Desktop.ViewModels;
namespace GestorDocente.Desktop.Views;
public partial class StudentEditorWindow:Window
{
    public StudentEditorWindow(){InitializeComponent();Closing+=(_,e)=>{if(DataContext is StudentEditorViewModel{IsSaving:true})e.Cancel=true;};}
    private void Cancel(object? sender,RoutedEventArgs e)=>Close();
    private async void Save(object? sender,RoutedEventArgs e){if(DataContext is StudentEditorViewModel model && await model.SaveAsync())Close();}
}
