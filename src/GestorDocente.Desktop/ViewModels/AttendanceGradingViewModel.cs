using GestorDocente.Application;
using GestorDocente.Domain;
namespace GestorDocente.Desktop.ViewModels;

public sealed class AttendanceStateGradeViewModel(string label,decimal? grade):ObservableObject
{
    public string Label{get;}=label;
    private string input=grade?.ToString("0.##",AcademicFormatting.Culture) ?? "";
    public string Input{get=>input;set{if(Set(ref input,value)){Raise(nameof(Error));Raise(nameof(Value));}}}
    public decimal? Value=>AcademicFormatting.TryDecimal(Input,out var value) && value is >=0 and <=5 ? value : null;
    public string Error=>Value is null ? "Escribe una calificación entre 0,0 y 5,0." : "";
}
public sealed class AttendanceGradingViewModel:ObservableObject
{
    private readonly WorkspaceService service;
    private readonly int cut;
    private readonly AcademicWorkspace source;
    private bool enabled,saving;
    private string error="";
    public IReadOnlyList<AttendanceStateGradeViewModel> StateGrades{get;}
    public string Context=>$"{source.CourseName} · Corte {cut}";
    public bool Enabled{get=>enabled;set{if(Set(ref enabled,value))Raise(nameof(CanSave));}}
    public bool IsSaving{get=>saving;private set{if(Set(ref saving,value)){Raise(nameof(CanEdit));Raise(nameof(CanSave));}}}
    public bool CanEdit=>!IsSaving;
    public bool CanSave=>CanEdit && (!Enabled || StateGrades.All(s=>s.Value is not null));
    public string Error{get=>error;private set=>Set(ref error,value);}
    public AttendanceGradingViewModel(WorkspaceService service,int cut)
    {
        this.service=service;this.cut=cut;source=service.Current;var policy=service.Cut(cut).AttendanceGrading;
        enabled=policy?.Enabled ?? true;
        StateGrades=[new("Presente (P)",policy?.Present),new("Ausente (A)",policy?.Absent),new("Justificada (J)",policy?.Justified),new("Tarde (T)",policy?.Late)];
        foreach(var state in StateGrades)state.PropertyChanged+=(_,_)=>Raise(nameof(CanSave));
    }
    public async Task<bool> SaveAsync()
    {
        if(!CanSave)return false;
        if(!ReferenceEquals(source,service.Current)){Error="La materia cambió. Cierra y vuelve a abrir la configuración de asistencia.";return false;}
        var prior=service.Cut(cut).AttendanceGrading;
        if(!Enabled && prior is null)return true;
        var policy=!Enabled ? prior! with{Enabled=false} : new AttendanceGradePolicy(true,StateGrades[0].Value!.Value,StateGrades[1].Value!.Value,StateGrades[2].Value!.Value,StateGrades[3].Value!.Value);
        IsSaving=true;
        try{await service.ConfigureAttendanceGradingAsync(cut,policy);return true;}
        catch(Exception ex){Error=service.Current!=source || ex is AcademicRefreshException ? "La configuración se guardó, pero falló el refresco. Cierra y pulsa Actualizar." : ex is AcademicValidationException ? ex.Message : $"No se guardó la configuración ({SafeErrorCode.From(ex)}). Reintenta.";return false;}
        finally{IsSaving=false;}
    }
}
