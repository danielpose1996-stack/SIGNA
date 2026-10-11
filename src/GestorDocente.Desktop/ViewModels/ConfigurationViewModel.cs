using System.Collections.ObjectModel;
using GestorDocente.Application;
using GestorDocente.Domain;
namespace GestorDocente.Desktop.ViewModels;

public sealed class ConfigurationViewModel : ObservableObject
{
    private readonly WorkspaceService service;
    private readonly int cut;
    private EvaluationGroup? selectedGroup;
    private string error="";
    public ObservableCollection<EvaluationGroup> Groups {get;}
    public EvaluationGroup? SelectedGroup {get=>selectedGroup;set {if(Set(ref selectedGroup,value))Raise(nameof(Activities));}}
    public IEnumerable<EvaluationActivity> Activities=>SelectedGroup?.Activities ?? [];
    public string Error {get=>error;private set=>Set(ref error,value);}
    public ConfigurationViewModel(WorkspaceService service,int cut) {this.service=service;this.cut=cut;Groups=new(WorkspaceService.Clone(service.Cut(cut).Groups));SelectedGroup=Groups.FirstOrDefault();}
    public void AddGroup() {var group=new EvaluationGroup {Name="Nuevo grupo",Weight=1,Activities=[new() {Name="Actividad 1",Weight=100}]};Groups.Add(group);SelectedGroup=group;}
    public void AddActivity()
    {
        if(SelectedGroup is null)return;
        if(SelectedGroup.IsPartial){Error="El parcial admite una sola actividad.";return;}
        SelectedGroup.Activities.Add(new() {Name="Nueva actividad",Weight=1});Raise(nameof(Activities));Error="";
    }
    public void RemoveGroup(EvaluationGroup group) {Groups.Remove(group);SelectedGroup=Groups.FirstOrDefault();}
    public void RemoveActivity(EvaluationActivity activity) {SelectedGroup?.Activities.Remove(activity);Raise(nameof(Activities));}
    public bool Save()
    {
        try {service.ConfigureGroups(cut,Groups.ToList());Error="";return true;}
        catch(Exception ex) when(ex is AcademicValidationException or IOException or System.Data.Common.DbException) {Error=ex is AcademicValidationException ? ex.Message : "No se guardó la configuración.";return false;}
    }
}
