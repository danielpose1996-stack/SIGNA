using GestorDocente.Application;
using GestorDocente.Domain;
namespace GestorDocente.Desktop.ViewModels;

public sealed class CatalogEditorViewModel(CatalogService catalog,AcademicSemester? semester,bool isSemester):ObservableObject
{
    public bool IsSemester=>isSemester;
    public bool IsCourse=>!IsSemester;
    public string Title=>IsSemester ? "Crear semestre" : "Crear asignatura";
    public int Year{get;set;}=DateTime.Now.Year;
    public int Term{get;set;}=1;
    public IReadOnlyList<int> Terms{get;}=[1,2];
    public string Code{get;set;}="";
    public string Name{get;set;}="";
    public string School{get;set;}="";
    public string Group{get;set;}="";
    public IReadOnlyList<AcademicSemester> Semesters=>catalog.Semesters;
    public AcademicSemester? SelectedSemester{get;set;}=semester;
    public string SavedSemesterId{get;private set;}="";
    private string error="";
    public string Error{get=>error;private set=>Set(ref error,value);}
    public bool Save()
    {
        try
        {
            if(IsSemester)SavedSemesterId=catalog.CreateSemester(Year,Term).Id;
            else SavedSemesterId=catalog.CreateCourse(SelectedSemester?.Id ?? "",Code,Name,School,Group).SemesterId;
            return true;
        }
        catch(Exception ex)when(ex is AcademicValidationException or IOException or System.Data.Common.DbException){Error=ex is AcademicValidationException ? ex.Message : "No se guardó. Revisa el acceso al archivo local y reintenta.";return false;}
    }
}
