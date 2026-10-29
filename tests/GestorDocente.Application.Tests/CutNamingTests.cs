using System.Text.Json;
using GestorDocente.Application;
using GestorDocente.Domain;
using Xunit;
namespace GestorDocente.Application.Tests;

public class CutNamingTests
{
    [Fact]
    public async Task Rename_is_atomic_snapshot_bound_and_preserves_data()
    {
        var store=new Store(DemoWorkspaceFactory.Create());var service=new WorkspaceService(store);var original=service.Current;
        await service.RenameCutsAsync(original,new Dictionary<int,string>{{1,"Primer período"},{2,"Segundo período"},{3,"Final"}});
        Assert.Equal("Primer período",service.Cut(1).Label);Assert.Equal(original.Cuts[0].Grades,service.Cut(1).Grades);
        await Assert.ThrowsAsync<AcademicValidationException>(()=>service.RenameCutsAsync(original,new Dictionary<int,string>{{1,"Obsoleto"}}));
        var before=service.Current;store.Fail=true;
        await Assert.ThrowsAsync<IOException>(()=>service.RenameCutsAsync(before,service.Current.Cuts.ToDictionary(c=>c.Number,_=>"Nuevo")));
        Assert.Same(before,service.Current);
    }
    [Theory]
    [InlineData("")] [InlineData("   ")] [InlineData("Nombre\ninválido")]
    public async Task Invalid_name_cannot_be_saved(string name)
    {
        var service=new WorkspaceService(new Store(DemoWorkspaceFactory.Create()));
        await Assert.ThrowsAsync<AcademicValidationException>(()=>service.RenameCutsAsync(service.Current,service.Current.Cuts.ToDictionary(c=>c.Number,_=>name)));
    }
    [Fact]
    public async Task Untouched_default_names_follow_renumbering_and_custom_names_follow_their_data()
    {
        var service=new WorkspaceService(new Store(DemoWorkspaceFactory.Create()));
        await service.RenameCutsAsync(service.Current,new Dictionary<int,string>{{1,"Primer período"},{2,"Corte 2"},{3,"Corte 3"}});
        Assert.Empty(service.Cut(2).Name);Assert.Empty(service.Cut(3).Name);
        var grades=service.Cut(2).Grades.ToArray();await service.DeleteCutAsync(service.PreviewCutDeletion(1),true);
        Assert.Equal("Corte 1",service.Cut(1).Label);Assert.Equal(grades,service.Cut(1).Grades);
    }
    [Fact]
    public void Version_seven_migration_keeps_weighted_configuration_and_records()
    {
        var old=DemoWorkspaceFactory.Create();old.SchemaVersion=7;
        var group=old.Cuts[0].Groups.First(g=>g.Mode==EvaluationMode.SubActivities);
        group.UseWeightedAverage=true;foreach(var activity in group.Activities)activity.Weight=100m/group.Activities.Count;
        group.Activities[^1].Weight=100-group.Activities.Take(group.Activities.Count-1).Sum(a=>a.Weight!.Value);
        var serialized=JsonSerializer.Serialize(old);var next=WorkspaceMigration.Upgrade(old);
        Assert.Equal(AcademicWorkspace.CurrentSchemaVersion,next.SchemaVersion);Assert.True(next.Cuts[0].Groups.Single(g=>g.Id==group.Id).UseWeightedAverage);
        Assert.Equal(group.Activities.Select(a=>a.Weight),next.Cuts[0].Groups.Single(g=>g.Id==group.Id).Activities.Select(a=>a.Weight));
        Assert.Equal(old.Cuts[0].Grades,next.Cuts[0].Grades);Assert.Equal(old.Cuts[0].Attendance,next.Cuts[0].Attendance);
        Assert.Equal(serialized,JsonSerializer.Serialize(old));Assert.Equal("Corte 1",next.Cuts[0].Label);
        next.Cuts[0].Name="Primer período";
        Assert.Equal("Primer período",CourseParameters.Capture(next).CreateCuts()[0].Label);
    }
    private sealed class Store(AcademicWorkspace data):IWorkspaceStore
    {
        public bool Fail;public AcademicWorkspace? Load()=>data;
        public void Save(AcademicWorkspace value){if(Fail)throw new IOException("Fallo ficticio");data=WorkspaceService.Clone(value);}
    }
}
