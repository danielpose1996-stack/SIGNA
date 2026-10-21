using System.Diagnostics;
using GestorDocente.Application;

namespace GestorDocente.Infrastructure;

/// <summary>Puerta de solo lectura antes de ceder el reinicio al actualizador.</summary>
public sealed class SqliteUpdateSafetyCheck(string databasePath,Func<string?> installationRoot,Func<bool>? anotherInstance=null):IUpdateSafetyCheck
{
    public Task VerifyAsync(CancellationToken cancellationToken=default)=>Task.Run(()=>
    {
        cancellationToken.ThrowIfCancellationRequested();
        if((anotherInstance ?? HasAnotherInstance)())
            throw Unsafe("Cierra las otras ventanas de SIGNA antes de actualizar, para proteger sus datos y cambios pendientes.");
        var root=installationRoot();
        if(string.IsNullOrWhiteSpace(root))throw Unsafe("No se pudo identificar la instalación actualizable. No se aplicó ningún cambio.");
        root=Path.GetFullPath(root);var database=Path.GetFullPath(databasePath);
        var comparison=OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
        if(database.Equals(root,comparison) || database.StartsWith(Path.TrimEndingDirectorySeparator(root)+Path.DirectorySeparatorChar,comparison))
            throw Unsafe("Los datos están dentro de la carpeta del programa. Guárdalos fuera de la instalación antes de actualizar; no se movió ni eliminó información.");
        if(Directory.Exists(root))
        {
            var options=new EnumerationOptions{RecurseSubdirectories=true,IgnoreInaccessible=false,AttributesToSkip=FileAttributes.ReparsePoint};
            foreach(var file in Directory.EnumerateFiles(root,"*",options))
            {
                cancellationToken.ThrowIfCancellationRequested();
                if(Path.GetExtension(file).ToLowerInvariant() is ".db" or ".sqlite" or ".sqlite3" or ".aulabk" || file.EndsWith(".aulabk.tmp",StringComparison.OrdinalIgnoreCase))
                    throw Unsafe("Hay una base o un respaldo dentro de la carpeta que se reemplazará. Consérvalo fuera del programa antes de actualizar.");
            }
        }
        try{_=SqliteBackupValidator.Validate(database);}
        catch(Exception ex){throw new ApplicationUpdateException(ApplicationUpdateFailure.UnsafeData,"Los datos locales no superaron la comprobación de integridad. La actualización se canceló; revisa o recupera tus datos antes de continuar.",ex);}
        cancellationToken.ThrowIfCancellationRequested();
    },cancellationToken);
    private static ApplicationUpdateException Unsafe(string message)=>new(ApplicationUpdateFailure.UnsafeData,message);
    private static bool HasAnotherInstance()
    {
        using var current=Process.GetCurrentProcess();var processes=Process.GetProcessesByName(current.ProcessName);
        try{return processes.Any(p=>p.Id!=current.Id);}
        finally{foreach(var process in processes)process.Dispose();}
    }
}
