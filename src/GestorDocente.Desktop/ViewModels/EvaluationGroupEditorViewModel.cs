using GestorDocente.Domain;

namespace GestorDocente.Desktop.ViewModels;

/// <summary>Campos del borrador visual; el caso de uso existente valida y persiste al guardar.</summary>
public sealed class EvaluationGroupEditorViewModel(EvaluationGroup group,Action changed):ObservableObject
{
    public EvaluationGroup Group{get;}=group;
    private string? weightInput;
    public string Name
    {
        get=>Group.Name;
        set{if(Group.Name==value)return;Group.Name=value;Raise();changed();}
    }
    public string WeightInput
    {
        get=>weightInput ?? Group.Weight.ToString("0.############################",AcademicFormatting.Culture);
        set
        {
            value??="";
            if(WeightInput==value)return;
            weightInput=value;
            if(AcademicFormatting.TryDecimal(value,out var weight) && weight is >0 and <=100)Group.Weight=weight;
            Raise();Raise(nameof(WeightError));changed();
        }
    }
    public string WeightError=>!AcademicFormatting.TryDecimal(WeightInput,out var weight) || weight is <=0 or >100
        ? "Escribe un porcentaje mayor que 0 y hasta 100 %." : "";
    public string DetailLabel=>Group.Mode==EvaluationMode.Direct ? "Nota directa · Ver opciones"
        : $"{Group.Activities.Count} subactividades · {(Group.UseWeightedAverage ? "Ponderadas" : "Promedio simple")}";
    public void RefreshDetails()=>Raise(nameof(DetailLabel));
}
