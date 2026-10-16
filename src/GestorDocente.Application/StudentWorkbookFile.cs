using GestorDocente.Domain;
namespace GestorDocente.Application;

public static class StudentWorkbookFile
{
    public static FileStream OpenRead(string path)
    {
        try{return new FileStream(path,FileMode.Open,FileAccess.Read,FileShare.ReadWrite,81920,FileOptions.SequentialScan);}
        catch(IOException ex)when((ex.HResult&0xffff) is 32 or 33)
        {throw new AcademicValidationException("El archivo Excel está bloqueado por otro programa. Guarda los cambios, ciérralo en Excel o en el programa que lo utiliza y vuelve a importarlo.",ex);}
        catch(UnauthorizedAccessException ex)
        {throw new AcademicValidationException("No tienes permiso para leer este Excel. Selecciona una copia guardada en una carpeta a la que tengas acceso.",ex);}
    }
}
