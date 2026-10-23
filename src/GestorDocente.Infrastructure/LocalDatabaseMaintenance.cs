using System.Diagnostics;
using GestorDocente.Domain;

namespace GestorDocente.Infrastructure;

internal static class LocalDatabaseMaintenance
{
    public static bool HasAnotherInstance()
    {
        using var current=Process.GetCurrentProcess();var processes=Process.GetProcessesByName(current.ProcessName);
        try{return processes.Any(p=>p.Id!=current.Id);}
        finally{foreach(var process in processes)process.Dispose();}
    }
    public static void RequireIdle(string database,Func<bool> anotherInstance)
    {
        if(anotherInstance())throw new AcademicValidationException("Cierra las otras ventanas de SIGNA antes de reemplazar o limpiar los datos.");
        if(File.Exists(database) && (File.GetAttributes(database)&FileAttributes.ReparsePoint)!=0)
            throw new AcademicValidationException("La base operativa es un enlace a otro archivo. No se reemplazó ni limpió ningún dato.");
        if(new[]{"-wal","-shm","-journal"}.Any(suffix=>File.Exists(database+suffix)))
            throw new AcademicValidationException("Hay archivos SQLite activos. Cierra otras instancias y espera a que termine el guardado antes de continuar.");
    }
}
