using Avalonia.Controls;
using Avalonia.Interactivity;
using GestorDocente.Desktop.ViewModels;
namespace GestorDocente.Desktop.Views;
public partial class WeeksWindow : Window
{
    public WeeksWindow(){InitializeComponent();Controls.WindowLayout.Configure(this);Closing+=(_,e)=>{if(Model?.IsSaving==true)e.Cancel=true;};}
    private WeeksViewModel? Model=>DataContext as WeeksViewModel;
    private async void Save(object? sender,RoutedEventArgs e){if(Model is {} model)await model.SaveAsync();}
    private void Edit(object? sender,RoutedEventArgs e)=>Model?.BeginEdit();
    private void CancelEdit(object? sender,RoutedEventArgs e)=>Model?.CancelEdit();
    private async void Remove(object? sender,RoutedEventArgs e){if(Model is {} model && model.SelectedWeek is {} w && await new ConfirmWindow($"¿Retirar «{w.Label}»? Se conservarán sus estados en Recuperar retirados y dejarán de contarse en la tabla. Las demás semanas se numerarán consecutivamente, conservando sus asistencias.","Retirar semana").ShowDialog<bool>(this))await model.RemoveAsync();}
    private void CloseDialog(object? sender,RoutedEventArgs e)=>Close();
}
