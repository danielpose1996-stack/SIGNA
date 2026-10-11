using System.Collections.ObjectModel;
using GestorDocente.Application;
using GestorDocente.Domain;
namespace GestorDocente.Desktop.ViewModels;

public sealed record WeekKindOption(string Name,WeekKind Kind) {public override string ToString()=>Name;}
public sealed class WeeksViewModel : ObservableObject
{
    private readonly WorkspaceService service;
    private readonly int cut;
    private string error="";
    public string Error {get=>error;private set=>Set(ref error,value);}
    public IReadOnlyList<WeekKindOption> Kinds {get;}=[new("Semana de clase",WeekKind.Class),new("Semana de actividades",WeekKind.Activities)];
    public WeekKindOption SelectedKind {get;set;}
    public AttendanceWeek? SelectedWeek {get;set;}
    public ObservableCollection<AttendanceWeek> Weeks {get;}=[];
    public bool CanAdd=>Weeks.Count<9;
    public string CountLabel=>$"{Weeks.Count} de 9 semanas · Corte {cut}";
    public WeeksViewModel(WorkspaceService service,int cut){this.service=service;this.cut=cut;SelectedKind=Kinds[0];Refresh();}
    public void Add()=>Try(()=>service.AddWeek(cut,SelectedKind.Kind));
    public void Remove()=>Try(()=>{if(SelectedWeek is {} week)service.RemoveWeek(cut,week.Number);});
    private void Try(Action action){try{action();Error="";Refresh();}catch(Exception ex) when(ex is AcademicValidationException or IOException or System.Data.Common.DbException){Error=ex is AcademicValidationException ? ex.Message : "No se guardó el cambio de semanas.";}}
    private void Refresh(){Weeks.Clear();foreach(var week in service.Cut(cut).Weeks)Weeks.Add(week);SelectedWeek=Weeks.FirstOrDefault();Raise(nameof(SelectedWeek));Raise(nameof(CanAdd));Raise(nameof(CountLabel));}
}

public sealed record RecoveryItem(string Kind,string Name,string Id,int Week=0);
public sealed class RecoveryViewModel : ObservableObject
{
    private readonly WorkspaceService service;
    private readonly int cut;
    private string error="";
    public string Error {get=>error;private set=>Set(ref error,value);}
    public ObservableCollection<RecoveryItem> Items {get;}=[];
    public RecoveryItem? Selected {get;set;}
    public bool IsEmpty=>Items.Count==0;
    public event Action<EvaluationGroup>? GroupRequested;
    public RecoveryViewModel(WorkspaceService service,int cut){this.service=service;this.cut=cut;Refresh();}
    public void Restore()
    {
        if(Selected is not {} item)return;
        try{
            if(item.Kind=="Actividad principal"){GroupRequested?.Invoke(service.Cut(cut).RemovedGroups.Single(g=>g.Id==item.Id));return;}
            if(item.Kind=="Semana")service.RestoreWeek(cut,item.Week);else service.RestoreActivity(cut,item.Id);
            Error="";Refresh();
        }catch(Exception ex) when(ex is AcademicValidationException or IOException or System.Data.Common.DbException){Error=ex is AcademicValidationException ? ex.Message : "No se recuperaron los registros.";}
    }
    public void Refresh(){Items.Clear();var data=service.Cut(cut);foreach(var g in data.RemovedGroups)Items.Add(new("Actividad principal",g.Name,g.Id));foreach(var a in data.RemovedActivities)Items.Add(new("Subactividad",a.Activity.Name,a.Activity.Id));foreach(var w in data.RemovedWeeks)Items.Add(new("Semana",w.Label,"",w.Number));Selected=Items.FirstOrDefault();Raise(nameof(Selected));Raise(nameof(IsEmpty));}
}
