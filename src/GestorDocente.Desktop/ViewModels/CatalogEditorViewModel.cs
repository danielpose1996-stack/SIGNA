using GestorDocente.Application;
using GestorDocente.Domain;
namespace GestorDocente.Desktop.ViewModels;

public sealed record CourseParameterCutPreview(string Title,string Activities,string AttendanceLabel);

public sealed class CatalogEditorViewModel(CatalogService catalog,AcademicSemester? semester,bool isSemester,CourseRecord? editing=null,
    CourseParameters? availableParameters=null,string parametersSource=""):ObservableObject
{
    public bool IsSemester=>isSemester;
    public bool IsCourse=>!IsSemester;
    public bool IsEditingCourse=>editing is not null;
    public double DialogWidth=>ShowParameters ? 600 : 550;
    public double DialogHeight=>ShowParameters ? 740 : 570;
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
    private CourseParameters? pastedParameters;
    private RelayCommand? pasteParametersCommand,clearParametersCommand;
    public bool ShowParameters=>IsCourse && !IsEditingCourse;
    public bool HasPastedParameters=>pastedParameters is not null;
    public bool CanPasteParameters=>ShowParameters && availableParameters is not null && !IsSaving && !HasPastedParameters;
    public string ParametersHint=>HasPastedParameters ?
        $"Parámetros de {parametersSource}. Revisa el resumen al final del formulario. No se copiarán estudiantes, notas, semanas ni asistencias." :
        availableParameters is null ? "Para reutilizar una configuración, pulsa Copiar parámetros en la tarjeta de cualquier materia y vuelve a Crear materia." :
        $"Disponibles: {parametersSource}. Pulsa Pegar parámetros para utilizarlos en esta materia.";
    public string ParametersWarning=>pastedParameters?.CutWeightsPending==true ?
        "Los porcentajes de los cortes de origen requieren ajuste. Después de crear la materia, revisa Configurar porcentajes hasta completar el 100 %." : "";
    public IReadOnlyList<CourseParameterCutPreview> ParameterPreview{get;private set;}=[];
    public RelayCommand PasteParametersCommand=>pasteParametersCommand??=new(_=>PasteParameters(),_=>CanPasteParameters);
    public RelayCommand ClearParametersCommand=>clearParametersCommand??=new(_=>ClearParameters(),_=>HasPastedParameters && !IsSaving);
    private void PasteParameters()
    {
        if(!CanPasteParameters)return;
        pastedParameters=availableParameters;
        ParameterPreview=pastedParameters!.Cuts.Select(PreviewCut).ToArray();
        NotifyParameters();
    }
    private static CourseParameterCutPreview PreviewCut(CourseCutParameters cut)=>new(
        $"Corte {cut.Number} · {cut.Weight:0.##} % de la nota final",
        cut.Groups.Count==0 ? "Sin actividades configuradas" : string.Join("\n",cut.Groups.Select(DescribeActivity)),
        cut.AttendanceGrading is {} policy ? $"Nota de asistencia activa: P {policy.Present:0.##} · A {policy.Absent:0.##} · J {policy.Justified:0.##} · T {policy.Late:0.##}" :
            "Nota de asistencia desactivada");
    private static string DescribeActivity(CourseActivityParameters activity)
    {
        var label=$"{activity.Name} · {activity.Weight:0.##} %";
        if(activity.Mode==EvaluationMode.Direct)return label+" · Nota directa";
        return label+$" · Promedio {(activity.UseWeightedAverage ? "ponderado" : "simple")}\n"+
            string.Join(" · ",activity.Subactivities.Select(a=>a.Name+(activity.UseWeightedAverage ? $" ({a.Weight:0.##} %)" : "")));
    }
    private void ClearParameters()
    {
        if(IsSaving)return;
        pastedParameters=null;ParameterPreview=[];NotifyParameters();
    }
    private void NotifyParameters()
    {
        Raise(nameof(HasPastedParameters));Raise(nameof(CanPasteParameters));Raise(nameof(ParametersHint));Raise(nameof(ParametersWarning));Raise(nameof(ParameterPreview));
        pasteParametersCommand?.Refresh();clearParametersCommand?.Refresh();
    }
    public string Error{get=>error;private set=>Set(ref error,value);}
    public bool IsSaving{get=>isSaving;private set{if(Set(ref isSaving,value)){Raise(nameof(CanSave));Raise(nameof(CanPasteParameters));pasteParametersCommand?.Refresh();clearParametersCommand?.Refresh();}}}
    public bool CanSave=>!IsSaving;
    public bool Save()
    {
        try
        {
            if(IsSemester)SavedSemesterId=catalog.CreateSemester(Year,Term).Id;
            else
            {
                var saved=editing is null ? catalog.CreateCourse(SelectedSemester?.Id ?? "",Code,Name,School,Group,pastedParameters) :
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
                var saved=editing is null ? await catalog.CreateCourseAsync(SelectedSemester?.Id ?? "",Code,Name,School,Group,pastedParameters) :
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
