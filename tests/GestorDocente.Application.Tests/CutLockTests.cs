using System.Text.Json;
using GestorDocente.Application;
using GestorDocente.Domain;
using Xunit;

namespace GestorDocente.Application.Tests;

public class CutLockTests
{
    [Fact]
    public async Task Locked_cut_rejects_all_academic_writes_and_unlock_restores_editing()
    {
        var store=new Store();
        var service=new WorkspaceService(store);
        await service.SetCutLockedAsync(1,true);
        var before=JsonSerializer.Serialize(service.Current);
        var count=store.Saves;
        Assert.Throws<AcademicValidationException>(()=>service.SetGrade(1,"s1","a",3));
        await Assert.ThrowsAsync<AcademicValidationException>(()=>service.SetGradeAsync(1,"s1","a",3));
        Assert.Throws<AcademicValidationException>(()=>service.ConfigureGroups(1,service.Cut(1).Groups));
        await Assert.ThrowsAsync<AcademicValidationException>(()=>service.ConfigureGroupsAsync(1,service.Cut(1).Groups));
        Assert.Throws<AcademicValidationException>(()=>service.AddWeek(1));
        await Assert.ThrowsAsync<AcademicValidationException>(()=>service.AddWeekAsync(1));
        await Assert.ThrowsAsync<AcademicValidationException>(()=>service.EditWeekAsync(1,1,WeekKind.Activities,null));
        Assert.Throws<AcademicValidationException>(()=>service.RemoveWeek(1,1));
        await Assert.ThrowsAsync<AcademicValidationException>(()=>service.RemoveWeekAsync(1,1));
        Assert.Throws<AcademicValidationException>(()=>service.RestoreWeek(1,99));
        await Assert.ThrowsAsync<AcademicValidationException>(()=>service.RestoreWeekAsync(1,99));
        Assert.Throws<AcademicValidationException>(()=>service.RemoveActivity(1,"sub"));
        Assert.Throws<AcademicValidationException>(()=>service.RestoreActivity(1,"sub"));
        await Assert.ThrowsAsync<AcademicValidationException>(()=>service.RestoreActivityAsync(1,"sub"));
        Assert.Throws<AcademicValidationException>(()=>service.SetAttendance(1,"s1",1,AttendanceState.Absent));
        await Assert.ThrowsAsync<AcademicValidationException>(()=>service.SetAttendanceAsync(1,"s1",1,AttendanceState.Absent));
        await Assert.ThrowsAsync<AcademicValidationException>(()=>service.ConfigureAttendanceGradingAsync(1,new(true,5,0,5,1)));
        var batch=GradePastePreview.BuildUniform(service.Current,1,"a",2);
        await Assert.ThrowsAsync<AcademicValidationException>(()=>service.PasteGradesAsync(batch,true));
        Assert.Throws<AcademicValidationException>(()=>service.PreviewCutDeletion(1));
        var deletion=service.PreviewStudentListDeletion();
        await Assert.ThrowsAsync<AcademicValidationException>(()=>service.DeleteStudentListAsync(deletion,true));
        Assert.Equal(count,store.Saves);
        Assert.Equal(before,JsonSerializer.Serialize(service.Current));
        await service.SetGradeAsync(2,"s1","dest",4);
        await service.SetCutLockedAsync(1,false);
        await service.SetGradeAsync(1,"s1","a",3);
        Assert.Equal(3,service.Cut(1).Grades.Single(g=>g.StudentId=="s1").Value);
    }

    [Fact]
    public void Legacy_cut_locks_default_to_unlocked_and_current_payload_requires_an_explicit_boolean()
    {
        var legacy=Data();legacy.SchemaVersion=8;
        foreach(var cut in legacy.Cuts)cut.IsLocked=true;
        var old=System.Text.Json.Nodes.JsonNode.Parse(JsonSerializer.Serialize(legacy))!;
        foreach(var cut in old["Cuts"]!.AsArray())cut!.AsObject().Remove("IsLocked");
        var upgraded=WorkspaceMigration.Upgrade(WorkspaceIntegrity.Deserialize(old.ToJsonString()));
        Assert.All(upgraded.Cuts,cut=>Assert.False(cut.IsLocked));
        Assert.Equal(legacy.Cuts[0].Grades,upgraded.Cuts[0].Grades);
        old["SchemaVersion"]=AcademicWorkspace.CurrentSchemaVersion;
        Assert.Throws<AcademicValidationException>(()=>WorkspaceIntegrity.Deserialize(old.ToJsonString()));
    }

    internal static AcademicWorkspace Data()=>new()
    {
        Students=[new("s1","01","Estudiante uno"),new("s2","02","Estudiante dos"),new("s3","03","Estudiante tres")],
        Cuts=[
            new(){Number=1,Weight=30,Groups=[new(){Id="a",Name="Trabajo",Weight=100}],
                Grades=[new("s1","a",4.5m),new("s2","a",0)],Weeks=[new(1)],Attendance=[new("s1",1,AttendanceState.Present),new("s2",1,AttendanceState.Late)],
                AttendanceGrading=new(true,5,0,4,1)},
            new(){Number=2,Weight=30,Groups=[new(){Id="dest",Name="Destino",Weight=100}],
                Grades=[new("s1","dest",1),new("s2","dest",1),new("s3","dest",2)]},
            new(){Number=3,Weight=40}]
    };
    private sealed class Store:IWorkspaceStore
    {
        public int Saves;
        public AcademicWorkspace? Load()=>Data();
        public void Save(AcademicWorkspace data)=>Saves++;
    }
}
