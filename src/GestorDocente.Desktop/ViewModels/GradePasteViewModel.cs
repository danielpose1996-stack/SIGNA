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
    private readonly int assignmentReplacements;
    private GradePastePreview? preview;
    private string input="",error="";
    private bool saving,confirmed,hasSaved;
    public string TargetName{get;}
    public bool AssignAll {get;}
    public string Heading => AssignAll ? "Asignar nota a todos" : "Pegar calificaciones";
    public string SaveLabel => AssignAll ? "Asignar nota" : "Guardar calificaciones";
    public string Context=>$"{source.CourseName} · {source.Period} · Grupo {source.GroupName} · Corte {cut}";
    public string Scope => AssignAll
        ? $"Se aplicará a los {studentIds.Length} estudiantes de la materia, incluidos los que oculta el filtro."
        : $"{studentIds.Length} estudiantes visibles, en el orden actual de la tabla. Si hay una búsqueda aplicada, solo se incluyen sus resultados.";
    public string Input{get=>input;set{if(Set(ref input,value)){preview=null;confirmed=false;Rows.ReplaceAll([]);Error="";Refresh();}}}
    public ObservableRangeCollection<GradePasteRow> Rows{get;}=[];
    public string Error{get=>error;private set=>Set(ref error,value);}
    public bool IsSaving{get=>saving;private set{if(Set(ref saving,value))Refresh();}}
    public bool HasSaved{get=>hasSaved;private set{if(Set(ref hasSaved,value))Refresh();}}
    public bool CanEdit=>!IsSaving && !HasSaved;
    public bool CanPreview=>!AssignAll && CanEdit && studentIds.Length>0;
    public bool HasPreview=>preview is not null;
    public bool RequiresReplacement=>preview?.ReplacedCount>0;
    public bool RequiresConfirmation => AssignAll || RequiresReplacement;
    public string ConfirmationText => AssignAll
        ? "Confirmo aplicar esta nota a todos los estudiantes de la materia."
        : "Confirmo reemplazar las notas existentes de esta columna para los estudiantes de la vista previa.";
    public bool ConfirmReplacement{get=>confirmed;set{if(Set(ref confirmed,value))Raise(nameof(CanSave));}}
    public bool CanSave => CanEdit && (!RequiresConfirmation || ConfirmReplacement) &&
        (AssignAll ? studentIds.Length > 0 && TryAssignment(out _) : preview?.CanSave == true);
    public string Validation => AssignAll
        ? $"Ingresa una nota entre 0,0 y 5,0. Se reemplazarán {assignmentReplacements} notas existentes de esta columna."
        : preview?.Validation ?? "Pega una sola columna y pulsa Vista previa. Aún no se ha guardado ninguna nota.";
    public RelayCommand PreviewCommand{get;}
    public GradePasteViewModel(MainViewModel owner,string targetId,bool assignAll=false)
    {
        this.owner=owner;service=owner.Service;source=service.Current;cut=owner.SelectedCut;this.targetId=targetId;
        AssignAll=assignAll;
        studentIds=assignAll ? source.Students.Select(student=>student.Id).ToArray() : owner.GradeRows.Select(r=>r.Student.Id).ToArray();
        TargetName=GradePastePreview.Build(source,cut,targetId,assignAll ? [] : studentIds,"").TargetName;
        if(assignAll)assignmentReplacements=service.Cut(cut).Grades.Count(grade=>grade.ActivityId==targetId);
        PreviewCommand=new(_=>Preview(),_=>CanPreview);
    }
    private bool TryAssignment(out decimal grade) =>
        AcademicFormatting.TryDecimal(Input, out grade) && grade is >= 0 and <= 5;
    public void Preview()
    {
        if(!CanPreview)return;
        try{preview=GradePastePreview.Build(source,cut,targetId,studentIds,Input);Rows.ReplaceAll(preview.Rows);confirmed=false;Error="";}
        catch(AcademicValidationException ex){preview=null;Rows.ReplaceAll([]);Error=ex.Message;}
        Refresh();
    }
    public async Task<bool> SaveAsync()
    {
        if(!CanSave)return false;
        if(owner.Service!=service || owner.SelectedCut!=cut ||
            (AssignAll ? !ReferenceEquals(service.Current,source) : !studentIds.SequenceEqual(owner.GradeRows.Select(r=>r.Student.Id))))
        {
            Error=AssignAll ? "La materia o el corte cambió. Vuelve a abrir Asignar nota a todos antes de guardar."
                : "La lista o el orden de estudiantes cambió. Cierra y vuelve a abrir Pegar notas antes de guardar.";
            return false;
        }
        IsSaving=true;var before=service.Current;
        try
        {
            var batch=preview;
            if(AssignAll)
            {
                owner.EnsureAcademicEditsSaved("asignar la nota");
                if(!TryAssignment(out var grade))return false;
                batch=GradePastePreview.BuildUniform(source,cut,targetId,grade);
            }
            await service.PasteGradesAsync(batch!,true);
            HasSaved=true;
            return true;
        }
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
        foreach(var name in new[]{nameof(CanEdit),nameof(CanPreview),nameof(HasPreview),nameof(RequiresReplacement),nameof(RequiresConfirmation),nameof(ConfirmReplacement),nameof(CanSave),nameof(Validation)})Raise(name);
        PreviewCommand?.Refresh();
    }
}
