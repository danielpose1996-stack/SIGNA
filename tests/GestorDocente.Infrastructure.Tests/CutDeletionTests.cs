using System.Text.Json;
using GestorDocente.Application;
using GestorDocente.Domain;
using GestorDocente.Infrastructure;
using Microsoft.Data.Sqlite;
using Xunit;

namespace GestorDocente.Infrastructure.Tests;

public class CutDeletionTests
{
    [Fact]
    public async Task Pending_weights_and_renumbered_records_survive_reopening_edit_and_encrypted_backup()
    {
        var directory=Path.Combine(Path.GetTempPath(),Guid.NewGuid().ToString("N"));Directory.CreateDirectory(directory);
        try
        {
            var path=Path.Combine(directory,"source.db");var catalog=new CatalogService(new SqliteCatalogStore(path));
            var term=catalog.CreateSemester(2026,2);var course=catalog.CreateCourse(term.Id,"FICTICIO","Asignatura ficticia","Escuela","");
            var service=catalog.Open(course.Id);await service.ImportStudentsAsync([new("DEMO-001","Estudiante ficticio")]);var student=service.Current.Students[0].Id;
            foreach(var cut in service.Current.Cuts.ToArray())await service.ConfigureGroupsAsync(cut.Number,[new(){Id=$"g{cut.Number}",Name=$"Trabajo {cut.Number}",Weight=100}]);
            await service.SetGradeAsync(1,student,"g1",1);await service.SetGradeAsync(2,student,"g2",4.5m);await service.SetGradeAsync(3,student,"g3",2.5m);
            await service.AddWeekAsync(2,WeekKind.Activities,new DateOnly(2026,10,12));await service.SetAttendanceAsync(2,student,1,AttendanceState.Present);
            await service.DeleteCutAsync(service.PreviewCutDeletion(1),true);
            var reopened=catalog.Open(course.Id);Assert.Equal(new decimal[]{30,40},reopened.Current.Cuts.Select(c=>c.Weight));
            Assert.True(reopened.Current.CutWeightsPending);Assert.Null(GradeCalculator.Final(reopened.Current,student));
            Assert.Equal(4.5m,reopened.Cut(1).Grades.Single().Value);Assert.Equal(2.5m,reopened.Cut(2).Grades.Single().Value);
            Assert.DoesNotContain(reopened.Current.Cuts.SelectMany(c=>c.Grades),g=>g.ActivityId=="g1");
            Assert.Equal(new DateOnly(2026,10,12),reopened.Cut(1).Weeks[0].Date);Assert.Equal(AttendanceState.Present,reopened.Cut(1).Attendance.Single().State);
            Assert.Equal(2,catalog.Courses(term.Id).Single().CutCount);
            catalog.UpdateCourse(catalog.Courses(term.Id).Single(),term.Id,"FICTICIO","Materia editada","Escuela","");
            var archive=Path.Combine(directory,"copia.aulabk");var target=Path.Combine(directory,"restored.db");var password="Contraseña ficticia de prueba larga";
            new EncryptedLocalBackupService(path).Create(archive,password);_=new SqliteCatalogStore(target);
            new EncryptedLocalBackupService(target).Restore(archive,password);
            var restored=new CatalogService(new SqliteCatalogStore(target)).Open(course.Id);Assert.True(restored.Current.CutWeightsPending);Assert.Equal(2,restored.Current.Cuts.Count);
            await restored.ConfigureCutWeightsAsync(new Dictionary<int,decimal>{{1,70},{2,30}});Assert.Equal(3.9m,GradeCalculator.Final(restored.Current,student));
            await restored.DeleteCutAsync(restored.PreviewCutDeletion(2),true);Assert.Single(restored.Current.Cuts);
            Assert.True(restored.Current.CutWeightsPending);await restored.ConfigureCutWeightsAsync(new Dictionary<int,decimal>{{1,100}});
            var final=new CatalogService(new SqliteCatalogStore(target)).Open(course.Id);Assert.Equal(4.5m,GradeCalculator.Final(final.Current,student));
        }
        finally{Directory.Delete(directory,true);}
    }

    [Fact]
    public async Task Another_instance_save_or_sqlite_failure_prevents_the_deletion_without_partial_renumbering()
    {
        var directory=Path.Combine(Path.GetTempPath(),Guid.NewGuid().ToString("N"));Directory.CreateDirectory(directory);
        try
        {
            var path=Path.Combine(directory,"source.db");var catalog=new CatalogService(new SqliteCatalogStore(path));
            var term=catalog.CreateSemester(2026,2);var course=catalog.CreateCourse(term.Id,"FICTICIO","Materia ficticia","Escuela","");
            var first=catalog.Open(course.Id);var second=catalog.Open(course.Id);var preview=second.PreviewCutDeletion(2);var original=second.Current;
            await first.AddCutAsync();await Assert.ThrowsAsync<AcademicValidationException>(()=>second.DeleteCutAsync(preview,true));Assert.Same(original,second.Current);
            var service=catalog.Open(course.Id);var before=JsonSerializer.Serialize(service.Current);
            using(var db=new SqliteConnection($"Data Source={path};Pooling=false"))
            {
                db.Open();using var cmd=db.CreateCommand();cmd.CommandText="CREATE TRIGGER fail_save BEFORE UPDATE ON course_workspaces BEGIN SELECT RAISE(ABORT,'failure fixture'); END";cmd.ExecuteNonQuery();
            }
            await Assert.ThrowsAsync<SqliteException>(()=>service.DeleteCutAsync(service.PreviewCutDeletion(4),true));
            Assert.False(service.IsSaving);Assert.Equal(before,JsonSerializer.Serialize(service.Current));
            Assert.Equal(before,JsonSerializer.Serialize(catalog.Open(course.Id).Current));
        }
        finally{Directory.Delete(directory,true);}
    }
}
