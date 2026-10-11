using System.Text.Json;
using GestorDocente.Application;
using GestorDocente.Domain;
using Microsoft.Data.Sqlite;

namespace GestorDocente.Infrastructure;

/// <summary>Almacenamiento mínimo exclusivo del workspace de demostración; no es el esquema productivo.</summary>
public sealed class SqliteWorkspaceStore : IWorkspaceStore
{
    private readonly string connectionString;
    public SqliteWorkspaceStore(string path)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);
        connectionString=new SqliteConnectionStringBuilder {DataSource=path,Mode=SqliteOpenMode.ReadWriteCreate,Pooling=false}.ToString();
        using var connection=Open();
        using var command=connection.CreateCommand();
        command.CommandText="CREATE TABLE IF NOT EXISTS demo_workspace (id INTEGER PRIMARY KEY CHECK(id=1), schema_version INTEGER NOT NULL, payload TEXT NOT NULL);";
        command.ExecuteNonQuery();
    }
    private SqliteConnection Open() {var connection=new SqliteConnection(connectionString);connection.Open();return connection;}
    public AcademicWorkspace? Load()
    {
        using var connection=Open(); using var command=connection.CreateCommand();
        command.CommandText="SELECT schema_version,payload FROM demo_workspace WHERE id=1;";
        using var reader=command.ExecuteReader(); if(!reader.Read())return null;
        if(reader.GetInt32(0)!=1)throw new AcademicValidationException("Versión de demostración incompatible. Los datos no se reemplazaron.");
        return JsonSerializer.Deserialize<AcademicWorkspace>(reader.GetString(1)) ?? throw new AcademicValidationException("La demostración almacenada está vacía o dañada.");
    }
    public void Save(AcademicWorkspace workspace)
    {
        if(!workspace.IsDemo || workspace.SchemaVersion!=1)throw new AcademicValidationException("Este almacenamiento admite únicamente la demostración versión 1.");
        var payload=JsonSerializer.Serialize(workspace);
        using var connection=Open(); using var transaction=connection.BeginTransaction(); using var command=connection.CreateCommand();
        command.Transaction=transaction;
        command.CommandText="INSERT INTO demo_workspace(id,schema_version,payload) VALUES(1,1,$payload) ON CONFLICT(id) DO UPDATE SET schema_version=1,payload=$payload;";
        command.Parameters.AddWithValue("$payload",payload);command.ExecuteNonQuery();transaction.Commit();
    }
}
