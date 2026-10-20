using System.Text.Json;
using GestorDocente.Application;
using GestorDocente.Domain;
using Xunit;

namespace GestorDocente.Application.Tests;

public class StudentBatchTests
{
    [Fact]
    public async Task Columns_preserve_blank_positions_and_commit_once_without_changing_existing_records()
    {
        var store=new Store();var service=new WorkspaceService(store);var before=service.Current;
        var preview=StudentBatchPreview.Build(before," Ana Ficticia \r\nBeto Ficticio\r\nCarla Ficticia\r\n","00001\r\n\r\nDOC-3\r\n","ana@example.test\n\ncarla@example.test\n","\n+57 300 000 0000\n\n");
        Assert.True(preview.CanSave);Assert.Equal(0,store.Saves);Assert.Same(before,service.Current);
        var changes=new List<WorkspaceChange>();service.ChangedDetailed+=changes.Add;
        await service.AddStudentsAsync(preview,true);
        Assert.Equal(1,store.Saves);Assert.Equal(WorkspaceChangeKind.Students,Assert.Single(changes).Kind);
        var added=service.Current.Students.TakeLast(3).ToArray();
        Assert.Equal(new[]{"Ana Ficticia","Beto Ficticio","Carla Ficticia"},added.Select(s=>s.FullName));
        Assert.Equal(new[]{"00001","","DOC-3"},added.Select(s=>s.Document));
        Assert.Equal(new[]{"ana@example.test","","carla@example.test"},added.Select(s=>s.Email));
        Assert.Equal(new[]{"","+57 300 000 0000",""},added.Select(s=>s.Phone));
        Assert.Equal(3,added.Select(s=>s.Id).Distinct().Count());
        Assert.Equal(before.Students,service.Current.Students.Take(before.Students.Count));
        Assert.Equal(JsonSerializer.Serialize(before.Cuts),JsonSerializer.Serialize(service.Current.Cuts));
    }

    [Fact]
    public async Task Names_alone_and_homonyms_do_not_require_documents()
    {
        var service=new WorkspaceService(new Store());
        await service.AddStudentsAsync(StudentBatchPreview.Build(service.Current,"Nombre Ficticio\rNombre Ficticio"),true);
        var added=service.Current.Students.TakeLast(2).ToArray();Assert.NotEqual(added[0].Id,added[1].Id);
        Assert.All(added,s=>{Assert.Empty(s.Document);Assert.Empty(s.Email);Assert.Empty(s.Phone);});
    }

    [Theory]
    [InlineData("", "", "", "")]
    [InlineData("Ana\n\nCarla", "", "", "")]
    [InlineData("Ana\nBeto", "001", "", "")]
    [InlineData("Ana\nBeto", "001\n002\n003", "", "")]
    [InlineData("Ana\nBeto", "same\nSAME", "", "")]
    [InlineData("Ana\nBeto", "", "invalido\ncorreo@example.test", "")]
    [InlineData("Ana\nBeto", "", "", "123")]
    [InlineData("Ana\t001\nBeto", "", "", "")]
    [InlineData("Ana\nBeto", "001\t\n002", "", "")]
    public async Task Invalid_rows_or_mismatched_columns_reject_the_entire_batch(string names,string documents,string emails,string phones)
    {
        var store=new Store();var service=new WorkspaceService(store);var before=service.Current;
        var preview=StudentBatchPreview.Build(before,names,documents,emails,phones);
        Assert.False(preview.CanSave);Assert.NotEmpty(preview.Validation);
        await Assert.ThrowsAsync<AcademicValidationException>(()=>service.AddStudentsAsync(preview,true));
        Assert.Same(before,service.Current);Assert.Equal(0,store.Saves);
    }

    [Fact]
    public async Task Existing_documents_are_rejected_instead_of_updating_contacts()
    {
        var store=new Store();var service=new WorkspaceService(store);var student=service.Current.Students[0];
        var preview=StudentBatchPreview.Build(service.Current,"Otro nombre ficticio",student.Document,"otro@example.test");
        Assert.False(preview.CanSave);Assert.Contains("documento",preview.Rows[0].Error,StringComparison.OrdinalIgnoreCase);
        await Assert.ThrowsAsync<AcademicValidationException>(()=>service.AddStudentsAsync(preview,true));
        Assert.Equal(student,service.Current.Students[0]);Assert.Equal(0,store.Saves);
    }

    [Fact]
    public async Task Confirmation_source_changes_and_write_failure_are_safe_and_retryable()
    {
        var store=new Store();var service=new WorkspaceService(store);
        var preview=StudentBatchPreview.Build(service.Current,"Ana Ficticia\nBeto Ficticio");
        await Assert.ThrowsAsync<AcademicValidationException>(()=>service.AddStudentsAsync(preview));
        var foreign=new WorkspaceService(new Store());
        await Assert.ThrowsAsync<AcademicValidationException>(()=>foreign.AddStudentsAsync(preview,true));
        await service.AddStudentAsync("Cambio ficticio");
        await Assert.ThrowsAsync<AcademicValidationException>(()=>service.AddStudentsAsync(preview,true));
        preview=StudentBatchPreview.Build(service.Current,"Ana Ficticia\nBeto Ficticio");var before=service.Current;store.Fail=true;
        await Assert.ThrowsAsync<IOException>(()=>service.AddStudentsAsync(preview,true));
        Assert.Same(before,service.Current);Assert.False(service.IsSaving);Assert.Equal(1,store.Saves);
        store.Fail=false;await service.AddStudentsAsync(preview,true);Assert.Equal(before.Students.Count+2,service.Current.Students.Count);
        await Assert.ThrowsAsync<AcademicValidationException>(()=>service.AddStudentsAsync(preview,true));Assert.Equal(2,store.Saves);
    }

    [Fact]
    public void Limits_and_existing_student_capacity_are_validated_before_save()
    {
        var data=DemoWorkspaceFactory.Create();
        var names=string.Join('\n',Enumerable.Repeat("Nombre ficticio",5001-data.Students.Count));
        Assert.False(StudentBatchPreview.Build(data,names).CanSave);
        Assert.Throws<AcademicValidationException>(()=>StudentBatchPreview.Build(data,string.Join('\n',Enumerable.Repeat("Nombre",5001))));
        Assert.False(StudentBatchPreview.Build(data,new string('N',241)).CanSave);
        Assert.False(StudentBatchPreview.Build(data,"Nombre",new string('D',81)).CanSave);
        Assert.False(StudentBatchPreview.Build(data,"Nombre","","",new string('1',41)).CanSave);
    }

    private sealed class Store:IWorkspaceStore
    {
        public int Saves;public bool Fail;
        public AcademicWorkspace? Load()=>DemoWorkspaceFactory.Create();
        public void Save(AcademicWorkspace data){if(Fail)throw new IOException("Fallo ficticio");Saves++;}
    }
}
