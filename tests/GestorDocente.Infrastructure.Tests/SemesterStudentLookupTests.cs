using GestorDocente.Application;
using GestorDocente.Domain;
using GestorDocente.Infrastructure;
using Xunit;

namespace GestorDocente.Infrastructure.Tests;

public class SemesterStudentLookupTests
{
    [Fact]
    public void Invalid_payload_in_another_semester_is_never_read_by_the_lookup()
    {
        WithFixture((path,store,catalog,current,other)=>
        {
            using(var db=new Microsoft.Data.Sqlite.SqliteConnection($"Data Source={path};Pooling=False"))
            {
                db.Open();using var command=db.CreateCommand();command.CommandText="UPDATE course_workspaces SET payload='not-json' WHERE course_id=$course";
                command.Parameters.AddWithValue("$course",other.Id);command.ExecuteNonQuery();
            }
            Assert.Equal(current.Id,Assert.Single(store.StudentEnrollments(current.SemesterId)).CourseId);
        });
    }

    [Fact]
    public void Payload_with_wrong_semester_is_reported_instead_of_showing_misassigned_students()
    {
        WithFixture((path,store,catalog,current,other)=>
        {
            using(var db=new Microsoft.Data.Sqlite.SqliteConnection($"Data Source={path};Pooling=False"))
            {
                db.Open();using var command=db.CreateCommand();
                command.CommandText="UPDATE course_workspaces SET payload=json_set(payload,'$.SemesterId',$semester) WHERE course_id=$course";
                command.Parameters.AddWithValue("$semester",other.SemesterId);command.Parameters.AddWithValue("$course",current.Id);command.ExecuteNonQuery();
            }
            var before=File.ReadAllBytes(path);
            Assert.Throws<AcademicValidationException>(()=>store.StudentEnrollments(current.SemesterId));
            Assert.Equal(before,File.ReadAllBytes(path));
        });
    }

    [Fact]
    public async Task Sqlite_lookup_reads_only_selected_period_without_touching_stored_data()
    {
        var path=Path.Combine(Path.GetTempPath(),$"signa-lookup-{Guid.NewGuid():N}.db");
        try
        {
            var store=new SqliteCatalogStore(path);var catalog=new CatalogService(store);
            var previous=catalog.CreateSemester(2026,1);var current=catalog.CreateSemester(2026,2);
            var old=catalog.CreateCourse(previous.Id,"OLD","Otra materia","Escuela","");
            var first=catalog.CreateCourse(current.Id,"G-1","Gerencia","Escuela","Nocturno");
            var second=catalog.CreateCourse(current.Id,"T-1","Trabajo de grado","Escuela","Diurno");
            catalog.CreateCourse(current.Id,"EMPTY","Materia sin estudiantes","Escuela","");
            foreach(var course in new[]{old,first,second})catalog.Open(course.Id).ImportStudents([new("F-01","Mariana López Torres","ficticia@example.test","3000000000")]);
            var before=File.ReadAllBytes(path);
            var matches=(await catalog.StudentIndexAsync(current.Id)).Find("mariana");
            Assert.Equal(new[]{first.Id,second.Id},Assert.Single(matches).Courses.Select(c=>c.Id));
            Assert.Equal(2,store.StudentEnrollments(current.Id).Count);
            Assert.Empty(store.StudentEnrollments("' OR 1=1 --"));
            Assert.Equal(before,File.ReadAllBytes(path));
        }
        finally{if(File.Exists(path))File.Delete(path);}
    }
    private static void WithFixture(Action<string,SqliteCatalogStore,CatalogService,CourseRecord,CourseRecord> verify)
    {
        var path=Path.Combine(Path.GetTempPath(),$"signa-lookup-validation-{Guid.NewGuid():N}.db");
        try
        {
            var store=new SqliteCatalogStore(path);var catalog=new CatalogService(store);
            var term=catalog.CreateSemester(2026,2);var previous=catalog.CreateSemester(2026,1);
            var current=catalog.CreateCourse(term.Id,"CURRENT","Materia actual ficticia","Escuela","");
            var other=catalog.CreateCourse(previous.Id,"OLD","Materia anterior ficticia","Escuela","");
            catalog.Open(current.Id).ImportStudents([new("F-01","Estudiante ficticio actual")]);
            verify(path,store,catalog,current,other);
        }
        finally{if(File.Exists(path))File.Delete(path);}
    }
}
