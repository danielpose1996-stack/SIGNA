using GestorDocente.Application;
using GestorDocente.Desktop.Styles;
using GestorDocente.Domain;

namespace GestorDocente.Desktop.ViewModels;
public sealed class AppearanceViewModel:ObservableObject
{
    private readonly InterfacePreferencesService service;
    private readonly Action<InterfacePreferences> apply;
    private PaletteOption selectedPalette;
    private AvatarOption selectedAvatar;
    private bool busy;
    private string error="";
    private string teacherName="";
    private bool useCustomColor,colorGrades;
    private int red,green,blue;
    public IReadOnlyList<PaletteOption> Palettes=>ThemePalettes.Options;
    public IReadOnlyList<AvatarOption> Avatars=>ThemePalettes.Avatars;
    public PaletteOption SelectedPalette{get=>selectedPalette;set{if(Set(ref selectedPalette,value) && !UseCustomColor)LoadRgb(value.Accent);}}
    public AvatarOption SelectedAvatar{get=>selectedAvatar;set{if(Set(ref selectedAvatar,value))Raise(nameof(PreviewAvatarKind));}}
    public string PreviewAvatarKind=>SelectedAvatar.Icon;
    public string CurrentAvatarKind=>Avatars.Single(a=>a.Id==service.Current.Avatar).Icon;
    public string TeacherName{get=>teacherName;set{if(Set(ref teacherName,value))Raise(nameof(PreviewTeacherName));}}
    public string PreviewTeacherName=>string.IsNullOrWhiteSpace(TeacherName) ? "Docente" : TeacherName.Trim();
    public string CurrentTeacherName=>string.IsNullOrWhiteSpace(service.Current.TeacherName) ? "Docente" : service.Current.TeacherName;
    public bool CurrentColorGrades=>service.Current.ColorGrades;
    public bool UseCustomColor{get=>useCustomColor;set=>Set(ref useCustomColor,value);}
    public bool ColorGrades{get=>colorGrades;set=>Set(ref colorGrades,value);}
    public int Red{get=>red;set{if(Set(ref red,Math.Clamp(value,0,255)))Raise(nameof(RgbHex));}}
    public int Green{get=>green;set{if(Set(ref green,Math.Clamp(value,0,255)))Raise(nameof(RgbHex));}}
    public int Blue{get=>blue;set{if(Set(ref blue,Math.Clamp(value,0,255)))Raise(nameof(RgbHex));}}
    public string RgbHex=>$"#{Red:X2}{Green:X2}{Blue:X2}";
    public string Warning=>service.Warning;
    public string Error{get=>error;private set=>Set(ref error,value);}
    public bool IsBusy{get=>busy;private set{if(Set(ref busy,value))Raise(nameof(CanEdit));}}
    public bool CanEdit=>!IsBusy;
    public RelayCommand DefaultsCommand{get;}
    public AppearanceViewModel(InterfacePreferencesService service,Action<InterfacePreferences>? apply=null)
    {
        this.service=service;this.apply=apply ?? (preferences=>ThemePalettes.Apply(Avalonia.Application.Current ?? throw new InvalidOperationException("La interfaz no está inicializada."),preferences));
        selectedPalette=Palettes.Single(p=>p.Id==service.Current.Palette);selectedAvatar=Avatars.Single(a=>a.Id==service.Current.Avatar);
        ResetDraft();DefaultsCommand=new(_=>{UseCustomColor=false;SelectedPalette=Palettes[0];LoadRgb(Palettes[0].Accent);SelectedAvatar=Avatars[0];ColorGrades=true;},_=>CanEdit);
    }
    private void LoadRgb(string hex){var color=Avalonia.Media.Color.Parse(hex);Red=color.R;Green=color.G;Blue=color.B;}
    public void ResetDraft(){UseCustomColor=service.Current.CustomAccent is not null;SelectedPalette=Palettes.Single(p=>p.Id==service.Current.Palette);SelectedAvatar=Avatars.Single(a=>a.Id==service.Current.Avatar);TeacherName=service.Current.TeacherName;ColorGrades=service.Current.ColorGrades;LoadRgb(service.Current.CustomAccent ?? SelectedPalette.Accent);Error="";}
    public async Task<bool> SaveAsync()
    {
        if(IsBusy)return false;IsBusy=true;DefaultsCommand.Refresh();var before=service.Current;
        try
        {
            if(SelectedPalette is null || SelectedAvatar is null)throw new AcademicValidationException("Selecciona una paleta y un icono del perfil.");
            await service.SaveAsync(new(SelectedPalette.Id,SelectedAvatar.Id,TeacherName.Trim(),UseCustomColor ? RgbHex : null,ColorGrades));apply(service.Current);
            Raise(nameof(CurrentAvatarKind));Raise(nameof(CurrentTeacherName));Raise(nameof(CurrentColorGrades));Raise(nameof(Warning));return true;
        }
        catch(Exception ex){Error=service.Current!=before ? "La apariencia se guardó, pero no pudo aplicarse. Reinicia la aplicación." : ex is AcademicValidationException ? ex.Message : $"No se guardó la apariencia ({SafeErrorCode.From(ex)}). Reintenta.";return false;}
        finally{IsBusy=false;DefaultsCommand.Refresh();}
    }
}
