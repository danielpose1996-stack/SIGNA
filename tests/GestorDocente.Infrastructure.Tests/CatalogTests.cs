using GestorDocente.Application;
using GestorDocente.Infrastructure;
using GestorDocente.Domain;
using Xunit;

namespace GestorDocente.Infrastructure.Tests;

public class CatalogTests
{
    [Fact]
    public void Imported_students_survive_reopening_without_leaking_to_other_course()
    {
        WithDatabase(path=>{var catalog=new CatalogService(new SqliteCatalogStore(path));var term=catalog.CreateSemester(2026,2);var a=catalog.CreateCourse(term.Id,"A","Materia A","Escuela","");var b=catalog.CreateCourse(term.Id,"B","Materia B","Escuela","");
            catalog.Open(a.Id).ImportStudents([new("000012","Estudiante ficticio")]);Assert.Equal("000012",catalog.Open(a.Id).Current.Students.Single().Document);Assert.Empty(catalog.Open(b.Id).Current.Students);Assert.Equal(1,catalog.Courses(term.Id).Single(c=>c.Id==a.Id).StudentCount);
        });
    }
    [Fact]
    public void Incompatible_catalog_is_rejected_without_overwriting_file()
    {
        WithDatabase(path=>{using(var db=new Microsoft.Data.Sqlite.SqliteConnection($"Data Source={path};Pooling=False")){db.Open();using var cmd=db.CreateCommand();cmd.CommandText="PRAGMA user_version=99";cmd.ExecuteNonQuery();}
            var before=File.ReadAllBytes(path);Assert.Throws<AcademicValidationException>(()=>new SqliteCatalogStore(path));Assert.Equal(before,File.ReadAllBytes(path));});
    }
    [Fact]
    public void Deleted_records_and_grades_survive_reopen_in_correct_course()
    {
        WithDatabase(path=>{var store=new SqliteCatalogStore(path);var catalog=new CatalogService(store);var term=catalog.CreateSemester(2026,1);
            var course=catalog.CreateCourse(term.Id,"ABC","Asignatura","Escuela","");var adapter=store.OpenCourse(course.Id);var data=adapter.Load()!;data.Students.Add(new("student","0001","Estudiante ficticio"));adapter.Save(data);
            var service=catalog.Open(course.Id);var group=new EvaluationGroup{Name="Trabajos",Weight=100,Mode=EvaluationMode.SubActivities,Activities=[new(){Id="activity",Name="Trabajo"}]};service.ConfigureGroups(1,[group]);service.SetGrade(1,"student","activity",4);service.AddWeek(1,WeekKind.Activities);service.SetAttendance(1,"student",1,AttendanceState.Late);service.RemoveActivity(1,"activity");service.RemoveWeek(1,1);
            var reopened=catalog.Open(course.Id);reopened.RestoreActivity(1,"activity");reopened.RestoreWeek(1,1);Assert.Equal(4m,GradeCalculator.Cut(reopened.Cut(1),"student"));Assert.Equal(AttendanceState.Late,reopened.Cut(1).Attendance.Single().State);Assert.Equal(1,catalog.Courses(term.Id).Single().StudentCount);
        });
    }
    [Fact]
    public void Catalog_retains_semesters_codes_and_independent_empty_courses()
    {
        WithDatabase(path=>{
            var catalog=new CatalogService(new SqliteCatalogStore(path));
            var first=catalog.CreateSemester(2026,1);var second=catalog.CreateSemester(2027,1);
            var a=catalog.CreateCourse(first.Id,"0012","Programación","Ingeniería","");
            var b=catalog.CreateCourse(second.Id,"0012","Programación","Ingeniería","");
            var reopened=new CatalogService(new SqliteCatalogStore(path));
            Assert.Equal(2,reopened.Semesters.Count);Assert.Equal("0012",reopened.Courses(first.Id)[0].Code);
            var wa=reopened.Open(a.Id);Assert.False(wa.Current.IsDemo);Assert.Empty(wa.Current.Students);Assert.Equal(3,wa.Current.Cuts.Count);
            wa.ConfigureGroups(1,[new(){Name="Trabajos",Weight=100,Mode=EvaluationMode.SubActivities,Activities=[new(){Name="Trabajo 1"}]}]);
            Assert.Empty(reopened.Open(b.Id).Cut(1).Groups);
            Assert.Single(reopened.Open(a.Id).Cut(1).Groups);
            Assert.Throws<AcademicValidationException>(()=>catalog.CreateCourse(first.Id,"0012","Otro","Ingeniería",""));
            Assert.Throws<AcademicValidationException>(()=>catalog.CreateCourse("inexistente","A","Otro","Escuela",""));
        });
    }
    [Fact]
    public void Stale_instance_does_not_overwrite_saved_workspace()
    {
        WithDatabase(path=>{
            var catalog=new CatalogService(new SqliteCatalogStore(path));var term=catalog.CreateSemester(2026,2);
            var course=catalog.CreateCourse(term.Id,"INF-3","Asignatura","Escuela","");
            var a=catalog.Open(course.Id);var b=catalog.Open(course.Id);
            a.AddWeek(1,WeekKind.Class);
            Assert.Throws<AcademicValidationException>(()=>b.AddWeek(1,WeekKind.Activities));
            Assert.Empty(b.Cut(1).Weeks);
            Assert.Equal(WeekKind.Class,catalog.Open(course.Id).Cut(1).Weeks.Single().Kind);
        });
    }
    [Theory]
    [InlineData("", "Asignatura", "Escuela")]
    [InlineData("X", "", "Escuela")]
    [InlineData("X", "Asignatura", "")]
    public void Required_fields_are_validated_before_insert(string code,string name,string school)
    {
        WithDatabase(path=>{
            var catalog=new CatalogService(new SqliteCatalogStore(path));var term=catalog.CreateSemester(2026,1);
            Assert.Throws<AcademicValidationException>(()=>catalog.CreateCourse(term.Id,code,name,school,""));
            Assert.Empty(catalog.Courses(term.Id));
        });
    }
    [Fact]
    public void Direct_grades_and_mode_changes_survive_sqlite_reopening()
    {
        WithDatabase(path=>{
            var catalog=new CatalogService(new SqliteCatalogStore(path));var term=catalog.CreateSemester(2026,2);
            var course=catalog.CreateCourse(term.Id,"D","Evaluación directa","Escuela","");var service=catalog.Open(course.Id);
            service.ImportStudents([new("00012345","Estudiante ficticio")]);var student=service.Current.Students.Single().Id;
            service.ConfigureGroups(1,[new(){Id="g",Name="Parcial",Weight=100}]);service.SetGrade(1,student,"g",4.5m);
            var reopened=catalog.Open(course.Id);Assert.Equal(2,reopened.Current.SchemaVersion);Assert.Empty(reopened.Cut(1).Groups[0].Activities);Assert.Equal(4.5m,GradeCalculator.Cut(reopened.Cut(1),student));
            reopened.ConfigureGroups(1,[new(){Id="g",Name="Parcial",Weight=100,Mode=EvaluationMode.SubActivities,Activities=[new(){Id="a",Name="Parte 1"},new(){Id="b",Name="Parte 2"}]}]);
            reopened.SetGrade(1,student,"b",3.5m);reopened=catalog.Open(course.Id);
            Assert.Equal(4m,GradeCalculator.Cut(reopened.Cut(1),student));Assert.Equal(2,reopened.Cut(1).Groups[0].Activities.Count);
        });
    }
    [Fact]
    public void Legacy_sqlite_workspace_upgrades_once_and_preserves_grade_values_and_states()
    {
        WithDatabase(path=>{
            var catalog=new CatalogService(new SqliteCatalogStore(path));var term=catalog.CreateSemester(2026,1);var course=catalog.CreateCourse(term.Id,"L","Anterior","Escuela","");
            var legacy=DemoWorkspaceFactory.Create();legacy.SchemaVersion=1;legacy.IsDemo=false;legacy.CourseId=course.Id;legacy.SemesterId=term.Id;
            var json=System.Text.Json.Nodes.JsonNode.Parse(System.Text.Json.JsonSerializer.Serialize(legacy))!;
            foreach(var cut in json["Cuts"]!.AsArray())foreach(var group in cut!["Groups"]!.AsArray())foreach(var activity in group!["Activities"]!.AsArray())activity!["Rubric"]=new System.Text.Json.Nodes.JsonObject{["Criteria"]=new System.Text.Json.Nodes.JsonArray()};
            using(var db=new Microsoft.Data.Sqlite.SqliteConnection($"Data Source={path};Pooling=False"))
            {
                db.Open();using var cmd=db.CreateCommand();cmd.CommandText="UPDATE course_workspaces SET schema_version=1,payload=$payload WHERE course_id=$id";cmd.Parameters.AddWithValue("$payload",json.ToJsonString());cmd.Parameters.AddWithValue("$id",course.Id);cmd.ExecuteNonQuery();
            }
            var reopened=catalog.Open(course.Id);Assert.Equal(2,reopened.Current.SchemaVersion);Assert.Equal(legacy.Students,reopened.Current.Students);Assert.Equal(legacy.Cuts[0].Grades,reopened.Cut(1).Grades);Assert.Equal(legacy.Cuts[0].Attendance,reopened.Cut(1).Attendance);
            using(var db=new Microsoft.Data.Sqlite.SqliteConnection($"Data Source={path};Pooling=False"))
            {
                db.Open();using var cmd=db.CreateCommand();cmd.CommandText="SELECT schema_version,revision,payload FROM course_workspaces WHERE course_id=$id";cmd.Parameters.AddWithValue("$id",course.Id);using var reader=cmd.ExecuteReader();Assert.True(reader.Read());Assert.Equal(2,reader.GetInt32(0));Assert.Equal(1,reader.GetInt64(1));Assert.DoesNotContain("Rubric",reader.GetString(2));
            }
            Assert.Equal(2,catalog.Open(course.Id).Current.SchemaVersion);
        });
    }
    private static void WithDatabase(Action<string> test)
    {
        var dir=Path.Combine(Path.GetTempPath(),"gestor-catalog-tests",Guid.NewGuid().ToString("N"));Directory.CreateDirectory(dir);
        try{test(Path.Combine(dir,"test.db"));}finally{Directory.Delete(dir,true);}
    }
}

