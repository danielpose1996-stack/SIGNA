using GestorDocente.Application;
using GestorDocente.Domain;
using Microsoft.Data.Sqlite;

namespace GestorDocente.Infrastructure;
public sealed class SqliteInterfacePreferencesStore(string path,ISavedChangesObserver? changes=null):IInterfacePreferencesStore
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
        var columns=Columns(db);
        using var command=db.CreateCommand();
        command.CommandText=$"SELECT id,schema_version,palette,avatar,{(columns.Contains("teacher_name") ? "teacher_name" : "''")},{(columns.Contains("custom_accent") ? "custom_accent" : "NULL")},{(columns.Contains("color_grades") ? "color_grades" : "1")} FROM interface_preferences";
        using var rows=command.ExecuteReader();if(!rows.Read())return new();
        if(rows.IsDBNull(0) || rows.IsDBNull(1) || rows.IsDBNull(2) || rows.IsDBNull(3) || rows.GetInt32(0)!=1 || rows.GetInt32(1)!=1 ||
            !Enum.TryParse<ProfileAvatar>(rows.GetString(3),out var avatar))
            throw new AcademicValidationException("Las preferencias de apariencia no son compatibles. Revisa el color y el icono desde el perfil.");
        if(rows.IsDBNull(4) || rows.IsDBNull(6) || rows.GetInt32(6) is not (0 or 1))
            throw new AcademicValidationException("El perfil o los colores guardados no son compatibles. Revisa Personalizar apariencia.");
        var accent=rows.IsDBNull(5) ? LegacyAccent(rows.GetString(2)) : rows.GetString(5);
        var result=new InterfacePreferences(accent,avatar,rows.GetString(4),rows.GetInt32(6)==1);result.Validate();
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
        // Optional additive extension; schema changes and values commit together.
        command.CommandText="CREATE TABLE IF NOT EXISTS interface_preferences(id INTEGER PRIMARY KEY CHECK(id=1),schema_version INTEGER NOT NULL CHECK(schema_version=1),palette TEXT NOT NULL,avatar TEXT NOT NULL)";
        command.ExecuteNonQuery();
        var columns=Columns(db,tx);
        foreach(var (name,definition) in new[]{("teacher_name","TEXT NOT NULL DEFAULT ''"),("custom_accent","TEXT"),("color_grades","INTEGER NOT NULL DEFAULT 1 CHECK(color_grades IN (0,1))")})
            if(!columns.Contains(name))
            {
                command.CommandText=$"ALTER TABLE interface_preferences ADD COLUMN {name} {definition}";
                command.ExecuteNonQuery();
            }
        command.CommandText="""
            INSERT INTO interface_preferences(id,schema_version,palette,avatar,teacher_name,custom_accent,color_grades) VALUES(1,1,'Blue',$avatar,$name,$accent,$grades)
            ON CONFLICT(id) DO UPDATE SET schema_version=1,palette=excluded.palette,avatar=excluded.avatar,teacher_name=excluded.teacher_name,custom_accent=excluded.custom_accent,color_grades=excluded.color_grades;
            """;
        command.Parameters.AddWithValue("$avatar",preferences.Avatar.ToString());
        command.Parameters.AddWithValue("$name",preferences.TeacherName);command.Parameters.AddWithValue("$accent",preferences.Accent);command.Parameters.AddWithValue("$grades",preferences.ColorGrades ? 1 : 0);
        command.ExecuteNonQuery();tx.Commit();changes?.Saved();
    }
    // Storage compatibility only: these historical choices are no longer a UI feature.
    private static string LegacyAccent(string value)=>value switch
    {
        "Blue" or "0"=>"#0067F5","Emerald" or "1"=>"#007A66","Violet" or "2"=>"#6D4ACB","Graphite" or "3"=>"#405A70",
        _=>throw new AcademicValidationException("El color anterior no es compatible. Selecciona un color desde Personalizar apariencia.")
    };
    private static HashSet<string> Columns(SqliteConnection db,SqliteTransaction? transaction=null)
    {
        using var command=db.CreateCommand();command.Transaction=transaction;command.CommandText="PRAGMA table_info(interface_preferences)";
        using var rows=command.ExecuteReader();HashSet<string> columns=new(StringComparer.Ordinal);
        while(rows.Read())columns.Add(rows.GetString(1));return columns;
    }
    private SqliteConnection Open(SqliteOpenMode mode)
    {
        var db=new SqliteConnection(new SqliteConnectionStringBuilder{DataSource=path,Mode=mode,Pooling=false,DefaultTimeout=5,ForeignKeys=true}.ToString());
        try{db.Open();return db;}catch{db.Dispose();throw;}
    }
}
