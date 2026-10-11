namespace GestorDocente.Domain;

public static class AcademicRules
{
    public static void ValidateGrade(decimal value)
    {
        if(value is <0 or >5) throw new AcademicValidationException("La nota debe estar entre 0,0 y 5,0.");
    }
    public static void ValidateWeekCount(int currentCount)
    {
        if(currentCount is <0 or >=9) throw new AcademicValidationException("Cada corte admite un máximo de nueve semanas.");
    }
    public static void ValidateGroups(IReadOnlyList<EvaluationGroup> groups)
    {
        if(groups.Count==0 || groups.Sum(x=>x.Weight)!=100) throw new AcademicValidationException("Los porcentajes de los grupos deben sumar 100 %.");
        if(groups.Select(x=>x.Id).Distinct().Count()!=groups.Count) throw new AcademicValidationException("Hay grupos duplicados.");
        var activityIds = new HashSet<string>();
        foreach(var group in groups)
        {
            Name(group.Name,"grupo"); Weight(group.Weight);
            if(group.IsPartial && group.Activities.Count!=1) throw new AcademicValidationException("El parcial admite exactamente una actividad y una nota por estudiante.");
            if(group.Activities.Count==0 || group.Activities.Sum(x=>x.Weight)!=100) throw new AcademicValidationException($"Los pesos de las actividades de {group.Name} deben sumar 100 %.");
            foreach(var activity in group.Activities)
            {
                Name(activity.Name,"actividad"); Weight(activity.Weight);
                if(!activityIds.Add(activity.Id)) throw new AcademicValidationException("Hay actividades duplicadas.");
                if(activity.Rubric is not null) ValidateRubric(activity.Rubric);
            }
        }
    }
    public static void ValidateRubric(RubricDefinition rubric)
    {
        if(rubric.Criteria.Count==0 || rubric.Criteria.Sum(x=>x.Weight)!=100) throw new AcademicValidationException("Los pesos de los criterios deben sumar exactamente 100 %.");
        if(rubric.Criteria.Select(x=>x.Id).Distinct().Count()!=rubric.Criteria.Count) throw new AcademicValidationException("Hay criterios duplicados.");
        foreach(var criterion in rubric.Criteria) {Name(criterion.Name,"criterio"); Weight(criterion.Weight);}
        if(rubric.Levels.Count==0) throw new AcademicValidationException("Agrega al menos un nivel de valoración.");
        var levels=rubric.Levels.OrderBy(x=>x.Lower).ToList();
        decimal next=0;
        foreach(var level in levels)
        {
            Name(level.Name,"nivel");
            if(level.Lower!=next || level.Upper<=level.Lower || level.Upper>100) throw new AcademicValidationException("Los niveles deben cubrir de 0 a 100 %, sin huecos ni superposiciones.");
            next=level.Upper;
        }
        if(next!=100) throw new AcademicValidationException("La escala debe terminar en 100 %.");
    }
    private static void Name(string value,string field)
    {
        if(string.IsNullOrWhiteSpace(value)) throw new AcademicValidationException($"El nombre del {field} es obligatorio.");
    }
    private static void Weight(decimal value)
    {
        if(value is <=0 or >100) throw new AcademicValidationException("Cada peso debe ser mayor que 0 y no superar 100 %.");
    }
}
