using System.Text.Json;
using GestorDocente.Application;
using GestorDocente.Domain;
using GestorDocente.Infrastructure;
using Microsoft.Data.Sqlite;
using Xunit;

namespace GestorDocente.Infrastructure.Tests;

public class CourseNormalizationAndLocksTests
{
    [Fact]
    public void Creation_and_editing_normalize_text_without_changing_course_identity()
    {
        WithDatabase(path=>
        {
            var catalog=new CatalogService(new SqliteCatalogStore(path));
            var term=catalog.CreateSemester(2026,2);
            var course=catalog.CreateCourse(term.Id," inf-001 "," investigación "," ingeniería "," nocturno 2 ");
            Assert.Equal("INF-001",course.Code);Assert.Equal("INVESTIGACIÓN",course.Name);
            Assert.Equal("INGENIERÍA",course.School);Assert.Equal("NOCTURNO 2",course.Group);
            var updated=catalog.UpdateCourse(course,term.Id,"0012","álgebra","ciencias","02");
            Assert.Equal(course.Id,updated.Id);Assert.Equal("0012",updated.Code);Assert.Equal("ÁLGEBRA",updated.Name);
            Assert.Equal("02",updated.Group);Assert.Single(catalog.Courses(term.Id));
        });
    }

    [Fact]
    public void Historical_normalization_is_atomic_idempotent_and_preserves_every_academic_record()
    {
        WithDatabase(path=>
        {
            var store=new SqliteCatalogStore(path);
            var term=new AcademicSemester("semestre-original",2026,2);store.AddSemester(term);
            var record=new CourseRecord("materia-original",term.Id,"inf-01","materia histórica","ingeniería","diurno 1");
            var data=new AcademicWorkspace{IsDemo=false,CourseId=record.Id,SemesterId=term.Id,CourseCode=record.Code,
                CourseName=record.Name,School=record.School,GroupName=record.Group,Period=term.Name,
                Cuts=AcademicRules.DefaultCuts(),Students=[new("alumno-id","001","Nombre de prueba","correo@example.test","00123")]};
            data.Cuts[0].Groups=[new(){Id="eval",Name="Actividad minúscula",Weight=100}];
            data.Cuts[0].Grades=[new("alumno-id","eval",4.2m)];
            data.Cuts[0].Weeks=[new(1)];data.Cuts[0].Attendance=[new("alumno-id",1,AttendanceState.Late)];
            data.Cuts[0].IsLocked=true;
            store.AddCourse(record,data);
            var reloaded=new CatalogService(new SqliteCatalogStore(path));
            var normalized=Assert.Single(reloaded.Courses(term.Id));
            Assert.Equal(record.Id,normalized.Id);Assert.Equal("MATERIA HISTÓRICA",normalized.Name);
            Assert.Equal(1,normalized.Revision);
            var actual=reloaded.Open(record.Id).Current;
            Assert.Equal(data.Students,actual.Students);
            Assert.Equal(JsonSerializer.Serialize(data.Cuts),JsonSerializer.Serialize(actual.Cuts));
            Assert.Equal("INF-01",actual.CourseCode);Assert.Equal("INGENIERÍA",actual.School);
            var backup=Assert.Single(Directory.GetFiles(Path.GetDirectoryName(path)!,"signa-antes-mayusculas-*.db"));
            using(var db=new SqliteConnection($"Data Source={backup};Mode=ReadOnly;Pooling=False"))
            {
                db.Open();using var cmd=db.CreateCommand();cmd.CommandText="PRAGMA integrity_check";Assert.Equal("ok",cmd.ExecuteScalar());
                cmd.CommandText="SELECT name FROM courses";Assert.Equal(record.Name,cmd.ExecuteScalar());
            }
            Assert.Equal(1,new SqliteCatalogStore(path).Courses(term.Id).Single().Revision);
            Assert.Single(Directory.GetFiles(Path.GetDirectoryName(path)!,"signa-antes-mayusculas-*.db"));
        });
    }

    [Fact]
    public void Lock_survives_reopening_and_failed_normalization_rolls_back_both_records()
    {
        WithDatabase(path=>
        {
            var catalog=new CatalogService(new SqliteCatalogStore(path));var term=catalog.CreateSemester(2026,1);
            var course=catalog.CreateCourse(term.Id,"A","MATERIA","ESCUELA","");
            catalog.Open(course.Id).SetCutLockedAsync(1,true).GetAwaiter().GetResult();
            Assert.True(catalog.Open(course.Id).Cut(1).IsLocked);
            using(var db=new SqliteConnection($"Data Source={path};Pooling=False"))
            {
                db.Open();using var cmd=db.CreateCommand();
                cmd.CommandText="UPDATE courses SET name='materia'; UPDATE course_workspaces SET payload=json_set(payload,'$.CourseName','materia'); CREATE TRIGGER reject_normalization BEFORE UPDATE ON course_workspaces BEGIN SELECT RAISE(ABORT,'fallo simulado'); END";
                cmd.ExecuteNonQuery();
            }
            Assert.Throws<SqliteException>(()=>new SqliteCatalogStore(path));
            using(var db=new SqliteConnection($"Data Source={path};Pooling=False"))
            {
                db.Open();using var cmd=db.CreateCommand();cmd.CommandText="SELECT name FROM courses";Assert.Equal("materia",cmd.ExecuteScalar());
                cmd.CommandText="SELECT json_extract(payload,'$.CourseName') FROM course_workspaces";Assert.Equal("materia",cmd.ExecuteScalar());
            }
        });
    }

    [Fact]
    public void Local_backup_restores_locked_cuts_and_records_on_another_database()
    {
        WithDatabase(path=>
        {
            var catalog=new CatalogService(new SqliteCatalogStore(path));var term=catalog.CreateSemester(2026,1);
            var course=catalog.CreateCourse(term.Id,"01","Materia","Escuela","Grupo");
            var service=catalog.Open(course.Id);service.ImportStudents([new("01","Estudiante ficticio")]);
            var id=service.Current.Students.Single().Id;
            service.ConfigureGroups(1,[new(){Id="a",Name="Actividad",Weight=100}]);
            service.SetGrade(1,id,"a",4);service.AddWeek(1);service.SetAttendance(1,id,1,AttendanceState.Present);
            service.SetCutLockedAsync(1,true).GetAwaiter().GetResult();
            var archive=Path.Combine(Path.GetDirectoryName(path)!,"copia.aulabk");
            new LocalBackupService(path,()=>false).Create(archive);
            var target=Path.Combine(Path.GetDirectoryName(path)!,"otro-equipo.db");
            _=new SqliteCatalogStore(target);
            new LocalBackupService(target,()=>false).Restore(archive);
            var restored=new CatalogService(new SqliteCatalogStore(target)).Open(course.Id);
            Assert.True(restored.Cut(1).IsLocked);
            Assert.Equal(4m,restored.Cut(1).Grades.Single().Value);
            Assert.Equal(AttendanceState.Present,restored.Cut(1).Attendance.Single().State);
            Assert.Throws<AcademicValidationException>(()=>restored.SetGrade(1,id,"a",5));
        });
    }

    private static void WithDatabase(Action<string> test)
    {
        var directory=Path.Combine(Path.GetTempPath(),"signa-normalizacion",Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        try{test(Path.Combine(directory,"catalogo.db"));}finally{Directory.Delete(directory,true);}
    }
}
