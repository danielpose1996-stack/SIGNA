using GestorDocente.Domain;
namespace GestorDocente.Application;

/// <summary>Snapshot-bound preview; only the service can create it.</summary>
public sealed class CutDeletionPreview
{
    internal AcademicWorkspace Source{get;}
    public int Number{get;}
    public string CourseName=>Source.CourseName;
    public decimal Weight{get;}
    public int ActivityCount{get;}
    public int SubactivityCount{get;}
    public int GradeCount{get;}
    public int AttendanceCount{get;}
    public int WeekCount{get;}
    internal CutDeletionPreview(AcademicWorkspace source,AcademicCut cut)
    {
        Source=source;Number=cut.Number;Weight=cut.Weight;
        var groups=cut.Groups.Concat(cut.RemovedGroups).ToArray();
        ActivityCount=groups.Length;
        SubactivityCount=groups.Sum(g=>g.Activities.Count)+cut.RemovedActivities.Count;
        GradeCount=cut.Grades.Count;AttendanceCount=cut.Attendance.Count;
        WeekCount=cut.Weeks.Count+cut.RemovedWeeks.Count;
    }
}
