namespace GestorDocente.Domain;

public enum ProfileAvatar{Monitor,Person,Book,Graduation}
public sealed record InterfacePreferences(string Accent="#0067F5",ProfileAvatar Avatar=ProfileAvatar.Monitor,
    string TeacherName="",bool ColorGrades=true)
{
    public void Validate()
    {
        if(!Enum.IsDefined(Avatar))
            throw new AcademicValidationException("El icono del perfil no es compatible. Selecciona una opción disponible.");
        if(TeacherName is null || TeacherName.Length>120 || TeacherName.Any(char.IsControl))
            throw new AcademicValidationException("El nombre del docente admite hasta 120 caracteres en una sola línea.");
        if(Accent is null || Accent.Length!=7 || Accent[0]!='#' || Accent.AsSpan(1).ContainsAnyExcept("0123456789ABCDEFabcdef"))
            throw new AcademicValidationException("Escribe un color hexadecimal válido con el formato #RRGGBB.");
    }
}
