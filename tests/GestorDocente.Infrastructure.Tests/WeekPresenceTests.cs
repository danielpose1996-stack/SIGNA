using GestorDocente.Application;
using GestorDocente.Domain;
using GestorDocente.Infrastructure;
using Microsoft.Data.Sqlite;
using Xunit;

namespace GestorDocente.Infrastructure.Tests;

public class WeekPresenceTests
{
    [Theory]
    [InlineData(WeekKind.Class)]
    [InlineData(WeekKind.Activities)]
    public async Task Bulk_presence_survives_reopening_in_one_revision_and_remains_individually_editable(WeekKind kind)
    {
        var directory=Path.Combine(Path.GetTempPath(),Guid.NewGuid().ToString("N"));Directory.CreateDirectory(directory);
        try
        {
            var path=Path.Combine(directory,"fixture.db");var catalog=new CatalogService(new SqliteCatalogStore(path));
            var semester=catalog.CreateSemester(2026,2);var course=catalog.CreateCourse(semester.Id,"F-1","Materia ficticia","Escuela","");
            var other=catalog.CreateCourse(semester.Id,"F-2","Otra materia ficticia","Escuela","");
            var otherBefore=System.Text.Json.JsonSerializer.Serialize(catalog.Open(other.Id).Current);
            var service=catalog.Open(course.Id);service.ImportStudents([new("F01","Estudiante ficticio uno"),new("F02","Estudiante ficticio dos")]);
            var before=Revision(path,course.Id);
            await service.AddWeekAsync(2,kind,new DateOnly(2026,10,13),markAllPresent:true);
            Assert.Equal(before+1,Revision(path,course.Id));
            var reopened=catalog.Open(course.Id);Assert.Equal(2,reopened.Cut(2).Attendance.Count);
            Assert.All(reopened.Cut(2).Attendance,a=>Assert.Equal(AttendanceState.Present,a.State));Assert.Empty(reopened.Cut(1).Weeks);
            var student=reopened.Current.Students[0];var report=AttendancePdfProjection.Build(reopened.Current,2,student.Id);
            Assert.Equal(100m,report.Percentage);Assert.Equal("Excelente",report.Status);Assert.Equal(new DateOnly(2026,10,13),Assert.Single(report.Lines).Date);
            Assert.True(new AttendancePdfExporter().Export(reopened.Current,2,student.Id).Length>1000);
            await reopened.SetAttendanceAsync(2,student.Id,1,AttendanceState.Absent);
            report=AttendancePdfProjection.Build(catalog.Open(course.Id).Current,2,student.Id);Assert.Equal(0m,report.Percentage);Assert.Equal("A - Ausente",Assert.Single(report.Lines).State);
            Assert.Equal(otherBefore,System.Text.Json.JsonSerializer.Serialize(catalog.Open(other.Id).Current));
            using var db=new SqliteConnection($"Data Source={path};Pooling=false");db.Open();using var check=db.CreateCommand();
            check.CommandText="PRAGMA integrity_check";Assert.Equal("ok",check.ExecuteScalar());check.CommandText="PRAGMA foreign_key_check";Assert.Null(check.ExecuteScalar());
        }
        finally{Directory.Delete(directory,true);}
    }

    [Fact]
    public async Task Stale_writer_cannot_save_any_part_of_a_bulk_week()
    {
        var directory=Path.Combine(Path.GetTempPath(),Guid.NewGuid().ToString("N"));Directory.CreateDirectory(directory);
        try
        {
            var path=Path.Combine(directory,"fixture.db");var catalog=new CatalogService(new SqliteCatalogStore(path));
            var semester=catalog.CreateSemester(2026,2);var course=catalog.CreateCourse(semester.Id,"F","Materia ficticia","Escuela","");
            var winner=catalog.Open(course.Id);winner.ImportStudents([new("F01","Estudiante ficticio")]);var stale=catalog.Open(course.Id);var source=stale.Current;
            await winner.AddWeekAsync(1);var revision=Revision(path,course.Id);
            await Assert.ThrowsAsync<AcademicValidationException>(()=>stale.AddWeekAsync(1,markAllPresent:true));
            Assert.Same(source,stale.Current);Assert.Empty(stale.Cut(1).Weeks);Assert.Empty(stale.Cut(1).Attendance);Assert.False(stale.IsSaving);
            var reopened=catalog.Open(course.Id);Assert.Single(reopened.Cut(1).Weeks);Assert.Empty(reopened.Cut(1).Attendance);Assert.Equal(revision,Revision(path,course.Id));
        }
        finally{Directory.Delete(directory,true);}
    }

    private static long Revision(string path,string course)
    {
        using var db=new SqliteConnection($"Data Source={path};Pooling=false");db.Open();using var query=db.CreateCommand();query.CommandText="SELECT revision FROM course_workspaces WHERE course_id=$id";query.Parameters.AddWithValue("$id",course);
        return (long)query.ExecuteScalar()!;
    }
}
