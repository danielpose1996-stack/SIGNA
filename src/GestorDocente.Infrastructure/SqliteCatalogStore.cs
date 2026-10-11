using System.Text.Json;
using GestorDocente.Application;
using GestorDocente.Domain;
using Microsoft.Data.Sqlite;

namespace GestorDocente.Infrastructure;

public sealed class SqliteCatalogStore : ICatalogStore
{
    private readonly string connectionString;
    public SqliteCatalogStore(string path)
    {
        var fullPath=Path.GetFullPath(path);
        var directory=Path.GetDirectoryName(fullPath)!;
        var newDirectory=!Directory.Exists(directory);
        var newFile=!File.Exists(fullPath);
        Directory.CreateDirectory(directory);
        if(newDirectory && !OperatingSystem.IsWindows())File.SetUnixFileMode(directory,UnixFileMode.UserRead|UnixFileMode.UserWrite|UnixFileMode.UserExecute);
        connectionString=new SqliteConnectionStringBuilder{DataSource=fullPath,ForeignKeys=true,Pooling=false,DefaultTimeout=5}.ToString();
        using var db=Open();using var read=db.CreateCommand();read.CommandText="PRAGMA user_version";var version=Convert.ToInt32(read.ExecuteScalar());
        if(version!=0 && version!=1)throw new AcademicValidationException("La versión del catálogo no es compatible. No se modificaron tus datos.");
        if(version==0)
        {
            using var check=db.CreateCommand();check.CommandText="SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name NOT LIKE 'sqlite_%'";
            if(Convert.ToInt32(check.ExecuteScalar())!=0)throw new AcademicValidationException("Este archivo pertenece a otro almacenamiento. No se modificó.");
            using var transaction=db.BeginTransaction();using var create=db.CreateCommand();create.Transaction=transaction;
            create.CommandText="""
                CREATE TABLE semesters(id TEXT PRIMARY KEY, year INTEGER NOT NULL, term INTEGER NOT NULL CHECK(term IN(1,2)), UNIQUE(year,term));
                CREATE TABLE courses(id TEXT PRIMARY KEY, semester_id TEXT NOT NULL REFERENCES semesters(id), code TEXT NOT NULL COLLATE NOCASE, name TEXT NOT NULL, school TEXT NOT NULL, group_name TEXT NOT NULL COLLATE NOCASE, UNIQUE(semester_id,code,group_name));
                CREATE TABLE course_workspaces(course_id TEXT PRIMARY KEY REFERENCES courses(id), schema_version INTEGER NOT NULL, revision INTEGER NOT NULL, payload TEXT NOT NULL);
                PRAGMA user_version=1;
                """;
            create.ExecuteNonQuery();transaction.Commit();
        }
        if(newFile && !OperatingSystem.IsWindows())File.SetUnixFileMode(fullPath,UnixFileMode.UserRead|UnixFileMode.UserWrite);
    }
    private SqliteConnection Open(){var db=new SqliteConnection(connectionString);db.Open();return db;}
    public IReadOnlyList<AcademicSemester> Semesters()
    {
        using var db=Open();using var cmd=db.CreateCommand();cmd.CommandText="SELECT id,year,term FROM semesters ORDER BY year DESC,term DESC";
        using var reader=cmd.ExecuteReader();var items=new List<AcademicSemester>();while(reader.Read())items.Add(new(reader.GetString(0),reader.GetInt32(1),reader.GetInt32(2)));return items;
    }
    public IReadOnlyList<CourseRecord> Courses(string semesterId)
    {
        using var db=Open();using var cmd=db.CreateCommand();cmd.CommandText="SELECT c.id,c.semester_id,c.code,c.name,c.school,c.group_name,CASE WHEN json_type(w.payload,'$.Students')='array' THEN json_array_length(w.payload,'$.Students') ELSE NULL END FROM courses c JOIN course_workspaces w ON w.course_id=c.id WHERE c.semester_id=$semester ORDER BY c.name,c.group_name";cmd.Parameters.AddWithValue("$semester",semesterId);
        using var reader=cmd.ExecuteReader();var items=new List<CourseRecord>();while(reader.Read())
        {
            if(reader.IsDBNull(6))throw new AcademicValidationException("Una materia tiene un listado de estudiantes incompleto. No se modificó el archivo local.");
            items.Add(new(reader.GetString(0),reader.GetString(1),reader.GetString(2),reader.GetString(3),reader.GetString(4),reader.GetString(5),reader.GetInt32(6)));
        }
        return items;
    }
    public void AddSemester(AcademicSemester semester)
    {
        using var db=Open();using var cmd=db.CreateCommand();cmd.CommandText="INSERT INTO semesters(id,year,term) VALUES($id,$year,$term)";cmd.Parameters.AddWithValue("$id",semester.Id);cmd.Parameters.AddWithValue("$year",semester.Year);cmd.Parameters.AddWithValue("$term",semester.Term);
        try{cmd.ExecuteNonQuery();}catch(SqliteException ex)when(ex.SqliteErrorCode==19){throw new AcademicValidationException("El semestre ya existe. Actualiza la lista.");}
    }
    public void AddCourse(CourseRecord course,AcademicWorkspace workspace)
    {
        WorkspaceIntegrity.Validate(workspace);
        if(workspace.IsDemo || workspace.CourseId!=course.Id || workspace.SemesterId!=course.SemesterId ||
           workspace.CourseCode!=course.Code || workspace.CourseName!=course.Name ||
           workspace.School!=course.School || workspace.GroupName!=course.Group)
            throw new AcademicValidationException("La materia no corresponde al catálogo; no se escribió ningún registro.");
        var payload=JsonSerializer.Serialize(workspace);
        using var db=Open();using var tx=db.BeginTransaction();using var cmd=db.CreateCommand();cmd.Transaction=tx;
        cmd.CommandText="INSERT INTO courses(id,semester_id,code,name,school,group_name) VALUES($id,$semester,$code,$name,$school,$group); INSERT INTO course_workspaces VALUES($id,$schema,0,$payload)";
        cmd.Parameters.AddWithValue("$schema",workspace.SchemaVersion);
        foreach(var (key,value) in new[]{("$id",course.Id),("$semester",course.SemesterId),("$code",course.Code),("$name",course.Name),("$school",course.School),("$group",course.Group),("$payload",payload)})cmd.Parameters.AddWithValue(key,value);
        try{cmd.ExecuteNonQuery();tx.Commit();}catch(SqliteException ex)when(ex.SqliteErrorCode==19){throw new AcademicValidationException("La materia ya existe o el semestre no está disponible. No se guardó un registro parcial.");}
    }
    public IWorkspaceStore OpenCourse(string id)=>new CourseStore(connectionString,id);

    private sealed class CourseStore(string connectionString,string id):IWorkspaceStore
    {
        private long revision;
        public AcademicWorkspace? Load()
        {
            using var db=new SqliteConnection(connectionString);db.Open();using var cmd=db.CreateCommand();cmd.CommandText="SELECT w.schema_version,w.revision,w.payload,c.semester_id,c.code,c.name,c.school,c.group_name,s.year,s.term FROM course_workspaces w JOIN courses c ON c.id=w.course_id JOIN semesters s ON s.id=c.semester_id WHERE w.course_id=$id";cmd.Parameters.AddWithValue("$id",id);
            using var reader=cmd.ExecuteReader();if(!reader.Read())throw new AcademicValidationException("La materia seleccionada no existe.");
            if(reader.GetInt32(0) is not (1 or 2))throw new AcademicValidationException("Versión de materia incompatible. Los datos no se reemplazaron.");
            revision=reader.GetInt64(1);var workspace=WorkspaceIntegrity.Deserialize(reader.GetString(2));
            if(workspace.IsDemo || workspace.CourseId!=id || workspace.SchemaVersion!=reader.GetInt32(0))throw new AcademicValidationException("Los datos o la versión no corresponden a la materia seleccionada.");
            if(workspace.SemesterId!=reader.GetString(3) || workspace.CourseCode!=reader.GetString(4) ||
               workspace.CourseName!=reader.GetString(5) || workspace.School!=reader.GetString(6) ||
               workspace.GroupName!=reader.GetString(7) || workspace.Period!=$"{reader.GetInt32(8)}-{reader.GetInt32(9)}")
                throw new AcademicValidationException("Los datos académicos no corresponden al catálogo. El archivo local no se modificó.");
            return workspace;
        }
        public void Save(AcademicWorkspace workspace)
        {
            if(workspace.IsDemo || workspace.CourseId!=id || workspace.SchemaVersion!=2)throw new AcademicValidationException("La materia o versión de datos no corresponde a este almacenamiento.");
            WorkspaceIntegrity.Validate(workspace);
            var payload=JsonSerializer.Serialize(workspace);
            using var db=new SqliteConnection(connectionString);db.Open();
            using(var metadata=db.CreateCommand())
            {
                metadata.CommandText="SELECT c.semester_id,c.code,c.name,c.school,c.group_name,s.year,s.term FROM courses c JOIN semesters s ON s.id=c.semester_id WHERE c.id=$id";
                metadata.Parameters.AddWithValue("$id",id);
                using var row=metadata.ExecuteReader();
                if(!row.Read() || workspace.SemesterId!=row.GetString(0) || workspace.CourseCode!=row.GetString(1) ||
                   workspace.CourseName!=row.GetString(2) || workspace.School!=row.GetString(3) ||
                   workspace.GroupName!=row.GetString(4) || workspace.Period!=$"{row.GetInt32(5)}-{row.GetInt32(6)}")
                    throw new AcademicValidationException("Los datos de la materia no corresponden al catálogo. No se guardó el cambio.");
            }
            using var tx=db.BeginTransaction();using var cmd=db.CreateCommand();cmd.Transaction=tx;
            cmd.CommandText="UPDATE course_workspaces SET payload=$payload,schema_version=$schema,revision=revision+1 WHERE course_id=$id AND revision=$revision";cmd.Parameters.AddWithValue("$schema",workspace.SchemaVersion);cmd.Parameters.AddWithValue("$payload",payload);cmd.Parameters.AddWithValue("$id",id);cmd.Parameters.AddWithValue("$revision",revision);
            if(cmd.ExecuteNonQuery()!=1)throw new AcademicValidationException("Otra instancia modificó esta materia. No se guardó el cambio; pulsa Actualizar para volver a leerla antes de reintentar.");
            tx.Commit();revision++;
        }
    }
}
