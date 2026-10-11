using Avalonia.Controls;
using Avalonia.Interactivity;
using GestorDocente.Desktop.ViewModels;
using GestorDocente.Domain;
namespace GestorDocente.Desktop.Views;
public partial class ConfigurationWindow:Window
{
    private bool handlingSave;
    public ConfigurationWindow()
    {
        InitializeComponent();Controls.WindowLayout.Configure(this);
        KeyDown+=(_,e)=>{if(e.Key==Avalonia.Input.Key.Escape && Model?.IsSaving!=true)Close();};
        Closing+=(_,e)=>{if(Model?.IsSaving==true)e.Cancel=true;};
    }
    private ConfigurationViewModel? Model=>DataContext as ConfigurationViewModel;
    private void Cancel(object? sender,RoutedEventArgs e)=>Close();
    private async void Save(object? sender,RoutedEventArgs e)
    {
        if(handlingSave || Model is not {} model || model.IsSaving)return;
        handlingSave=true;
        try
        {
            CommitTables();
            if(model.HasModeChanges && !await new ConfirmWindow("Cambiar la modalidad conserva los registros anteriores. De nota directa a subactividades, la nota pasa a la primera. De subactividades a directa, se utiliza el promedio completo y las subactividades quedan recuperables. ¿Guardar estos cambios?","Cambiar modalidad"){Width=600,Height=300}.ShowDialog<bool>(this))return;
            if(await model.SaveAsync())Close();
        }
        catch(Exception ex){model.ReportSaveError(ex);}
        finally{handlingSave=false;}
    }
    private void CommitTables()
    {
        if(!GroupsTable.CommitEdit(DataGridEditingUnit.Row,true) || !ActivitiesTable.CommitEdit(DataGridEditingUnit.Row,true))
            throw new AcademicValidationException("Revisa los nombres y porcentajes que estás editando antes de continuar.");
    }
    private void AddGroup(object? sender,RoutedEventArgs e)
    {
        if(Model is not {} model || model.IsSaving)return;
        try{CommitTables();model.AddGroup();}catch(Exception ex){model.ReportSaveError(ex);}
    }
    private void AddActivity(object? sender,RoutedEventArgs e)
    {
        if(Model is not {} model || model.IsSaving)return;
        try{CommitTables();model.AddActivity();}catch(Exception ex){model.ReportSaveError(ex);}
    }
    private async void RemoveGroup(object? sender,RoutedEventArgs e)
    {
        if(Model?.SelectedGroup is {} group && await new ConfirmWindow($"¿Retirar «{group.Name}»? Al guardar, sus notas y subactividades quedarán recuperables. Ajusta las otras actividades para sumar 100 %.").ShowDialog<bool>(this))Model.RemoveGroup(group);
    }
    private async void RemoveActivity(object? sender,RoutedEventArgs e)
    {
        if(ActivitiesTable.SelectedItem is EvaluationActivity activity && await new ConfirmWindow($"¿Retirar «{activity.Name}»? Sus notas quedarán recuperables y cambiará el promedio.").ShowDialog<bool>(this))Model?.RemoveActivity(activity);
    }
}
