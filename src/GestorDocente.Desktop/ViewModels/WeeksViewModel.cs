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
    private bool isSaving;
    private bool markAllPresent;
    public bool MarkAllPresent {get=>markAllPresent;set=>Set(ref markAllPresent,value);}
    public string Error {get=>error;private set=>Set(ref error,value);}
    public bool IsSaving {get=>isSaving;private set{if(Set(ref isSaving,value)){Raise(nameof(CanAdd));Raise(nameof(CanEdit));}}}
    public bool CanEdit=>!IsSaving;
    public IReadOnlyList<WeekKindOption> Kinds {get;}=[new("Semana de clase",WeekKind.Class),new("Semana de actividades",WeekKind.Activities)];
    public WeekKindOption SelectedKind {get;set;}
    private DateTime? selectedDate;
    public DateTime? SelectedDate {get=>selectedDate;set=>Set(ref selectedDate,value);}
    public RelayCommand ClearDateCommand {get;}
    private DateOnly? WeekDate=>SelectedDate is {} date ? DateOnly.FromDateTime(date) : null;
    public AttendanceWeek? SelectedWeek {get;set;}
    public ObservableCollection<AttendanceWeek> Weeks {get;}=[];
    public bool CanAdd=>Weeks.Count<9 && !IsSaving;
    public string CountLabel=>$"{Weeks.Count} de 9 semanas · Corte {cut}";
    public WeeksViewModel(WorkspaceService service,int cut){this.service=service;this.cut=cut;SelectedKind=Kinds[0];ClearDateCommand=new(_=>SelectedDate=null);Refresh();}
    public void Add()=>Try(()=>{service.AddWeek(cut,SelectedKind.Kind,WeekDate,MarkAllPresent);MarkAllPresent=false;});
    public void Remove()=>Try(()=>{if(SelectedWeek is {} week)service.RemoveWeek(cut,week.Number);});
    public Task AddAsync()=>TryAsync(async()=>{await service.AddWeekAsync(cut,SelectedKind.Kind,WeekDate,MarkAllPresent);SelectedDate=null;MarkAllPresent=false;});
    public Task RemoveAsync()=>TryAsync(()=>SelectedWeek is {} week ? service.RemoveWeekAsync(cut,week.Number) : Task.CompletedTask);
    private void Try(Action action){var before=service.Current;try{action();Error="";Refresh();}catch(Exception ex){Error=WeekError(ex,service.Current!=before);if(service.Current!=before)Refresh();}}
    private async Task TryAsync(Func<Task> action)
    {
        if(IsSaving)return;
        IsSaving=true;var before=service.Current;
        try{await action();Error="";Refresh();}
        catch(Exception ex){Error=WeekError(ex,service.Current!=before);if(service.Current!=before)Refresh();}
        finally{IsSaving=false;}
    }
    private static string WeekError(Exception ex,bool committed)=>committed || ex is AcademicRefreshException ?
        "El cambio de semana se guardó, pero falló el refresco. Pulsa Actualizar." :
        ex is AcademicValidationException ? ex.Message : $"No se guardó el cambio de semanas ({SafeErrorCode.From(ex)}). Reintenta.";
    private void Refresh(){Weeks.Clear();foreach(var week in service.Cut(cut).Weeks)Weeks.Add(week);SelectedWeek=Weeks.FirstOrDefault();Raise(nameof(SelectedWeek));Raise(nameof(CanAdd));Raise(nameof(CountLabel));}
}

public sealed record RecoveryItem(string Kind,string Name,string Id,int Week=0);
public sealed class RecoveryViewModel : ObservableObject
{
    private readonly WorkspaceService service;
    private readonly int cut;
    private string error="";
    private bool isSaving;
    public string Error {get=>error;private set=>Set(ref error,value);}
    public void ReportError(string message)=>Error=message;
    public bool IsSaving{get=>isSaving;private set{if(Set(ref isSaving,value))Raise(nameof(CanEdit));}}
    public bool CanEdit=>!IsSaving;
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
        }catch(Exception ex){Error=ex is AcademicValidationException or AcademicRefreshException ? ex.Message : $"No se recuperaron los registros ({SafeErrorCode.From(ex)}).";}
    }
    public async Task RestoreAsync()
    {
        if(IsSaving || Selected is not {} item)return;
        if(item.Kind=="Actividad principal")
        {
            try{GroupRequested?.Invoke(service.Cut(cut).RemovedGroups.Single(g=>g.Id==item.Id));}
            catch(Exception ex){Error=ex is AcademicValidationException ? ex.Message : $"No se abrió la configuración ({SafeErrorCode.From(ex)}).";}
            return;
        }
        IsSaving=true;var before=service.Current;
        try
        {
            if(item.Kind=="Semana")await service.RestoreWeekAsync(cut,item.Week);
            else await service.RestoreActivityAsync(cut,item.Id);
            Error="";Refresh();
        }
        catch(Exception ex)
        {
            Error=service.Current!=before || ex is AcademicRefreshException ?
                "El registro se recuperó, pero falló el refresco. Pulsa Actualizar." :
                ex is AcademicValidationException ? ex.Message : $"No se recuperó el registro ({SafeErrorCode.From(ex)}).";
            if(service.Current!=before)Refresh();
        }
        finally{IsSaving=false;}
    }
    public void Refresh(){Items.Clear();var data=service.Cut(cut);foreach(var g in data.RemovedGroups)Items.Add(new("Actividad principal",g.Name,g.Id));foreach(var a in data.RemovedActivities)Items.Add(new("Subactividad",a.Activity.Name,a.Activity.Id));foreach(var w in data.RemovedWeeks)Items.Add(new("Semana",w.Label,"",w.Number));Selected=Items.FirstOrDefault();Raise(nameof(Selected));Raise(nameof(IsEmpty));}
}
