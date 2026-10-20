using GestorDocente.Domain;

namespace GestorDocente.Desktop.ViewModels;

/// <summary>Texto de edición y validación del borrador; no escribe en persistencia.</summary>
public sealed class SubactivityViewModel:ObservableObject
{
    public EvaluationActivity Activity{get;}
    public string Id=>Activity.Id;
    public string Name{get=>Activity.Name;set{if(Activity.Name==value)return;Activity.Name=value;Raise();}}
    private string weightInput;
    private bool usesWeights;
    private readonly Action changed;
    public SubactivityViewModel(EvaluationActivity activity,bool usesWeights,Action changed)
    {
        Activity=activity;this.usesWeights=usesWeights;this.changed=changed;
        weightInput=activity.Weight?.ToString("0.############################",AcademicFormatting.Culture) ?? "";
    }
    public bool UsesWeights{get=>usesWeights;set{if(Set(ref usesWeights,value))Raise(nameof(WeightError));}}
    public string WeightInput
    {
        get=>weightInput;
        set
        {
            if(!Set(ref weightInput,value))return;
            if(string.IsNullOrWhiteSpace(value))Activity.Weight=null;
            else if(AcademicFormatting.TryDecimal(value,out var weight) && weight is >=0 and <=100)Activity.Weight=weight;
            Raise(nameof(WeightError));changed();
        }
    }
    public string WeightError=>!UsesWeights ? "" : string.IsNullOrWhiteSpace(WeightInput) ? "Falta el porcentaje interno."
        : !AcademicFormatting.TryDecimal(WeightInput,out var value) || value is <0 or >100 ? "Escribe un porcentaje entre 0 y 100 %." : "";
}
