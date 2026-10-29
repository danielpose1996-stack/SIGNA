using GestorDocente.Application;
using GestorDocente.Domain;
namespace GestorDocente.Desktop.ViewModels;

public sealed class CutNameDraft:ObservableObject
{
    public int Number{get;}
    public decimal Weight{get;}
    private string name;
    public string Name{get=>name;set{if(Set(ref name,value)){Raise(nameof(Error));}}}
    public string Error=>string.IsNullOrWhiteSpace(Name) || Name.Length>80 || Name.Any(char.IsControl) ? "Escribe un nombre de 1 a 80 caracteres, sin saltos de línea." : "";
    public string WeightLabel=>$"{Weight:0.##} % de la nota final";
    public CutNameDraft(AcademicCut cut){Number=cut.Number;Weight=cut.Weight;name=cut.Label;}
}

public sealed class CutManagementViewModel:ObservableObject
{
    private readonly WorkspaceService service;
    internal WorkspaceService Service=>service;
    private AcademicWorkspace source;
    private bool busy;
    private string error="";
    public ObservableRangeCollection<CutNameDraft> Cuts{get;}=[];
    public bool IsBusy{get=>busy;private set{if(Set(ref busy,value))Notify();}}
    public string Error{get=>error;private set=>Set(ref error,value);}
    public bool HasChanges=>Cuts.Any(c=>c.Name.Trim()!=source.Cuts.Single(s=>s.Number==c.Number).Label);
    public bool CanEdit=>!IsBusy;
    public bool CanModify=>!IsBusy && !HasChanges;
    public bool CanDelete=>CanModify && Cuts.Count>1;
    public bool CanSave=>!IsBusy && HasChanges && Cuts.All(c=>c.Error.Length==0);
    public string Hint=>HasChanges ? "Guarda los nombres antes de agregar, eliminar o configurar porcentajes." : $"{Cuts.Count} cortes · Total: {Cuts.Sum(c=>c.Weight):0.##} %. Agregar un corte no redistribuye los porcentajes.";
    public event Action<CutDeletionPreview>? DeleteRequested;
    public RelayCommand AddCommand{get;}
    public RelayCommand DeleteCommand{get;}
    public CutManagementViewModel(WorkspaceService service)
    {
        this.service=service;source=service.Current;
        AddCommand=new(_=>_ = AddAsync(),_=>CanModify);
        DeleteCommand=new(value=>RequestDelete(value),value=>CanDelete && value is int n && Cuts.Any(c=>c.Number==n));
        Refresh();
    }
    private void Refresh()
    {
        foreach(var row in Cuts)row.PropertyChanged-=DraftChanged;
        source=service.Current;Cuts.ReplaceAll(source.Cuts.OrderBy(c=>c.Number).Select(c=>new CutNameDraft(c)));
        foreach(var row in Cuts)row.PropertyChanged+=DraftChanged;
        Notify();
    }
    private void DraftChanged(object? sender,System.ComponentModel.PropertyChangedEventArgs e){if(e.PropertyName==nameof(CutNameDraft.Name))Notify();}
    private void Notify(){foreach(var p in new[]{nameof(CanEdit),nameof(CanModify),nameof(CanDelete),nameof(CanSave),nameof(HasChanges),nameof(Hint)})Raise(p);AddCommand.Refresh();DeleteCommand.Refresh();}
    public void ReportError(Exception ex)=>Error=ex is AcademicValidationException or AcademicRefreshException ? ex.Message : $"No se completó la operación ({SafeErrorCode.From(ex)}). Revisa los datos y vuelve a intentarlo.";
    private void RequestDelete(object? value)
    {
        if(!CanDelete || value is not int number)return;
        try{DeleteRequested?.Invoke(service.PreviewCutDeletion(number));}catch(Exception ex){ReportError(ex);}
    }
    public async Task<bool> SaveAsync()
    {
        if(!CanSave)return false;
        IsBusy=true;Error="";
        try{await service.RenameCutsAsync(source,Cuts.ToDictionary(c=>c.Number,c=>c.Name));Refresh();return true;}
        catch(Exception ex){ReportError(ex);return false;}
        finally{IsBusy=false;}
    }
    public async Task AddAsync()
    {
        if(!CanModify)return;IsBusy=true;Error="";
        try{await service.AddCutAsync();Refresh();}catch(Exception ex){ReportError(ex);}finally{IsBusy=false;}
    }
    public async Task<bool> DeleteAsync(CutDeletionPreview preview,bool confirmed)
    {
        if(!CanDelete || !confirmed)return false;IsBusy=true;Error="";
        try{await service.DeleteCutAsync(preview,true);Refresh();return true;}
        catch(Exception ex){ReportError(ex);return false;}finally{IsBusy=false;}
    }
    public void RefreshWeights()=>Refresh();
}
