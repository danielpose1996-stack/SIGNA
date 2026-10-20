using System.Text.Json;
using GestorDocente.Application;
using GestorDocente.Domain;
using GestorDocente.Infrastructure;
using Microsoft.Data.Sqlite;
using Xunit;

namespace GestorDocente.Infrastructure.Tests;

public class StudentBatchPersistenceTests
{
    [Fact]
    public async Task Batch_reopens_with_contacts_and_does_not_modify_other_courses_or_existing_academic_data()
    {
        var folder=Path.Combine(Path.GetTempPath(),"signa-039-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(folder);
        try
        {
            var path=Path.Combine(folder,"catalog.db");var catalog=new CatalogService(new SqliteCatalogStore(path));
            var semester=catalog.CreateSemester(2026,2);var course=catalog.CreateCourse(semester.Id,"TEST","Materia ficticia","Escuela","A");
            var other=catalog.CreateCourse(semester.Id,"OTHER","Otra materia ficticia","Escuela","B");var otherBefore=JsonSerializer.Serialize(catalog.Open(other.Id).Current);
            var service=catalog.Open(course.Id);await service.AddStudentAsync("Existente ficticio");var id=service.Current.Students[0].Id;
            await service.ConfigureGroupsAsync(1,[new(){Id="direct",Name="Trabajo",Weight=100}]);await service.SetGradeAsync(1,id,"direct",4);
            await service.AddWeekAsync(1);await service.SetAttendanceAsync(1,id,1,AttendanceState.Present);
            var cuts=JsonSerializer.Serialize(service.Current.Cuts);
            await service.AddStudentsAsync(StudentBatchPreview.Build(service.Current,"Ana Ficticia\nBeto Ficticio","00001\n00002","ana@example.test\n\n","\n3000000000\n"),true);
            var reopened=catalog.Open(course.Id);Assert.Equal(JsonSerializer.Serialize(service.Current),JsonSerializer.Serialize(reopened.Current));
            Assert.Equal(cuts,JsonSerializer.Serialize(reopened.Current.Cuts));Assert.Equal(otherBefore,JsonSerializer.Serialize(catalog.Open(other.Id).Current));
            Assert.Equal("ana@example.test",reopened.Current.Students[1].Email);Assert.Equal("3000000000",reopened.Current.Students[2].Phone);
            var archive=Path.Combine(folder,"respaldo.aulabk");const string password="Contraseña ficticia temporal";
            new EncryptedLocalBackupService(path).Create(archive,password);var restored=Path.Combine(folder,"restored.db");
            new EncryptedLocalBackupService(restored).Restore(archive,password);
            Assert.Equal(JsonSerializer.Serialize(reopened.Current),JsonSerializer.Serialize(new CatalogService(new SqliteCatalogStore(restored)).Open(course.Id).Current));
            using var db=new SqliteConnection($"Data Source={restored};Pooling=False");db.Open();using var check=db.CreateCommand();
            check.CommandText="PRAGMA integrity_check";Assert.Equal("ok",check.ExecuteScalar());check.CommandText="PRAGMA foreign_key_check";Assert.Null(check.ExecuteScalar());
        }
        finally{Directory.Delete(folder,true);}
    }

    [Fact]
    public async Task Concurrent_writer_rejects_the_entire_student_batch()
    {
        var path=Path.Combine(Path.GetTempPath(),"signa-039-"+Guid.NewGuid().ToString("N")+".db");
        try
        {
            var catalog=new CatalogService(new SqliteCatalogStore(path));var semester=catalog.CreateSemester(2026,1);
            var course=catalog.CreateCourse(semester.Id,"TEST","Materia ficticia","Escuela","A");var first=catalog.Open(course.Id);var second=catalog.Open(course.Id);
            var preview=StudentBatchPreview.Build(second.Current,"Ana Ficticia\nBeto Ficticio");var before=second.Current;
            await first.AddStudentAsync("Cambio de otra instancia");
            await Assert.ThrowsAsync<AcademicValidationException>(()=>second.AddStudentsAsync(preview,true));
            Assert.Same(before,second.Current);Assert.False(second.IsSaving);
            Assert.Equal("Cambio de otra instancia",Assert.Single(catalog.Open(course.Id).Current.Students).FullName);
        }
        finally{if(File.Exists(path))File.Delete(path);}
    }

    [Fact]
    public async Task SQLite_write_failure_rolls_back_and_the_same_review_can_be_retried()
    {
        var path=Path.Combine(Path.GetTempPath(),"signa-039-"+Guid.NewGuid().ToString("N")+".db");
        try
        {
            var catalog=new CatalogService(new SqliteCatalogStore(path));var semester=catalog.CreateSemester(2026,1);
            var course=catalog.CreateCourse(semester.Id,"TEST","Materia ficticia","Escuela","A");var service=catalog.Open(course.Id);
            var before=service.Current;var preview=StudentBatchPreview.Build(before,"Ana Ficticia\nBeto Ficticio");
            using var db=new SqliteConnection($"Data Source={path};Pooling=False");db.Open();using var command=db.CreateCommand();
            command.CommandText="CREATE TRIGGER fail_batch BEFORE UPDATE ON course_workspaces BEGIN SELECT RAISE(ABORT,'Fallo ficticio'); END";command.ExecuteNonQuery();
            await Assert.ThrowsAsync<SqliteException>(()=>service.AddStudentsAsync(preview,true));Assert.Same(before,service.Current);Assert.False(service.IsSaving);
            Assert.Empty(catalog.Open(course.Id).Current.Students);
            command.CommandText="DROP TRIGGER fail_batch";command.ExecuteNonQuery();
            await service.AddStudentsAsync(preview,true);Assert.Equal(2,catalog.Open(course.Id).Current.Students.Count);
        }
        finally{if(File.Exists(path))File.Delete(path);}
    }
}
