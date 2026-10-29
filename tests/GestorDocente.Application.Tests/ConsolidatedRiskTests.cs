using GestorDocente.Application;
using GestorDocente.Domain;
using GestorDocente.Desktop.ViewModels;
using Xunit;
namespace GestorDocente.Application.Tests;

public class ConsolidatedRiskTests
{
    [Fact]
    public void Pending_cuts_and_explicit_zero_have_distinct_completion_and_no_writes()
    {
        var data=Data();var view=new ConsolidatedViewModel();view.Refresh(data);
        Assert.Null(view.Rows[0].MinimumValue);Assert.False(view.Rows[0].CutCompletion[1]);
        data.Cuts[0].Grades.Add(new("s1","p",0));view.Refresh(data);
        var row=view.Rows[0];Assert.True(row.CutCompletion[1]);Assert.False(row.CutCompletion[2]);
        Assert.Equal(4.29m,row.MinimumValue);Assert.Equal("Crítico",row.Status);Assert.Single(view.RiskStudents);
        Assert.Single(data.Cuts[0].Grades);Assert.Empty(data.Cuts[1].Grades);
        var report=ConsolidatedPdfProjection.Build(data);Assert.Equal(row.Minimum,report.Students[0].Minimum);
    }
    [Fact]
    public void Cards_only_include_delicate_and_critical_and_update_after_edits()
    {
        var data=Data();data.Students.Add(new("s2","doc2","Alumno Dos","dos@example.invalid"));
        data.Students.Add(new("s3","doc3","Alumno Tres"));
        data.Cuts[0].Grades=[new("s1","p",2),new("s2","p",0),new("s3","p",5)];
        var view=new ConsolidatedViewModel();view.Refresh(data);
        Assert.Equal(new[]{"s2","s1"},view.RiskStudents.Select(r=>r.Student.Id));
        view.ApplyGrade(new(WorkspaceChangeKind.Grade,1,"s2","p",Grade:5));
        Assert.Single(view.RiskStudents);Assert.Equal("s1",view.RiskStudents[0].Student.Id);
        view.ApplyGrade(new(WorkspaceChangeKind.Grade,1,"s1","p",Grade:null));Assert.Empty(view.RiskStudents);
    }
    [Fact]
    public void Complete_subactivities_are_required_and_weights_changes_recalculate_risk()
    {
        var data=Data();data.Cuts[0].Groups=[new(){Id="g",Name="Talleres",Weight=100,Mode=EvaluationMode.SubActivities,Activities=[new(){Id="a",Name="Uno"},new(){Id="b",Name="Dos"}]}];
        data.Cuts[0].Grades=[new("s1","a",5)];var view=new ConsolidatedViewModel();view.Refresh(data);
        Assert.Null(view.Rows[0].MinimumValue);Assert.Equal(.75m,view.Rows[0].FinalValue);
        view.ApplyGrade(new(WorkspaceChangeKind.Grade,1,"s1","b",Grade:0));Assert.Equal("Delicado",view.Rows[0].Status);
        data.Cuts[0].Grades.Add(new("s1","b",0));data.Cuts[0].Weight=60;data.Cuts[1].Weight=20;data.Cuts[2].Weight=20;
        view.Refresh(data);Assert.Equal(3.75m,view.Rows[0].MinimumValue);
    }
    private static AcademicWorkspace Data()=>new(){Students=[new("s1","doc1","Alumno Uno","uno@example.invalid")],Cuts=[
        new(){Number=1,Weight=30,Groups=[new(){Id="p",Name="Parcial",Weight=100}]},
        new(){Number=2,Weight=30,Groups=[new(){Id="p",Name="Parcial",Weight=100}]},
        new(){Number=3,Weight=40,Groups=[new(){Id="p",Name="Parcial",Weight=100}]}]};
}
