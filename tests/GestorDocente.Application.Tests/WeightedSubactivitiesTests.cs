using System.Text.Json;
using System.Text.Json.Nodes;
using GestorDocente.Application;
using GestorDocente.Domain;
using Xunit;

namespace GestorDocente.Application.Tests;

public class WeightedSubactivitiesTests
{
    [Fact]
    public async Task Configuration_updates_calculations_without_changing_grades_or_other_cuts()
    {
        var store=new Store();var service=new WorkspaceService(store);
        await service.ConfigureGroupsAsync(1,[Group()]);
        await service.SetGradeAsync(1,"s1","a",3);await service.SetGradeAsync(1,"s1","b",5);
        var grades=JsonSerializer.Serialize(service.Cut(1).Grades);var other=JsonSerializer.Serialize(service.Cut(2));
        Assert.Equal(4.2m,new AcademicCutReadIndex(service.Cut(1)).Cut("s1"));
        var config=WorkspaceService.Clone(service.Cut(1).Groups);config[0].UseWeightedAverage=false;
        await service.ConfigureGroupsAsync(1,config);Assert.Equal(4m,GradeCalculator.Cut(service.Cut(1),"s1"));
        config[0].UseWeightedAverage=true;await service.ConfigureGroupsAsync(1,config);
        Assert.Equal(4.2m,GradeCalculator.Cut(service.Cut(1),"s1"));Assert.Equal(grades,JsonSerializer.Serialize(service.Cut(1).Grades));
        Assert.Equal(other,JsonSerializer.Serialize(service.Cut(2)));
    }

    [Fact]
    public async Task Invalid_edits_removal_and_recovery_are_atomic_and_do_not_redistribute_weights()
    {
        var store=new Store();var service=new WorkspaceService(store);await service.ConfigureGroupsAsync(1,[Group()]);
        await service.SetGradeAsync(1,"s1","b",5);var before=JsonSerializer.Serialize(service.Current);var saves=store.Saves;
        var bad=Group();bad.Activities.RemoveAt(1);
        await Assert.ThrowsAsync<AcademicValidationException>(()=>service.ConfigureGroupsAsync(1,[bad]));
        Assert.Throws<AcademicValidationException>(()=>service.RemoveActivity(1,"b"));
        Assert.Equal(saves,store.Saves);Assert.Equal(before,JsonSerializer.Serialize(service.Current));
        bad.Activities[0].Weight=100;await service.ConfigureGroupsAsync(1,[bad]);
        Assert.Single(service.Cut(1).RemovedActivities);var removed=JsonSerializer.Serialize(service.Current);
        await Assert.ThrowsAsync<AcademicValidationException>(()=>service.RestoreActivityAsync(1,"b"));
        Assert.Equal(removed,JsonSerializer.Serialize(service.Current));
        await service.ConfigureGroupsAsync(1,[Group()]);Assert.Empty(service.Cut(1).RemovedActivities);
        Assert.Equal(5m,service.Cut(1).Grades.Single(g=>g.ActivityId=="b").Value);
    }

    [Fact]
    public async Task Conversion_to_direct_uses_weighted_average_without_overwriting_retired_grades()
    {
        var service=new WorkspaceService(new Store());await service.ConfigureGroupsAsync(1,[Group()]);
        await service.SetGradeAsync(1,"s1","a",3);await service.SetGradeAsync(1,"s1","b",5);
        await service.ConfigureGroupsAsync(1,[new(){Id="g",Name="Parcial",Weight=100}]);
        Assert.Equal(4.2m,service.Cut(1).Grades.Single(g=>g.ActivityId=="g").Value);
        Assert.Equal(2,service.Cut(1).RemovedActivities.Count);
        Assert.Equal(new decimal?[]{40,60},service.Cut(1).RemovedActivities.Select(a=>a.Activity.Weight));
    }

    [Fact]
    public void Previous_schema_migrates_to_simple_average_and_preserves_original_snapshot()
    {
        var old=DemoWorkspaceFactory.Create();old.SchemaVersion=6;
        old.Cuts[0].Groups=[Group()];old.Cuts[0].Grades=[new("s1","a",3),new("s1","b",5)];
        var original=JsonSerializer.Serialize(old);var migrated=WorkspaceMigration.Upgrade(old);
        Assert.Equal(7,migrated.SchemaVersion);Assert.False(migrated.Cuts[0].Groups[0].UseWeightedAverage);
        Assert.All(migrated.Cuts[0].Groups[0].Activities,a=>Assert.Null(a.Weight));
        Assert.Equal(4m,GradeCalculator.Cut(migrated.Cuts[0],"s1"));Assert.Equal(original,JsonSerializer.Serialize(old));
        Assert.Equal(JsonSerializer.Serialize(old.Cuts[0].Grades),JsonSerializer.Serialize(migrated.Cuts[0].Grades));
    }

    [Theory]
    [InlineData("mode")] [InlineData("weight")]
    public void Current_schema_cannot_silently_lose_weighted_fields(string missing)
    {
        var data=DemoWorkspaceFactory.Create();data.Cuts[0].Groups=[Group()];
        var json=JsonNode.Parse(JsonSerializer.Serialize(data))!;var group=json["Cuts"]![0]!["Groups"]![0]!;
        if(missing=="mode")group.AsObject().Remove("UseWeightedAverage");
        else group["Activities"]![0]!.AsObject().Remove("Weight");
        Assert.Throws<AcademicValidationException>(()=>WorkspaceIntegrity.Deserialize(json.ToJsonString()));
    }

    [Fact]
    public void Pdf_distinguishes_internal_and_parent_percentages_without_materializing_zeroes()
    {
        var data=DemoWorkspaceFactory.Create();data.Cuts[0].Groups=[Group()];data.Cuts[0].Grades=[new("s1","b",5)];
        var before=JsonSerializer.Serialize(data);var report=NotesPdfProjection.Build(data,1);
        Assert.Contains("40 %",report.Columns[0].ShareLabel);Assert.Contains("60 %",report.Columns[1].ShareLabel);
        Assert.Contains("ponderado",report.Columns[2].ShareLabel);Assert.Equal(100m,report.Columns[0].Weight);
        Assert.Equal(new decimal?[]{null,5,3},report.Students[0].Grades);Assert.Equal(3m,report.Students[0].Total);
        Assert.Equal(before,JsonSerializer.Serialize(data));
    }

    private static EvaluationGroup Group()=>new(){Id="g",Name="Parcial",Weight=100,Mode=EvaluationMode.SubActivities,UseWeightedAverage=true,
        Activities=[new(){Id="a",Name="Teoría",Weight=40},new(){Id="b",Name="Práctica",Weight=60}]};
    private sealed class Store:IWorkspaceStore
    {
        private AcademicWorkspace data=DemoWorkspaceFactory.Create();public int Saves;
        public AcademicWorkspace? Load()=>WorkspaceService.Clone(data);
        public void Save(AcademicWorkspace value){data=WorkspaceService.Clone(value);Saves++;}
    }
}
