using GestorDocente.Domain;
using Xunit;

namespace GestorDocente.Domain.Tests;

public class AcademicRulesTests
{
    [Theory]
    [InlineData("0")][InlineData("5")][InlineData("4.25")]
    public void Grade_accepts_inclusive_range(string value) => AcademicRules.ValidateGrade(decimal.Parse(value, System.Globalization.CultureInfo.InvariantCulture));

    [Theory]
    [InlineData(-0.01)][InlineData(5.01)]
    public void Grade_rejects_outside_range(double value) => Assert.Throws<AcademicValidationException>(() => AcademicRules.ValidateGrade((decimal)value));

    [Fact]
    public void Weighted_group_and_cut_use_two_levels_of_weights()
    {
        var cut = Cut();
        cut.Grades = [new("s", "a", 4m), new("s", "b", 2m), new("s", "p", 5m)];
        Assert.Equal(2.5m, GradeCalculator.Group(cut, cut.Groups[0], "s"));
        Assert.Equal(3.5m, GradeCalculator.Cut(cut, "s"));
    }

    [Fact]
    public void Pending_grade_is_not_silently_zero()
    {
        var cut = Cut(); cut.Grades = [new("s", "a", 0m)];
        Assert.Null(GradeCalculator.Cut(cut, "s"));
    }

    [Fact]
    public void Partial_requires_exactly_one_activity()
    {
        var cut = Cut(); cut.Groups[1].Activities.Add(new() { Id="extra", Name="Extra", Weight=0 });
        Assert.Throws<AcademicValidationException>(() => AcademicRules.ValidateGroups(cut.Groups));
    }

    [Fact]
    public void Both_group_and_activity_percentages_are_validated()
    {
        var cut = Cut(); cut.Groups[0].Weight = 59;
        Assert.Throws<AcademicValidationException>(() => AcademicRules.ValidateGroups(cut.Groups));
        cut = Cut(); cut.Groups[0].Activities[0].Weight = 24;
        Assert.Throws<AcademicValidationException>(() => AcademicRules.ValidateGroups(cut.Groups));
    }

    [Fact]
    public void Rubric_requires_100_percent_and_continuous_non_overlapping_levels()
    {
        var rubric = Rubric(); AcademicRules.ValidateRubric(rubric);
        rubric.Criteria[0].Weight = 99;
        Assert.Throws<AcademicValidationException>(() => AcademicRules.ValidateRubric(rubric));
        rubric = Rubric(); rubric.Levels[1].Lower = 49;
        Assert.Throws<AcademicValidationException>(() => AcademicRules.ValidateRubric(rubric));
        rubric = Rubric(); rubric.Levels[1].Lower = 51;
        Assert.Throws<AcademicValidationException>(() => AcademicRules.ValidateRubric(rubric));
    }

    [Fact]
    public void Rubric_assessment_weights_criteria()
    {
        var rubric = Rubric(); rubric.Criteria = [new() { Id="c1", Name="Análisis", Weight=25 }, new() { Id="c2", Name="Desarrollo", Weight=75 }];
        Assert.Equal(2.5m, GradeCalculator.Rubric(rubric, new Dictionary<string,decimal> { ["c1"]=4, ["c2"]=2 }));
    }

    [Fact]
    public void Attendance_excludes_pending_and_no_class_without_policy_assumptions()
    {
        var entries = new AttendanceState?[] { AttendanceState.Present, AttendanceState.Absent, AttendanceState.Justified, AttendanceState.Late, null, AttendanceState.NoClass };
        var summary = AttendanceSummary.From(entries);
        Assert.Equal(1,summary.Present); Assert.Equal(1,summary.Absent); Assert.Equal(1,summary.Justified); Assert.Equal(1,summary.Late);
        Assert.Equal(4,summary.Recorded);
    }

    [Fact]
    public void Ninth_week_is_allowed_and_tenth_rejected()
    {
        AcademicRules.ValidateWeekCount(8);
        Assert.Throws<AcademicValidationException>(() => AcademicRules.ValidateWeekCount(9));
    }

    private static AcademicCut Cut() => new() { Number=1, Groups=[
        new() { Id="g", Name="Talleres", Weight=60, Activities=[new() { Id="a",Name="A",Weight=25 },new() { Id="b",Name="B",Weight=75 }] },
        new() { Id="partial", Name="Parcial", Weight=40, IsPartial=true, Activities=[new() { Id="p",Name="Parcial",Weight=100 }] }
    ] };
    private static RubricDefinition Rubric() => new() { Criteria=[new() { Id="c",Name="Criterio",Weight=100 }], Levels=[new() { Name="Inicial",Lower=0,Upper=50 },new() { Name="Avanzado",Lower=50,Upper=100 }] };
}
