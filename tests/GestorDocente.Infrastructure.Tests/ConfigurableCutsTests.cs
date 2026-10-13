using System.Text.Json.Nodes;
using GestorDocente.Application;
using GestorDocente.Domain;
using GestorDocente.Infrastructure;
using Microsoft.Data.Sqlite;
using Xunit;

namespace GestorDocente.Infrastructure.Tests;

public class ConfigurableCutsTests
{
    [Fact]
    public async Task Additional_cuts_weights_dates_and_grades_survive_sqlite_backup_edit_and_reopening()
    {
        var directory=Path.Combine(Path.GetTempPath(),Guid.NewGuid().ToString("N"));Directory.CreateDirectory(directory);
        try
        {
            var path=Path.Combine(directory,"source.db");var catalog=new CatalogService(new SqliteCatalogStore(path));
            var semester=catalog.CreateSemester(2026,2);var course=catalog.CreateCourse(semester.Id,"FIC-1","Materia ficticia","Escuela ficticia","");
            var service=catalog.Open(course.Id);Assert.Equal(new decimal[]{30,30,40},service.Current.Cuts.Select(c=>c.Weight));
            await service.ImportStudentsAsync([new("FICTICIO-001","Estudiante ficticio")]);var id=service.Current.Students[0].Id;
            await service.AddCutAsync();await service.AddCutAsync();
            await service.ConfigureCutWeightsAsync(new Dictionary<int,decimal>{{1,20},{2,20},{3,20},{4,20},{5,20}});
            await service.ConfigureGroupsAsync(5,[new(){Id="p",Name="Proyecto",Weight=100}]);await service.SetGradeAsync(5,id,"p",4.5m);
            await service.AddWeekAsync(5,WeekKind.Activities,new DateOnly(2026,10,12));await service.AddWeekAsync(5);
            await service.SetAttendanceAsync(5,id,1,AttendanceState.Late);
            var reopened=catalog.Open(course.Id);Assert.Equal(5,reopened.Current.Cuts.Count);Assert.Equal(0.9m,GradeCalculator.Final(reopened.Current,id));
            Assert.Null(reopened.Cut(5).Weeks[1].Date);Assert.Equal(new DateOnly(2026,10,12),reopened.Cut(5).Weeks[0].Date);
            Assert.Equal(5,catalog.Courses(semester.Id).Single().CutCount);
            var pdf=new AttendancePdfExporter().Export(reopened.Current,5,id);Assert.True(pdf.Length>1000);
            catalog.UpdateCourse(catalog.Courses(semester.Id).Single(),semester.Id,"FIC-1","Materia editada","Escuela ficticia","");
            var archive=Path.Combine(directory,"respaldo.aulabak");var target=Path.Combine(directory,"target.db");var password="contraseña ficticia de prueba larga";
            new EncryptedLocalBackupService(path).Create(archive,password);
            _=new SqliteCatalogStore(target);new EncryptedLocalBackupService(target).Restore(archive,password);
            var restored=new CatalogService(new SqliteCatalogStore(target)).Open(course.Id);
            Assert.Equal(5,restored.Current.Cuts.Count);Assert.Equal(4.5m,restored.Cut(5).Grades.Single().Value);Assert.Equal(20,restored.Cut(5).Weight);
            var preview=catalog.InspectSemesterDeletion(semester.Id);Assert.Equal(1,preview.GradeCount);Assert.Equal(1,preview.AttendanceCount);
        }
        finally{Directory.Delete(directory,true);}
    }

    [Fact]
    public void Version_two_payload_migrates_once_and_invalid_legacy_does_not_write()
    {
        var directory=Path.Combine(Path.GetTempPath(),Guid.NewGuid().ToString("N"));Directory.CreateDirectory(directory);
        try
        {
            var path=Path.Combine(directory,"db.sqlite");var catalog=new CatalogService(new SqliteCatalogStore(path));
            var semester=catalog.CreateSemester(2026,2);var course=catalog.CreateCourse(semester.Id,"X","Materia ficticia","Escuela","");
            var json=JsonNode.Parse(System.Text.Json.JsonSerializer.Serialize(catalog.Open(course.Id).Current))!;json["SchemaVersion"]=2;
            foreach(var cut in json["Cuts"]!.AsArray())cut!.AsObject().Remove("Weight");
            using(var db=new SqliteConnection($"Data Source={path};Pooling=false"))
            {
                db.Open();using var cmd=db.CreateCommand();cmd.CommandText="UPDATE course_workspaces SET schema_version=2,payload=$p";cmd.Parameters.AddWithValue("$p",json.ToJsonString());cmd.ExecuteNonQuery();
            }
            Assert.Equal(AcademicWorkspace.CurrentSchemaVersion,catalog.Open(course.Id).Current.SchemaVersion);_ = catalog.Open(course.Id);
            using var verify=new SqliteConnection($"Data Source={path};Pooling=false");verify.Open();using var read=verify.CreateCommand();read.CommandText="SELECT revision FROM course_workspaces";Assert.Equal(1L,read.ExecuteScalar());
            json["Cuts"]!.AsArray().RemoveAt(2);
            using var corrupt=verify.CreateCommand();corrupt.CommandText="UPDATE course_workspaces SET schema_version=2,payload=$p";corrupt.Parameters.AddWithValue("$p",json.ToJsonString());corrupt.ExecuteNonQuery();
            Assert.Throws<AcademicValidationException>(()=>catalog.Open(course.Id));Assert.Equal(1L,read.ExecuteScalar());
        }
        finally{Directory.Delete(directory,true);}
    }
}
