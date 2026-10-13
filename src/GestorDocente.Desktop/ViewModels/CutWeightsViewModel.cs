using System.Globalization;
using GestorDocente.Application;
using GestorDocente.Domain;

namespace GestorDocente.Desktop.ViewModels;

public sealed class CutWeightDraft : ObservableObject
{
    public int Number{get;}
    public string Label=>$"Corte {Number}";
    private string weight;
    public string Weight{get=>weight;set=>Set(ref weight,value);}
    public decimal? Value=>decimal.TryParse(Weight.Replace(',','.'),NumberStyles.AllowDecimalPoint|NumberStyles.AllowLeadingSign,CultureInfo.InvariantCulture,out var value) ? value : null;
    public string Error=>Value is null ? "Escribe un porcentaje válido." : Value is <0 or >100 ? "Usa un valor entre 0 y 100 %." : "";
    public CutWeightDraft(int number,decimal value){Number=number;weight=value.ToString("0.##",CultureInfo.CurrentCulture);PropertyChanged+=(_,e)=>{if(e.PropertyName==nameof(Weight)){Raise(nameof(Value));Raise(nameof(Error));}};}
}
public sealed class CutWeightsViewModel : ObservableObject
{
    private readonly WorkspaceService service;
    private bool isSaving;
    private string error="";
    public IReadOnlyList<CutWeightDraft> Cuts{get;}
    public string Error{get=>error;private set=>Set(ref error,value);}
    public bool IsSaving{get=>isSaving;private set{if(Set(ref isSaving,value)){Raise(nameof(CanEdit));Raise(nameof(CanSave));}}}
    public bool CanEdit=>!IsSaving;
    public bool IsValid=>Cuts.All(c=>c.Error.Length==0) && Cuts.Sum(c=>c.Value ?? 0)==100;
    public bool CanSave=>IsValid && !IsSaving;
    public string TotalLabel=>$"Total: {Cuts.Sum(c=>c.Value ?? 0):0.##} % de 100 %";
    public string Validation=>IsValid ? "Ponderación válida." : "Los porcentajes de todos los cortes deben sumar exactamente 100 %.";
    public CutWeightsViewModel(WorkspaceService service)
    {
        this.service=service;Cuts=service.Current.Cuts.OrderBy(c=>c.Number).Select(c=>new CutWeightDraft(c.Number,c.Weight)).ToArray();
        foreach(var cut in Cuts)cut.PropertyChanged+=(_,e)=>{if(e.PropertyName==nameof(CutWeightDraft.Weight)){Raise(nameof(TotalLabel));Raise(nameof(Validation));Raise(nameof(CanSave));}};
    }
    public async Task<bool> SaveAsync()
    {
        if(IsSaving)return false;
        if(!IsValid){Error=Validation;return false;}
        IsSaving=true;Error="";
        try{await service.ConfigureCutWeightsAsync(Cuts.ToDictionary(c=>c.Number,c=>c.Value!.Value));return true;}
        catch(Exception ex){Error=ex is AcademicRefreshException or AcademicValidationException ? ex.Message : $"No se guardaron los porcentajes ({SafeErrorCode.From(ex)}). Reintenta.";return false;}
        finally{IsSaving=false;}
    }
}
