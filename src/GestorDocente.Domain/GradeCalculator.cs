namespace GestorDocente.Domain;
public static class GradeCalculator
{
    public static decimal? Group(AcademicCut cut,EvaluationGroup group,string studentId)
    {
        if(group.Mode==EvaluationMode.Direct)return Value(cut,studentId,group.Id);
        if(group.Activities.Count==0)return null;
        decimal total=0;
        foreach(var activity in group.Activities)
        {
            var grade=Value(cut,studentId,activity.Id);
            if(grade is null)return null;
            total+=grade.Value;
        }
        return total/group.Activities.Count;
    }
    private static decimal? Value(AcademicCut cut,string studentId,string id)
    {
        var grade=cut.Grades.SingleOrDefault(x=>x.StudentId==studentId && x.ActivityId==id);
        if(grade is not null)AcademicRules.ValidateGrade(grade.Value);
        return grade?.Value;
    }
    public static decimal? Cut(AcademicCut cut,string studentId)
    {
        if(cut.Groups.Count==0)return null;
        try{AcademicRules.ValidateGroups(cut.Groups,true);}catch(AcademicValidationException){return null;}
        decimal total=0;
        foreach(var group in cut.Groups)
        {
            var value=Group(cut,group,studentId);
            if(value is null)return null;
            total+=value.Value*group.Weight/100;
        }
        return total;
    }
}
