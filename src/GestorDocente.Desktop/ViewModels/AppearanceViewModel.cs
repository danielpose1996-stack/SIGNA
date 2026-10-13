using GestorDocente.Application;
using GestorDocente.Desktop.Styles;
using GestorDocente.Domain;

namespace GestorDocente.Desktop.ViewModels;
public sealed class AppearanceViewModel:ObservableObject
{
    private readonly InterfacePreferencesService service;
    private readonly Action<InterfacePalette> apply;
    private PaletteOption selectedPalette;
    private AvatarOption selectedAvatar;
    private bool busy;
    private string error="";
    public IReadOnlyList<PaletteOption> Palettes=>ThemePalettes.Options;
    public IReadOnlyList<AvatarOption> Avatars=>ThemePalettes.Avatars;
    public PaletteOption SelectedPalette{get=>selectedPalette;set=>Set(ref selectedPalette,value);}
    public AvatarOption SelectedAvatar{get=>selectedAvatar;set{if(Set(ref selectedAvatar,value))Raise(nameof(PreviewAvatarKind));}}
    public string PreviewAvatarKind=>SelectedAvatar.Icon;
    public string CurrentAvatarKind=>Avatars.Single(a=>a.Id==service.Current.Avatar).Icon;
    public string Warning=>service.Warning;
    public string Error{get=>error;private set=>Set(ref error,value);}
    public bool IsBusy{get=>busy;private set{if(Set(ref busy,value))Raise(nameof(CanEdit));}}
    public bool CanEdit=>!IsBusy;
    public RelayCommand DefaultsCommand{get;}
    public AppearanceViewModel(InterfacePreferencesService service,Action<InterfacePalette>? apply=null)
    {
        this.service=service;this.apply=apply ?? (palette=>ThemePalettes.Apply(Avalonia.Application.Current ?? throw new InvalidOperationException("La interfaz no está inicializada."),palette));
        selectedPalette=Palettes.Single(p=>p.Id==service.Current.Palette);selectedAvatar=Avatars.Single(a=>a.Id==service.Current.Avatar);
        DefaultsCommand=new(_=>{SelectedPalette=Palettes[0];SelectedAvatar=Avatars[0];},_=>CanEdit);
    }
    public void ResetDraft(){SelectedPalette=Palettes.Single(p=>p.Id==service.Current.Palette);SelectedAvatar=Avatars.Single(a=>a.Id==service.Current.Avatar);Error="";}
    public async Task<bool> SaveAsync()
    {
        if(IsBusy)return false;IsBusy=true;DefaultsCommand.Refresh();var before=service.Current;
        try
        {
            if(SelectedPalette is null || SelectedAvatar is null)throw new AcademicValidationException("Selecciona una paleta y un icono del perfil.");
            await service.SaveAsync(new(SelectedPalette.Id,SelectedAvatar.Id));apply(service.Current.Palette);
            Raise(nameof(CurrentAvatarKind));Raise(nameof(Warning));return true;
        }
        catch(Exception ex){Error=service.Current!=before ? "La apariencia se guardó, pero no pudo aplicarse. Reinicia la aplicación." : ex is AcademicValidationException ? ex.Message : $"No se guardó la apariencia ({SafeErrorCode.From(ex)}). Reintenta.";return false;}
        finally{IsBusy=false;DefaultsCommand.Refresh();}
    }
}
