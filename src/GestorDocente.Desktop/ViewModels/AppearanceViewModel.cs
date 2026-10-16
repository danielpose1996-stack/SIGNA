using GestorDocente.Application;
using GestorDocente.Desktop.Styles;
using GestorDocente.Domain;

namespace GestorDocente.Desktop.ViewModels;
public sealed class AppearanceViewModel:ObservableObject
{
    private readonly InterfacePreferencesService service;
    private readonly Action<InterfacePreferences> apply;
    private AvatarOption selectedAvatar;
    private bool busy,colorGrades,hasPreview;
    private string error="",teacherName="",hexInput="",hexError="",selectedHex="";
    private ColorSelection selection;
    public IReadOnlyList<AvatarOption> Avatars=>InterfaceTheme.Avatars;
    public AvatarOption SelectedAvatar{get=>selectedAvatar;set{if(Set(ref selectedAvatar,value))Raise(nameof(PreviewAvatarKind));}}
    public string PreviewAvatarKind=>SelectedAvatar.Icon;
    public string CurrentAvatarKind=>Avatars.Single(a=>a.Id==service.Current.Avatar).Icon;
    public string TeacherName{get=>teacherName;set{if(Set(ref teacherName,value))Raise(nameof(PreviewTeacherName));}}
    public string PreviewTeacherName=>string.IsNullOrWhiteSpace(TeacherName) ? "Docente" : TeacherName.Trim();
    public string CurrentTeacherName=>string.IsNullOrWhiteSpace(service.Current.TeacherName) ? "Docente" : service.Current.TeacherName;
    public bool CurrentColorGrades=>service.Current.ColorGrades;
    public bool ColorGrades{get=>colorGrades;set=>Set(ref colorGrades,value);}
    public string HexInput
    {
        get=>hexInput;
        set
        {
            if(!Set(ref hexInput,value ?? ""))return;
            try{var candidate=hexInput.Trim().ToUpperInvariant();var next=ColorSelection.FromHex(candidate,selection.Hue);UpdateColor(next,candidate);}
            catch(AcademicValidationException){HexError="Escribe seis dígitos hexadecimales, por ejemplo #2563EB.";}
        }
    }
    public string SelectedHex=>selectedHex;
    public string HexError{get=>hexError;private set{if(Set(ref hexError,value))Raise(nameof(CanSave));}}
    public double Hue{get=>selection.Hue;set{if(double.IsFinite(value) && value!=Hue)UpdateColor(selection with{Hue=Math.Clamp(value,0,360)});}}
    public double Saturation{get=>selection.Saturation;set{if(double.IsFinite(value) && value!=Saturation)UpdateColor(selection with{Saturation=Math.Clamp(value,0,1)});}}
    public double Brightness{get=>selection.Value;set{if(double.IsFinite(value) && value!=Brightness)UpdateColor(selection with{Value=Math.Clamp(value,0,1)});}}
    public string HsvLabel=>$"Tono {Hue:0}° · Saturación {Saturation*100:0} % · Brillo {Brightness*100:0} %";
    public string Warning=>service.Warning;
    public string Error{get=>error;private set=>Set(ref error,value);}
    public bool IsBusy{get=>busy;private set{if(Set(ref busy,value)){Raise(nameof(CanEdit));Raise(nameof(CanSave));}}}
    public bool CanEdit=>!IsBusy;
    public bool CanSave=>CanEdit && HexError.Length==0;
    public AppearanceViewModel(InterfacePreferencesService service,Action<InterfacePreferences>? apply=null)
    {
        this.service=service;this.apply=apply ?? (preferences=>InterfaceTheme.Apply(Avalonia.Application.Current ?? throw new InvalidOperationException("La interfaz no está inicializada."),preferences));
        selectedAvatar=Avatars.Single(a=>a.Id==service.Current.Avatar);LoadDraft();
    }
    private void UpdateColor(ColorSelection next,string? exactHex=null)
    {
        selection=next;selectedHex=exactHex ?? next.ToHex();hexInput=selectedHex;HexError="";
        foreach(var name in new[]{nameof(Hue),nameof(Saturation),nameof(Brightness),nameof(HsvLabel),nameof(SelectedHex),nameof(HexInput)})Raise(name);
        try{apply(service.Current with{Accent=selectedHex});hasPreview=true;Error="";}
        catch(Exception ex){Error=$"No se pudo aplicar la vista previa ({SafeErrorCode.From(ex)}). Reintenta.";}
    }
    private void LoadDraft()
    {
        selectedHex=service.Current.Accent;hexInput=selectedHex;selection=ColorSelection.FromHex(selectedHex);HexError="";
        SelectedAvatar=Avatars.Single(a=>a.Id==service.Current.Avatar);TeacherName=service.Current.TeacherName;ColorGrades=service.Current.ColorGrades;Error="";
        foreach(var name in new[]{nameof(Hue),nameof(Saturation),nameof(Brightness),nameof(HsvLabel),nameof(SelectedHex),nameof(HexInput)})Raise(name);
    }
    public void CancelPreview(){if(!hasPreview)return;apply(service.Current);hasPreview=false;}
    public void ResetDraft(){CancelPreview();LoadDraft();}
    public async Task<bool> SaveAsync()
    {
        if(!CanSave)return false;IsBusy=true;var before=service.Current;
        try
        {
            if(SelectedAvatar is null)throw new AcademicValidationException("Selecciona un icono del perfil.");
            await service.SaveAsync(new(selectedHex,SelectedAvatar.Id,TeacherName.Trim(),ColorGrades));apply(service.Current);hasPreview=false;
            Error="";Raise(nameof(CurrentAvatarKind));Raise(nameof(CurrentTeacherName));Raise(nameof(CurrentColorGrades));Raise(nameof(Warning));return true;
        }
        catch(Exception ex){Error=service.Current!=before ? "La apariencia se guardó, pero no pudo aplicarse. Reinicia la aplicación." : ex is AcademicValidationException ? ex.Message : $"No se guardó la apariencia ({SafeErrorCode.From(ex)}). Reintenta.";return false;}
        finally{IsBusy=false;}
    }
}
