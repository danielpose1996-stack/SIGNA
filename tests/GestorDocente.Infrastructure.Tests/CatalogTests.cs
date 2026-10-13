using GestorDocente.Application;
using GestorDocente.Infrastructure;
using GestorDocente.Domain;
using Xunit;

namespace GestorDocente.Infrastructure.Tests;

public class CatalogTests
{
    [Fact]
    public void Editing_course_preserves_students_grades_and_attendance_when_moving_semester()
    {
        WithDatabase(path=>
        {
            var catalog=new CatalogService(new SqliteCatalogStore(path));
            var first=catalog.CreateSemester(2026,1);var second=catalog.CreateSemester(2026,2);
            var original=catalog.CreateCourse(first.Id,"MAT-1","Materia anterior","Escuela A","G1");
            var workspace=catalog.Open(original.Id);
            workspace.ImportStudents([new("F-01","Estudiante ficticio")]);
            var student=workspace.Current.Students.Single();
            workspace.ConfigureGroups(1,[new(){Id="parcial",Name="Parcial",Weight=100}]);
            workspace.SetGrade(1,student.Id,"parcial",4.2m);
            workspace.AddWeek(1,WeekKind.Class);workspace.SetAttendance(1,student.Id,1,AttendanceState.Present);
            var updated=catalog.UpdateCourse(original,second.Id,"MAT-2","Materia editada","Escuela B","G2");
            Assert.Equal(original.Id,updated.Id);
            Assert.Empty(catalog.Courses(first.Id));
            Assert.Equal("Materia editada",Assert.Single(catalog.Courses(second.Id)).Name);
            var reopened=catalog.Open(original.Id).Current;
            Assert.Equal("2026-2",reopened.Period);Assert.Equal("MAT-2",reopened.CourseCode);
            Assert.Equal(student,reopened.Students.Single());
            Assert.Equal(4.2m,reopened.Cuts[0].Grades.Single().Value);
            Assert.Equal(AttendanceState.Present,reopened.Cuts[0].Attendance.Single().State);
        });
    }
    [Fact]
    public void Editing_course_rejects_duplicate_or_stale_metadata_without_partial_change()
    {
        WithDatabase(path=>
        {
            var catalog=new CatalogService(new SqliteCatalogStore(path));
            var term=catalog.CreateSemester(2026,1);
            var original=catalog.CreateCourse(term.Id,"ONE","Primera","Escuela","A");
            catalog.CreateCourse(term.Id,"TWO","Segunda","Escuela","A");
            Assert.Throws<AcademicValidationException>(()=>catalog.UpdateCourse(original,term.Id,"TWO","Primera","Escuela","A"));
            Assert.Equal("ONE",catalog.Open(original.Id).Current.CourseCode);
            var destination=catalog.CreateSemester(2026,2);
            catalog.CreateCourse(destination.Id,"ONE","En destino","Escuela","A");
            Assert.Throws<AcademicValidationException>(()=>catalog.UpdateCourse(original,destination.Id,"ONE","Primera","Escuela","A"));
            Assert.Equal(term.Id,catalog.Open(original.Id).Current.SemesterId);
            var edited=catalog.UpdateCourse(original,term.Id,"NEXT","Nueva","Escuela","A");
            Assert.Throws<AcademicValidationException>(()=>catalog.UpdateCourse(original,term.Id,"OTHER","Desactualizada","Escuela","A"));
            Assert.Equal(edited.Code,catalog.Open(original.Id).Current.CourseCode);
        });
    }
    [Fact]
    public void Failed_course_edit_rolls_back_catalog_and_workspace_together()
    {
        WithDatabase(path=>
        {
            var catalog=new CatalogService(new SqliteCatalogStore(path));
            var term=catalog.CreateSemester(2026,1);
            var original=catalog.CreateCourse(term.Id,"SAFE","Materia original","Escuela","A");
            using(var db=new Microsoft.Data.Sqlite.SqliteConnection($"Data Source={path};Pooling=False"))
            {
                db.Open();using var command=db.CreateCommand();
                command.CommandText="CREATE TRIGGER reject_workspace_edit BEFORE UPDATE ON course_workspaces BEGIN SELECT RAISE(ABORT, 'fallo simulado'); END";
                command.ExecuteNonQuery();
            }
            Assert.Throws<Microsoft.Data.Sqlite.SqliteException>(()=>catalog.UpdateCourse(original,term.Id,"NEW","Materia nueva","Otra escuela","B"));
            Assert.Equal("SAFE",Assert.Single(catalog.Courses(term.Id)).Code);
            Assert.Equal("Materia original",catalog.Open(original.Id).Current.CourseName);
        });
    }
    [Fact]
    public void Deleting_one_course_removes_its_workspace_but_preserves_other_courses_and_semester()
    {
        WithDatabase(path=>
        {
            var catalog=new CatalogService(new SqliteCatalogStore(path));var semester=catalog.CreateSemester(2026,1);
            var removed=catalog.CreateCourse(semester.Id,"DEL","Materia ficticia A","Escuela","");
            var kept=catalog.CreateCourse(semester.Id,"KEEP","Materia ficticia B","Escuela","");
            catalog.Open(removed.Id).ImportStudents([new("F-1","Alumna ficticia")]);
            Assert.Throws<AcademicValidationException>(()=>catalog.DeleteCourse("inexistente"));
            Assert.Equal(2,catalog.Courses(semester.Id).Count);
            catalog.DeleteCourse(removed.Id);
            Assert.Single(catalog.Courses(semester.Id));
            Assert.Equal(kept.Id,catalog.Courses(semester.Id).Single().Id);
            Assert.Throws<AcademicValidationException>(()=>catalog.Open(removed.Id));
            Assert.Empty(catalog.Open(kept.Id).Current.Students);
            Assert.Single(catalog.Semesters);
        });
    }
    [Fact]
    public void Failed_course_delete_rolls_back_its_workspace_and_grades()
    {
        WithDatabase(path=>
        {
            var catalog=new CatalogService(new SqliteCatalogStore(path));var semester=catalog.CreateSemester(2026,1);
            var course=catalog.CreateCourse(semester.Id,"SAFE","Materia ficticia","Escuela","");
            catalog.Open(course.Id).ImportStudents([new("F-1","Alumna ficticia")]);
            using(var db=new Microsoft.Data.Sqlite.SqliteConnection($"Data Source={path};Pooling=False"))
            {
                db.Open();using var command=db.CreateCommand();
                command.CommandText="CREATE TRIGGER prevent_course_delete BEFORE DELETE ON courses BEGIN SELECT RAISE(ABORT, 'simulated failure'); END";
                command.ExecuteNonQuery();
            }
            Assert.Throws<Microsoft.Data.Sqlite.SqliteException>(()=>catalog.DeleteCourse(course.Id));
            Assert.Single(catalog.Courses(semester.Id));
            Assert.Single(catalog.Open(course.Id).Current.Students);
        });
    }
    [Fact]
    public void Imported_students_survive_reopening_without_leaking_to_other_course()
    {
        WithDatabase(path=>{var catalog=new CatalogService(new SqliteCatalogStore(path));var term=catalog.CreateSemester(2026,2);var a=catalog.CreateCourse(term.Id,"A","Materia A","Escuela","");var b=catalog.CreateCourse(term.Id,"B","Materia B","Escuela","");
            catalog.Open(a.Id).ImportStudents([new("000012","Estudiante ficticio")]);Assert.Equal("000012",catalog.Open(a.Id).Current.Students.Single().Document);Assert.Empty(catalog.Open(b.Id).Current.Students);Assert.Equal(1,catalog.Courses(term.Id).Single(c=>c.Id==a.Id).StudentCount);
        });
    }
    [Fact]
    public void Calculating_pending_grades_never_writes_zeros_or_changes_sqlite_revision()
    {
        WithDatabase(path=>
        {
            var catalog=new CatalogService(new SqliteCatalogStore(path));
            var semester=catalog.CreateSemester(2026,1);
            var course=catalog.CreateCourse(semester.Id,"PEND","Materia ficticia","Escuela","");
            var service=catalog.Open(course.Id);
            service.ImportStudents([new("F-001","Estudiante ficticio")]);
            var student=service.Current.Students.Single();
            service.ConfigureGroups(1,[new(){Id="g",Name="Talleres",Weight=40,Mode=EvaluationMode.SubActivities,
                Activities=[new(){Id="a",Name="Taller 1"},new(){Id="b",Name="Taller 2"}]},
                new(){Id="p",Name="Parcial",Weight=60}]);
            service.SetGrade(1,student.Id,"a",4m);
            (long Revision,string Payload) Snapshot()
            {
                using var db=new Microsoft.Data.Sqlite.SqliteConnection($"Data Source={path};Pooling=False");
                db.Open();using var command=db.CreateCommand();
                command.CommandText="SELECT revision,payload FROM course_workspaces WHERE course_id=$id";
                command.Parameters.AddWithValue("$id",course.Id);
                using var row=command.ExecuteReader();Assert.True(row.Read());
                return (row.GetInt64(0),row.GetString(1));
            }
            var before=Snapshot();
            var reopened=catalog.Open(course.Id);
            Assert.Equal(0.8m,GradeCalculator.Cut(reopened.Cut(1),student.Id));
            Assert.Equal(0.8m,new AcademicCutReadIndex(reopened.Cut(1)).Cut(student.Id));
            Assert.Equal(0.8m,AcademicReportProjection.Build(reopened.Current,1,AcademicReportKind.Notes).Rows.Single()[^2]);
            Assert.Equal(before,Snapshot());
            Assert.Single(catalog.Open(course.Id).Cut(1).Grades);
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
            var reopened=catalog.Open(course.Id);Assert.Equal(AcademicWorkspace.CurrentSchemaVersion,reopened.Current.SchemaVersion);Assert.Empty(reopened.Cut(1).Groups[0].Activities);Assert.Equal(4.5m,GradeCalculator.Cut(reopened.Cut(1),student));
            reopened.ConfigureGroups(1,[new(){Id="g",Name="Parcial",Weight=100,Mode=EvaluationMode.SubActivities,Activities=[new(){Id="a",Name="Parte 1"},new(){Id="b",Name="Parte 2"}]}]);
            reopened.SetGrade(1,student,"b",3.5m);reopened=catalog.Open(course.Id);
            Assert.Equal(4m,GradeCalculator.Cut(reopened.Cut(1),student));Assert.Equal(2,reopened.Cut(1).Groups[0].Activities.Count);
        });
    }
    [Theory]
    [InlineData("json_remove(payload,'$.Students')")]
    [InlineData("json_set(payload,'$.SemesterId','otra')")]
    public void Inconsistent_payload_is_rejected_without_rewriting_it(string expression)
    {
        WithDatabase(path=>{
            var catalog=new CatalogService(new SqliteCatalogStore(path));var term=catalog.CreateSemester(2026,1);
            var course=catalog.CreateCourse(term.Id,"AUD","Auditoría","Escuela","");
            using(var db=new Microsoft.Data.Sqlite.SqliteConnection($"Data Source={path};Pooling=False"))
            {
                db.Open();using var cmd=db.CreateCommand();cmd.CommandText=$"UPDATE course_workspaces SET payload={expression} WHERE course_id=$id";
                cmd.Parameters.AddWithValue("$id",course.Id);cmd.ExecuteNonQuery();
            }
            Assert.Throws<AcademicValidationException>(()=>catalog.Open(course.Id));
            if(expression.Contains("Students",StringComparison.Ordinal))
                Assert.Throws<AcademicValidationException>(()=>catalog.Courses(term.Id));
        });
    }
    [Fact]
    public void Save_rejects_metadata_mismatch_before_commit()
    {
        WithDatabase(path=>{
            var store=new SqliteCatalogStore(path);var catalog=new CatalogService(store);
            var term=catalog.CreateSemester(2026,1);var course=catalog.CreateCourse(term.Id,"META","Materia","Escuela","");
            var adapter=store.OpenCourse(course.Id);var data=adapter.Load()!;data.SemesterId="otro";
            Assert.Throws<AcademicValidationException>(()=>adapter.Save(data));
            Assert.Equal(term.Id,catalog.Open(course.Id).Current.SemesterId);
        });
    }
    [Fact]
    public void Catalog_refresh_failure_reports_committed_semester_without_duplicate_insert()
    {
        WithDatabase(path=>{
            var catalog=new CatalogService(new SqliteCatalogStore(path));
            catalog.Changed+=()=>throw new InvalidOperationException("Observador ficticio");
            var error=Assert.Throws<AcademicRefreshException>(()=>catalog.CreateSemester(2026,1));
            var created=Assert.IsType<AcademicSemester>(error.CommittedEntity);
            Assert.Equal(created.Id,new CatalogService(new SqliteCatalogStore(path)).Semesters.Single().Id);
        });
    }
    [Fact]
    public void Legacy_sqlite_workspace_upgrades_once_and_preserves_grade_values_and_states()
    {
        WithDatabase(path=>{
            var catalog=new CatalogService(new SqliteCatalogStore(path));var term=catalog.CreateSemester(2026,1);var course=catalog.CreateCourse(term.Id,"L","Anterior","Escuela","");
            var legacy=DemoWorkspaceFactory.Create();legacy.SchemaVersion=1;legacy.IsDemo=false;legacy.CourseId=course.Id;legacy.SemesterId=term.Id;
            legacy.CourseCode=course.Code;legacy.CourseName=course.Name;legacy.School=course.School;legacy.GroupName=course.Group;legacy.Period=term.Name;
            var json=System.Text.Json.Nodes.JsonNode.Parse(System.Text.Json.JsonSerializer.Serialize(legacy))!;
            foreach(var cut in json["Cuts"]!.AsArray())foreach(var group in cut!["Groups"]!.AsArray())foreach(var activity in group!["Activities"]!.AsArray())activity!["Rubric"]=new System.Text.Json.Nodes.JsonObject{["Criteria"]=new System.Text.Json.Nodes.JsonArray()};
            using(var db=new Microsoft.Data.Sqlite.SqliteConnection($"Data Source={path};Pooling=False"))
            {
                db.Open();using var cmd=db.CreateCommand();cmd.CommandText="UPDATE course_workspaces SET schema_version=1,payload=$payload WHERE course_id=$id";cmd.Parameters.AddWithValue("$payload",json.ToJsonString());cmd.Parameters.AddWithValue("$id",course.Id);cmd.ExecuteNonQuery();
            }
            var reopened=catalog.Open(course.Id);Assert.Equal(AcademicWorkspace.CurrentSchemaVersion,reopened.Current.SchemaVersion);Assert.Equal(legacy.Students,reopened.Current.Students);Assert.Equal(legacy.Cuts[0].Grades,reopened.Cut(1).Grades);Assert.Equal(legacy.Cuts[0].Attendance,reopened.Cut(1).Attendance);
            using(var db=new Microsoft.Data.Sqlite.SqliteConnection($"Data Source={path};Pooling=False"))
            {
                db.Open();using var cmd=db.CreateCommand();cmd.CommandText="SELECT schema_version,revision,payload FROM course_workspaces WHERE course_id=$id";cmd.Parameters.AddWithValue("$id",course.Id);using var reader=cmd.ExecuteReader();Assert.True(reader.Read());Assert.Equal(AcademicWorkspace.CurrentSchemaVersion,reader.GetInt32(0));Assert.Equal(1,reader.GetInt64(1));Assert.DoesNotContain("Rubric",reader.GetString(2));
            }
            Assert.Equal(AcademicWorkspace.CurrentSchemaVersion,catalog.Open(course.Id).Current.SchemaVersion);
        });
    }
    private static void WithDatabase(Action<string> test)
    {
        var dir=Path.Combine(Path.GetTempPath(),"gestor-catalog-tests",Guid.NewGuid().ToString("N"));Directory.CreateDirectory(dir);
        try{test(Path.Combine(dir,"test.db"));}finally{Directory.Delete(dir,true);}
    }
}

