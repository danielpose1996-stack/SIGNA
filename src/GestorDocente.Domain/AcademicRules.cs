namespace GestorDocente.Domain;
public static class AcademicRules
{
    public static void ValidateGrade(decimal value)
    {
        if(value is <0 or >5)throw new AcademicValidationException("La nota debe estar entre 0,0 y 5,0.");
    }
    public static void ValidateWeekCount(int currentCount)
    {
        if(currentCount is <0 or >=9)throw new AcademicValidationException("Cada corte admite un máximo de nueve semanas.");
    }
    public static void ValidateGroups(IReadOnlyList<EvaluationGroup> groups,bool allowEmptyActivities=false)
    {
        if(groups.Count==0 || groups.Sum(g=>g.Weight)!=100)throw new AcademicValidationException("Los porcentajes de las actividades principales deben sumar 100 %.");
        var ids=new HashSet<string>(StringComparer.Ordinal);
        foreach(var group in groups)
        {
            Name(group.Name,"actividad principal");Weight(group.Weight);
            if(string.IsNullOrWhiteSpace(group.Id) || !ids.Add(group.Id))throw new AcademicValidationException("Hay actividades principales duplicadas o sin identificador.");
        }
        foreach(var group in groups)
        {
            if(!Enum.IsDefined(group.Mode))throw new AcademicValidationException("Selecciona nota directa o subactividades.");
            if(group.Mode==EvaluationMode.Direct && group.Activities.Count>0)throw new AcademicValidationException("La nota directa no debe contener subactividades activas.");
            if(group.Mode==EvaluationMode.SubActivities && !allowEmptyActivities && group.Activities.Count==0)throw new AcademicValidationException($"Agrega una subactividad a {group.Name} o utiliza nota directa.");
            foreach(var activity in group.Activities)
            {
                Name(activity.Name,"subactividad");
                if(string.IsNullOrWhiteSpace(activity.Id) || !ids.Add(activity.Id))throw new AcademicValidationException("Hay identificadores de actividades o subactividades duplicados.");
            }
        }
    }
    private static void Name(string value,string field)
    {
        if(string.IsNullOrWhiteSpace(value))throw new AcademicValidationException($"El nombre de la {field} es obligatorio.");
    }
    private static void Weight(decimal value)
    {
        if(value is <=0 or >100)throw new AcademicValidationException("Cada porcentaje debe ser mayor que 0 y no superar 100 %.");
    }
}
