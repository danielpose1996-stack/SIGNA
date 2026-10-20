using GestorDocente.Domain;
using Xunit;

namespace GestorDocente.Domain.Tests;

public class WeightedSubactivitiesTests
{
    [Fact]
    public void Internal_weights_do_not_change_parent_weight_or_cut_weight()
    {
        var cut=Cut();var group=cut.Groups[0];var index=new AcademicCutReadIndex(cut);
        Assert.Equal(4.2m,GradeCalculator.Group(cut,group,"s"));Assert.Equal(4.2m,index.Group(group,"s"));
        Assert.Equal(3.1m,GradeCalculator.Cut(cut,"s"));Assert.Equal(3.1m,index.Cut("s"));
        Assert.Equal(50m,group.Weight);
        Assert.Equal(0.93m,GradeCalculator.Final([(index.Cut("s"),30m),(0m,70m)]));
    }

    [Fact]
    public void Simple_average_remains_default_and_ignores_preserved_internal_weights()
    {
        var cut=Cut();cut.Groups[0].UseWeightedAverage=false;
        Assert.False(new EvaluationGroup().UseWeightedAverage);
        Assert.Equal(4m,GradeCalculator.Group(cut,cut.Groups[0],"s"));
        Assert.Equal(3m,new AcademicCutReadIndex(cut).Cut("s"));
    }

    [Fact]
    public void Missing_grade_is_zero_only_in_calculation_and_other_groups_are_independent()
    {
        var cut=Cut();cut.Grades.RemoveAt(0);var index=new AcademicCutReadIndex(cut);
        Assert.Equal(3m,index.Group(cut.Groups[0],"s"));Assert.Equal(2.5m,GradeCalculator.Cut(cut,"s"));
        Assert.Null(index.Grade("s","theory"));Assert.True(index.HasPendingGrades("s"));Assert.Equal(2,cut.Grades.Count);
        Assert.Equal(2m,index.Group(cut.Groups[1],"s"));
    }

    [Theory]
    [InlineData(null,60)] [InlineData(40,59)] [InlineData(40,61)] [InlineData(-1,101)] [InlineData(101,-1)]
    public void Missing_invalid_or_incomplete_internal_percentages_are_rejected(int? first,int second)
    {
        var cut=Cut();cut.Groups[0].Activities[0].Weight=first;cut.Groups[0].Activities[1].Weight=second;
        Assert.Throws<AcademicValidationException>(()=>AcademicRules.ValidateGroups(cut.Groups,true));
        Assert.Null(GradeCalculator.Cut(cut,"s"));Assert.Null(new AcademicCutReadIndex(cut).Cut("s"));
    }

    [Fact]
    public void Zero_and_decimal_weights_are_supported_without_rounding()
    {
        var cut=Cut();cut.Groups[0].Activities[0].Weight=0;cut.Groups[0].Activities[1].Weight=100;
        AcademicRules.ValidateGroups(cut.Groups);Assert.Equal(5m,GradeCalculator.Group(cut,cut.Groups[0],"s"));
        cut.Groups[0].Activities[0].Weight=33.33m;cut.Groups[0].Activities[1].Weight=66.67m;
        AcademicRules.ValidateGroups(cut.Groups);Assert.Equal(4.3334m,new AcademicCutReadIndex(cut).Group(cut.Groups[0],"s"));
    }

    [Fact]
    public void Weighted_group_cannot_be_saved_without_subactivities()
    {
        var cut=Cut();cut.Groups[0].Activities.Clear();
        Assert.Throws<AcademicValidationException>(()=>AcademicRules.ValidateGroups(cut.Groups,true));
    }

    private static AcademicCut Cut()=>new(){Number=1,Weight=30,Groups=[
        new(){Id="p",Name="Parcial",Weight=50,Mode=EvaluationMode.SubActivities,UseWeightedAverage=true,
            Activities=[new(){Id="theory",Name="Teoría",Weight=40},new(){Id="practice",Name="Práctica",Weight=60}]},
        new(){Id="d",Name="Trabajo",Weight=50}],Grades=[new("s","theory",3),new("s","practice",5),new("s","d",2)]};
}
