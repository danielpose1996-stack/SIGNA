using GestorDocente.Application;
using GestorDocente.Domain;
using Microsoft.Data.Sqlite;

namespace GestorDocente.Infrastructure;

/// <summary>Reinicializa solo SQLite. No borra carpetas ni archivos del docente o del actualizador.</summary>
public sealed class SqliteLocalDataResetService(string databasePath,Func<bool>? anotherInstance=null):ILocalDataResetService
{
    private readonly string path=Path.GetFullPath(databasePath);
    public LocalDataResetResult Reset(bool confirmed)
    {
        if(!confirmed)throw new AcademicValidationException("La limpieza requiere confirmación. No se eliminó ningún dato.");
        LocalDatabaseMaintenance.RequireIdle(path,anotherInstance ?? LocalDatabaseMaintenance.HasAnotherInstance);
        using var db=new SqliteConnection(new SqliteConnectionStringBuilder{DataSource=path,Mode=SqliteOpenMode.ReadWrite,Pooling=false,ForeignKeys=true,DefaultTimeout=5}.ToString());
        db.Open();using(var setup=db.CreateCommand())
        {setup.CommandText="PRAGMA user_version";if(Convert.ToInt32(setup.ExecuteScalar())!=1)throw new AcademicValidationException("La base no admite esta limpieza. No se modificó.");
            setup.CommandText="PRAGMA secure_delete=ON; PRAGMA locking_mode=EXCLUSIVE";setup.ExecuteNonQuery();}
        using(var transaction=db.BeginTransaction())
        {
            using var command=db.CreateCommand();command.Transaction=transaction;
            command.CommandText="SELECT name FROM sqlite_master WHERE type='table' AND name NOT LIKE 'sqlite_%'";
            using(var tables=command.ExecuteReader())
                while(tables.Read())if(tables.GetString(0) is not ("semesters" or "courses" or "course_workspaces" or "interface_preferences" or "backup_preferences"))
                    throw new AcademicValidationException("El catálogo contiene tablas no reconocidas. No se eliminó ningún dato.");
            command.CommandText="DELETE FROM course_workspaces; DELETE FROM courses; DELETE FROM semesters; DROP TABLE IF EXISTS interface_preferences; DROP TABLE IF EXISTS backup_preferences";
            command.ExecuteNonQuery();transaction.Commit();
        }
        // El borrado ya fue confirmado. Un fallo de compactación debe recargar igualmente
        // el catálogo vacío y anunciarse, para no volver a guardar el ViewModel anterior.
        try{using var compact=db.CreateCommand();compact.CommandText="VACUUM";compact.ExecuteNonQuery();return new();}
        catch(Exception ex)when(ex is SqliteException or IOException or UnauthorizedAccessException)
        {return new($"Los datos se limpiaron, pero no se pudo compactar el archivo ({ex.GetType().Name}). Revisa el espacio y los permisos del equipo.");}
    }
}
