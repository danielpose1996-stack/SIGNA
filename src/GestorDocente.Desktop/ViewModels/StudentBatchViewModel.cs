using GestorDocente.Application;
using GestorDocente.Domain;

namespace GestorDocente.Desktop.ViewModels;

public sealed class StudentBatchDisplayRow(StudentBatchRow row,bool showPhone):ObservableObject
{
    private bool visible=showPhone;
    public int Number=>row.Number;
    public string FullName=>row.FullName;
    public string Document=>row.Document;
    public string Email=>row.Email;
    public string Error=>row.Error;
    public string PhoneDisplay=>row.Phone.Length==0 ? "" : visible ? row.Phone : "••••••••••";
    public void ShowPhone(bool show){visible=show;Raise(nameof(PhoneDisplay));}
}

public sealed class StudentBatchViewModel:ObservableObject
{
    private readonly WorkspaceService service;
    private StudentBatchPreview? preview;
    private string names="",documents="",emails="",phones="",error="";
    private bool saving,saved,showPhones;
    public StudentBatchViewModel(WorkspaceService service)
    {
        this.service=service;
        PreviewCommand=new(_=>Preview(),_=>CanEdit);
    }
    public string Names{get=>names;set{if(Set(ref names,value ?? ""))Invalidate();}}
    public string Documents{get=>documents;set{if(Set(ref documents,value ?? ""))Invalidate();}}
    public string Emails{get=>emails;set{if(Set(ref emails,value ?? ""))Invalidate();}}
    public string Phones{get=>phones;set{if(Set(ref phones,value ?? ""))Invalidate();}}
    public ObservableRangeCollection<StudentBatchDisplayRow> Rows{get;}=[];
    public bool ShowPhones{get=>showPhones;set{if(Set(ref showPhones,value))foreach(var row in Rows)row.ShowPhone(value);}}
    public string Error{get=>error;private set=>Set(ref error,value);}
    public bool IsSaving{get=>saving;private set{if(Set(ref saving,value))Refresh();}}
    public bool HasSaved=>saved;
    public bool CanEdit=>!IsSaving && !HasSaved;
    public bool CanSave=>CanEdit && preview?.CanSave==true;
    public int Count=>preview?.Rows.Count ?? 0;
    public string Validation=>preview?.Validation ?? "Pega los nombres y, si los tienes, sus documentos y contactos. Pulsa Revisar lista antes de guardar.";
    public RelayCommand PreviewCommand{get;}
    public void Preview()
    {
        if(!CanEdit)return;
        try
        {
            preview=StudentBatchPreview.Build(service.Current,Names,Documents,Emails,Phones);
            Rows.ReplaceAll(preview.Rows.Select(r=>new StudentBatchDisplayRow(r,ShowPhones)));Error="";
        }
        catch(AcademicValidationException ex){preview=null;Rows.ReplaceAll([]);Error=ex.Message;}
        Refresh();
    }
    public async Task<bool> SaveAsync()
    {
        if(!CanSave || preview is null)return false;
        IsSaving=true;var before=service.Current;
        try{await service.AddStudentsAsync(preview,true);saved=true;Raise(nameof(HasSaved));return true;}
        catch(Exception ex)
        {
            if(service.Current!=before || ex is AcademicRefreshException)
            {
                saved=true;Raise(nameof(HasSaved));Error="Los estudiantes se guardaron, pero falló el refresco. Pulsa Actualizar en la materia.";return true;
            }
            Error=ex is AcademicValidationException ? ex.Message : $"No se guardó ningún estudiante ({SafeErrorCode.From(ex)}). La lista se conserva; puedes reintentar.";
            return false;
        }
        finally{IsSaving=false;}
    }
    private void Invalidate(){preview=null;Rows.ReplaceAll([]);Error="";Refresh();}
    private void Refresh()
    {
        foreach(var name in new[]{nameof(CanEdit),nameof(CanSave),nameof(Count),nameof(Validation)})Raise(name);
        PreviewCommand.Refresh();
    }
}
