using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Input;
using Avalonia.Threading;
using Avalonia.VisualTree;
using GestorDocente.Desktop.ViewModels;
using GestorDocente.Domain;
namespace GestorDocente.Desktop.Views;
public partial class ConfigurationWindow:Window
{
    private bool handlingSave;
    private bool confirmingRemoval;
    private ConfigurationViewModel? observed;
    public ConfigurationWindow()
    {
        InitializeComponent();Controls.WindowLayout.Configure(this);
        KeyDown+=(_,e)=>{
            if(e.Key==Key.Escape && Model?.IsSaving!=true && !confirmingRemoval)Close();
            if(e.Key==Key.S && e.KeyModifiers.HasFlag(KeyModifiers.Control)){e.Handled=true;Save(this,new RoutedEventArgs());}
        };
        DataContextChanged+=(_,_)=>Attach();
        Closed+=(_,_)=>{if(observed is not null)observed.PropertyChanged-=PresentationChanged;};
        Closing+=(_,e)=>{if(Model?.IsSaving==true)e.Cancel=true;};
    }
    private ConfigurationViewModel? Model=>DataContext as ConfigurationViewModel;
    private void Attach()
    {
        if(observed is not null)observed.PropertyChanged-=PresentationChanged;
        observed=Model;if(observed is not null)observed.PropertyChanged+=PresentationChanged;
        UpdateWeightColumn();
    }
    private void PresentationChanged(object? sender,System.ComponentModel.PropertyChangedEventArgs e)
    {
        if(e.PropertyName is nameof(ConfigurationViewModel.UsesWeightedAverage) or nameof(ConfigurationViewModel.SelectedGroup))UpdateWeightColumn();
    }
    private void UpdateWeightColumn()=>ActivitiesTable.Columns[1].IsVisible=Model?.UsesWeightedAverage==true;
    private void SelectGroupAtRow(object? sender,RoutedEventArgs e)
    {
        if(sender is Control{DataContext:EvaluationGroupEditorViewModel row} && Model is {} model && !model.IsSaving)model.SelectedGroup=row.Group;
    }
    private void SelectGroupDetails(object? sender,RoutedEventArgs e)
    {
        SelectGroupAtRow(sender,e);
        if(Model?.Activities.FirstOrDefault() is {} row)FocusName(ActivitiesTable,row,"SubactivityNameInput");
        else AddSubactivityButton.Focus();
    }
    private static void FocusName(DataGrid table,object row,string name)=>Dispatcher.UIThread.Post(()=>
    {
        if(!table.IsEffectivelyVisible)return;
        table.ScrollIntoView(row,null);table.UpdateLayout();
        var input=table.GetVisualDescendants().OfType<TextBox>().FirstOrDefault(t=>t.Name==name && ReferenceEquals(t.DataContext,row));
        if(input is not null){input.Focus();input.SelectAll();}
    },DispatcherPriority.Loaded);
    private void Cancel(object? sender,RoutedEventArgs e)=>Close();
    private async void Save(object? sender,RoutedEventArgs e)
    {
        if(handlingSave || confirmingRemoval || Model is not {} model || model.IsSaving)return;
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
        try{CommitTables();model.AddGroup();if(model.SelectedGroupEditor is {} row)FocusName(GroupsTable,row,"GroupNameInput");}catch(Exception ex){model.ReportSaveError(ex);}
    }
    private void AddActivity(object? sender,RoutedEventArgs e)
    {
        if(Model is not {} model || model.IsSaving)return;
        try{CommitTables();model.AddActivity();if(model.Activities.LastOrDefault() is {} row)FocusName(ActivitiesTable,row,"SubactivityNameInput");}catch(Exception ex){model.ReportSaveError(ex);}
    }
    private async void RemoveGroupAtRow(object? sender,RoutedEventArgs e)
    {
        if(confirmingRemoval || sender is not Button {DataContext:EvaluationGroupEditorViewModel row} || Model is not {} model || model.IsSaving)return;
        var group=row.Group;confirmingRemoval=true;
        try
        {
            CommitTables();
            if(await new ConfirmWindow($"¿Eliminar «{group.Name}» del corte? Al guardar, la actividad y sus notas pasarán a Recuperar retirados. Ajusta los porcentajes restantes para sumar 100 %.","Eliminar actividad",true){Height=270}.ShowDialog<bool>(this))model.RemoveGroup(group);
        }
        catch(Exception ex){model.ReportSaveError(ex);}
        finally{confirmingRemoval=false;}
    }
    private async void RemoveActivityAtRow(object? sender,RoutedEventArgs e)
    {
        if(confirmingRemoval || sender is not Button {DataContext:SubactivityViewModel activity} || Model is not {} model || model.IsSaving)return;
        confirmingRemoval=true;
        try
        {
            CommitTables();
            if(await new ConfirmWindow($"¿Eliminar «{activity.Name}» de «{model.SelectedGroupTitle}»? Al guardar, sus notas pasarán a Recuperar retirados y cambiará el promedio. Si usas porcentajes internos, ajústalos para sumar 100 %.","Eliminar subactividad",true){Height=285}.ShowDialog<bool>(this))model.RemoveActivity(activity);
        }
        catch(Exception ex){model.ReportSaveError(ex);}
        finally{confirmingRemoval=false;}
    }
}
