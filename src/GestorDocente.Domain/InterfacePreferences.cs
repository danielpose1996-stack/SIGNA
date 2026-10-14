namespace GestorDocente.Domain;

public enum InterfacePalette{Blue,Emerald,Violet,Graphite}
public enum ProfileAvatar{Monitor,Person,Book,Graduation}
public sealed record InterfacePreferences(InterfacePalette Palette=InterfacePalette.Blue,ProfileAvatar Avatar=ProfileAvatar.Monitor,
    string TeacherName="",string? CustomAccent=null,bool ColorGrades=true)
{
    public void Validate()
    {
        if(!Enum.IsDefined(Palette) || !Enum.IsDefined(Avatar))
            throw new AcademicValidationException("La paleta o el icono del perfil no son compatibles. Selecciona una opción disponible.");
        if(TeacherName is null || TeacherName.Length>120 || TeacherName.Any(char.IsControl))
            throw new AcademicValidationException("El nombre del docente admite hasta 120 caracteres en una sola línea.");
        if(CustomAccent is not null && (CustomAccent.Length!=7 || CustomAccent[0]!='#' ||
            CustomAccent.AsSpan(1).ContainsAnyExcept("0123456789ABCDEFabcdef")))
            throw new AcademicValidationException("El color personalizado debe contener tres canales RGB válidos (#RRGGBB).");
    }
}
