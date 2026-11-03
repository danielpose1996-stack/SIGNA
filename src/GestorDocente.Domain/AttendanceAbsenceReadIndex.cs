namespace GestorDocente.Domain;

/// <summary>Counts only A in active weeks. Applies confirmed changes without touching the workspace.</summary>
public sealed class AttendanceAbsenceReadIndex
{
    private readonly Dictionary<int,HashSet<int>> weeks=[];
    private readonly HashSet<(int Cut,string Student,int Week)> absent=[];
    private readonly Dictionary<(int Cut,string Student),int> counts=[];
    public AttendanceAbsenceReadIndex(IEnumerable<AcademicCut> cuts)
    {
        foreach(var cut in cuts)
        {
            weeks.Add(cut.Number,cut.Weeks.Select(w=>w.Number).ToHashSet());
            foreach(var entry in cut.Attendance.Where(a=>a.State==AttendanceState.Absent && weeks[cut.Number].Contains(a.WeekNumber)))
            {
                if(!absent.Add((cut.Number,entry.StudentId,entry.WeekNumber)))throw new AcademicValidationException("Hay ausencias duplicadas en el corte.");
                counts[(cut.Number,entry.StudentId)]=Count(cut.Number,entry.StudentId)+1;
            }
        }
    }
    public int Count(int cut,string student)=>counts.GetValueOrDefault((cut,student));
    public int Total(string student)=>weeks.Keys.Sum(cut=>Count(cut,student));
    public void ApplyAttendance(int cut,string student,int week,AttendanceState? state)
    {
        if(!weeks.TryGetValue(cut,out var active) || !active.Contains(week))return;
        var key=(cut,student,week);var wasAbsent=absent.Contains(key);var isAbsent=state==AttendanceState.Absent;
        if(wasAbsent==isAbsent)return;
        if(isAbsent)absent.Add(key);else absent.Remove(key);
        counts[(cut,student)]=Count(cut,student)+(isAbsent ? 1 : -1);
    }
}
