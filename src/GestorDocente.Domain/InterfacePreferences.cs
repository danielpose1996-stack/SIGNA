namespace GestorDocente.Domain;

public enum InterfacePalette{Blue,Emerald,Violet,Graphite}
public enum ProfileAvatar{Monitor,Person,Book,Graduation}
public sealed record InterfacePreferences(InterfacePalette Palette=InterfacePalette.Blue,ProfileAvatar Avatar=ProfileAvatar.Monitor)
{
    public void Validate()
    {
        if(!Enum.IsDefined(Palette) || !Enum.IsDefined(Avatar))
            throw new AcademicValidationException("La paleta o el icono del perfil no son compatibles. Selecciona una opción disponible.");
    }
}
