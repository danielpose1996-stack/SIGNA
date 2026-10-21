using System.Collections.ObjectModel;
using GestorDocente.Application;
using GestorDocente.Domain;

namespace GestorDocente.Desktop.ViewModels;

public sealed record CourseSummary(string Name, string Code, string Group, string Period, int StudentCount, bool IsDemo, string Id="",string School="",int CutCount=3)
{
    public string Details => $"Código: {Code}   ·   Período: {Period}{(Group.Length>0 ? $"   ·   Grupo: {Group}" : "")}";
    public string StudentsLabel => $"{StudentCount} estudiantes · {CutCount} cortes";
    public string GroupLabel=>string.IsNullOrWhiteSpace(Group) ? "Sin grupo" : Group;
    public bool CanDelete=>!IsDemo && !string.IsNullOrWhiteSpace(Id);
}

public sealed class HomeViewModel : ObservableObject
{
    private readonly WorkspaceService service;
    private readonly CatalogService? catalog;
    private AcademicSemester? selectedSemester;
    private int refreshVersion;
    private bool applyingCatalogSnapshot;
    private bool isLoading;
    private string error="";
    public ObservableCollection<AcademicSemester> Semesters {get;}=[];
    public bool HasCatalog=>catalog is not null;
    public bool CanCreateCourse=>HasCatalog && SelectedSemester is not null;
    public AcademicSemester? SelectedSemester
    {
        get=>selectedSemester;
        set{if(applyingCatalogSnapshot || value is null && Semesters.Count>0)return;if(Set(ref selectedSemester,value)){Raise(nameof(CanCreateCourse));_ = RefreshCoursesAsync();}}
    }
    public ObservableCollection<CourseSummary> Courses { get; } = [];
    public bool HasCourses => Courses.Count > 0;
    public bool IsEmpty => !HasCourses && !IsLoading && Error.Length==0;
    public bool IsLoading{get=>isLoading;private set{if(Set(ref isLoading,value))Raise(nameof(IsEmpty));}}
    public string Error{get=>error;private set{if(Set(ref error,value))Raise(nameof(IsEmpty));}}
    public RelayCommand RefreshCommand{get;}

    public HomeViewModel(WorkspaceService service,CatalogService? catalog=null,bool loadImmediately=true)
    {
        this.service = service;
        this.catalog=catalog;
        RefreshCommand=new(_=>_ = RefreshAsync(),_=>catalog is not null && !IsLoading);
        if(catalog is null)Refresh();else if(loadImmediately)_ = RefreshAsync();
        if(catalog is null)service.ChangedDetailed += change=>{if(change.Kind is WorkspaceChangeKind.Students or WorkspaceChangeKind.Structure)Refresh();};
        else catalog.Changed+=()=>_ = RefreshAsync();
    }

    public void Refresh()
    {
        if(catalog is not null)
        {
            _ = RefreshAsync();return;
        }
        Courses.Clear();
        var workspace = service.Current;
        if (!string.IsNullOrWhiteSpace(workspace.CourseName))
            Courses.Add(new(workspace.CourseName, workspace.CourseCode, workspace.GroupName,
                workspace.Period, workspace.Students.Count, workspace.IsDemo, CutCount:workspace.Cuts.Count));
        Raise(nameof(HasCourses));
        Raise(nameof(IsEmpty));
    }
    public async Task RefreshAsync()
    {
        if(catalog is null){Refresh();return;}
        var version=Interlocked.Increment(ref refreshVersion);
        var previous=selectedSemester?.Id;
        IsLoading=true;RefreshCommand.Refresh();Error="";
        try
        {
            var snapshot=await Task.Run(()=>
            {
                var semesters=catalog.Semesters;
                var selected=semesters.FirstOrDefault(s=>s.Id==previous) ?? semesters.FirstOrDefault();
                var courses=selected is null ? [] : catalog.Courses(selected.Id)
                    .Select(c=>new CourseSummary(c.Name,c.Code,c.Group,selected.Name,c.StudentCount,false,c.Id,c.School,c.CutCount)).ToArray();
                return (semesters,selected,courses);
            });
            if(version!=refreshVersion)return;
            applyingCatalogSnapshot=true;
            try
            {
                Semesters.Clear();foreach(var semester in snapshot.semesters)Semesters.Add(semester);
                selectedSemester=Semesters.FirstOrDefault(s=>s.Id==snapshot.selected?.Id);
                Raise(nameof(SelectedSemester));Raise(nameof(CanCreateCourse));
            }
            finally{applyingCatalogSnapshot=false;}
            Courses.Clear();foreach(var course in snapshot.courses)Courses.Add(course);
            Raise(nameof(HasCourses));Raise(nameof(IsEmpty));
        }
        catch(Exception ex){if(version==refreshVersion)Error=ex is AcademicValidationException ? ex.Message : $"No se pudo listar las materias ({SafeErrorCode.From(ex)}). Pulsa Reintentar.";}
        finally{if(version==refreshVersion){IsLoading=false;RefreshCommand.Refresh();}}
    }
    private async Task RefreshCoursesAsync()
    {
        if(catalog is null)return;
        var version=Interlocked.Increment(ref refreshVersion);
        var semester=selectedSemester;
        IsLoading=true;RefreshCommand.Refresh();Error="";
        try
        {
            var courses=semester is null ? [] : await Task.Run(()=>catalog.Courses(semester.Id)
                .Select(c=>new CourseSummary(c.Name,c.Code,c.Group,semester.Name,c.StudentCount,false,c.Id,c.School,c.CutCount)).ToArray());
            if(version!=refreshVersion)return;
            Courses.Clear();foreach(var course in courses)Courses.Add(course);
        }
        catch(Exception ex){if(version==refreshVersion)Error=ex is AcademicValidationException ? ex.Message : $"No se pudo listar las materias ({SafeErrorCode.From(ex)}). Pulsa Reintentar.";}
        finally{if(version==refreshVersion){IsLoading=false;RefreshCommand.Refresh();}}
        Raise(nameof(HasCourses));Raise(nameof(IsEmpty));
    }
}
