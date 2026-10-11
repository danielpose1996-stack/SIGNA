namespace GestorDocente.Domain;

public static class GradeCalculator
{
    public static decimal? Group(AcademicCut cut, EvaluationGroup group, string studentId)
    {
        decimal total=0;
        foreach(var activity in group.Activities)
        {
            var grade=cut.Grades.SingleOrDefault(x=>x.StudentId==studentId && x.ActivityId==activity.Id);
            if(grade is null) return null;
            AcademicRules.ValidateGrade(grade.Value);
            total+=grade.Value*activity.Weight/100;
        }
        return total;
    }
    public static decimal? Cut(AcademicCut cut,string studentId)
    {
        AcademicRules.ValidateGroups(cut.Groups);
        decimal total=0;
        foreach(var group in cut.Groups)
        {
            var result=Group(cut,group,studentId);
            if(result is null) return null;
            total+=result.Value*group.Weight/100;
        }
        return total;
    }
    public static decimal Rubric(RubricDefinition rubric,IReadOnlyDictionary<string,decimal> scores)
    {
        AcademicRules.ValidateRubric(rubric);
        if(scores.Count!=rubric.Criteria.Count) throw new AcademicValidationException("Completa todos los criterios de la rúbrica.");
        decimal total=0;
        foreach(var criterion in rubric.Criteria)
        {
            if(!scores.TryGetValue(criterion.Id,out var score)) throw new AcademicValidationException($"Falta evaluar {criterion.Name}.");
            AcademicRules.ValidateGrade(score); total+=score*criterion.Weight/100;
        }
        return total;
    }
}
