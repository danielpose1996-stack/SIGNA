using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using GestorDocente.Application;
using GestorDocente.Domain;
using GestorDocente.Infrastructure;
using Microsoft.Data.Sqlite;
using Xunit;
namespace GestorDocente.Infrastructure.Tests;

public class ConsolidatedReportTests
{
    [Fact]
    public void Pdf_is_paginated_and_its_contract_excludes_personal_identifiers_and_contacts()
    {
        var data=DemoWorkspaceFactory.Create();data.CourseName="Asignatura ficticia de análisis de datos";
        data.Students=Enumerable.Range(1,65).Select(i=>new Student($"id-{i}",$"DOC-PRIVADO-{i}",$"Estudiante ficticio {i:000} con apellidos extensos de demostración",$"correo-privado-{i}@example.invalid","CELULAR-PRIVADO")).ToList();
        data.Cuts=Enumerable.Range(1,7).Select(i=>new AcademicCut{Number=i,Name=i==1 ? "Primer período de investigación académica" : $"Período {i}",Weight=i==7 ? 40 : 10,
            Groups=[new(){Id="p",Name="Parcial",Weight=100}],Grades=i<3 ? data.Students.Select((s,n)=>new GradeEntry(s.Id,"p",n%6)).ToList() : []}).ToList();
        var report=ConsolidatedPdfProjection.Build(data);var json=JsonSerializer.Serialize(report);
        foreach(var secret in new[]{"DOC-PRIVADO","correo-privado","CELULAR-PRIVADO","id-1"})Assert.DoesNotContain(secret,json);
        Assert.DoesNotContain(typeof(ConsolidatedPdfStudent).GetProperties(),p=>p.Name is "Document" or "Email" or "Phone" or "StudentId");
        var exporter=new ConsolidatedPdfExporter();var bytes=exporter.Export(report);
        Assert.StartsWith("%PDF-",Encoding.ASCII.GetString(bytes,0,5));Assert.True(Regex.Matches(Encoding.Latin1.GetString(bytes),@"/Type /Page\b").Count>=9);
        if(Environment.GetEnvironmentVariable("SIGNA_048_PREVIEW") is {} directory)
        {
            Directory.CreateDirectory(directory);File.WriteAllBytes(Path.Combine(directory,"consolidado-multipagina.pdf"),bytes);
            File.WriteAllBytes(Path.Combine(directory,"consolidado-vacio.pdf"),exporter.Export(report with{Students=[]}));
            var example=DemoWorkspaceFactory.Create();example.CourseName="Asignatura de demostración";
            example.Students=Enumerable.Range(1,6).Select(i=>new Student($"s{i}","",$"Estudiante ficticio {i}")).ToList();
            foreach(var cut in example.Cuts){cut.Groups=[new(){Id="p",Name="Evaluación",Weight=100}];cut.Grades.Clear();}
            example.Cuts[0].Name="Primer período";example.Cuts[1].Name="Segundo período";example.Cuts[2].Name="Cierre académico";
            example.Cuts[0].Grades=[new("s1","p",0),new("s2","p",2),new("s3","p",3),new("s4","p",5),new("s5","p",0)];
            example.Cuts[1].Grades=[new("s4","p",5),new("s5","p",0)];
            File.WriteAllBytes(Path.Combine(directory,"consolidado-ejemplo.pdf"),exporter.Export(ConsolidatedPdfProjection.Build(example)));
        }
    }
    [Fact]
    public async Task Named_weighted_cuts_survive_sqlite_reopen_and_portable_local_backup()
    {
        var directory=Path.Combine(Path.GetTempPath(),$"signa-cortes-{Guid.NewGuid():N}");Directory.CreateDirectory(directory);
        try
        {
            var path=Path.Combine(directory,"operativa.db");var catalog=new CatalogService(new SqliteCatalogStore(path));
            var semester=catalog.CreateSemester(2026,2);var course=catalog.CreateCourse(semester.Id,"FIC","Materia ficticia","Escuela ficticia","");
            var service=catalog.Open(course.Id);await service.AddStudentAsync("Estudiante ficticio");var id=service.Current.Students[0].Id;
            await service.ConfigureGroupsAsync(1,[new(){Id="g",Name="Parcial",Weight=100,Mode=EvaluationMode.SubActivities,UseWeightedAverage=true,Activities=[new(){Id="a",Name="Teoría",Weight=40},new(){Id="b",Name="Práctica",Weight=60}]}]);
            await service.SetGradeAsync(1,id,"a",0);await service.SetGradeAsync(1,id,"b",5);await service.AddWeekAsync(1,markAllPresent:true);
            // Simulate an installed v7 payload (before cut names) in a disposable database.
            var legacy=WorkspaceService.Clone(service.Current);legacy.SchemaVersion=7;
            var legacyJson=System.Text.Json.Nodes.JsonNode.Parse(JsonSerializer.Serialize(legacy))!;
            foreach(var cut in legacyJson["Cuts"]!.AsArray())cut!.AsObject().Remove("Name");
            using(var db=new SqliteConnection(new SqliteConnectionStringBuilder{DataSource=path,Pooling=false}.ToString()))
            {
                db.Open();using var update=db.CreateCommand();update.CommandText="UPDATE course_workspaces SET schema_version=7,payload=$payload WHERE course_id=$course";
                update.Parameters.AddWithValue("$payload",legacyJson.ToJsonString());update.Parameters.AddWithValue("$course",course.Id);Assert.Equal(1,update.ExecuteNonQuery());
            }
            service=catalog.Open(course.Id);Assert.True(service.Cut(1).Groups[0].UseWeightedAverage);
            Assert.Equal(new decimal?[]{40,60},service.Cut(1).Groups[0].Activities.Select(a=>a.Weight));
            await service.RenameCutsAsync(service.Current,new Dictionary<int,string>{{1,"Primer período"},{2,"Segundo período"},{3,"Final"}});
            var expected=JsonSerializer.Serialize(service.Current);Assert.Equal(expected,JsonSerializer.Serialize(catalog.Open(course.Id).Current));
            var archive=Path.Combine(directory,"portable.aulabk");new LocalBackupService(path,()=>false).Create(archive);
            var restored=Path.Combine(directory,"otro-equipo.db");new LocalBackupService(restored,()=>false).Restore(archive);
            var recovered=new CatalogService(new SqliteCatalogStore(restored)).Open(course.Id);
            Assert.Equal(expected,JsonSerializer.Serialize(recovered.Current));Assert.Equal("Primer período",recovered.Cut(1).Label);
            Assert.Equal(3m,new ConsolidatedReadIndex(recovered.Current).For(id).Cuts[1].Value);
            using var connection=new SqliteConnection(new SqliteConnectionStringBuilder{DataSource=restored,Pooling=false}.ToString());connection.Open();using var command=connection.CreateCommand();
            command.CommandText="PRAGMA integrity_check";Assert.Equal("ok",command.ExecuteScalar());command.CommandText="PRAGMA foreign_key_check";Assert.Null(command.ExecuteScalar());
        }
        finally{Directory.Delete(directory,true);}
    }
}
