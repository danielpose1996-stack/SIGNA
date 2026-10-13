using System.Collections.ObjectModel;
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
    public DashboardViewModel(CatalogService? catalog,Func<AcademicWorkspace> current)
    {
        this.catalog=catalog;this.current=current;
        RefreshCommand=new(_=>_ = RefreshAsync(),_=>!IsLoading);
        _=RefreshAsync();
    }
    public async Task RefreshAsync()
    {
        var request=Interlocked.Increment(ref version);
        var selectedPeriod=SelectedSemester?.Id;
        IsLoading=true;RefreshCommand.Refresh();Error="";
        try
        {
            var result=await Task.Run(() =>
            {
                if(catalog is null)
                {
                    var workspace=current();
                    return (Semesters:Array.Empty<AcademicSemester>(),Selected:(AcademicSemester?)null,
                        Cards:string.IsNullOrWhiteSpace(workspace.CourseName) ? Array.Empty<PerformanceCourseCard>() :
                            new[]{new PerformanceCourseCard(AcademicPerformanceProjection.Build(workspace))});
                }
                var semesters=catalog.Semesters.ToArray();
                var selected=semesters.FirstOrDefault(s=>s.Id==selectedPeriod) ?? semesters.FirstOrDefault();
                var cards=selected is null ? [] : catalog.Courses(selected.Id)
                    .Select(course=>new PerformanceCourseCard(AcademicPerformanceProjection.Build(catalog.Open(course.Id).Current))).ToArray();
                return (Semesters:semesters,Selected:selected,Cards:cards);
            });
            if(request!=version)return;
            applying=true;
            try
            {
                Semesters.Clear();foreach(var semester in result.Semesters)Semesters.Add(semester);
                SelectedSemester=Semesters.FirstOrDefault(s=>s.Id==result.Selected?.Id);
            }
            finally{applying=false;}
            Courses.Clear();foreach(var card in result.Cards)Courses.Add(card);
            Raise(nameof(IsEmpty));
        }
        catch(Exception ex){if(request==version)Error=ex is AcademicValidationException ? ex.Message : $"No se pudieron calcular las estadísticas ({SafeErrorCode.From(ex)}). Pulsa Actualizar.";}
        finally{if(request==version){IsLoading=false;RefreshCommand.Refresh();}}
    }
}
