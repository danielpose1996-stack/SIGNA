using GestorDocente.Application;
using GestorDocente.Domain;
using Microsoft.Data.Sqlite;

namespace GestorDocente.Infrastructure;
public sealed class SqliteInterfacePreferencesStore(string path):IInterfacePreferencesStore
{
    private readonly string path=Path.GetFullPath(path);
    public InterfacePreferences Load()
    {
        using var db=Open(SqliteOpenMode.ReadOnly);return Read(db);
    }
    internal static InterfacePreferences Read(SqliteConnection db)
    {
        using(var exists=db.CreateCommand())
        {
            exists.CommandText="SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name='interface_preferences'";
            if(Convert.ToInt32(exists.ExecuteScalar())==0)return new();
        }
        using var command=db.CreateCommand();command.CommandText="SELECT id,schema_version,palette,avatar FROM interface_preferences";
        using var rows=command.ExecuteReader();if(!rows.Read())return new();
        if(rows.IsDBNull(0) || rows.IsDBNull(1) || rows.IsDBNull(2) || rows.IsDBNull(3) || rows.GetInt32(0)!=1 || rows.GetInt32(1)!=1 ||
            !Enum.TryParse<InterfacePalette>(rows.GetString(2),out var palette) || !Enum.TryParse<ProfileAvatar>(rows.GetString(3),out var avatar))
            throw new AcademicValidationException("Las preferencias de apariencia no son compatibles. Puedes volver a seleccionar la paleta y el icono desde el perfil.");
        var result=new InterfacePreferences(palette,avatar);result.Validate();
        if(rows.Read())throw new AcademicValidationException("Las preferencias de apariencia tienen registros duplicados.");
        return result;
    }
    public void Save(InterfacePreferences preferences)
    {
        preferences.Validate();using var db=Open(SqliteOpenMode.ReadWrite);
        using(var version=db.CreateCommand())
        {
            version.CommandText="PRAGMA user_version";
            if(Convert.ToInt32(version.ExecuteScalar())!=1)throw new AcademicValidationException("El catálogo no admite esta configuración de apariencia.");
        }
        using var tx=db.BeginTransaction();using var command=db.CreateCommand();command.Transaction=tx;
        // Extensión opcional del catálogo: no cambia las tablas ni versiones académicas.
        command.CommandText="""
            CREATE TABLE IF NOT EXISTS interface_preferences(id INTEGER PRIMARY KEY CHECK(id=1),schema_version INTEGER NOT NULL CHECK(schema_version=1),palette TEXT NOT NULL,avatar TEXT NOT NULL);
            INSERT INTO interface_preferences(id,schema_version,palette,avatar) VALUES(1,1,$palette,$avatar)
            ON CONFLICT(id) DO UPDATE SET schema_version=1,palette=excluded.palette,avatar=excluded.avatar;
            """;
        command.Parameters.AddWithValue("$palette",preferences.Palette.ToString());command.Parameters.AddWithValue("$avatar",preferences.Avatar.ToString());
        command.ExecuteNonQuery();tx.Commit();
    }
    private SqliteConnection Open(SqliteOpenMode mode)
    {
        var db=new SqliteConnection(new SqliteConnectionStringBuilder{DataSource=path,Mode=mode,Pooling=false,DefaultTimeout=5,ForeignKeys=true}.ToString());
        try{db.Open();return db;}catch{db.Dispose();throw;}
    }
}
