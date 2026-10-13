using GestorDocente.Domain;

namespace GestorDocente.Application;

public interface IAttendancePdfExporter
{
    byte[] Export(AcademicWorkspace workspace,int cutNumber,string studentId);
}

public sealed record AttendancePdfLine(int Week,string Kind,string State,DateOnly? Date=null);
public sealed record AttendancePdfReport(
    string Course,string Code,string Period,string School,string Group,int Cut,
    string Student,string Document,IReadOnlyList<AttendancePdfLine> Lines,
    AttendanceSummary Summary,int Unfilled)
{
    public decimal? Percentage=>Summary.Percentage;
    public string Status=>AttendanceFormatting.Status(Summary.Classification);
}

public static class AttendancePdfProjection
{
    public static AttendancePdfReport Build(AcademicWorkspace workspace,int cutNumber,string studentId)
    {
        var cut=workspace.Cuts.SingleOrDefault(c=>c.Number==cutNumber)
            ?? throw new AcademicValidationException("Selecciona un corte válido.");
        var student=workspace.Students.SingleOrDefault(s=>s.Id==studentId)
            ?? throw new AcademicValidationException("Selecciona un estudiante de la materia.");
        var attendance=new Dictionary<int,AttendanceState>();
        foreach(var entry in cut.Attendance.Where(a=>a.StudentId==studentId))
        {
            if(!Enum.IsDefined(entry.State) || !attendance.TryAdd(entry.WeekNumber,entry.State))
                throw new AcademicValidationException("Los registros de asistencia del estudiante contienen datos duplicados o inválidos.");
        }
        var states=cut.Weeks.Select(w=>attendance.TryGetValue(w.Number,out var state) ? (AttendanceState?)state : null).ToArray();
        var lines=cut.Weeks.Select((week,i)=>new AttendancePdfLine(week.Number,
            week.Kind==WeekKind.Class ? "Clase" : "Actividades",states[i] switch
            {
                AttendanceState.Present=>"P - Presente",AttendanceState.Absent=>"A - Ausente",
                AttendanceState.Justified=>"J - Justificada",AttendanceState.Late=>"T - Tarde",
                AttendanceState.NoClass=>"Sin clase",_=>"Sin diligenciar"
            },week.Date)).ToArray();
        return new(workspace.CourseName,workspace.CourseCode,workspace.Period,workspace.School,
            workspace.GroupName,cutNumber,student.FullName,student.Document,lines,
            AttendanceSummary.From(states),states.Count(s=>s is null));
    }
}
