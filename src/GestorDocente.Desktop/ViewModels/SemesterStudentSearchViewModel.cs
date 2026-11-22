using GestorDocente.Application;
using GestorDocente.Domain;

namespace GestorDocente.Desktop.ViewModels;

public sealed class SemesterStudentSearchViewModel:ObservableObject
{
    private readonly CatalogService? catalog;
    private readonly DeferredUiAction searchUpdate;
    private AcademicSemester? semester;
    private SemesterStudentIndex? index;
    private int loadVersion;
    private string searchInput="",error="";
    private bool isLoading;
    public ObservableRangeCollection<SemesterStudentMatch> Results{get;}=[];
    public bool HasSemester=>semester is not null;
    public string PeriodLabel=>semester is null ? "Selecciona un período" : $"Período {semester.Name}";
    public string SearchInput
    {
        get=>searchInput;
        set
        {
            if(!Set(ref searchInput,value ?? ""))return;
            Raise(nameof(HasQuery));ClearCommand.Refresh();
            if(!HasQuery){searchUpdate.Cancel();Filter();}else searchUpdate.Schedule();
        }
    }
    public bool HasQuery=>!string.IsNullOrWhiteSpace(SearchInput);
    public bool IsLoading{get=>isLoading;private set{if(Set(ref isLoading,value)){NotifyResults();RetryCommand.Refresh();}}}
    public string Error{get=>error;private set{if(Set(ref error,value))NotifyResults();}}
    public bool ShowResults=>HasQuery && !IsLoading && index is not null && Error.Length==0;
    public bool NoMatches=>ShowResults && Results.Count==0;
    public string ResultsLabel=>Results.Count==1 ? "1 estudiante encontrado" : $"{Results.Count} estudiantes encontrados";
    public RelayCommand ClearCommand{get;}
    public RelayCommand RetryCommand{get;}

    public SemesterStudentSearchViewModel(CatalogService? catalog)
    {
        this.catalog=catalog;
        searchUpdate=new(TimeSpan.FromMilliseconds(100),Filter);
        ClearCommand=new(_=>SearchInput="",_=>HasQuery);
        RetryCommand=new(_=>_ = LoadAsync(semester),_=>HasSemester && !IsLoading);
    }
    public void ClearScope(AcademicSemester? selected)
    {
        loadVersion++;searchUpdate.Cancel();semester=selected;index=null;
        Results.ReplaceAll([]);Error="";IsLoading=false;
        Raise(nameof(HasSemester));Raise(nameof(PeriodLabel));NotifyResults();RetryCommand.Refresh();
    }
    public async Task LoadAsync(AcademicSemester? selected)
    {
        ClearScope(selected);
        if(catalog is null || selected is null)return;
        var version=loadVersion;IsLoading=true;
        try
        {
            var loaded=await catalog.StudentIndexAsync(selected.Id);
            if(version!=loadVersion)return;
            index=loaded;searchUpdate.Cancel();Filter();
        }
        catch(Exception ex)
        {
            if(version==loadVersion)Error=ex is AcademicValidationException ? ex.Message :
                $"No se pudieron consultar los estudiantes de este período ({SafeErrorCode.From(ex)}). Pulsa Reintentar.";
        }
        finally{if(version==loadVersion){IsLoading=false;NotifyResults();}}
    }
    public void FlushSearch()=>searchUpdate.Flush();
    private void Filter()
    {
        var found=index?.Find(SearchInput) ?? [];
        if(!Results.SequenceEqual(found))Results.ReplaceAll(found);
        NotifyResults();
    }
    private void NotifyResults(){Raise(nameof(ShowResults));Raise(nameof(NoMatches));Raise(nameof(ResultsLabel));}
}
