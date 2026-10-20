namespace GestorDocente.Domain;
public static class AcademicRules
{
    public static decimal DefaultCutWeight(int number)=>number switch {1 or 2=>30m,3=>40m,_=>0m};
    public static List<AcademicCut> DefaultCuts()=>Enumerable.Range(1,3).Select(n=>new AcademicCut{Number=n,Weight=DefaultCutWeight(n)}).ToList();
    public static void ValidateCutWeights(IEnumerable<decimal> weights)
    {
        var values=weights.ToArray();
        ValidateCutWeightValues(values);
        if(values.Sum()!=100)
            throw new AcademicValidationException("Los porcentajes de todos los cortes deben estar entre 0 y 100 y sumar exactamente 100 %.");
    }
    public static void ValidateCutWeightValues(IReadOnlyList<decimal> values)
    {
        if(values.Count==0 || values.Any(w=>w is <0 or >100))
            throw new AcademicValidationException("La materia debe conservar al menos un corte y sus porcentajes deben estar entre 0 y 100 %.");
    }
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
            ValidateSubactivityWeights(group);
            foreach(var activity in group.Activities)
            {
                Name(activity.Name,"subactividad");
                if(string.IsNullOrWhiteSpace(activity.Id) || !ids.Add(activity.Id))throw new AcademicValidationException("Hay identificadores de actividades o subactividades duplicados.");
            }
        }
    }
    public static void ValidateSubactivityWeights(EvaluationGroup group)
    {
        if(group.Activities.Any(a=>a.Weight is <0 or >100))
            throw new AcademicValidationException($"Los porcentajes internos de «{group.Name}» deben estar entre 0 y 100 %.");
        if(group.Mode!=EvaluationMode.SubActivities || !group.UseWeightedAverage)return;
        if(group.Activities.Count==0 || group.Activities.Any(a=>a.Weight is null) || group.Activities.Sum(a=>a.Weight ?? 0m)!=100m)
            throw new AcademicValidationException($"Asigna un porcentaje a cada subactividad de «{group.Name}». Los porcentajes internos deben sumar exactamente 100 %. Ajusta los valores o desactiva la ponderación interna.");
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
