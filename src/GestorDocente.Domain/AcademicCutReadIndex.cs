namespace GestorDocente.Domain;

/// <summary>Índice de un corte; permite aplicar cambios individuales ya persistidos sin reconstruirlo.</summary>
public sealed class AcademicCutReadIndex
{
    private readonly AcademicCut cut;
    private readonly Dictionary<(string Student,string Activity),decimal> grades;
    private readonly Dictionary<(string Student,int Week),AttendanceState> attendance;
    private readonly bool groupsValid;

    public AcademicCutReadIndex(AcademicCut cut)
    {
        this.cut=cut;
        grades=new(cut.Grades.Count);
        foreach(var grade in cut.Grades)
            if(!grades.TryAdd((grade.StudentId,grade.ActivityId),grade.Value))
                throw new AcademicValidationException("Hay notas duplicadas en el corte.");
        attendance=new(cut.Attendance.Count);
        foreach(var entry in cut.Attendance)
            if(!attendance.TryAdd((entry.StudentId,entry.WeekNumber),entry.State))
                throw new AcademicValidationException("Hay asistencias duplicadas en el corte.");
        if(cut.Groups.Count==0)return;
        try{AcademicRules.ValidateGroups(cut.Groups,true);groupsValid=true;}
        catch(AcademicValidationException){groupsValid=false;}
    }
    public decimal? Grade(string studentId,string activityId)
    {
        if(!grades.TryGetValue((studentId,activityId),out var value))return null;
        AcademicRules.ValidateGrade(value);return value;
    }
    public AttendanceState? Attendance(string studentId,int week)=>
        attendance.TryGetValue((studentId,week),out var value) ? value : null;
    public decimal? AttendanceGrade(string studentId)=>
        cut.AttendanceGrading?.Average(cut.Weeks.Select(w=>Attendance(studentId,w.Number)));
    public void ApplyGrade(string studentId,string activityId,decimal? value)
    {
        if(value is { } grade){AcademicRules.ValidateGrade(grade);grades[(studentId,activityId)]=grade;}
        else grades.Remove((studentId,activityId));
    }
    public void ApplyAttendance(string studentId,int week,AttendanceState? state)
    {
        if(state is { } value)attendance[(studentId,week)]=value;
        else attendance.Remove((studentId,week));
    }
    public decimal? Group(EvaluationGroup group,string studentId)
    {
        if(group.Mode==EvaluationMode.Direct)return Grade(studentId,group.Id) ?? 0m;
        if(group.Activities.Count==0)return null;
        decimal sum=0;
        foreach(var activity in group.Activities)
        {
            var value=Grade(studentId,activity.Id);
            sum+=value ?? 0m;
        }
        return sum/group.Activities.Count;
    }
    public bool HasPendingGrades(EvaluationGroup group,string studentId)=>group.Mode==EvaluationMode.Direct
        ? Grade(studentId,group.Id) is null
        : group.Activities.Count==0 || group.Activities.Any(activity=>Grade(studentId,activity.Id) is null);
    public bool HasPendingGrades(string studentId)=>cut.Groups.Count==0 || cut.Groups.Any(group=>HasPendingGrades(group,studentId));
    public decimal? Cut(string studentId)
    {
        if(!groupsValid)return null;
        decimal sum=0;
        foreach(var group in cut.Groups)
        {
            var value=Group(group,studentId);
            if(value is null)return null;
            sum+=value.Value*group.Weight/100;
        }
        return sum;
    }
}
