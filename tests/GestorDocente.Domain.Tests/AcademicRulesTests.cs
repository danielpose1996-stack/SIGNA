using GestorDocente.Domain;
using Xunit;
namespace GestorDocente.Domain.Tests;
public class AcademicRulesTests
{
    [Theory][InlineData("0")][InlineData("5")][InlineData("4.25")]
    public void Grade_accepts_inclusive_range(string value)=>AcademicRules.ValidateGrade(decimal.Parse(value,System.Globalization.CultureInfo.InvariantCulture));
    [Theory][InlineData(-0.01)][InlineData(5.01)]
    public void Grade_rejects_outside_range(double value)=>Assert.Throws<AcademicValidationException>(()=>AcademicRules.ValidateGrade((decimal)value));
    [Fact]
    public void Simple_average_and_direct_grade_produce_weighted_cut()
    {
        var cut=Cut();cut.Grades=[new("s","a",4),new("s","b",2),new("s","direct",5)];
        Assert.Equal(3m,GradeCalculator.Group(cut,cut.Groups[0],"s"));
        Assert.Equal(3.8m,GradeCalculator.Cut(cut,"s"));
    }
    [Fact]
    public void Direct_grade_works_without_any_subactivity_and_zero_is_valid()
    {
        var group=new EvaluationGroup{Id="direct",Name="Trabajo personalizado",Weight=100};
        var cut=new AcademicCut{Groups=[group],Grades=[new("s","direct",0)]};
        AcademicRules.ValidateGroups(cut.Groups);
        Assert.Empty(group.Activities);Assert.Equal(0m,GradeCalculator.Cut(cut,"s"));
        cut.Grades.Clear();Assert.Equal(0m,GradeCalculator.Cut(cut,"s"));
    }
    [Fact]
    public void Partial_name_has_no_special_constraint()
    {
        var cut=Cut();cut.Groups[0].Name="Parcial";AcademicRules.ValidateGroups(cut.Groups);
        Assert.Equal(2,cut.Groups[0].Activities.Count);
    }
    [Fact]
    public void Pending_subactivity_counts_as_zero_but_empty_configuration_is_not_calculable()
    {
        var cut=Cut();cut.Grades=[new("s","a",0),new("s","direct",5)];Assert.Equal(2m,GradeCalculator.Cut(cut,"s"));
        cut.Groups[0].Activities.Clear();Assert.Null(GradeCalculator.Cut(cut,"s"));
    }
    [Fact]
    public void Missing_direct_and_subactivity_grades_contribute_zero_without_becoming_saved_grades()
    {
        var cut=Cut();cut.Groups[0].Weight=40;cut.Groups[1].Weight=60;
        cut.Grades=[new("s","a",4)];
        var index=new AcademicCutReadIndex(cut);
        Assert.Equal(2m,GradeCalculator.Group(cut,cut.Groups[0],"s"));
        Assert.Equal(0m,GradeCalculator.Group(cut,cut.Groups[1],"s"));
        Assert.Equal(0.8m,GradeCalculator.Cut(cut,"s"));
        Assert.Equal(0.8m,index.Cut("s"));
        Assert.Null(index.Grade("s","b"));Assert.Null(index.Grade("s","direct"));
        Assert.True(index.HasPendingGrades("s"));
        Assert.Single(cut.Grades);
        index.ApplyGrade("s","b",0);
        index.ApplyGrade("s","direct",0);
        Assert.Equal(0.8m,index.Cut("s"));
        Assert.False(index.HasPendingGrades("s"));
        Assert.Single(cut.Grades);
    }
    [Fact]
    public void All_missing_grades_calculate_zero_only_when_the_cut_is_configured()
    {
        var cut=Cut();
        Assert.Equal(0m,GradeCalculator.Cut(cut,"s"));
        Assert.Equal(0m,new AcademicCutReadIndex(cut).Cut("s"));
        cut.Groups.Clear();
        Assert.Null(GradeCalculator.Cut(cut,"s"));
        Assert.Null(new AcademicCutReadIndex(cut).Cut("s"));
    }
    [Theory][InlineData(99)][InlineData(101)]
    public void Main_percentages_must_total_exactly_one_hundred(int weight)
    {
        var group=new EvaluationGroup{Id="g",Name="Proyecto",Weight=weight};
        Assert.Throws<AcademicValidationException>(()=>AcademicRules.ValidateGroups([group]));
    }
    [Fact]
    public void Duplicate_grade_targets_or_subactivities_in_direct_mode_are_rejected()
    {
        var cut=Cut();cut.Groups[0].Activities[0].Id="direct";
        Assert.Throws<AcademicValidationException>(()=>AcademicRules.ValidateGroups(cut.Groups));
        cut=Cut();cut.Groups[0].Mode=EvaluationMode.Direct;
        Assert.Throws<AcademicValidationException>(()=>AcademicRules.ValidateGroups(cut.Groups));
    }
    [Fact]
    public void Attendance_excludes_pending_and_no_class_without_policy_assumptions()
    {
        var summary=AttendanceSummary.From([AttendanceState.Present,AttendanceState.Absent,AttendanceState.Justified,AttendanceState.Late,null,AttendanceState.NoClass]);
        Assert.Equal(1,summary.Present);Assert.Equal(1,summary.Absent);Assert.Equal(1,summary.Justified);Assert.Equal(1,summary.Late);Assert.Equal(4,summary.Recorded);
    }
    [Fact]
    public void Ninth_week_is_allowed_and_tenth_rejected()
    {
        AcademicRules.ValidateWeekCount(8);Assert.Throws<AcademicValidationException>(()=>AcademicRules.ValidateWeekCount(9));
    }
    private static AcademicCut Cut()=>new(){Number=1,Groups=[
        new(){Id="g",Name="Talleres",Weight=60,Mode=EvaluationMode.SubActivities,Activities=[new(){Id="a",Name="A"},new(){Id="b",Name="B"}]},
        new(){Id="direct",Name="Parcial",Weight=40}
    ]};
}
