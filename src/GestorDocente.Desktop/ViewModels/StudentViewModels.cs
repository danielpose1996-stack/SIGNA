using GestorDocente.Application;
using GestorDocente.Domain;
namespace GestorDocente.Desktop.ViewModels;
public sealed class StudentDetailsViewModel:ObservableObject
{
    private Student student;
    private readonly WorkspaceService? service;
    private bool busy;
    private string error="";
    public Student Student=>student;
    public bool IsBusy{get=>busy;set{if(Set(ref busy,value))Raise(nameof(CanManage));}}
    public bool CanManage=>service is not null && !busy && !service.IsSaving;
    public string Error{get=>error;private set=>Set(ref error,value);}
    private bool phoneVisible;
    public string FullName=>student.FullName;
    public string Document=>student.Document.Length==0 ? "No registrado" : student.Document;
    public string Email=>student.Email.Length==0 ? "No registrado" : student.Email;
    public bool HasPhone=>student.Phone.Length>0;
    public bool PhoneVisible{get=>phoneVisible;private set{if(Set(ref phoneVisible,value)){Raise(nameof(PhoneDisplay));Raise(nameof(EyeLabel));}}}
    public string PhoneDisplay=>!HasPhone ? "No registrado" : PhoneVisible ? student.Phone : "••••••••••";
    public string EyeLabel=>PhoneVisible ? "Ocultar celular" : "Mostrar celular";
    public RelayCommand TogglePhoneCommand{get;}
    public StudentDetailsViewModel(Student student,WorkspaceService? service=null){this.student=student;this.service=service;TogglePhoneCommand=new(_=>PhoneVisible=!PhoneVisible,_=>HasPhone);}
    public StudentEditorViewModel Editor()=>new(service ?? throw new AcademicValidationException("Abre una materia para editar al estudiante."),student);
    public void Refresh()
    {
        if(service is null)return;
        student=service.Current.Students.Single(s=>s.Id==student.Id);PhoneVisible=false;Error="";
        foreach(var name in new[]{nameof(FullName),nameof(Document),nameof(Email),nameof(HasPhone),nameof(PhoneDisplay),nameof(CanManage)})Raise(name);
        TogglePhoneCommand.Refresh();
    }
    public StudentDeletionPreview PreviewDeletion()=>service?.PreviewStudentDeletion(student.Id)
        ?? throw new AcademicValidationException("Abre una materia para eliminar al estudiante.");
    public async Task<bool> DeleteAsync(StudentDeletionPreview preview,bool confirmed)
    {
        if(service is null || IsBusy)return false;IsBusy=true;var before=service.Current;
        try{await service.DeleteStudentAsync(preview,confirmed);return true;}
        catch(Exception ex){Error=ex is AcademicValidationException or AcademicRefreshException ? ex.Message : $"No se eliminó el estudiante ({SafeErrorCode.From(ex)}). Reintenta.";return service.Current!=before;}
        finally{IsBusy=false;}
    }
    public void ReportError(Exception ex)=>Error=ex is AcademicValidationException or AcademicRefreshException ? ex.Message : $"No se completó la operación ({SafeErrorCode.From(ex)}). Reintenta.";
}
public sealed class StudentEditorViewModel:ObservableObject
{
    private readonly WorkspaceService service;
    private readonly Student? original;
    public string Title=>original is null ? "Agregar estudiante" : "Editar información";
    public string ActionLabel=>original is null ? "Agregar estudiante" : "Guardar cambios";
    public string Explanation=>original is null ? "Solo el nombre completo es obligatorio. Se incorporará a las notas y asistencias de esta materia." : "Solo el nombre es obligatorio. Sus notas y asistencias se conservan al editar la información.";
    public StudentEditorViewModel(WorkspaceService service,Student? original=null)
    {
        this.service=service;this.original=original;
        if(original is not null){FullName=original.FullName;Document=original.Document;Email=original.Email;Phone=original.Phone;}
    }
    public string FullName{get;set;}="";
    public string Document{get;set;}="";
    public string Email{get;set;}="";
    public string Phone{get;set;}="";
    private bool phoneVisible;
    public char PhoneMask=>phoneVisible ? '\0' : '●';
    public string EyeLabel=>phoneVisible ? "Ocultar celular" : "Mostrar celular";
    private RelayCommand? toggle;
    public RelayCommand TogglePhoneCommand=>toggle ??=new(_=>{phoneVisible=!phoneVisible;Raise(nameof(PhoneMask));Raise(nameof(EyeLabel));});
    private string error="";private bool saving;
    public string Error{get=>error;private set=>Set(ref error,value);}
    public bool IsSaving{get=>saving;private set{if(Set(ref saving,value))Raise(nameof(CanEdit));}}
    public bool CanEdit=>!IsSaving;
    public async Task<bool> SaveAsync()
    {
        if(IsSaving)return false;IsSaving=true;var before=service.Current;
        try
        {
            if(original is null)await service.AddStudentAsync(FullName,Document,Email,Phone);
            else await service.EditStudentAsync(original,FullName,Document,Email,Phone);
            return true;
        }
        catch(Exception ex){Error=service.Current!=before || ex is AcademicRefreshException ? "El estudiante se guardó, pero falló el refresco. Pulsa Actualizar." : ex is AcademicValidationException ? ex.Message : $"No se guardó el estudiante ({SafeErrorCode.From(ex)}). Reintenta.";return service.Current!=before;}
        finally{IsSaving=false;}
    }
}
