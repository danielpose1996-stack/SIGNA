using System.Text.Json.Serialization;

namespace GestorDocente.Domain;

public sealed class AcademicWorkspace
{
    public const int CurrentSchemaVersion=6;
    public int SchemaVersion { get; set; } = CurrentSchemaVersion;
    public bool CutWeightsPending { get; set; }
    public bool IsDemo { get; set; } = true;
    public string CourseId { get; set; } = "";
    public string SemesterId { get; set; } = "";
    public string School { get; set; } = "";
    public string CourseName { get; set; } = "Programación Orientada a Objetos";
    public string CourseCode { get; set; } = "INF-301";
    public string GroupName { get; set; } = "01";
    public string Period { get; set; } = "2026-2";
    public List<Student> Students { get; set; } = [];
    public List<AcademicCut> Cuts { get; set; } = [];
}
public sealed record Student(string Id, string Document, string FullName,string Email="",string Phone="");
public sealed class AcademicCut
{
    public int Number { get; set; }
    public decimal Weight { get; set; }
    public AttendanceGradePolicy? AttendanceGrading { get; set; }
    public List<EvaluationGroup> Groups { get; set; } = [];
    public List<GradeEntry> Grades { get; set; } = [];
    public List<AttendanceWeek> Weeks { get; set; } = [];
    public List<AttendanceEntry> Attendance { get; set; } = [];
    public List<RemovedActivity> RemovedActivities { get; set; } = [];
    public List<EvaluationGroup> RemovedGroups { get; set; } = [];
    public List<AttendanceWeek> RemovedWeeks { get; set; } = [];
    // Number is the immutable historical key. Only this projection renumbers labels.
    [JsonIgnore]
    public IReadOnlyList<AttendanceWeek> DisplayWeeks => Weeks.Select((week,index)=>week with {DisplayNumber=index+1}).ToArray();
}
public enum EvaluationMode { Direct, SubActivities }
public sealed class EvaluationGroup
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Name { get; set; } = "";
    public decimal Weight { get; set; }
    public EvaluationMode Mode { get; set; } = EvaluationMode.Direct;
    public List<EvaluationActivity> Activities { get; set; } = [];
}
public sealed class EvaluationActivity
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
}
public sealed record GradeEntry(string StudentId, string ActivityId, decimal Value);
public sealed record RemovedActivity(string GroupId, EvaluationActivity Activity);
public enum WeekKind { Class, Activities }
public sealed record AttendanceWeek(int Number, WeekKind Kind = WeekKind.Class, DateOnly? Date = null)
{
    [JsonIgnore]
    public int DisplayNumber { get; init; } = Number;
    public string DateLabel => Date?.ToString("dd/MM/yyyy",System.Globalization.CultureInfo.InvariantCulture) ?? "";
    public string Label => $"Semana {DisplayNumber} · {(Kind == WeekKind.Class ? "Clase" : "Actividades")}{(Date is null ? "" : $" · {DateLabel}")}";
    public string ShortLabel => $"Semana {DisplayNumber}\n{(Kind == WeekKind.Class ? "Clase" : "Actividades")}{(Date is null ? "" : $"\n{DateLabel}")}";
}
public enum AttendanceState { Present, Absent, Justified, Late, NoClass }
public sealed record AttendanceEntry(string StudentId, int WeekNumber, AttendanceState State);
public enum AttendanceClassification { NoRecords, Low, Regular, Good, Excellent }
public sealed record AttendanceSummary(int Present, int Absent, int Justified, int Late)
{
    public int Recorded => Present + Absent + Justified + Late;
    public int Attended=>Present+Late;
    public int Evaluable=>Attended+Absent;
    public decimal? Percentage=>Evaluable==0 ? null : Attended*100m/Evaluable;
    public AttendanceClassification Classification=>Percentage switch
    {
        null=>AttendanceClassification.NoRecords,
        100m=>AttendanceClassification.Excellent,
        >=75m=>AttendanceClassification.Good,
        >=50m=>AttendanceClassification.Regular,
        _=>AttendanceClassification.Low
    };
    public static AttendanceSummary From(IEnumerable<AttendanceState?> states)
    {
        var list = states.ToList();
        return new(list.Count(x=>x==AttendanceState.Present), list.Count(x=>x==AttendanceState.Absent), list.Count(x=>x==AttendanceState.Justified), list.Count(x=>x==AttendanceState.Late));
    }
}
public sealed class AcademicValidationException(string message,Exception? innerException=null) : Exception(message,innerException);
