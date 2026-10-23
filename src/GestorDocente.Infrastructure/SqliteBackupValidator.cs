using System.Text.Json;
using GestorDocente.Application;
using GestorDocente.Domain;
using Microsoft.Data.Sqlite;

namespace GestorDocente.Infrastructure;

/// <summary>Verifica la copia completa sin abrir servicios que migren o escriban sus payloads.</summary>
internal static class SqliteBackupValidator
{
    public static (int Semesters,int Courses,int Students) Validate(string path)
    {
        using var db=new SqliteConnection(new SqliteConnectionStringBuilder
        {DataSource=path,Mode=SqliteOpenMode.ReadOnly,Pooling=false,DefaultTimeout=5}.ToString());
        db.Open();
        using(var command=db.CreateCommand())
        {
            command.CommandText="PRAGMA user_version";
            if(Convert.ToInt32(command.ExecuteScalar())!=1)
                throw new AcademicValidationException("La versión de la base de respaldo no es compatible.");
            command.CommandText="PRAGMA integrity_check";
            using var rows=command.ExecuteReader();var count=0;
            while(rows.Read())
            {
                if(!string.Equals(rows.GetString(0),"ok",StringComparison.OrdinalIgnoreCase))
                    throw new AcademicValidationException("La base del respaldo no supera la verificación de integridad.");
                count++;
            }
            if(count!=1)throw new AcademicValidationException("No se pudo verificar la integridad del respaldo.");
        }
        using(var command=db.CreateCommand())
        {
            command.CommandText="PRAGMA foreign_key_check";using var rows=command.ExecuteReader();
            if(rows.Read())throw new AcademicValidationException("El respaldo contiene relaciones incompletas.");
        }
        var semesters=ReadSemesters(db);var courses=0;var students=0;
        using(var command=db.CreateCommand())
        {
            // LEFT JOIN: una materia sin workspace no puede desaparecer de la verificación.
            command.CommandText="SELECT c.id,c.semester_id,c.code,c.name,c.school,c.group_name,w.schema_version,w.revision,w.payload FROM courses c LEFT JOIN course_workspaces w ON w.course_id=c.id";
            using var rows=command.ExecuteReader();
            while(rows.Read())
            {
                if(Enumerable.Range(0,9).Any(rows.IsDBNull) || rows.GetInt32(6) is <1 or >AcademicWorkspace.CurrentSchemaVersion || rows.GetInt64(7)<0)
                    throw new AcademicValidationException("El respaldo contiene una materia incompleta o incompatible.");
                if(!semesters.TryGetValue(rows.GetString(1),out var semester))
                    throw new AcademicValidationException("Una materia del respaldo no tiene un semestre válido.");
                AcademicWorkspace workspace;
                try{workspace=WorkspaceIntegrity.Deserialize(rows.GetString(8));}
                catch(JsonException){throw new AcademicValidationException("Los datos de una materia del respaldo no son legibles.");}
                if(workspace.IsDemo || workspace.SchemaVersion!=rows.GetInt32(6) ||
                    workspace.CourseId!=rows.GetString(0) || workspace.SemesterId!=semester.Id ||
                    workspace.CourseCode!=rows.GetString(2) || workspace.CourseName!=rows.GetString(3) ||
                    workspace.School!=rows.GetString(4) || workspace.GroupName!=rows.GetString(5) || workspace.Period!=semester.Name ||
                    string.IsNullOrWhiteSpace(workspace.CourseId) || string.IsNullOrWhiteSpace(workspace.CourseCode) ||
                    string.IsNullOrWhiteSpace(workspace.CourseName) || string.IsNullOrWhiteSpace(workspace.School))
                    throw new AcademicValidationException("Los datos de una materia del respaldo no corresponden al catálogo.");
                // Los formatos históricos se validan en memoria; se conservan sus bytes y revisiones originales.
                _=WorkspaceMigration.Upgrade(workspace);
                courses++;students+=workspace.Students.Count;
            }
        }
        _=SqliteInterfacePreferencesStore.Read(db);
        _=SqliteBackupPreferencesStore.Read(db);
        return(semesters.Count,courses,students);
    }

    private static Dictionary<string,AcademicSemester> ReadSemesters(SqliteConnection db)
    {
        using var command=db.CreateCommand();command.CommandText="SELECT id,year,term FROM semesters";
        using var rows=command.ExecuteReader();Dictionary<string,AcademicSemester> result=[];
        while(rows.Read())
        {
            if(rows.IsDBNull(0) || rows.IsDBNull(1) || rows.IsDBNull(2) || string.IsNullOrWhiteSpace(rows.GetString(0)) ||
                rows.GetInt32(1) is <1000 or >9999 || rows.GetInt32(2) is <1 or >2)
                throw new AcademicValidationException("El respaldo contiene un semestre inválido.");
            var semester=new AcademicSemester(rows.GetString(0),rows.GetInt32(1),rows.GetInt32(2));
            if(!result.TryAdd(semester.Id,semester))throw new AcademicValidationException("El respaldo contiene semestres duplicados.");
        }
        return result;
    }
}
