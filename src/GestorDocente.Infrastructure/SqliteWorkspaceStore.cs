using System.Text.Json;
using GestorDocente.Application;
using GestorDocente.Domain;
using Microsoft.Data.Sqlite;

namespace GestorDocente.Infrastructure;

/// <summary>Almacenamiento mínimo exclusivo del workspace de demostración; no es el esquema productivo.</summary>
public sealed class SqliteWorkspaceStore : IWorkspaceStore
{
    private readonly string connectionString;
    private string? lastPayload;
    public SqliteWorkspaceStore(string path)
    {
        var fullPath=Path.GetFullPath(path);var directory=Path.GetDirectoryName(fullPath)!;
        var newDirectory=!Directory.Exists(directory);var newFile=!File.Exists(fullPath);
        Directory.CreateDirectory(directory);
        if(newDirectory && !OperatingSystem.IsWindows())File.SetUnixFileMode(directory,UnixFileMode.UserRead|UnixFileMode.UserWrite|UnixFileMode.UserExecute);
        connectionString=new SqliteConnectionStringBuilder {DataSource=fullPath,Mode=SqliteOpenMode.ReadWriteCreate,Pooling=false}.ToString();
        using var connection=Open();
        using var command=connection.CreateCommand();
        command.CommandText="CREATE TABLE IF NOT EXISTS demo_workspace (id INTEGER PRIMARY KEY CHECK(id=1), schema_version INTEGER NOT NULL, payload TEXT NOT NULL);";
        command.ExecuteNonQuery();
        if(newFile && !OperatingSystem.IsWindows())File.SetUnixFileMode(fullPath,UnixFileMode.UserRead|UnixFileMode.UserWrite);
    }
    private SqliteConnection Open() {var connection=new SqliteConnection(connectionString);connection.Open();return connection;}
    public AcademicWorkspace? Load()
    {
        using var connection=Open(); using var command=connection.CreateCommand();
        command.CommandText="SELECT schema_version,payload FROM demo_workspace WHERE id=1;";
        using var reader=command.ExecuteReader(); if(!reader.Read())return null;
        if(reader.GetInt32(0) is <1 or >AcademicWorkspace.CurrentSchemaVersion)throw new AcademicValidationException("Versión de demostración incompatible. Los datos no se reemplazaron.");
        var payload=reader.GetString(1);
        var workspace=WorkspaceIntegrity.Deserialize(payload);
        if(workspace.SchemaVersion!=reader.GetInt32(0) || !workspace.IsDemo)throw new AcademicValidationException("La versión o los datos de demostración no corresponden al archivo local.");
        lastPayload=payload;
        return workspace;
    }
    public void Save(AcademicWorkspace workspace)
    {
        if(!workspace.IsDemo || workspace.SchemaVersion!=AcademicWorkspace.CurrentSchemaVersion)throw new AcademicValidationException("Este almacenamiento admite únicamente el formato actual de demostración.");
        WorkspaceIntegrity.Validate(workspace);
        var payload=JsonSerializer.Serialize(workspace);
        using var connection=Open(); using var transaction=connection.BeginTransaction(); using var command=connection.CreateCommand();
        command.Transaction=transaction;
        command.CommandText=lastPayload is null ?
            "INSERT INTO demo_workspace(id,schema_version,payload) VALUES(1,$schema,$payload)" :
            "UPDATE demo_workspace SET schema_version=$schema,payload=$payload WHERE id=1 AND payload=$previous";
        command.Parameters.AddWithValue("$schema",workspace.SchemaVersion);
        command.Parameters.AddWithValue("$payload",payload);
        if(lastPayload is not null)command.Parameters.AddWithValue("$previous",lastPayload);
        try
        {
            if(command.ExecuteNonQuery()!=1)throw new AcademicValidationException("Otra instancia modificó la demostración. Vuelve a abrirla antes de guardar.");
            transaction.Commit();lastPayload=payload;
        }
        catch(SqliteException ex)when(ex.SqliteErrorCode==19)
        {throw new AcademicValidationException("Otra instancia creó la demostración. Vuelve a abrirla antes de guardar.");}
    }
}
