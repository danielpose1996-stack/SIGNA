using System.Text.Json;
using GestorDocente.Application;
using GestorDocente.Domain;
using GestorDocente.Infrastructure;
using Microsoft.Data.Sqlite;
using Xunit;

namespace GestorDocente.Infrastructure.Tests;

public class WeightedSubactivitiesTests
{
    [Fact]
    public async Task Weighted_configuration_and_original_grades_survive_reopen_and_encrypted_portable_backup()
    {
        var folder=Path.Combine(Path.GetTempPath(),$"signa-ponderacion-{Guid.NewGuid():N}");Directory.CreateDirectory(folder);
        try
        {
            var path=Path.Combine(folder,"original.db");var catalog=new CatalogService(new SqliteCatalogStore(path));
            var semester=catalog.CreateSemester(2026,2);var course=catalog.CreateCourse(semester.Id,"FIC","Materia ficticia","Escuela ficticia","");
            var service=catalog.Open(course.Id);await service.AddStudentAsync("Estudiante ficticio");var student=service.Current.Students[0].Id;
            await service.ConfigureGroupsAsync(1,[new(){Id="g",Name="Parcial",Weight=100,Mode=EvaluationMode.SubActivities,UseWeightedAverage=true,
                Activities=[new(){Id="a",Name="Teoría",Weight=40},new(){Id="b",Name="Práctica",Weight=60}]}]);
            await service.SetGradeAsync(1,student,"b",5);await service.AddWeekAsync(1,markAllPresent:true);
            var expected=JsonSerializer.Serialize(service.Current);var reopened=catalog.Open(course.Id);
            Assert.Equal(expected,JsonSerializer.Serialize(reopened.Current));Assert.Equal(3m,new AcademicCutReadIndex(reopened.Cut(1)).Cut(student));
            Assert.Single(reopened.Cut(1).Grades);Assert.Equal(0.9m,GradeCalculator.Final(reopened.Current,student));
            var copy=Path.Combine(folder,"ponderado.aulabk");const string password="Contraseña ficticia para prueba portable";
            new EncryptedLocalBackupService(path).Create(copy,password);
            var restoredPath=Path.Combine(folder,"otro-equipo.db");new EncryptedLocalBackupService(restoredPath).Restore(copy,password);
            var restored=new CatalogService(new SqliteCatalogStore(restoredPath)).Open(course.Id);
            Assert.Equal(expected,JsonSerializer.Serialize(restored.Current));Assert.Equal(3m,GradeCalculator.Cut(restored.Cut(1),student));
            Assert.Single(restored.Cut(1).Grades);Assert.Single(restored.Cut(1).Attendance);
            if(Environment.GetEnvironmentVariable("SIGNA_036_PREVIEW") is {} preview)
            {
                Directory.CreateDirectory(preview);
                File.WriteAllBytes(Path.Combine(preview,"notas-ponderadas.pdf"),new NotesPdfExporter().Export(NotesPdfProjection.Build(restored.Current,1)));
            }
        }
        finally{Directory.Delete(folder,true);}
    }

    [Fact]
    public async Task Previous_sqlite_payload_migrates_once_with_identical_grades_and_simple_average()
    {
        var path=Path.Combine(Path.GetTempPath(),$"signa-migracion-pesos-{Guid.NewGuid():N}.db");
        try
        {
            var catalog=new CatalogService(new SqliteCatalogStore(path));var semester=catalog.CreateSemester(2026,1);
            var course=catalog.CreateCourse(semester.Id,"FIC","Materia ficticia","Escuela ficticia","");
            var service=catalog.Open(course.Id);await service.AddStudentAsync("Estudiante ficticio");
            var student=service.Current.Students[0].Id;
            service.ConfigureGroups(1,[new(){Id="g",Name="Trabajos",Weight=100,Mode=EvaluationMode.SubActivities,
                Activities=[new(){Id="a",Name="A"},new(){Id="b",Name="B"}]}]);
            service.SetGrade(1,student,"a",3);service.SetGrade(1,student,"b",5);
            var old=WorkspaceService.Clone(service.Current);old.SchemaVersion=6;
            using(var db=new SqliteConnection($"Data Source={path};Pooling=False"))
            {
                db.Open();using var cmd=db.CreateCommand();cmd.CommandText="UPDATE course_workspaces SET schema_version=6,payload=$payload WHERE course_id=$id";
                cmd.Parameters.AddWithValue("$id",course.Id);cmd.Parameters.AddWithValue("$payload",JsonSerializer.Serialize(old));cmd.ExecuteNonQuery();
            }
            var upgraded=catalog.Open(course.Id);Assert.Equal(7,upgraded.Current.SchemaVersion);
            Assert.False(upgraded.Cut(1).Groups[0].UseWeightedAverage);Assert.Equal(4m,GradeCalculator.Cut(upgraded.Cut(1),student));
            Assert.Equal(JsonSerializer.Serialize(old.Cuts[0].Grades),JsonSerializer.Serialize(upgraded.Cut(1).Grades));
            using var check=new SqliteConnection($"Data Source={path};Pooling=False");check.Open();
            long Revision(){using var cmd=check.CreateCommand();cmd.CommandText="SELECT revision FROM course_workspaces";return (long)cmd.ExecuteScalar()!;}
            var revision=Revision();catalog.Open(course.Id);Assert.Equal(revision,Revision());
        }
        finally{File.Delete(path);}
    }
}
