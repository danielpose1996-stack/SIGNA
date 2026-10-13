using System.Net.Mail;
namespace GestorDocente.Domain;
public static class StudentRules
{
    public static void Validate(Student student,bool documentRequired=false)
    {
        if(string.IsNullOrWhiteSpace(student.FullName) || student.FullName.Length>240)throw new AcademicValidationException("Escribe el nombre completo (máximo 240 caracteres).");
        if(student.Document is null || student.Document.Length>80 || documentRequired && string.IsNullOrWhiteSpace(student.Document))throw new AcademicValidationException("Revisa el documento (máximo 80 caracteres).");
        if(student.Email is null || student.Email.Length>254 || student.Email.Length>0 && (!MailAddress.TryCreate(student.Email,out var address) || !string.Equals(address.Address,student.Email,StringComparison.OrdinalIgnoreCase)))throw new AcademicValidationException("Escribe un correo electrónico válido o deja el campo vacío.");
        if(student.Phone is null || student.Phone.Length>40 || student.Phone.Any(char.IsControl))throw new AcademicValidationException("Revisa el celular (máximo 40 caracteres).");
    }
}
