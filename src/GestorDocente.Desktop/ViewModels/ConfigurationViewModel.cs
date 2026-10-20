using System.Collections.ObjectModel;
using GestorDocente.Application;
using GestorDocente.Domain;
namespace GestorDocente.Desktop.ViewModels;
public sealed class ConfigurationViewModel:ObservableObject
{
    private readonly WorkspaceService service;
    private readonly int cut;
    private readonly Dictionary<string,List<EvaluationActivity>> drafts=[];
    private readonly Dictionary<string,ObservableCollection<SubactivityViewModel>> collections=[];
    private readonly ObservableCollection<SubactivityViewModel> emptyActivities=[];
    private EvaluationGroup? selectedGroup;
    private string error="";
    public ObservableCollection<EvaluationGroup> Groups{get;}
    public ObservableCollection<EvaluationGroupEditorViewModel> GroupEditors{get;}
    public string Context=>$"{service.Current.CourseName} · Corte {cut}";
    public bool GroupsEmpty=>Groups.Count==0;
    public string SelectedGroupTitle=>SelectedGroup?.Name ?? "Detalle de la actividad";
    public string MainWeightSummary=>$"Total del corte: {Groups.Sum(g=>g.Weight).ToString("0.############################",AcademicFormatting.Culture)} % de 100 %.";
    public string MainWeightError=>GroupEditors.FirstOrDefault(g=>g.WeightError.Length>0) is {} invalid
        ? $"{invalid.Name}: {invalid.WeightError}"
        : Groups.Count==0 ? "Agrega una actividad para comenzar."
        : Groups.Sum(g=>g.Weight)!=100m ? "Ajusta los porcentajes del corte hasta completar el 100 %." : "";
    public EvaluationGroupEditorViewModel? SelectedGroupEditor
    {
        get=>GroupEditors.FirstOrDefault(g=>g.Group==SelectedGroup);
        set{if(value is not null || Groups.Count==0)SelectedGroup=value?.Group;}
    }
    public EvaluationGroup? SelectedGroup
    {
        get=>selectedGroup;
        set{if(Set(ref selectedGroup,value)){Raise(nameof(SelectedGroupEditor));RefreshSelected();}}
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
    public bool UsesWeightedAverage
    {
        get=>UsesSubactivities && SelectedGroup?.UseWeightedAverage==true;
        set
        {
            if(IsSaving || !UsesSubactivities || SelectedGroup is not {} group || value==UsesWeightedAverage)return;
            group.UseWeightedAverage=value;
            foreach(var row in Activities)row.UsesWeights=value;
            Raise();Raise(nameof(ModeDescription));RefreshWeights();
        }
    }
    public string ModeDescription=>!UsesSubactivities ? "Una nota directa por estudiante en la tabla. No necesitas crear subactividades."
        : UsesWeightedAverage ? "Cada nota aporta su porcentaje interno. El porcentaje principal dentro del corte se conserva."
        : "Las notas de estas subactividades se promedian por igual.";
    public string InternalWeightSummary=>UsesWeightedAverage
        ? $"Total interno: {Activities.Sum(a=>a.Activity.Weight ?? 0m).ToString("0.############################",AcademicFormatting.Culture)} % de 100 %."
        : "Promedio simple: todas las subactividades tienen el mismo valor.";
    public string InternalWeightError=>!UsesWeightedAverage ? "" : Activities.FirstOrDefault(a=>a.WeightError.Length>0) is {} invalid
        ? $"{invalid.Name}: {invalid.WeightError}"
        : Activities.Count==0 || Activities.Sum(a=>a.Activity.Weight ?? 0m)!=100m ? "Los porcentajes internos deben sumar exactamente 100 %." : "";
    public bool HasModeChanges=>Groups.Any(g=>service.Cut(cut).Groups.Any(old=>old.Id==g.Id && old.Mode!=g.Mode));
    public ObservableCollection<SubactivityViewModel> Activities
    {
        get
        {
            if(SelectedGroup is not {} group || !UsesSubactivities)return emptyActivities;
            if(!collections.TryGetValue(group.Id,out var items))collections[group.Id]=items=new(group.Activities.Select(a=>new SubactivityViewModel(a,group.UseWeightedAverage,RefreshWeights)));
            return items;
        }
    }
    private bool isSaving;
    private string errorDetails="";
    public bool IsSaving{get=>isSaving;private set{if(Set(ref isSaving,value)){Raise(nameof(CanEdit));Raise(nameof(SaveLabel));}}}
    public bool CanEdit=>!IsSaving;
    public string SaveLabel=>IsSaving ? "Guardando…" : "Guardar cambios";
    public string ErrorDetails{get=>errorDetails;private set=>Set(ref errorDetails,value);}
    public string Error{get=>error;private set=>Set(ref error,value);}
    public ConfigurationViewModel(WorkspaceService service,int cut,EvaluationGroup? restore=null,RemovedActivity? restoreActivity=null)
    {
        this.service=service;this.cut=cut;Groups=new(WorkspaceService.Clone(service.Cut(cut).Groups));
        if(restore is not null)Groups.Add(WorkspaceService.Clone(restore));
        GroupEditors=new(Groups.Select(g=>new EvaluationGroupEditorViewModel(g,RefreshOverview)));
        if(restoreActivity is not null)
        {
            var group=Groups.SingleOrDefault(g=>g.Id==restoreActivity.GroupId)
                ?? throw new AcademicValidationException("Recupera primero la actividad principal de esta subactividad.");
            if(group.Mode!=EvaluationMode.SubActivities || !group.UseWeightedAverage)
                throw new AcademicValidationException("Actualiza la lista de retirados antes de recuperar esta subactividad.");
            group.Activities.Add(WorkspaceService.Clone(restoreActivity.Activity));SelectedGroup=group;
        }
        else SelectedGroup=Groups.LastOrDefault();
    }
    public void AddGroup()
    {
        if(IsSaving)return;
        var group=new EvaluationGroup{Name="Nueva actividad",Weight=1};Groups.Add(group);GroupEditors.Add(new(group,RefreshOverview));SelectedGroup=group;Error="";RefreshOverview();
    }
    public void AddActivity()
    {
        if(IsSaving || SelectedGroup is not {} group)return;
        if(!UsesSubactivities){UsesSubactivities=true;return;}
        var items=Activities;var activity=new EvaluationActivity{Name=$"{group.Name} {group.Activities.Count+1}"};
        group.Activities.Add(activity);items.Add(new(activity,group.UseWeightedAverage,RefreshWeights));Error="";RefreshWeights();
    }
    public void RemoveGroup(EvaluationGroup group)
    {
        if(IsSaving || !Groups.Remove(group))return;
        var editor=GroupEditors.FirstOrDefault(g=>g.Group==group);if(editor is not null)GroupEditors.Remove(editor);
        collections.Remove(group.Id);drafts.Remove(group.Id);
        if(SelectedGroup==group)SelectedGroup=Groups.FirstOrDefault();
        Error="";RefreshOverview();
    }
    public void RemoveActivity(SubactivityViewModel activity)
    {
        if(IsSaving || SelectedGroup is not {} group)return;
        if(group.Activities.Remove(activity.Activity)){Activities.Remove(activity);Error="";RefreshWeights();}
    }
    public bool Save()
    {
        if(IsSaving)return false;
        var before=service.Current;
        try{ValidateDrafts();service.ConfigureGroups(cut,Groups.ToList());Error="";ErrorDetails="";return true;}
        catch(Exception ex){ReportSaveError(ex,service.Current!=before);return false;}
    }
    public async Task<bool> SaveAsync()
    {
        if(IsSaving)return false;
        IsSaving=true;Error="";ErrorDetails="";var before=service.Current;
        try{ValidateDrafts();await service.ConfigureGroupsAsync(cut,Groups.ToList());return true;}
        catch(Exception ex){ReportSaveError(ex,service.Current!=before);return false;}
        finally{IsSaving=false;}
    }
    public void ReportSaveError(Exception ex,bool committed=false)
    {
        Error=committed || ex is AcademicRefreshException ? "Los datos se guardaron, pero falló la actualización de la pantalla. Cierra esta ventana y pulsa Actualizar. Consulta los detalles del error."
            : ex is AcademicValidationException ? ex.Message
            : ex is UnauthorizedAccessException ? "No se guardó: no tienes permiso para escribir en el almacenamiento local."
            : ex is IOException or System.Data.Common.DbException ? $"No se guardó la configuración ({SafeErrorCode.From(ex)}). Revisa el archivo local y reintenta."
            : $"No se guardó la configuración por un error inesperado ({SafeErrorCode.From(ex)}).";
        ErrorDetails=$"{SafeErrorCode.From(ex)} · código 0x{ex.HResult:X8}";
    }
    private void RefreshSelected()
    {
        foreach(var name in new[]{nameof(HasSelectedGroup),nameof(SelectedGroupTitle),nameof(UsesSubactivities),nameof(UsesWeightedAverage),nameof(Activities),nameof(ModeDescription)})Raise(name);
        RefreshWeights();
    }
    private void RefreshWeights(){Raise(nameof(InternalWeightSummary));Raise(nameof(InternalWeightError));SelectedGroupEditor?.RefreshDetails();}
    private void RefreshOverview(){Raise(nameof(MainWeightSummary));Raise(nameof(MainWeightError));Raise(nameof(GroupsEmpty));Raise(nameof(SelectedGroupTitle));}
    private void ValidateDrafts()
    {
        if(GroupEditors.FirstOrDefault(g=>g.WeightError.Length>0) is {} invalidGroup)
            throw new AcademicValidationException($"Revisa el porcentaje de «{invalidGroup.Name}»: {invalidGroup.WeightError}");
        foreach(var group in Groups.Where(g=>g.Mode==EvaluationMode.SubActivities && g.UseWeightedAverage))
            if(collections.TryGetValue(group.Id,out var rows) && rows.FirstOrDefault(a=>a.WeightError.Length>0) is {} invalid)
                throw new AcademicValidationException($"Revisa el porcentaje de «{invalid.Name}» en «{group.Name}»: {invalid.WeightError}");
    }
}
