using Avalonia.Controls;
using Avalonia.Interactivity;
using GestorDocente.Desktop.ViewModels;
using GestorDocente.Domain;
namespace GestorDocente.Desktop.Views;
public partial class WeeksWindow : Window
{
    private bool confirmingRemoval;
    public WeeksWindow(){InitializeComponent();Controls.WindowLayout.Configure(this);Closing+=(_,e)=>{if(Model?.IsSaving==true || confirmingRemoval)e.Cancel=true;};}
    private WeeksViewModel? Model=>DataContext as WeeksViewModel;
    private async void Save(object? sender,RoutedEventArgs e){if(Model is {} model)await model.SaveAsync();}
    private void NewWeek(object? sender,RoutedEventArgs e)
    {
        if(Model is not {} model || !model.CanStartNew)return;
        model.CancelEdit();model.MarkAllPresent=false;WeekKindInput.Focus();
    }
    private void EditAtRow(object? sender,RoutedEventArgs e)
    {
        if(confirmingRemoval || Model is not {} model || !model.CanSelectWeek || sender is not Button{DataContext:AttendanceWeek week})return;
        model.SelectedWeek=week;model.BeginEdit();WeekKindInput.Focus();
    }
    private void CancelEdit(object? sender,RoutedEventArgs e)=>Model?.CancelEdit();
    private async void RemoveAtRow(object? sender,RoutedEventArgs e)
    {
        if(confirmingRemoval || Model is not {} model || !model.CanSelectWeek || sender is not Button{DataContext:AttendanceWeek week})return;
        confirmingRemoval=true;
        try
        {
            if(await new ConfirmWindow($"¿Eliminar «{week.Label}»? Sus registros pasarán a Recuperar retirados y dejarán de contarse en la tabla. Las demás semanas se numerarán consecutivamente y conservarán sus asistencias.","Eliminar semana",true){Height=285}.ShowDialog<bool>(this))
            {
                model.SelectedWeek=week;await model.RemoveAsync();
            }
        }
        catch(Exception ex){model.ReportUiError(ex);}
        finally{confirmingRemoval=false;}
    }
    private void CloseDialog(object? sender,RoutedEventArgs e)=>Close();
}
