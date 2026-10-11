namespace GestorDocente.Domain;

public sealed class AcademicWorkspace
{
    public int SchemaVersion { get; set; } = 1;
    public bool IsDemo { get; set; } = true;
    public string CourseName { get; set; } = "Programación Orientada a Objetos";
    public string CourseCode { get; set; } = "INF-301";
    public string GroupName { get; set; } = "01";
    public string Period { get; set; } = "2026-2";
    public List<Student> Students { get; set; } = [];
    public List<AcademicCut> Cuts { get; set; } = [];
}
public sealed record Student(string Id, string Document, string FullName);
public sealed class AcademicCut
{
    public int Number { get; set; }
    public List<EvaluationGroup> Groups { get; set; } = [];
    public List<GradeEntry> Grades { get; set; } = [];
    public List<AttendanceWeek> Weeks { get; set; } = [];
    public List<AttendanceEntry> Attendance { get; set; } = [];
}
public sealed class EvaluationGroup
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Name { get; set; } = "";
    public decimal Weight { get; set; }
    public bool IsPartial { get; set; }
    public List<EvaluationActivity> Activities { get; set; } = [];
}
public sealed class EvaluationActivity
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public decimal Weight { get; set; }
    public RubricDefinition? Rubric { get; set; }
}
public sealed class RubricDefinition
{
    public List<RubricCriterion> Criteria { get; set; } = [];
    public List<RubricLevel> Levels { get; set; } = [];
}
public sealed class RubricCriterion
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public decimal Weight { get; set; }
}
public sealed class RubricLevel
{
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public decimal Lower { get; set; }
    public decimal Upper { get; set; }
}
public sealed record GradeEntry(string StudentId, string ActivityId, decimal Value, RubricAssessment? Assessment = null);
public sealed record RubricAssessment(RubricDefinition Rubric, Dictionary<string, decimal> Scores);
public sealed record AttendanceWeek(int Number);
public enum AttendanceState { Present, Absent, Justified, Late, NoClass }
public sealed record AttendanceEntry(string StudentId, int WeekNumber, AttendanceState State);
public sealed record AttendanceSummary(int Present, int Absent, int Justified, int Late)
{
    public int Recorded => Present + Absent + Justified + Late;
    public static AttendanceSummary From(IEnumerable<AttendanceState?> states)
    {
        var list = states.ToList();
        return new(list.Count(x=>x==AttendanceState.Present), list.Count(x=>x==AttendanceState.Absent), list.Count(x=>x==AttendanceState.Justified), list.Count(x=>x==AttendanceState.Late));
    }
}
public sealed class AcademicValidationException(string message) : Exception(message);
