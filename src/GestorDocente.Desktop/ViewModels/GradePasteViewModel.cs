using GestorDocente.Application;
using GestorDocente.Domain;
namespace GestorDocente.Desktop.ViewModels;

public sealed class GradePasteViewModel:ObservableObject
{
    private readonly MainViewModel owner;
    private readonly WorkspaceService service;
    private readonly AcademicWorkspace source;
    private readonly int cut;
    private readonly string targetId;
    private readonly string[] studentIds;
    private GradePastePreview? preview;
    private string input="",error="";
    private bool saving,confirmed,hasSaved;
    public string TargetName{get;}
    public string Context=>$"{source.CourseName} · {source.Period} · Grupo {source.GroupName} · Corte {cut}";
    public string Scope=>$"{studentIds.Length} estudiantes visibles, en el orden actual de la tabla. Si hay una búsqueda aplicada, solo se incluyen sus resultados.";
    public string Input{get=>input;set{if(Set(ref input,value)){preview=null;confirmed=false;Rows.ReplaceAll([]);Error="";Refresh();}}}
    public ObservableRangeCollection<GradePasteRow> Rows{get;}=[];
    public string Error{get=>error;private set=>Set(ref error,value);}
    public bool IsSaving{get=>saving;private set{if(Set(ref saving,value))Refresh();}}
    public bool HasSaved{get=>hasSaved;private set{if(Set(ref hasSaved,value))Refresh();}}
    public bool CanEdit=>!IsSaving && !HasSaved;
    public bool CanPreview=>CanEdit && studentIds.Length>0;
    public bool HasPreview=>preview is not null;
    public bool RequiresReplacement=>preview?.ReplacedCount>0;
    public bool ConfirmReplacement{get=>confirmed;set{if(Set(ref confirmed,value))Raise(nameof(CanSave));}}
    public bool CanSave=>CanEdit && preview?.CanSave==true && (!RequiresReplacement || ConfirmReplacement);
    public string Validation=>preview?.Validation ?? "Pega una sola columna y pulsa Vista previa. Aún no se ha guardado ninguna nota.";
    public RelayCommand PreviewCommand{get;}
    public GradePasteViewModel(MainViewModel owner,string targetId)
    {
        this.owner=owner;service=owner.Service;source=service.Current;cut=owner.SelectedCut;this.targetId=targetId;
        studentIds=owner.GradeRows.Select(r=>r.Student.Id).ToArray();
        TargetName=GradePastePreview.Build(source,cut,targetId,studentIds,"").TargetName;
        PreviewCommand=new(_=>Preview(),_=>CanPreview);
    }
    public void Preview()
    {
        if(!CanPreview)return;
        try{preview=GradePastePreview.Build(source,cut,targetId,studentIds,Input);Rows.ReplaceAll(preview.Rows);confirmed=false;Error="";}
        catch(AcademicValidationException ex){preview=null;Rows.ReplaceAll([]);Error=ex.Message;}
        Refresh();
    }
    public async Task<bool> SaveAsync()
    {
        if(!CanSave || preview is null)return false;
        if(owner.Service!=service || owner.SelectedCut!=cut || !studentIds.SequenceEqual(owner.GradeRows.Select(r=>r.Student.Id)))
        {Error="La lista o el orden de estudiantes cambió. Cierra y vuelve a abrir Pegar notas antes de guardar.";return false;}
        IsSaving=true;var before=service.Current;
        try{await service.PasteGradesAsync(preview,true);HasSaved=true;return true;}
        catch(Exception ex)
        {
            if(service.Current!=before || ex is AcademicRefreshException)
            {HasSaved=true;owner.Message="Las notas se guardaron, pero falló el refresco. Pulsa Actualizar.";return true;}
            Error=ex is AcademicValidationException ? ex.Message : $"No se guardó ninguna nota ({SafeErrorCode.From(ex)}). El listado se conserva; reintenta.";return false;
        }
        finally{IsSaving=false;}
    }
    private void Refresh()
    {
        foreach(var name in new[]{nameof(CanEdit),nameof(CanPreview),nameof(HasPreview),nameof(RequiresReplacement),nameof(ConfirmReplacement),nameof(CanSave),nameof(Validation)})Raise(name);
        PreviewCommand?.Refresh();
    }
}
