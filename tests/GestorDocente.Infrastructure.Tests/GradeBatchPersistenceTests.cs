using System.Text.Json;
using System.Text.Json.Nodes;
using GestorDocente.Application;
using GestorDocente.Domain;
using GestorDocente.Infrastructure;
using Microsoft.Data.Sqlite;
using Xunit;
namespace GestorDocente.Infrastructure.Tests;

public class GradeBatchPersistenceTests
{
    [Fact]
    public async Task Batch_and_attendance_policy_survive_reopen_and_encrypted_restore()
    {
        var folder=Path.Combine(Path.GetTempPath(),"signa-034-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(folder);
        try
        {
            var path=Path.Combine(folder,"original.db");var catalog=new CatalogService(new SqliteCatalogStore(path));
            var semester=catalog.CreateSemester(2026,2);var course=catalog.CreateCourse(semester.Id,"TEST","Materia ficticia","Escuela ficticia","A");
            var service=catalog.Open(course.Id);await service.AddStudentAsync("Ana Ficticia");await service.AddStudentAsync("Beto Ficticio");
            var a=service.Current.Students[0].Id;var b=service.Current.Students[1].Id;
            await service.ConfigureGroupsAsync(1,[new(){Id="direct",Name="Trabajo",Weight=50},new(){Id="group",Name="Talleres",Weight=50,Mode=EvaluationMode.SubActivities,Activities=[new(){Id="sub",Name="Taller 1"}]}]);
            await service.PasteGradesAsync(GradePastePreview.Build(service.Current,1,"direct",[b,a],"3.5\n5"),true);
            await service.PasteGradesAsync(GradePastePreview.Build(service.Current,1,"sub",[a,b],"4,5\n0"),true);
            await service.AddWeekAsync(1);await service.AddWeekAsync(1);await service.SetAttendanceAsync(1,a,1,AttendanceState.Present);await service.SetAttendanceAsync(1,a,2,AttendanceState.Justified);
            await service.ConfigureAttendanceGradingAsync(1,new(true,4,0,2,1));await service.ConfigureAttendanceGradingAsync(2,new(false,5,0,3,2));
            var expected=JsonSerializer.Serialize(service.Current);var snapshot=catalog.Open(course.Id);Assert.Equal(expected,JsonSerializer.Serialize(snapshot.Current));
            Assert.Equal(3m,new AcademicCutReadIndex(snapshot.Cut(1)).AttendanceGrade(a));Assert.Null(new AcademicCutReadIndex(snapshot.Cut(1)).AttendanceGrade(b));
            var index=new AcademicCutReadIndex(snapshot.Cut(1));Assert.Equal(4.75m,index.Cut(a));Assert.Equal(1.75m,index.Cut(b));
            var archive=Path.Combine(folder,"respaldo.aulabk");const string password="Contraseña ficticia temporal";
            new EncryptedLocalBackupService(path).Create(archive,password);
            var restored=Path.Combine(folder,"otro-equipo.db");new EncryptedLocalBackupService(restored).Restore(archive,password);
            Assert.Equal(expected,JsonSerializer.Serialize(new CatalogService(new SqliteCatalogStore(restored)).Open(course.Id).Current));
            using var db=new SqliteConnection($"Data Source={restored};Pooling=False");db.Open();using var command=db.CreateCommand();
            command.CommandText="PRAGMA integrity_check";Assert.Equal("ok",command.ExecuteScalar());command.CommandText="PRAGMA foreign_key_check";Assert.Null(command.ExecuteScalar());
        }
        finally{Directory.Delete(folder,true);}
    }
    [Fact]
    public async Task Concurrent_writer_rejects_entire_batch_and_invalid_configuration_is_detected()
    {
        var path=Path.Combine(Path.GetTempPath(),"signa-034-"+Guid.NewGuid().ToString("N")+".db");
        try
        {
            var catalog=new CatalogService(new SqliteCatalogStore(path));var semester=catalog.CreateSemester(2026,1);var course=catalog.CreateCourse(semester.Id,"TEST","Materia ficticia","Escuela","B");
            var seed=catalog.Open(course.Id);await seed.AddStudentAsync("Ana Ficticia");await seed.ConfigureGroupsAsync(1,[new(){Id="g",Name="Parcial",Weight=100}]);var student=seed.Current.Students[0].Id;
            var first=catalog.Open(course.Id);var second=catalog.Open(course.Id);var before=second.Current;
            var preview=GradePastePreview.Build(before,1,"g",[student],"5");await first.SetGradeAsync(1,student,"g",3);
            await Assert.ThrowsAsync<AcademicValidationException>(()=>second.PasteGradesAsync(preview,true));Assert.Same(before,second.Current);Assert.False(second.IsSaving);
            Assert.Equal(3m,new AcademicCutReadIndex(catalog.Open(course.Id).Cut(1)).Grade(student,"g"));
            using var db=new SqliteConnection($"Data Source={path};Pooling=False");db.Open();using var command=db.CreateCommand();
            command.CommandText="SELECT payload FROM course_workspaces";var json=JsonNode.Parse((string)command.ExecuteScalar()!)!;
            json["Cuts"]![0]!["AttendanceGrading"]=new JsonObject{["Enabled"]=true,["Present"]=5,["Absent"]=0,["Justified"]=4}; // Falta T; nunca asumir 0.
            command.CommandText="UPDATE course_workspaces SET payload=$payload";command.Parameters.AddWithValue("$payload",json.ToJsonString());command.ExecuteNonQuery();
            Assert.Throws<AcademicValidationException>(()=>catalog.Open(course.Id));
        }
        finally{if(File.Exists(path))File.Delete(path);}
    }
}
