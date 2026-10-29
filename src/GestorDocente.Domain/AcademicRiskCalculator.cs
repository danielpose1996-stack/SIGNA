namespace GestorDocente.Domain;

public enum AcademicRisk { NoCompletedCuts, WeightsPending, Assured, Normal, Delicate, Critical, Unreachable }
public sealed record CutProgress(decimal? Grade,decimal Weight,bool Completed);
public sealed record AcademicRiskResult(decimal? Minimum,AcademicRisk Risk)
{
    public string Label=>Risk switch
    {
        AcademicRisk.Assured=>"Aprobado asegurado", AcademicRisk.Normal=>"Normal",
        AcademicRisk.Delicate=>"Delicado", AcademicRisk.Critical=>"Crítico",
        AcademicRisk.Unreachable=>"No alcanzable", AcademicRisk.WeightsPending=>"Configurar porcentajes",
        _=>"Sin cortes completos"
    };
    // Never display a rounded target lower than the exact required average.
    public decimal? DisplayMinimum=>Minimum is {} value ? value>decimal.MaxValue/100m ? value : decimal.Ceiling(value*100m)/100m : null;
}

public static class AcademicRiskCalculator
{
    public static AcademicRiskResult Calculate(IEnumerable<CutProgress> cuts,bool weightsPending=false)
    {
        var items=cuts.ToArray();
        AcademicRules.ValidateCutWeightValues(items.Select(c=>c.Weight).ToArray());
        foreach(var item in items)
        {
            if(item.Grade is {} grade)AcademicRules.ValidateGrade(grade);
            if(item.Completed && item.Grade is null)throw new AcademicValidationException("Un corte completo debe tener una nota definitiva.");
        }
        if(weightsPending)return new(null,AcademicRisk.WeightsPending);
        AcademicRules.ValidateCutWeights(items.Select(c=>c.Weight));
        if(!items.Any(c=>c.Completed))return new(null,AcademicRisk.NoCompletedCuts);
        var earned=items.Where(c=>c.Completed).Sum(c=>c.Grade!.Value*c.Weight/100m);
        if(earned>=3m)return new(0m,AcademicRisk.Assured);
        var remaining=items.Where(c=>!c.Completed).Sum(c=>c.Weight);
        if(remaining==0)return new(null,AcademicRisk.Unreachable);
        var deficit=(3m-earned)*100m;
        if(remaining<deficit/decimal.MaxValue)return new(null,AcademicRisk.Unreachable);
        var minimum=deficit/remaining;
        return new(minimum,minimum<=3m ? AcademicRisk.Normal : minimum<=4m ? AcademicRisk.Delicate
            : minimum<=5m ? AcademicRisk.Critical : AcademicRisk.Unreachable);
    }
}
