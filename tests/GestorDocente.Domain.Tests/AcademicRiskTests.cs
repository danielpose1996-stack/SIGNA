using GestorDocente.Domain;
using Xunit;

namespace GestorDocente.Domain.Tests;

public class AcademicRiskTests
{
    [Theory]
    [InlineData(5, "2.1428571428571428571428571429", AcademicRisk.Normal)]
    [InlineData(3, "3", AcademicRisk.Normal)]
    [InlineData(2, "3.4285714285714285714285714286", AcademicRisk.Delicate)]
    [InlineData(0, "4.2857142857142857142857142857", AcademicRisk.Critical)]
    public void One_complete_cut_uses_remaining_weight(decimal grade,string expected,AcademicRisk risk)
    {
        var result=AcademicRiskCalculator.Calculate([new(grade,30,true),new(0,30,false),new(0,40,false)]);
        Assert.Equal(decimal.Parse(expected,System.Globalization.CultureInfo.InvariantCulture),result.Minimum);Assert.Equal(risk,result.Risk);
    }
    [Fact]
    public void Two_complete_cuts_solve_only_the_last_weight_and_can_be_unreachable()
    {
        var result=AcademicRiskCalculator.Calculate([new(2,30,true),new(2,30,true),new(null,40,false)]);
        Assert.Equal(4.5m,result.Minimum);Assert.Equal(AcademicRisk.Critical,result.Risk);
        result=AcademicRiskCalculator.Calculate([new(0,30,true),new(0,30,true),new(null,40,false)]);
        Assert.Equal(7.5m,result.Minimum);Assert.Equal(AcademicRisk.Unreachable,result.Risk);
    }
    [Fact]
    public void Partial_pending_grades_are_ignored_and_explicit_zero_is_complete()
    {
        Assert.Equal(4.2857142857142857142857142857m,AcademicRiskCalculator.Calculate([new(0,30,true),new(5,70,false)]).Minimum);
        Assert.Null(AcademicRiskCalculator.Calculate([new(0,30,false),new(5,70,false)]).Minimum);
    }
    [Theory]
    [InlineData(3,AcademicRisk.Normal)] [InlineData(3.0001,AcademicRisk.Delicate)]
    [InlineData(4,AcademicRisk.Delicate)] [InlineData(4.0001,AcademicRisk.Critical)]
    [InlineData(5,AcademicRisk.Critical)] [InlineData(5.0001,AcademicRisk.Unreachable)]
    public void Thresholds_use_exact_required_grade(decimal required,AcademicRisk risk)
    {
        var result=AcademicRiskCalculator.Calculate([new(6-required,50,true),new(null,50,false)]);
        Assert.Equal(required,result.Minimum);Assert.Equal(risk,result.Risk);
    }
    [Fact]
    public void Assured_and_finished_failure_do_not_divide_by_zero()
    {
        Assert.Equal(AcademicRisk.Assured,AcademicRiskCalculator.Calculate([new(5,60,true),new(null,40,false)]).Risk);
        var failed=AcademicRiskCalculator.Calculate([new(2,100,true)]);
        Assert.Equal(AcademicRisk.Unreachable,failed.Risk);Assert.Null(failed.Minimum);
        Assert.Equal(AcademicRisk.Assured,AcademicRiskCalculator.Calculate([new(3,100,true)]).Risk);
    }
    [Fact]
    public void Arbitrary_cuts_zero_weights_and_invalid_totals_are_supported()
    {
        Assert.Equal(3,AcademicRiskCalculator.Calculate([new(0,0,true),new(null,20,false),new(null,20,false),new(null,60,false)]).Minimum);
        Assert.Equal(AcademicRisk.WeightsPending,AcademicRiskCalculator.Calculate([new(4,30,true),new(null,40,false)],true).Risk);
        Assert.Throws<AcademicValidationException>(()=>AcademicRiskCalculator.Calculate([new(4,30,true),new(null,40,false)]));
    }
    [Fact]
    public void Minimum_display_does_not_understate_a_fraction_and_tiny_weights_never_crash()
    {
        var result=AcademicRiskCalculator.Calculate([new(2,30,true),new(null,70,false)]);
        Assert.Equal(3.43m,result.DisplayMinimum);
        Assert.Equal(AcademicRisk.Unreachable,AcademicRiskCalculator.Calculate([new(0,100,true),new(null,0.0000000000000000000000000001m,false)]).Risk);
    }
}
