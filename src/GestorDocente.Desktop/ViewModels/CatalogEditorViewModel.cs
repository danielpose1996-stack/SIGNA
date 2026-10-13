using GestorDocente.Application;
using GestorDocente.Domain;
namespace GestorDocente.Desktop.ViewModels;

public sealed class CatalogEditorViewModel(CatalogService catalog,AcademicSemester? semester,bool isSemester,CourseRecord? editing=null):ObservableObject
{
    public bool IsSemester=>isSemester;
    public bool IsCourse=>!IsSemester;
    public bool IsEditingCourse=>editing is not null;
    public string Title=>IsSemester ? "Crear semestre" : IsEditingCourse ? "Editar materia" : "Crear asignatura";
    public int Year{get;set;}=DateTime.Now.Year;
    public int Term{get;set;}=1;
    public IReadOnlyList<int> Terms{get;}=[1,2];
    public string Code{get;set;}=editing?.Code ?? "";
    public string Name{get;set;}=editing?.Name ?? "";
    public string School{get;set;}=editing?.School ?? "";
    public string Group{get;set;}=editing?.Group ?? "";
    public IReadOnlyList<AcademicSemester> Semesters=>catalog.Semesters;
    public AcademicSemester? SelectedSemester{get;set;}=editing is null ? semester : catalog.Semesters.FirstOrDefault(s=>s.Id==editing?.SemesterId);
    public string SavedSemesterId{get;private set;}="";
    public string SavedCourseId{get;private set;}="";
    private string error="";
    private bool isSaving;
    public string Error{get=>error;private set=>Set(ref error,value);}
    public bool IsSaving{get=>isSaving;private set{if(Set(ref isSaving,value))Raise(nameof(CanSave));}}
    public bool CanSave=>!IsSaving;
    public bool Save()
    {
        try
        {
            if(IsSemester)SavedSemesterId=catalog.CreateSemester(Year,Term).Id;
            else
            {
                var saved=editing is null ? catalog.CreateCourse(SelectedSemester?.Id ?? "",Code,Name,School,Group) :
                    catalog.UpdateCourse(editing,SelectedSemester?.Id ?? "",Code,Name,School,Group);
                SavedSemesterId=saved.SemesterId;SavedCourseId=saved.Id;
            }
            return true;
        }
        catch(Exception ex){Error=ex is AcademicRefreshException ? ex.Message : ex is AcademicValidationException ? ex.Message : $"No se guardó ({SafeErrorCode.From(ex)}). Revisa el acceso al archivo local y reintenta.";return false;}
    }
    public async Task<bool> SaveAsync()
    {
        if(IsSaving)return false;
        IsSaving=true;
        try
        {
            if(IsSemester)SavedSemesterId=(await catalog.CreateSemesterAsync(Year,Term)).Id;
            else
            {
                var saved=editing is null ? await catalog.CreateCourseAsync(SelectedSemester?.Id ?? "",Code,Name,School,Group) :
                    await catalog.UpdateCourseAsync(editing,SelectedSemester?.Id ?? "",Code,Name,School,Group);
                SavedSemesterId=saved.SemesterId;SavedCourseId=saved.Id;
            }
            Error="";return true;
        }
        catch(AcademicRefreshException ex)
        {
            // La escritura ya está confirmada. No permitir un segundo INSERT accidental.
            Error=ex.Message;
            SavedSemesterId=ex.CommittedEntity switch
            {
                AcademicSemester saved=>saved.Id,
                CourseRecord saved=>saved.SemesterId,
                _=>SelectedSemester?.Id ?? ""
            };
            if(ex.CommittedEntity is CourseRecord course)SavedCourseId=course.Id;
            return true;
        }
        catch(Exception ex){Error=ex is AcademicValidationException ? ex.Message : $"No se guardó ({SafeErrorCode.From(ex)}). Revisa el acceso al archivo local y reintenta.";return false;}
        finally{IsSaving=false;}
    }
}
