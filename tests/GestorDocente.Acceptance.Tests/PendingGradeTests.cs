using Avalonia.Headless.XUnit;
using GestorDocente.Application;
using GestorDocente.Desktop.ViewModels;
using GestorDocente.Domain;
using Xunit;

namespace GestorDocente.Acceptance.Tests;

public class PendingGradeTests
{
    [AvaloniaFact]
    public void Pending_cells_remain_blank_while_group_cut_and_course_totals_use_zero()
    {
        var data=DemoWorkspaceFactory.Create();
        var student=data.Students[0];
        data.Cuts[0].Groups=[new(){Id="g",Name="Talleres",Weight=40,Mode=EvaluationMode.SubActivities,
            Activities=[new(){Id="a",Name="Taller 1"},new(){Id="b",Name="Taller 2"}]},
            new(){Id="p",Name="Parcial",Weight=60}];
        data.Cuts[0].Grades=[new(student.Id,"a",4m)];
        var service=new WorkspaceService(new MemoryStore(data));
        var vm=new MainViewModel(service);
        var row=vm.GradeRows[0];
        Assert.Equal(string.Empty,row.Cells["b"].Input);
        Assert.Equal(string.Empty,row.Cells["p"].Input);
        Assert.Equal("2,00",row.GroupTotals["g"]);
        Assert.Equal("0,00",row.GroupTotals["p"]);
        Assert.Equal("0,80",row.Total);
        Assert.Equal("Pendiente",row.Completion);
        Assert.Equal(AcademicFormatting.Grade(0.8m/data.Students.Count),vm.CourseAverage);
        Assert.Single(service.Cut(1).Grades);

        service.SetGrade(1,student.Id,"p",5m);
        Assert.Equal("3,80",vm.GradeRows[0].Total);
        Assert.Equal("Pendiente",vm.GradeRows[0].Completion);
        service.SetGrade(1,student.Id,"b",0m);
        Assert.Equal("Completo",vm.GradeRows[0].Completion);
        service.SetGrade(1,student.Id,"b",null);
        Assert.Equal(string.Empty,vm.GradeRows[0].Cells["b"].Input);
        Assert.Equal("3,80",vm.GradeRows[0].Total);
        Assert.Equal("Pendiente",vm.GradeRows[0].Completion);
        Assert.DoesNotContain(service.Cut(1).Grades,g=>g.ActivityId=="b" && g.StudentId==student.Id);
    }

    private sealed class MemoryStore(AcademicWorkspace data):IWorkspaceStore
    {
        public AcademicWorkspace? Load()=>WorkspaceService.Clone(data);
        public void Save(AcademicWorkspace value)=>data=WorkspaceService.Clone(value);
    }
}
