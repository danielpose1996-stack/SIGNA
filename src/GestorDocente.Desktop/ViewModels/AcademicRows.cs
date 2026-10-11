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
    public string Input {get=>input;set=>Set(ref input,value);}
    public string Error {get=>error;private set=>Set(ref error,value);}
    public GradeCellViewModel(WorkspaceService service,int cut,string studentId,string targetId)
    {
        this.service=service;this.cut=cut;this.studentId=studentId;activityId=targetId;
        var value=service.Cut(cut).Grades.SingleOrDefault(x=>x.StudentId==studentId && x.ActivityId==targetId)?.Value;
        input=value?.ToString("0.00",AcademicFormatting.Culture) ?? "";
    }
    public void Commit()
    {
        try {
            decimal? value=null;
            if(!string.IsNullOrWhiteSpace(Input))
            {
                if(!AcademicFormatting.TryDecimal(Input,out var parsed))throw new AcademicValidationException("Escribe una nota entre 0,0 y 5,0.");
                AcademicRules.ValidateGrade(parsed); value=parsed;
            }
            var old=service.Cut(cut).Grades.SingleOrDefault(x=>x.StudentId==studentId && x.ActivityId==activityId)?.Value;
            Error="";
            if(old!=value)service.SetGrade(cut,studentId,activityId,value);
            else Input=value?.ToString("0.00",AcademicFormatting.Culture) ?? "";
        } catch(Exception ex) when(ex is AcademicValidationException or IOException or System.Data.Common.DbException) {Error=ex is AcademicValidationException ? ex.Message : "No se guardó la nota. Reintenta.";}
    }
}
public sealed class GradeRowViewModel
{
    public int Number {get;}
    public Student Student {get;}
    public string Document=>Student.Document;
    public string FullName=>Student.FullName;
    public Dictionary<string,GradeCellViewModel> Cells {get;}
    public string Total {get;}
    public string Completion {get;}
    public Dictionary<string,string> GroupTotals {get;}
    public GradeRowViewModel(WorkspaceService service,int cut,Student student,int number)
    {
        Student=student;Number=number;
        GroupTotals=service.Cut(cut).Groups.ToDictionary(g=>g.Id,g=>AcademicFormatting.Grade(GradeCalculator.Group(service.Cut(cut),g,student.Id)));
        Cells=service.Cut(cut).Groups.SelectMany(g=>g.Mode==EvaluationMode.Direct ? new[]{g.Id} : g.Activities.Select(a=>a.Id)).ToDictionary(id=>id,id=>new GradeCellViewModel(service,cut,student.Id,id));
        var total=GradeCalculator.Cut(service.Cut(cut),student.Id);
        Total=AcademicFormatting.Grade(total);Completion=total is null ? "Pendiente" : "Completo";
    }
}
public sealed record AttendanceOption(string Label,AttendanceState? State);
public sealed class AttendanceCellViewModel : ObservableObject
{
    public static IReadOnlyList<AttendanceOption> Options {get;}=[new("—",null),new("P",AttendanceState.Present),new("A",AttendanceState.Absent),new("J",AttendanceState.Justified),new("T",AttendanceState.Late)];
    private AttendanceOption selected;
    private string error="";
    private readonly Action<AttendanceState?> save;
    public AttendanceOption Selected
    {
        get=>selected;
        set {
            if(value is null || value.State==selected.State)return;
            try {save(value.State);selected=value;Raise();Raise(nameof(Tone));Error="";}
            catch(Exception ex) when(ex is AcademicValidationException or IOException or System.Data.Common.DbException) {Error=ex is AcademicValidationException ? ex.Message : "No se guardó la asistencia.";Raise();}
        }
    }
    public string Error {get=>error;private set=>Set(ref error,value);}
    public string Tone=>Selected.State switch {AttendanceState.Present=>"#E9FBF1",AttendanceState.Absent=>"#FFF0F0",AttendanceState.Justified=>"#FFF8E6",AttendanceState.Late=>"#F6EEFF",_=>"#F0F4F9"};
    public AttendanceCellViewModel(AttendanceState? state,Action<AttendanceState?> save) {this.save=save;selected=Options.FirstOrDefault(x=>x.State==state) ?? Options[0];}
}
public sealed record WeeklyDetail(string Title,string Code,string Description);
public sealed class AttendanceRowViewModel
{
    public Student Student {get;}
    public int Number {get;}
    public string Document=>Student.Document;
    public string FullName=>Student.FullName;
    public Dictionary<int,AttendanceCellViewModel> Cells {get;}
    public AttendanceSummary Summary {get;}
    public string Total=>$"{Summary.Present} P / {Summary.Recorded}";
    public string Percentage=>"Por definir";
    public string Status=>Summary.Recorded==0 ? "Sin registros" : "Registrado";
    public ObservableCollection<WeeklyDetail> WeeklyDetails {get;}
    public AttendanceRowViewModel(WorkspaceService service,int cut,Student student,int number)
    {
        Student=student;Number=number;
        var data=service.Cut(cut);
        Cells=data.Weeks.ToDictionary(x=>x.Number,x=>new AttendanceCellViewModel(data.Attendance.SingleOrDefault(a=>a.StudentId==student.Id && a.WeekNumber==x.Number)?.State,state=>service.SetAttendance(cut,student.Id,x.Number,state)));
        Summary=AttendanceSummary.From(data.Weeks.Select(w=>data.Attendance.SingleOrDefault(x=>x.StudentId==student.Id && x.WeekNumber==w.Number)?.State));
        WeeklyDetails=new(data.Weeks.Select(w=> {
            var state=data.Attendance.SingleOrDefault(x=>x.StudentId==student.Id && x.WeekNumber==w.Number)?.State;
            var text=state switch {AttendanceState.Present=>"Presente",AttendanceState.Absent=>"Ausente",AttendanceState.Justified=>"Justificada",AttendanceState.Late=>"Tarde",AttendanceState.NoClass=>"Sin clase",_=>"Sin diligenciar"};
            var code=state switch {AttendanceState.Present=>"P",AttendanceState.Absent=>"A",AttendanceState.Justified=>"J",AttendanceState.Late=>"T",_=>"—"};
            return new WeeklyDetail(w.Label,code,text);
        }));
    }
}
