using GestorDocente.Application;
using GestorDocente.Domain;
using Microsoft.Data.Sqlite;

namespace GestorDocente.Infrastructure;

public sealed partial class SqliteCatalogStore
{
    public IReadOnlyList<CourseStudentEnrollment> StudentEnrollments(string semesterId)
    {
        using var db=new SqliteConnection(new SqliteConnectionStringBuilder(connectionString){Mode=SqliteOpenMode.ReadOnly}.ToString());
        db.Open();using var command=db.CreateCommand();
        command.CommandText="""
            SELECT c.semester_id,c.id,c.name,c.code,c.group_name,w.schema_version,
                   json_type(w.payload,'$.Students'),student.type,
                   json_extract(student.value,'$.Id'),json_extract(student.value,'$.FullName'),
                   json_extract(student.value,'$.Document'),
                   json_extract(w.payload,'$.CourseId'),json_extract(w.payload,'$.SemesterId')
            FROM courses c
            LEFT JOIN course_workspaces w ON w.course_id=c.id
            LEFT JOIN json_each(w.payload,'$.Students') student ON 1=1
            WHERE c.semester_id=$semester
            ORDER BY c.name,c.group_name,c.id,student.key
            """;
        command.Parameters.AddWithValue("$semester",semesterId);
        using var rows=command.ExecuteReader();List<CourseStudentEnrollment> result=[];
        while(rows.Read())
        {
            if(rows.IsDBNull(5) || rows.GetInt32(5) is <1 or >AcademicWorkspace.CurrentSchemaVersion ||
               rows.IsDBNull(6) || rows.GetString(6)!="array")
                throw new AcademicValidationException("Una materia del período tiene un listado de estudiantes incompleto o incompatible. Actualiza la lista y revisa el archivo local.");
            if(rows.IsDBNull(11) || rows.IsDBNull(12) || rows.GetString(11)!=rows.GetString(1) || rows.GetString(12)!=semesterId)
                throw new AcademicValidationException("Los datos de una materia no corresponden al período seleccionado. No se modificaron los datos.");
            if(rows.IsDBNull(7))continue; // A valid empty roster still has a row from the left join.
            if(rows.GetString(7)!="object" || rows.IsDBNull(8) || rows.IsDBNull(9) || string.IsNullOrWhiteSpace(rows.GetString(8)))
                throw new AcademicValidationException("El listado de una materia contiene un estudiante incompleto. No se modificaron los datos.");
            var id=rows.GetString(8);var name=rows.GetString(9);var document=rows.IsDBNull(10) ? "" : rows.GetString(10);
            StudentRules.Validate(new(id,document,name));
            result.Add(new(rows.GetString(0),rows.GetString(1),rows.GetString(2),rows.GetString(3),rows.GetString(4),id,name,document));
        }
        return result;
    }
}
