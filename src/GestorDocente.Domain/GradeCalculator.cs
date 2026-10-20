namespace GestorDocente.Domain;
public static class GradeCalculator
{
    public static decimal? Final(AcademicWorkspace workspace,string studentId)
    {
        return Final(workspace.Cuts.Select(c=>(Cut(c,studentId),c.Weight)),workspace.CutWeightsPending);
    }
    public static decimal? Final(IEnumerable<(decimal? Grade,decimal Weight)> cutResults,bool weightsPending=false)
    {
        var results=cutResults.ToArray();
        AcademicRules.ValidateCutWeightValues(results.Select(c=>c.Weight).ToArray());
        foreach(var result in results)if(result.Grade is {} value)AcademicRules.ValidateGrade(value);
        if(weightsPending)return null;
        AcademicRules.ValidateCutWeights(results.Select(c=>c.Weight));
        return results.Sum(c=>(c.Grade ?? 0m)*c.Weight/100m);
    }
    public static decimal? Group(AcademicCut cut,EvaluationGroup group,string studentId)
        =>CalculateGroup(group,id=>Value(cut,studentId,id));
    internal static decimal? CalculateGroup(EvaluationGroup group,Func<string,decimal?> gradeFor)
    {
        if(group.Mode==EvaluationMode.Direct)return gradeFor(group.Id) ?? 0m;
        if(group.Activities.Count==0)return null;
        if(group.UseWeightedAverage)AcademicRules.ValidateSubactivityWeights(group);
        decimal total=0;
        foreach(var activity in group.Activities)
        {
            var grade=gradeFor(activity.Id) ?? 0m;
            total+=group.UseWeightedAverage ? grade*activity.Weight!.Value/100m : grade;
        }
        return group.UseWeightedAverage ? total : total/group.Activities.Count;
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
