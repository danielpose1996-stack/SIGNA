using System.Collections.ObjectModel;
using GestorDocente.Application;
using GestorDocente.Domain;
namespace GestorDocente.Desktop.ViewModels;
public sealed class ConfigurationViewModel:ObservableObject
{
    private readonly WorkspaceService service;
    private readonly int cut;
    private readonly Dictionary<string,List<EvaluationActivity>> drafts=[];
    private readonly Dictionary<string,ObservableCollection<EvaluationActivity>> collections=[];
    private readonly ObservableCollection<EvaluationActivity> emptyActivities=[];
    private EvaluationGroup? selectedGroup;
    private string error="";
    public ObservableCollection<EvaluationGroup> Groups{get;}
    public EvaluationGroup? SelectedGroup
    {
        get=>selectedGroup;
        set{if(Set(ref selectedGroup,value))RefreshSelected();}
    }
    public bool HasSelectedGroup=>SelectedGroup is not null;
    public bool UsesSubactivities
    {
        get=>SelectedGroup?.Mode==EvaluationMode.SubActivities;
        set
        {
            if(SelectedGroup is not {} group || value==UsesSubactivities)return;
            if(IsSaving)return;
            if(value)
            {
                group.Mode=EvaluationMode.SubActivities;
                group.Activities=drafts.GetValueOrDefault(group.Id) ?? [new(){Name=$"{group.Name} 1"}];
            }
            else
            {
                drafts[group.Id]=group.Activities;group.Activities=[];group.Mode=EvaluationMode.Direct;
            }
            collections.Remove(group.Id);
            RefreshSelected();
        }
    }
    public string ModeDescription=>UsesSubactivities ? "Las notas de estas subactividades se promedian por igual." : "Una nota directa por estudiante en la tabla. No necesitas crear subactividades.";
    public bool HasModeChanges=>Groups.Any(g=>service.Cut(cut).Groups.Any(old=>old.Id==g.Id && old.Mode!=g.Mode));
    public ObservableCollection<EvaluationActivity> Activities
    {
        get
        {
            if(SelectedGroup is not {} group || !UsesSubactivities)return emptyActivities;
            if(!collections.TryGetValue(group.Id,out var items))collections[group.Id]=items=new(group.Activities);
            return items;
        }
    }
    private bool isSaving;
    private string errorDetails="";
    public bool IsSaving{get=>isSaving;private set{if(Set(ref isSaving,value)){Raise(nameof(CanEdit));Raise(nameof(SaveLabel));}}}
    public bool CanEdit=>!IsSaving;
    public string SaveLabel=>IsSaving ? "Guardando…" : "Guardar configuración";
    public string ErrorDetails{get=>errorDetails;private set=>Set(ref errorDetails,value);}
    public string Error{get=>error;private set=>Set(ref error,value);}
    public ConfigurationViewModel(WorkspaceService service,int cut,EvaluationGroup? restore=null)
    {
        this.service=service;this.cut=cut;Groups=new(WorkspaceService.Clone(service.Cut(cut).Groups));
        if(restore is not null)Groups.Add(WorkspaceService.Clone(restore));SelectedGroup=Groups.LastOrDefault();
    }
    public void AddGroup()
    {
        if(IsSaving)return;
        var group=new EvaluationGroup{Name="Nueva actividad",Weight=1};Groups.Add(group);SelectedGroup=group;Error="";
    }
    public void AddActivity()
    {
        if(IsSaving || SelectedGroup is not {} group)return;
        if(!UsesSubactivities){UsesSubactivities=true;return;}
        var items=Activities;var activity=new EvaluationActivity{Name=$"{group.Name} {group.Activities.Count+1}"};
        group.Activities.Add(activity);items.Add(activity);Error="";
    }
    public void RemoveGroup(EvaluationGroup group){if(IsSaving)return;Groups.Remove(group);collections.Remove(group.Id);SelectedGroup=Groups.FirstOrDefault();}
    public void RemoveActivity(EvaluationActivity activity)
    {
        if(IsSaving || SelectedGroup is not {} group)return;
        if(group.Activities.Remove(activity))Activities.Remove(activity);
    }
    public bool Save()
    {
        if(IsSaving)return false;
        var before=service.Current;
        try{service.ConfigureGroups(cut,Groups.ToList());Error="";ErrorDetails="";return true;}
        catch(Exception ex){ReportSaveError(ex,service.Current!=before);return false;}
    }
    public async Task<bool> SaveAsync()
    {
        if(IsSaving)return false;
        IsSaving=true;Error="";ErrorDetails="";var before=service.Current;
        try{await service.ConfigureGroupsAsync(cut,Groups.ToList());return true;}
        catch(Exception ex){ReportSaveError(ex,service.Current!=before);return false;}
        finally{IsSaving=false;}
    }
    public void ReportSaveError(Exception ex,bool committed=false)
    {
        Error=committed ? "Los datos se guardaron, pero falló la actualización de la pantalla. Cierra esta ventana y pulsa Actualizar. Consulta los detalles del error."
            : ex is AcademicValidationException ? ex.Message
            : ex is UnauthorizedAccessException ? "No se guardó: no tienes permiso para escribir en el almacenamiento local."
            : ex is IOException or System.Data.Common.DbException ? $"No se guardó la configuración: {ex.Message}"
            : $"No se guardó la configuración por un error inesperado ({ex.GetType().Name}): {ex.Message}";
        ErrorDetails=ex.ToString();
    }
    private void RefreshSelected()
    {
        foreach(var name in new[]{nameof(HasSelectedGroup),nameof(UsesSubactivities),nameof(Activities),nameof(ModeDescription)})Raise(name);
    }
}
