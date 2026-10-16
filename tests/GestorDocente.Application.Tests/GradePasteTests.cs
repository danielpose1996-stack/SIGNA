using System.Text.Json;
using GestorDocente.Application;
using GestorDocente.Domain;
using Xunit;
namespace GestorDocente.Application.Tests;

public class GradePasteTests
{
    [Fact]
    public async Task Preview_and_save_follow_exact_supplied_order_with_one_atomic_write()
    {
        var store=new Store();var service=new WorkspaceService(store);var ids=new[]{"s3","s1","s2"};
        var preview=GradePastePreview.Build(service.Current,1,"t1",ids,"4,25\r\n0\r\n5.0\r\n");
        Assert.True(preview.CanSave);Assert.Equal(ids,preview.Rows.Select(r=>r.StudentId));Assert.Equal(service.Current.Students[2].FullName,preview.Rows[0].FullName);
        var other=service.Cut(1).Grades.Where(g=>g.ActivityId!="t1" || !ids.Contains(g.StudentId)).ToArray();
        var otherCuts=JsonSerializer.Serialize(service.Current.Cuts.Skip(1));
        await service.PasteGradesAsync(preview,true);
        Assert.Equal(1,store.Saves);Assert.Equal(4.25m,new AcademicCutReadIndex(service.Cut(1)).Grade("s3","t1"));
        Assert.Equal(0m,new AcademicCutReadIndex(service.Cut(1)).Grade("s1","t1"));
        Assert.Equal(5m,new AcademicCutReadIndex(service.Cut(1)).Grade("s2","t1"));
        Assert.Equal(other,service.Cut(1).Grades.Where(g=>g.ActivityId!="t1" || !ids.Contains(g.StudentId)));
        Assert.Equal(otherCuts,JsonSerializer.Serialize(service.Current.Cuts.Skip(1)));Assert.False(service.IsSaving);
    }
    [Theory]
    [InlineData("4\n")] [InlineData("4\n5\n3")] [InlineData("4\n\n5")] [InlineData("4\t5\n3")]
    [InlineData("Nota\n4")] [InlineData("-0,1\n4")] [InlineData("5.1\n4")] [InlineData("1,234.5\n4")]
    [InlineData("\n4")] [InlineData("4\n5\n\n")] [InlineData("NaN\n4")]
    [InlineData("4\t\n5")] [InlineData("\t4\n5")]
    public async Task Invalid_lists_never_write_or_shift_assignments(string text)
    {
        var store=new Store();var service=new WorkspaceService(store);var snapshot=service.Current;
        var preview=GradePastePreview.Build(snapshot,1,"t1",["s1","s2"],text);Assert.False(preview.CanSave);
        await Assert.ThrowsAsync<AcademicValidationException>(()=>service.PasteGradesAsync(preview,true));Assert.Same(snapshot,service.Current);Assert.Equal(0,store.Saves);
    }
    [Fact]
    public async Task Confirmation_staleness_and_storage_failure_preserve_data()
    {
        var store=new Store();var service=new WorkspaceService(store);
        var preview=GradePastePreview.Build(service.Current,1,"t1",["s1"],"3,75");
        await Assert.ThrowsAsync<AcademicValidationException>(()=>service.PasteGradesAsync(preview));Assert.Equal(0,store.Saves);
        await service.SetGradeAsync(1,"s2","t1",2);
        await Assert.ThrowsAsync<AcademicValidationException>(()=>service.PasteGradesAsync(preview,true));Assert.Equal(1,store.Saves);
        preview=GradePastePreview.Build(service.Current,1,"t1",["s1"],"3,75");var before=service.Current;store.Fail=true;
        await Assert.ThrowsAsync<IOException>(()=>service.PasteGradesAsync(preview,true));Assert.Same(before,service.Current);Assert.False(service.IsSaving);
        store.Fail=false;await service.PasteGradesAsync(preview,true);Assert.Equal(3.75m,new AcademicCutReadIndex(service.Cut(1)).Grade("s1","t1"));
    }
    [Fact]
    public void Only_editable_targets_and_unique_current_students_can_be_mapped()
    {
        var data=DemoWorkspaceFactory.Create();
        Assert.Throws<AcademicValidationException>(()=>GradePastePreview.Build(data,1,"missing",["s1"],"3"));
        Assert.Throws<AcademicValidationException>(()=>GradePastePreview.Build(data,1,"t1",["s1","s1"],"3\n4"));
        Assert.Throws<AcademicValidationException>(()=>GradePastePreview.Build(data,1,"t1",["foreign"],"3"));
        Assert.Throws<AcademicValidationException>(()=>GradePastePreview.Build(data,1,"t",["s1"],"3")); // Promedio calculado, no editable.
    }
    [Fact]
    public async Task A_direct_activity_accepts_bulk_grades_without_filling_other_pending_cells()
    {
        var store=new Store();var service=new WorkspaceService(store);await service.SetGradeAsync(1,"s1","t2",null);
        await service.PasteGradesAsync(GradePastePreview.Build(service.Current,1,"p",["s2","s1"],"0\n5"),true);
        var index=new AcademicCutReadIndex(service.Cut(1));Assert.Equal(0m,index.Grade("s2","p"));Assert.Equal(5m,index.Grade("s1","p"));Assert.Null(index.Grade("s1","t2"));Assert.NotNull(index.Cut("s1"));
    }
    private sealed class Store:IWorkspaceStore
    {
        public int Saves;public bool Fail;public AcademicWorkspace? Load()=>DemoWorkspaceFactory.Create();
        public void Save(AcademicWorkspace data){if(Fail)throw new IOException("Fallo ficticio");Saves++;}
    }
}
