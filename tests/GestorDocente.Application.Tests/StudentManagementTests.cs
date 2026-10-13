using System.Text.Json;
using GestorDocente.Application;
using GestorDocente.Domain;
using Xunit;

namespace GestorDocente.Application.Tests;
public class StudentManagementTests
{
    [Fact]
    public async Task Editing_preserves_identity_grades_attendance_and_allows_clearing_optional_fields()
    {
        var service=new WorkspaceService(new Store());var student=service.Current.Students[0];
        var cuts=JsonSerializer.Serialize(service.Current.Cuts);
        await service.EditStudentAsync(student,"Nombre editado", "DOC-NUEVO","editado@example.test","+57 3000000000");
        var edited=service.Current.Students[0];Assert.Equal(student.Id,edited.Id);Assert.Equal("DOC-NUEVO",edited.Document);
        Assert.Equal(cuts,JsonSerializer.Serialize(service.Current.Cuts));
        await service.EditStudentAsync(edited,"Nombre editado","","","");Assert.Empty(service.Current.Students[0].Email);
        Assert.Empty(service.Current.Students[0].Document);Assert.Empty(service.Current.Students[0].Phone);
        await Assert.ThrowsAsync<AcademicValidationException>(()=>service.EditStudentAsync(student,"Edición obsoleta","","",""));
        await Assert.ThrowsAsync<AcademicValidationException>(()=>service.EditStudentAsync(service.Current.Students[0],"","","",""));
        await Assert.ThrowsAsync<AcademicValidationException>(()=>service.EditStudentAsync(service.Current.Students[0],"Duplicado",service.Current.Students[1].Document,"",""));
    }
    [Fact]
    public async Task Deletion_requires_current_confirmation_and_only_removes_target_records_from_all_cuts()
    {
        var service=new WorkspaceService(new Store());var student=service.Current.Students[0];
        var before=JsonSerializer.Serialize(service.Current);var preview=service.PreviewStudentDeletion(student.Id);
        Assert.True(preview.GradeCount>0);Assert.True(preview.AttendanceCount>0);
        await Assert.ThrowsAsync<AcademicValidationException>(()=>service.DeleteStudentAsync(preview));Assert.Equal(before,JsonSerializer.Serialize(service.Current));
        var others=service.Current.Students.Skip(1).ToArray();
        var grades=service.Current.Cuts.Select(c=>c.Grades.Where(g=>g.StudentId!=student.Id).ToArray()).ToArray();
        var attendance=service.Current.Cuts.Select(c=>c.Attendance.Where(a=>a.StudentId!=student.Id).ToArray()).ToArray();
        await service.DeleteStudentAsync(preview,true);Assert.Equal(others,service.Current.Students);
        for(var i=0;i<3;i++){Assert.Equal(grades[i],service.Current.Cuts[i].Grades);Assert.Equal(attendance[i],service.Current.Cuts[i].Attendance);}
        await Assert.ThrowsAsync<AcademicValidationException>(()=>service.DeleteStudentAsync(preview,true));
    }
    [Fact]
    public async Task Foreign_or_stale_preview_and_failed_write_preserve_snapshot()
    {
        var store=new Store();var service=new WorkspaceService(store);var preview=service.PreviewStudentDeletion("s1");
        await service.SetGradeAsync(1,"s2","t1",4);
        await Assert.ThrowsAsync<AcademicValidationException>(()=>service.DeleteStudentAsync(preview,true));
        var other=new WorkspaceService(new Store());await Assert.ThrowsAsync<AcademicValidationException>(()=>service.DeleteStudentAsync(other.PreviewStudentDeletion("s1"),true));
        var snapshot=service.Current;store.Fail=true;
        await Assert.ThrowsAsync<IOException>(()=>service.DeleteStudentAsync(service.PreviewStudentDeletion("s1"),true));Assert.Same(snapshot,service.Current);Assert.False(service.IsSaving);
        await Assert.ThrowsAsync<IOException>(()=>service.EditStudentAsync(snapshot.Students[0],"Nombre nuevo","","",""));Assert.Same(snapshot,service.Current);
    }
    private sealed class Store:IWorkspaceStore
    {
        public bool Fail;public AcademicWorkspace? Load()=>DemoWorkspaceFactory.Create();
        public void Save(AcademicWorkspace data){if(Fail)throw new IOException("Fallo ficticio de escritura");}
    }
}
