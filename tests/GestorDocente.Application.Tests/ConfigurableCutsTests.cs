using System.Text.Json;
using GestorDocente.Application;
using GestorDocente.Domain;
using Xunit;

namespace GestorDocente.Application.Tests;

public class ConfigurableCutsTests
{
    [Fact]
    public async Task Failed_add_cut_and_failed_version_two_migration_do_not_change_the_original()
    {
        var data=DemoWorkspaceFactory.Create();var store=new Store(data);var service=new WorkspaceService(store);store.Fail=true;
        await Assert.ThrowsAsync<IOException>(()=>service.AddCutAsync());Assert.Same(data,service.Current);Assert.Equal(3,service.Current.Cuts.Count);Assert.False(service.IsSaving);
        data.SchemaVersion=2;foreach(var cut in data.Cuts)cut.Weight=0;
        var before=JsonSerializer.Serialize(data);
        Assert.Throws<IOException>(()=>new WorkspaceService(store));Assert.Equal(before,JsonSerializer.Serialize(data));
    }
    [Theory]
    [InlineData("missing-weight")]
    [InlineData("invalid-total")]
    [InlineData("duplicate-cut")]
    [InlineData("future-version")]
    public void Invalid_current_schema_is_rejected_before_any_write(string fault)
    {
        var data=DemoWorkspaceFactory.Create();
        if(fault=="missing-weight")
        {
            var json=System.Text.Json.Nodes.JsonNode.Parse(JsonSerializer.Serialize(data))!;
            json["Cuts"]![0]!.AsObject().Remove("Weight");
            Assert.Throws<AcademicValidationException>(()=>WorkspaceIntegrity.Deserialize(json.ToJsonString()));return;
        }
        if(fault=="invalid-total")data.Cuts[0].Weight=20;
        if(fault=="duplicate-cut")data.Cuts[2].Number=2;
        if(fault=="future-version")data.SchemaVersion=AcademicWorkspace.CurrentSchemaVersion+1;
        var store=new Store(data);Assert.Throws<AcademicValidationException>(()=>new WorkspaceService(store));Assert.Equal(0,store.Saves);
    }
    [Fact]
    public async Task Additional_cut_has_independent_evaluation_and_optional_dated_weeks()
    {
        var store=new Store(DemoWorkspaceFactory.Create());var service=new WorkspaceService(store);
        var before=JsonSerializer.Serialize(service.Cut(1));
        var number=await service.AddCutAsync();
        Assert.Equal(4,number);Assert.Equal(0,service.Cut(4).Weight);
        await service.ConfigureGroupsAsync(4,[new(){Id="g",Name="Trabajos",Weight=100,Mode=EvaluationMode.SubActivities,Activities=[new(){Id="a",Name="Trabajo 1"},new(){Id="b",Name="Trabajo 2"}]}]);
        await service.SetGradeAsync(4,"s1","a",4);
        await service.AddWeekAsync(4,WeekKind.Activities,new DateOnly(2026,10,12));
        await service.SetAttendanceAsync(4,"s1",1,AttendanceState.Late);
        var reopened=new WorkspaceService(store);
        Assert.Equal(2,GradeCalculator.Cut(reopened.Cut(4),"s1"));
        Assert.Equal(new DateOnly(2026,10,12),reopened.Cut(4).Weeks[0].Date);
        Assert.Equal(AttendanceState.Late,reopened.Cut(4).Attendance[0].State);
        Assert.Equal(before,JsonSerializer.Serialize(reopened.Cut(1)));
    }

    [Fact]
    public async Task Cut_weights_validate_total_and_failed_save_keeps_previous_snapshot()
    {
        var store=new Store(DemoWorkspaceFactory.Create());var service=new WorkspaceService(store);
        var before=service.Current;
        await Assert.ThrowsAsync<AcademicValidationException>(()=>service.ConfigureCutWeightsAsync(new Dictionary<int,decimal>{{1,30},{2,30},{3,30}}));
        Assert.Same(before,service.Current);
        await Assert.ThrowsAsync<AcademicValidationException>(()=>service.ConfigureCutWeightsAsync(new Dictionary<int,decimal>{{1,-1},{2,61},{3,40}}));
        await Assert.ThrowsAsync<AcademicValidationException>(()=>service.ConfigureCutWeightsAsync(new Dictionary<int,decimal>{{1,60},{3,40}}));
        store.Fail=true;
        await Assert.ThrowsAsync<IOException>(()=>service.ConfigureCutWeightsAsync(new Dictionary<int,decimal>{{1,20},{2,30},{3,50}}));
        Assert.Same(before,service.Current);Assert.False(service.IsSaving);
        store.Fail=false;await service.ConfigureCutWeightsAsync(new Dictionary<int,decimal>{{1,20},{2,30},{3,50}});
        Assert.Equal(new decimal[]{20,30,50},new WorkspaceService(store).Current.Cuts.Select(c=>c.Weight));
    }

    [Fact]
    public void Version_two_migrates_once_preserving_grades_and_original_object()
    {
        var old=DemoWorkspaceFactory.Create();old.SchemaVersion=2;foreach(var cut in old.Cuts)cut.Weight=0;
        var original=JsonSerializer.Serialize(old);var store=new Store(old);
        var service=new WorkspaceService(store);
        Assert.Equal(AcademicWorkspace.CurrentSchemaVersion,service.Current.SchemaVersion);Assert.Equal(new decimal[]{30,30,40},service.Current.Cuts.Select(c=>c.Weight));
        Assert.Equal(old.Cuts[0].Grades,service.Cut(1).Grades);Assert.Equal(original,JsonSerializer.Serialize(old));
        _=new WorkspaceService(store);Assert.Equal(1,store.Saves);
    }

    [Fact]
    public void Consolidated_uses_zero_for_missing_grades_without_materializing_them()
    {
        var data=DemoWorkspaceFactory.Create();
        foreach(var cut in data.Cuts){cut.Groups=[new(){Id="p",Name="Parcial",Weight=100}];cut.Grades.Clear();}
        data.Cuts[0].Grades.Add(new("s1","p",5));data.Cuts[2].Grades.Add(new("s1","p",4));
        var before=JsonSerializer.Serialize(data);
        Assert.Equal(3.1m,GradeCalculator.Final(data,"s1"));
        Assert.Equal(0,GradeCalculator.Final(data,"s2"));
        Assert.Equal(before,JsonSerializer.Serialize(data));
        data.Cuts.Add(new(){Number=4,Weight=0});Assert.Equal(3.1m,GradeCalculator.Final(data,"s1"));
    }

    private sealed class Store(AcademicWorkspace data):IWorkspaceStore
    {
        public int Saves;public bool Fail;private AcademicWorkspace snapshot=data;
        public AcademicWorkspace? Load()=>snapshot;
        public void Save(AcademicWorkspace workspace){if(Fail)throw new IOException("Prueba de escritura fallida");snapshot=WorkspaceService.Clone(workspace);Saves++;}
    }
}
