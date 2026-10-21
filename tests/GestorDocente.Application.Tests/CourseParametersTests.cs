using System.Text.Json;
using GestorDocente.Application;
using GestorDocente.Domain;
using Xunit;

namespace GestorDocente.Application.Tests;

public class CourseParametersTests
{
    [Fact]
    public void Snapshot_contains_only_active_academic_parameters_and_no_source_identifiers_or_records()
    {
        var source=Source();var before=JsonSerializer.Serialize(source);
        var parameters=CourseParameters.Capture(source);
        var serialized=JsonSerializer.Serialize(parameters);
        foreach(var excluded in new[]{"Students","Grades","Weeks","Attendance\"","Removed","Description","PRIVATE","source-group","source-child","identity-name","identity-code"})
            Assert.DoesNotContain(excluded,serialized);
        Assert.Equal(4,parameters.Cuts.Count);
        Assert.Equal(new[]{25m,25m,25m,25m},parameters.Cuts.Select(c=>c.Weight));
        Assert.Equal(before,JsonSerializer.Serialize(source));
        var cuts=parameters.CreateCuts();
        Assert.All(cuts,c=>{Assert.Empty(c.Grades);Assert.Empty(c.Weeks);Assert.Empty(c.Attendance);Assert.Empty(c.RemovedGroups);Assert.Empty(c.RemovedActivities);Assert.Empty(c.RemovedWeeks);});
        Assert.Equal("Talleres",cuts[0].Groups[0].Name);Assert.Equal(20m,cuts[0].Groups[0].Weight);
        Assert.False(cuts[0].Groups[0].UseWeightedAverage);
        Assert.Equal(new[]{"Taller 1","Taller 2"},cuts[0].Groups[0].Activities.Select(a=>a.Name));
        Assert.True(cuts[1].Groups[0].UseWeightedAverage);
        Assert.Equal(new decimal?[]{40,60},cuts[1].Groups[0].Activities.Select(a=>a.Weight));
        Assert.Equal(EvaluationMode.Direct,cuts[0].Groups[1].Mode);
    }

    [Fact]
    public void Every_materialization_is_independent_and_has_new_group_and_subactivity_ids()
    {
        var source=Source();var parameters=CourseParameters.Capture(source);
        source.Cuts[0].Groups[0].Name="Cambio posterior";
        source.Cuts[0].Groups[0].Activities.Clear();
        var first=parameters.CreateCuts();var second=parameters.CreateCuts();
        Assert.Equal("Talleres",first[0].Groups[0].Name);Assert.Equal(2,first[0].Groups[0].Activities.Count);
        var firstIds=Ids(first);var secondIds=Ids(second);
        Assert.Empty(firstIds.Intersect(secondIds));Assert.Empty(firstIds.Intersect(Ids(source.Cuts)));
        first[0].Groups[0].Activities[0].Name="Solo en destino";first[0].Groups[0].Weight=70;
        Assert.Equal("Taller 1",second[0].Groups[0].Activities[0].Name);Assert.Equal(20m,second[0].Groups[0].Weight);
        Assert.Equal("Talleres",parameters.Cuts[0].Groups[0].Name);
    }

    [Fact]
    public void Only_enabled_attendance_grading_is_copied_and_pending_cut_weights_are_preserved()
    {
        var source=Source();source.Cuts[0].Weight=10;source.CutWeightsPending=true;
        var parameters=CourseParameters.Capture(source);var cuts=parameters.CreateCuts();
        Assert.True(parameters.CutWeightsPending);Assert.Equal(10m,cuts[0].Weight);
        Assert.Equal(source.Cuts[0].AttendanceGrading,cuts[0].AttendanceGrading);
        Assert.Null(cuts[1].AttendanceGrading);Assert.Null(cuts[2].AttendanceGrading);
    }

    [Fact]
    public void Invalid_source_parameters_are_rejected_without_modifying_the_source()
    {
        var source=Source();source.Cuts[1].Groups[0].Activities[0].Weight=90;
        var before=JsonSerializer.Serialize(source);
        Assert.Throws<AcademicValidationException>(()=>CourseParameters.Capture(source));
        Assert.Equal(before,JsonSerializer.Serialize(source));
    }

    private static string[] Ids(IEnumerable<AcademicCut> cuts)=>cuts.SelectMany(c=>c.Groups).SelectMany(g=>g.Activities.Select(a=>a.Id).Prepend(g.Id)).ToArray();
    private static AcademicWorkspace Source()=>new()
    {
        IsDemo=false,CourseId="PRIVATE-course",CourseName="identity-name",CourseCode="identity-code",
        Students=[new("PRIVATE-student","PRIVATE-document","PRIVATE-name","private@example.invalid","1234567")],
        Cuts=[
            new(){Number=1,Weight=25,AttendanceGrading=new(true,5,0,4,2),Groups=[
                new(){Id="source-group",Name="Talleres",Weight=20,Mode=EvaluationMode.SubActivities,Activities=[new(){Id="source-child",Name="Taller 1",Description="PRIVATE-description"},new(){Name="Taller 2"}]},
                new(){Id="direct",Name="Parcial",Weight=80}],
                Grades=[new("PRIVATE-student","source-child",4.75m)],Weeks=[new(1)],Attendance=[new("PRIVATE-student",1,AttendanceState.Present)],
                RemovedGroups=[new(){Name="PRIVATE-removed",Weight=10}],RemovedActivities=[new("source-group",new(){Name="PRIVATE-removed-child"})],RemovedWeeks=[new(2)]},
            new(){Number=2,Weight=25,AttendanceGrading=new(false,1,2,3,4),Groups=[new(){Name="Parcial",Weight=100,Mode=EvaluationMode.SubActivities,UseWeightedAverage=true,Activities=[new(){Name="Teoría",Weight=40},new(){Name="Práctica",Weight=60}]}]},
            new(){Number=3,Weight=25},new(){Number=4,Weight=25}]
    };
}
