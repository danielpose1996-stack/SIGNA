using System.Collections.ObjectModel;
using System.Globalization;
using GestorDocente.Application;
using GestorDocente.Domain;

namespace GestorDocente.Desktop.ViewModels;

public static class AcademicFormatting
{
    public static readonly CultureInfo Culture=CultureInfo.GetCultureInfo("es-CO");
    public static string Grade(decimal? grade)=>grade?.ToString("0.00",Culture) ?? "—";
    public static bool TryDecimal(string text,out decimal value)=>decimal.TryParse(text.Trim().Replace(',','.'),NumberStyles.AllowDecimalPoint|NumberStyles.AllowLeadingSign,CultureInfo.InvariantCulture,out value);
}
public sealed class GradeCellViewModel : ObservableObject
{
    private readonly WorkspaceService service;
    private readonly int cut;
    private readonly string studentId,activityId;
    private string input,error="";
    private decimal? originalValue;
    private bool isSaving;
    public string Input {get=>input;set=>Set(ref input,value);}
    public string Error {get=>error;private set=>Set(ref error,value);}
    public bool IsSaving {get=>isSaving;private set{if(Set(ref isSaving,value))Raise(nameof(CanEdit));}}
    public bool CanEdit=>!IsSaving;
    public GradeCellViewModel(WorkspaceService service,int cut,string studentId,string targetId,AcademicCutReadIndex? index=null)
    {
        this.service=service;this.cut=cut;this.studentId=studentId;activityId=targetId;
        var value=(index ?? new AcademicCutReadIndex(service.Cut(cut))).Grade(studentId,targetId);
        originalValue=value;
        input=value?.ToString("0.00",AcademicFormatting.Culture) ?? "";
    }
    public void Commit()
    {
        var before=service.Current;
        try {
            decimal? value=null;
            if(!string.IsNullOrWhiteSpace(Input))
            {
                if(!AcademicFormatting.TryDecimal(Input,out var parsed))throw new AcademicValidationException("Escribe una nota entre 0,0 y 5,0.");
                AcademicRules.ValidateGrade(parsed); value=parsed;
            }
            Error="";
            if(originalValue!=value){service.SetGrade(cut,studentId,activityId,value);originalValue=value;}
            else Input=value?.ToString("0.00",AcademicFormatting.Culture) ?? "";
        } catch(Exception ex) {if(service.Current!=before)originalValue=new AcademicCutReadIndex(service.Cut(cut)).Grade(studentId,activityId);Error=SaveError(ex,service.Current!=before);}
    }
    public async Task CommitAsync()
    {
        if(IsSaving)return;
        var before=service.Current;
        try
        {
            decimal? value=null;
            if(!string.IsNullOrWhiteSpace(Input))
            {
                if(!AcademicFormatting.TryDecimal(Input,out var parsed))throw new AcademicValidationException("Escribe una nota entre 0,0 y 5,0.");
                AcademicRules.ValidateGrade(parsed);value=parsed;
            }
            Error="";
            if(originalValue==value){Input=value?.ToString("0.00",AcademicFormatting.Culture) ?? "";return;}
            IsSaving=true;
            await service.SetGradeAsync(cut,studentId,activityId,value);
            originalValue=value;
        }
        catch(Exception ex){if(service.Current!=before)originalValue=new AcademicCutReadIndex(service.Cut(cut)).Grade(studentId,activityId);Error=SaveError(ex,service.Current!=before);}
        finally{IsSaving=false;}
    }
    private static string SaveError(Exception ex,bool committed)=>
        committed || ex is AcademicRefreshException ? "La nota se guardó, pero falló el refresco. Pulsa Actualizar para verla." :
        ex is AcademicValidationException ? ex.Message :
        ex is UnauthorizedAccessException ? "No se guardó la nota: acceso denegado al archivo local." :
        $"No se guardó la nota ({SafeErrorCode.From(ex)}). Reintenta o revisa el archivo local.";
}
public sealed class GradeRowViewModel
{
    public int Number {get;}
    public Student Student {get;}
    public string Document=>Student.Document;
    public string FullName=>Student.FullName;
    public Dictionary<string,GradeCellViewModel> Cells {get;}
    public string Total {get;}
    public decimal? TotalValue {get;}
    public string Completion {get;}
    public Dictionary<string,string> GroupTotals {get;}
    public GradeRowViewModel(WorkspaceService service,int cut,Student student,int number,AcademicCutReadIndex? index=null)
    {
        Student=student;Number=number;
        var data=service.Cut(cut);index ??=new AcademicCutReadIndex(data);
        GroupTotals=data.Groups.ToDictionary(g=>g.Id,g=>AcademicFormatting.Grade(index.Group(g,student.Id)));
        Cells=data.Groups.SelectMany(g=>g.Mode==EvaluationMode.Direct ? new[]{g.Id} : g.Activities.Select(a=>a.Id)).ToDictionary(id=>id,id=>new GradeCellViewModel(service,cut,student.Id,id,index));
        var total=index.Cut(student.Id);
        TotalValue=total;
        Total=AcademicFormatting.Grade(total);Completion=total is null || index.HasPendingGrades(student.Id) ? "Pendiente" : "Completo";
    }
}
public sealed record AttendanceOption(string Label,AttendanceState? State);
public sealed class AttendanceCellViewModel : ObservableObject
{
    public static IReadOnlyList<AttendanceOption> Options {get;}=[new("—",null),new("P",AttendanceState.Present),new("A",AttendanceState.Absent),new("J",AttendanceState.Justified),new("T",AttendanceState.Late)];
    private AttendanceOption selected;
    private string error="";
    private bool isSaving;
    private readonly Func<AttendanceState?,Task> save;
    public AttendanceOption Selected
    {
        get=>selected;
        set {
            if(value is null || IsSaving || value.State==selected.State && Error.Length==0)return;
            selected=value;Raise();Raise(nameof(Tone));
            _=SaveSelectionAsync(value);
        }
    }
    public string Error {get=>error;private set=>Set(ref error,value);}
    public bool IsSaving {get=>isSaving;private set{if(Set(ref isSaving,value))Raise(nameof(CanEdit));}}
    public bool CanEdit=>!IsSaving;
    public string Tone=>Selected.State switch {AttendanceState.Present=>"#E9FBF1",AttendanceState.Absent=>"#FFF0F0",AttendanceState.Justified=>"#FFF8E6",AttendanceState.Late=>"#F6EEFF",_=>"#F0F4F9"};
    public AttendanceCellViewModel(AttendanceState? state,Func<AttendanceState?,Task> save) {this.save=save;selected=Options.FirstOrDefault(x=>x.State==state) ?? Options[0];}
    private async Task SaveSelectionAsync(AttendanceOption option)
    {
        IsSaving=true;Error="";
        try{await save(option.State);}
        catch(Exception ex){Error=ex is AcademicRefreshException ? "La asistencia se guardó, pero falló el refresco. Pulsa Actualizar." : ex is AcademicValidationException ? ex.Message : $"No se guardó la asistencia ({SafeErrorCode.From(ex)}). Selecciónala de nuevo para reintentar.";}
        finally{IsSaving=false;}
    }
}
public sealed record WeeklyDetail(string Title,string Code,string Description);
public sealed class AttendanceRowViewModel
{
    public decimal? AttendanceGradeValue{get;}
    public string AttendanceGrade=>AcademicFormatting.Grade(AttendanceGradeValue);
    public Student Student {get;}
    public int Number {get;}
    public string Document=>Student.Document;
    public string FullName=>Student.FullName;
    public Dictionary<int,AttendanceCellViewModel> Cells {get;}
    public AttendanceSummary Summary {get;}
    public string Total=>$"{Summary.Attended} / {Summary.Evaluable}";
    public string Percentage=>AttendanceFormatting.Percentage(Summary.Percentage);
    public string Status=>AttendanceFormatting.Status(Summary.Classification);
    public string StatusTone=>Summary.Classification switch
    {
        AttendanceClassification.Excellent=>"#078653",AttendanceClassification.Good=>"#0067D9",
        AttendanceClassification.Regular=>"#966800",AttendanceClassification.Low=>"#B53742",_=>"#60779D"
    };
    public ObservableCollection<WeeklyDetail> WeeklyDetails {get;}
    public AttendanceRowViewModel(WorkspaceService service,int cut,Student student,int number,AcademicCutReadIndex? index=null)
    {
        Student=student;Number=number;
        var data=service.Cut(cut);index ??=new AcademicCutReadIndex(data);
        Cells=data.Weeks.ToDictionary(x=>x.Number,x=>new AttendanceCellViewModel(index.Attendance(student.Id,x.Number),state=>service.SetAttendanceAsync(cut,student.Id,x.Number,state)));
        AttendanceGradeValue=index.AttendanceGrade(student.Id);
        Summary=AttendanceSummary.From(data.Weeks.Select(w=>index.Attendance(student.Id,w.Number)));
        WeeklyDetails=new(data.DisplayWeeks.Select(w=> {
            var state=index.Attendance(student.Id,w.Number);
            var text=state switch {AttendanceState.Present=>"Presente",AttendanceState.Absent=>"Ausente",AttendanceState.Justified=>"Justificada",AttendanceState.Late=>"Tarde",AttendanceState.NoClass=>"Sin clase",_=>"Sin diligenciar"};
            var code=state switch {AttendanceState.Present=>"P",AttendanceState.Absent=>"A",AttendanceState.Justified=>"J",AttendanceState.Late=>"T",_=>"—"};
            return new WeeklyDetail(w.Label,code,text);
        }));
    }
}
