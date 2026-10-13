using System.Text.Json;
using GestorDocente.Application;
using GestorDocente.Domain;
using Xunit;

namespace GestorDocente.Application.Tests;

public class CutDeletionTests
{
    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public async Task Deleting_any_default_cut_renumbers_preserves_other_data_and_requires_weight_adjustment(int number)
    {
        var store=new Store(DemoWorkspaceFactory.Create());var service=new WorkspaceService(store);
        var expected=service.Current.Cuts.Where(c=>c.Number!=number).Select(WorkspaceService.Clone).ToArray();
        var preview=service.PreviewCutDeletion(number);
        Assert.Equal(number,preview.Number);Assert.True(preview.GradeCount>0);Assert.True(preview.AttendanceCount>0);
        await service.DeleteCutAsync(preview,true);
        Assert.Equal(new[]{1,2},service.Current.Cuts.Select(c=>c.Number));Assert.True(service.Current.CutWeightsPending);
        for(var i=0;i<2;i++){expected[i].Number=i+1;Assert.Equal(JsonSerializer.Serialize(expected[i]),JsonSerializer.Serialize(service.Current.Cuts[i]));}
        Assert.Null(GradeCalculator.Final(service.Current,"s1"));
        var reopened=new WorkspaceService(store);Assert.True(reopened.Current.CutWeightsPending);
        await reopened.SetGradeAsync(1,"s1","t1",4);await reopened.AddWeekAsync(1,WeekKind.Activities,new DateOnly(2026,10,12));
        await reopened.ConfigureCutWeightsAsync(new Dictionary<int,decimal>{{1,50},{2,50}});
        Assert.False(reopened.Current.CutWeightsPending);Assert.NotNull(GradeCalculator.Final(reopened.Current,"s1"));
    }

    [Fact]
    public async Task Confirmation_and_last_cut_are_required_and_single_cut_can_be_reweighted()
    {
        var store=new Store(DemoWorkspaceFactory.Create());var service=new WorkspaceService(store);
        var preview=service.PreviewCutDeletion(1);var before=JsonSerializer.Serialize(service.Current);
        await Assert.ThrowsAsync<AcademicValidationException>(()=>service.DeleteCutAsync(preview));
        Assert.Equal(before,JsonSerializer.Serialize(service.Current));Assert.Equal(0,store.Saves);
        await service.DeleteCutAsync(preview,true);await service.DeleteCutAsync(service.PreviewCutDeletion(1),true);
        Assert.Single(service.Current.Cuts);Assert.Equal(40,service.Cut(1).Weight);
        await Assert.ThrowsAsync<AcademicValidationException>(()=>service.DeleteCutAsync(service.PreviewCutDeletion(1),true));
        await service.ConfigureCutWeightsAsync(new Dictionary<int,decimal>{{1,100}});
        Assert.False(service.Current.CutWeightsPending);Assert.NotNull(GradeCalculator.Final(service.Current,"s1"));
        await service.AddCutAsync();Assert.Equal(2,service.Current.Cuts.Count);
        var final=GradeCalculator.Final(service.Current,"s1");
        await service.DeleteCutAsync(service.PreviewCutDeletion(2),true);
        Assert.False(service.Current.CutWeightsPending);Assert.Equal(final,GradeCalculator.Final(service.Current,"s1"));
    }

    [Fact]
    public async Task Stale_preview_foreign_preview_and_failed_write_never_delete_data()
    {
        var store=new Store(DemoWorkspaceFactory.Create());var service=new WorkspaceService(store);
        var stale=service.PreviewCutDeletion(1);await service.SetGradeAsync(2,"s1","t1",1);
        await Assert.ThrowsAsync<AcademicValidationException>(()=>service.DeleteCutAsync(stale,true));
        var other=new WorkspaceService(new Store(DemoWorkspaceFactory.Create()));
        await Assert.ThrowsAsync<AcademicValidationException>(()=>service.DeleteCutAsync(other.PreviewCutDeletion(1),true));
        var before=service.Current;store.Fail=true;
        await Assert.ThrowsAsync<IOException>(()=>service.DeleteCutAsync(service.PreviewCutDeletion(1),true));
        Assert.Same(before,service.Current);Assert.False(service.IsSaving);Assert.Equal(3,service.Current.Cuts.Count);
    }

    [Fact]
    public void Version_three_migration_preserves_custom_weights_and_all_four_cuts()
    {
        var data=DemoWorkspaceFactory.Create();data.SchemaVersion=3;
        data.Cuts[0].Weight=10;data.Cuts[1].Weight=20;data.Cuts[2].Weight=30;data.Cuts.Add(new(){Number=4,Weight=40});
        var before=JsonSerializer.Serialize(data);var store=new Store(data);var service=new WorkspaceService(store);
        Assert.Equal(AcademicWorkspace.CurrentSchemaVersion,service.Current.SchemaVersion);Assert.Equal(new decimal[]{10,20,30,40},service.Current.Cuts.Select(c=>c.Weight));
        Assert.False(service.Current.CutWeightsPending);Assert.Equal(before,JsonSerializer.Serialize(data));
        _=new WorkspaceService(store);Assert.Equal(1,store.Saves);
    }

    private sealed class Store(AcademicWorkspace data):IWorkspaceStore
    {
        private AcademicWorkspace snapshot=data;public int Saves;public bool Fail;
        public AcademicWorkspace? Load()=>snapshot;
        public void Save(AcademicWorkspace value){if(Fail)throw new IOException("Fallo de prueba");snapshot=WorkspaceService.Clone(value);Saves++;}
    }
}
