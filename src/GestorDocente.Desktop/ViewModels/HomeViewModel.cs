using System.Collections.ObjectModel;
using GestorDocente.Application;
using GestorDocente.Domain;

namespace GestorDocente.Desktop.ViewModels;

public sealed record CourseSummary(string Name, string Code, string Group, string Period, int StudentCount, bool IsDemo, string Id="",string School="")
{
    public string Details => $"Código: {Code}   ·   Período: {Period}{(Group.Length>0 ? $"   ·   Grupo: {Group}" : "")}";
    public string StudentsLabel => $"{StudentCount} estudiantes · 3 cortes";
}

public sealed class HomeViewModel : ObservableObject
{
    private readonly WorkspaceService service;
    private readonly CatalogService? catalog;
    private AcademicSemester? selectedSemester;
    public ObservableCollection<AcademicSemester> Semesters {get;}=[];
    public bool HasCatalog=>catalog is not null;
    public bool CanCreateCourse=>HasCatalog && SelectedSemester is not null;
    public AcademicSemester? SelectedSemester
    {
        get=>selectedSemester;
        set{if(value is null && Semesters.Count>0)return;if(Set(ref selectedSemester,value)){RefreshCourses();Raise(nameof(CanCreateCourse));}}
    }
    public ObservableCollection<CourseSummary> Courses { get; } = [];
    public bool HasCourses => Courses.Count > 0;
    public bool IsEmpty => !HasCourses;

    public HomeViewModel(WorkspaceService service,CatalogService? catalog=null)
    {
        this.service = service;
        this.catalog=catalog;
        Refresh();
        if(catalog is null)service.ChangedDetailed += change=>{if(change.Kind==WorkspaceChangeKind.Students)Refresh();};
        else catalog.Changed+=Refresh;
    }

    public void Refresh()
    {
        if(catalog is not null)
        {
            var previous=SelectedSemester?.Id;Semesters.Clear();foreach(var semester in catalog.Semesters)Semesters.Add(semester);
            selectedSemester=Semesters.FirstOrDefault(s=>s.Id==previous) ?? Semesters.FirstOrDefault();
            Raise(nameof(SelectedSemester));Raise(nameof(CanCreateCourse));RefreshCourses();return;
        }
        Courses.Clear();
        var workspace = service.Current;
        if (!string.IsNullOrWhiteSpace(workspace.CourseName))
            Courses.Add(new(workspace.CourseName, workspace.CourseCode, workspace.GroupName,
                workspace.Period, workspace.Students.Count, workspace.IsDemo));
        Raise(nameof(HasCourses));
        Raise(nameof(IsEmpty));
    }
    private void RefreshCourses()
    {
        Courses.Clear();
        if(catalog is not null && SelectedSemester is not null)
            foreach(var course in catalog.Courses(SelectedSemester.Id))Courses.Add(new(course.Name,course.Code,course.Group,SelectedSemester.Name,course.StudentCount,false,course.Id,course.School));
        Raise(nameof(HasCourses));Raise(nameof(IsEmpty));
    }
}
