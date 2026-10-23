using GestorDocente.Application;
using GestorDocente.Domain;
using Microsoft.Data.Sqlite;

namespace GestorDocente.Infrastructure;

public sealed class SqliteBackupPreferencesStore(string databasePath):IBackupPreferencesStore
{
    private readonly string path=Path.GetFullPath(databasePath);
    public string? LoadFolder(){using var db=Open(SqliteOpenMode.ReadOnly);return Read(db);}
    internal static string? Read(SqliteConnection db)
    {
        using var query=db.CreateCommand();query.CommandText="SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name='backup_preferences'";
        if(Convert.ToInt32(query.ExecuteScalar())==0)return null;
        query.CommandText="SELECT id,folder FROM backup_preferences";using var rows=query.ExecuteReader();
        if(!rows.Read())return null;
        if(rows.IsDBNull(0) || rows.GetInt32(0)!=1 || rows.IsDBNull(1))throw InvalidPreferences();
        var value=rows.GetString(1);
        // Guardar una ruta Windows en una copia no debe impedir restaurarla en macOS/Ubuntu.
        if(string.IsNullOrWhiteSpace(value) || value.Length>4096 || value.Contains('\0') || rows.Read())throw InvalidPreferences();
        return value;
    }
    public void SaveFolder(string folder)
    {
        if(string.IsNullOrWhiteSpace(folder) || !Path.IsPathFullyQualified(folder) || !Directory.Exists(folder))
            throw new AcademicValidationException("Selecciona una carpeta local disponible para los respaldos.");
        folder=Path.GetFullPath(folder);using var db=Open(SqliteOpenMode.ReadWrite);
        using var transaction=db.BeginTransaction();using var command=db.CreateCommand();command.Transaction=transaction;
        command.CommandText="PRAGMA user_version";
        if(Convert.ToInt32(command.ExecuteScalar())!=1)throw InvalidPreferences();
        command.CommandText="CREATE TABLE IF NOT EXISTS backup_preferences(id INTEGER PRIMARY KEY CHECK(id=1),folder TEXT NOT NULL); INSERT INTO backup_preferences(id,folder) VALUES(1,$folder) ON CONFLICT(id) DO UPDATE SET folder=excluded.folder";
        command.Parameters.AddWithValue("$folder",folder);command.ExecuteNonQuery();transaction.Commit();
    }
    private SqliteConnection Open(SqliteOpenMode mode)
    {
        var db=new SqliteConnection(new SqliteConnectionStringBuilder{DataSource=path,Mode=mode,Pooling=false,DefaultTimeout=5}.ToString());
        try{db.Open();return db;}catch{db.Dispose();throw;}
    }
    private static AcademicValidationException InvalidPreferences()=>new("La configuración de respaldos no es compatible. No se modificó la base.");
}
