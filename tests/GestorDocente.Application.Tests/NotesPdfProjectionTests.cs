using System.Text.Json;
using GestorDocente.Application;
using GestorDocente.Domain;
using Xunit;

namespace GestorDocente.Application.Tests;
public class NotesPdfProjectionTests
{
    [Fact]
    public void Shared_report_contains_only_names_and_academic_values_and_does_not_write_zeroes()
    {
        var data=DemoWorkspaceFactory.Create();data.Students=[new("ID-PRIVADO","DOC-PRIVADO","Nombre ficticio completo","privado@example.test","3001234567")];
        data.Cuts[0].Groups=[new(){Id="t",Name="Talleres",Weight=20,Mode=EvaluationMode.SubActivities,Activities=[new(){Id="t1",Name="Taller 1"},new(){Id="t2",Name="Taller 2"}]},new(){Id="p",Name="Parcial",Weight=80}];
        data.Cuts[0].Grades=[new("ID-PRIVADO","t1",4),new("ID-PRIVADO","p",3)];
        var before=JsonSerializer.Serialize(data);var report=NotesPdfProjection.Build(data,1);
        Assert.Equal(data.CourseName,report.Course);Assert.Equal(data.Period,report.Period);Assert.Equal(data.GroupName,report.Group);Assert.Equal(1,report.Cut);
        var row=Assert.Single(report.Students);Assert.Equal("Nombre ficticio completo",row.FullName);
        Assert.Equal(new decimal?[]{4,null,2,3},row.Grades);Assert.Equal(2.8m,row.Total);
        Assert.Equal(new[]{"Taller 1","Taller 2","Promedio Talleres","Parcial"},report.Columns.Select(c=>c.Name));
        Assert.Contains("1/2",report.Columns[0].ShareLabel);Assert.Equal(20m,report.Columns[0].Weight);
        var json=JsonSerializer.Serialize(report);
        foreach(var privateValue in new[]{"ID-PRIVADO","DOC-PRIVADO","privado@example.test","3001234567","StudentId","Document","Email","Phone"})Assert.DoesNotContain(privateValue,json);
        Assert.Equal(before,JsonSerializer.Serialize(data));
    }
    [Fact]
    public void Simplified_report_projects_only_names_and_the_same_cut_totals()
    {
        var data=DemoWorkspaceFactory.Create();
        data.Students=[new("s1","DOC-PRIVADO","Estudiante ficticio","privado@example.test","3001234567"),
            new("s2","","Estudiante sin notas")];
        var cut=data.Cuts[0];
        cut.Name="Primer período";
        cut.Groups=[new(){Id="t",Name="Talleres",Weight=40,Mode=EvaluationMode.SubActivities,UseWeightedAverage=true,
            Activities=[new(){Id="t1",Name="Teoría",Weight=40},new(){Id="t2",Name="Práctica",Weight=60}]},
            new(){Id="p",Name="Parcial",Weight=60}];
        cut.Grades=[new("s1","t1",5),new("s1","p",0)];
        var before=JsonSerializer.Serialize(data);

        var detailed=NotesPdfProjection.Build(data,1);
        var simplified=NotesPdfProjection.Build(data,1,NotesPdfFormat.Simplified);

        Assert.Equal(NotesPdfFormat.Detailed,detailed.Format);
        Assert.Equal(NotesPdfFormat.Simplified,simplified.Format);
        Assert.Equal("Primer período",simplified.CutLabel);
        Assert.Empty(simplified.Columns);
        Assert.All(simplified.Students,row=>Assert.Empty(row.Grades));
        Assert.Equal(detailed.Students.Select(row=>(row.FullName,row.Total)),simplified.Students.Select(row=>(row.FullName,row.Total)));
        Assert.Equal(new decimal?[]{0.8m,0m},simplified.Students.Select(row=>row.Total));
        var json=JsonSerializer.Serialize(simplified);
        foreach(var omitted in new[]{"DOC-PRIVADO","privado@example.test","3001234567","Talleres","Teoría","Práctica","Parcial"})
            Assert.DoesNotContain(omitted,json);
        Assert.Equal(before,JsonSerializer.Serialize(data));
        Assert.All(NotesPdfProjection.Build(data,2,NotesPdfFormat.Simplified).Students,
            row=>Assert.Equal(new AcademicCutReadIndex(data.Cuts[1]).Cut(data.Students.Single(s=>s.FullName==row.FullName).Id),row.Total));
        cut.Groups.Clear();
        Assert.All(NotesPdfProjection.Build(data,1,NotesPdfFormat.Simplified).Students,row=>Assert.Null(row.Total));
    }

    [Fact]
    public void Entire_roster_selected_cut_and_only_active_activities_are_exported()
    {
        var service=new WorkspaceService(new Store());service.RemoveActivity(2,"t1");
        var report=NotesPdfProjection.Build(service.Current,2);
        Assert.Equal(10,report.Students.Count);Assert.DoesNotContain(report.Columns,c=>c.Name=="Taller 1");
        Assert.All(report.Students,row=>Assert.Equal(new AcademicCutReadIndex(service.Cut(2)).Cut(service.Current.Students.Single(s=>s.FullName==row.FullName).Id),row.Total));
        Assert.Throws<AcademicValidationException>(()=>NotesPdfProjection.Build(service.Current,99));
    }
    private sealed class Store:IWorkspaceStore{public AcademicWorkspace? Load()=>DemoWorkspaceFactory.Create();public void Save(AcademicWorkspace data){} }
}
