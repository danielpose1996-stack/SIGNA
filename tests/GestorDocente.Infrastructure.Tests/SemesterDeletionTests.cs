using GestorDocente.Application;
using GestorDocente.Domain;
using GestorDocente.Infrastructure;
using Microsoft.Data.Sqlite;
using Xunit;

namespace GestorDocente.Infrastructure.Tests;

public class SemesterDeletionTests
{
    [Fact]
    public void Deletion_preview_counts_records_and_deletes_only_confirmed_semester()
    {
        WithDatabase(path=>
        {
            var catalog=new CatalogService(new SqliteCatalogStore(path));
            var removed=catalog.CreateSemester(2026,1);var retained=catalog.CreateSemester(2026,2);
            var course=catalog.CreateCourse(removed.Id,"DEL","Materia ficticia","Escuela","");
            var kept=catalog.CreateCourse(retained.Id,"KEEP","Materia conservada","Escuela","");
            var service=catalog.Open(course.Id);service.ImportStudents([new("F-1","Estudiante ficticio")]);
            var student=service.Current.Students[0];
            service.ConfigureGroups(1,[new(){Id="g",Name="Parcial",Weight=100}]);service.SetGrade(1,student.Id,"g",4);
            service.AddWeek(1,WeekKind.Class);service.SetAttendance(1,student.Id,1,AttendanceState.Present);
            var preview=catalog.InspectSemesterDeletion(removed.Id);
            Assert.Equal(1,preview.CourseCount);Assert.Equal(1,preview.StudentCount);
            Assert.Equal(1,preview.GradeCount);Assert.Equal(1,preview.AttendanceCount);
            catalog.DeleteSemester(preview);
            Assert.Equal(retained.Id,Assert.Single(catalog.Semesters).Id);
            Assert.Equal(kept.Id,Assert.Single(catalog.Courses(retained.Id)).Id);
            Assert.Throws<AcademicValidationException>(()=>catalog.Open(course.Id));
        });
    }
    [Fact]
    public void Stale_confirmation_is_rejected_when_a_course_changes()
    {
        WithDatabase(path=>
        {
            var catalog=new CatalogService(new SqliteCatalogStore(path));var term=catalog.CreateSemester(2026,1);
            var course=catalog.CreateCourse(term.Id,"C","Materia ficticia","Escuela","");
            var preview=catalog.InspectSemesterDeletion(term.Id);
            catalog.Open(course.Id).AddWeek(1,WeekKind.Class);
            Assert.Throws<AcademicValidationException>(()=>catalog.DeleteSemester(preview));
            Assert.Single(catalog.Semesters);Assert.Single(catalog.Courses(term.Id));
            Assert.Single(catalog.Open(course.Id).Cut(1).Weeks);
        });
    }
    [Fact]
    public void Empty_semester_can_be_deleted_and_failed_deletion_rolls_back_everything()
    {
        WithDatabase(path=>
        {
            var catalog=new CatalogService(new SqliteCatalogStore(path));var empty=catalog.CreateSemester(2026,1);
            catalog.DeleteSemester(catalog.InspectSemesterDeletion(empty.Id));Assert.Empty(catalog.Semesters);
            var term=catalog.CreateSemester(2026,2);var course=catalog.CreateCourse(term.Id,"C","Materia ficticia","Escuela","");
            using(var db=new SqliteConnection($"Data Source={path};Pooling=False"))
            {
                db.Open();using var command=db.CreateCommand();
                command.CommandText="CREATE TRIGGER reject_semester_delete BEFORE DELETE ON semesters BEGIN SELECT RAISE(ABORT,'fallo simulado'); END";
                command.ExecuteNonQuery();
            }
            var preview=catalog.InspectSemesterDeletion(term.Id);
            Assert.Throws<SqliteException>(()=>catalog.DeleteSemester(preview));
            Assert.Single(catalog.Semesters);Assert.Single(catalog.Courses(term.Id));
            Assert.Equal(course.Id,catalog.Open(course.Id).Current.CourseId);
        });
    }
    private static void WithDatabase(Action<string> test)
    {
        var directory=Path.Combine(Path.GetTempPath(),"aula-semester-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(directory);
        try{test(Path.Combine(directory,"test.db"));}finally{Directory.Delete(directory,true);}
    }
}
