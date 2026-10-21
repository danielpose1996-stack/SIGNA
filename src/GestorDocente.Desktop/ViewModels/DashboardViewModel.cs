using System.Collections.ObjectModel;
using System.Collections.Concurrent;
using GestorDocente.Application;
using GestorDocente.Domain;

namespace GestorDocente.Desktop.ViewModels;

public sealed record CutMetricViewModel(int Number,string Average,string Range,string Coverage,double BarWidth)
{
    public static CutMetricViewModel From(CutPerformance cut)=>new(cut.Number,
        !cut.Configured ? "Sin configuración" : cut.Average is null ? "Sin estudiantes" : AcademicFormatting.Grade(cut.Average),
        !cut.Configured ? "Configura 100 % en este corte" : cut.Minimum is null ? "Sin resultados todavía" : $"Mín {AcademicFormatting.Grade(cut.Minimum)} · Máx {AcademicFormatting.Grade(cut.Maximum)}",
        $"{cut.Complete} completos · {cut.Pending} pendientes",Math.Clamp((double)(cut.Average ?? 0m)/5d*160d,0d,160d));
}
public sealed class PerformanceCourseCard
{
    public CoursePerformance Data{get;}
    public PerformanceCourseCard(CoursePerformance data)
    {
        Data=data;
        Cuts=data.Cuts.Select(CutMetricViewModel.From).ToArray();
    }
    public string Name=>Data.Name;
    public string Details=>$"{Data.Code} · {Data.Period}{(Data.Group.Length>0 ? " · Grupo "+Data.Group : "")}";
    public string StudentsLabel=>$"{Data.StudentCount} estudiantes";
    public IReadOnlyList<CutMetricViewModel> Cuts{get;}
}

public sealed class DashboardViewModel : ObservableObject
{
    private readonly CatalogService? catalog;
    private readonly Func<AcademicWorkspace> current;
    private AcademicSemester? selectedSemester;
    private int version;
    private bool applying;
    private bool isLoading;
    private string error="";
    private CancellationTokenSource? cancellation;
    private sealed record CachedCourse(CourseRecord Stamp,PerformanceCourseCard Card);
    private readonly ConcurrentDictionary<string,CachedCourse> cache=new();
    public ObservableCollection<AcademicSemester> Semesters {get;}=[];
    public ObservableCollection<PerformanceCourseCard> Courses {get;}=[];
    public bool HasCatalog=>catalog is not null;
    public bool IsEmpty=>!IsLoading && Courses.Count==0 && Error.Length==0;
    public bool IsLoading{get=>isLoading;private set{if(Set(ref isLoading,value))Raise(nameof(IsEmpty));}}
    public string Error{get=>error;private set{if(Set(ref error,value))Raise(nameof(IsEmpty));}}
    public AcademicSemester? SelectedSemester
    {
        get=>selectedSemester;
        set{if(Set(ref selectedSemester,value) && !applying)_=RefreshAsync();}
    }
    public RelayCommand RefreshCommand{get;}
    public DashboardViewModel(CatalogService? catalog,Func<AcademicWorkspace> current,bool loadImmediately=true)
    {
        this.catalog=catalog;this.current=current;
        RefreshCommand=new(_=>_ = RefreshAsync(force:true),_=>!IsLoading);
        if(catalog is not null)catalog.Changed+=Invalidate;
        if(loadImmediately)_=RefreshAsync();
    }
    public void Invalidate()=>cache.Clear();
    public void Suspend()=>cancellation?.Cancel();
    private PerformanceCourseCard ReadCard(CourseRecord course,CancellationToken token,bool force)
    {
        token.ThrowIfCancellationRequested();
        if(!force && course.Revision>=0 && cache.TryGetValue(course.Id,out var entry) && entry.Stamp==course)return entry.Card;
        var workspace=catalog!.Open(course.Id).Current;token.ThrowIfCancellationRequested();
        var card=new PerformanceCourseCard(AcademicPerformanceProjection.Build(workspace));token.ThrowIfCancellationRequested();
        if(course.Revision>=0)cache[course.Id]=new(course,card);
        return card;
    }
    public async Task RefreshAsync(bool force=false)
    {
        cancellation?.Cancel();
        var requestCancellation=new CancellationTokenSource();cancellation=requestCancellation;
        var token=requestCancellation.Token;
        var request=Interlocked.Increment(ref version);
        var selectedPeriod=SelectedSemester?.Id;
        IsLoading=true;RefreshCommand.Refresh();Error="";
        try
        {
            if(catalog is null)
            {
                var workspace=current();
                var card=await Task.Run(()=>string.IsNullOrWhiteSpace(workspace.CourseName) ? null : new PerformanceCourseCard(AcademicPerformanceProjection.Build(workspace)),token);
                token.ThrowIfCancellationRequested();Semesters.Clear();Courses.Clear();if(card is not null)Courses.Add(card);Raise(nameof(IsEmpty));return;
            }
            var result=await Task.Run(() =>
            {
                token.ThrowIfCancellationRequested();
                var semesters=catalog.Semesters.ToArray();
                var selected=semesters.FirstOrDefault(s=>s.Id==selectedPeriod) ?? semesters.FirstOrDefault();
                var courses=selected is null ? [] : catalog.Courses(selected.Id).ToArray();
                return (Semesters:semesters,Selected:selected,Courses:courses);
            },token);
            token.ThrowIfCancellationRequested();
            applying=true;
            try
            {
                Semesters.Clear();foreach(var semester in result.Semesters)Semesters.Add(semester);
                SelectedSemester=Semesters.FirstOrDefault(s=>s.Id==result.Selected?.Id);
            }
            finally{applying=false;}
            var ids=result.Courses.Select(c=>c.Id).ToHashSet();
            foreach(var id in cache.Keys)if(!ids.Contains(id))cache.TryRemove(id,out _);
            Courses.Clear();
            foreach(var course in result.Courses)
            {
                var card=await Task.Run(()=>ReadCard(course,token,force),token);
                token.ThrowIfCancellationRequested();Courses.Add(card);
            }
            Raise(nameof(IsEmpty));
        }
        catch(OperationCanceledException)when(token.IsCancellationRequested){ /* Una solicitud nueva reemplazó exclusivamente esta lectura. */ }
        catch(Exception ex){if(request==version)Error=ex is AcademicValidationException ? ex.Message : $"No se pudieron calcular las estadísticas ({SafeErrorCode.From(ex)}). Pulsa Actualizar.";}
        finally{if(request==version){IsLoading=false;RefreshCommand.Refresh();cancellation=null;}requestCancellation.Dispose();}
    }
}
