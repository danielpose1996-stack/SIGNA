using GestorDocente.Domain;
using Xunit;
namespace GestorDocente.Domain.Tests;

public class AttendanceGradeTests
{
    [Fact]
    public void Average_includes_justified_and_excludes_pending_and_no_class()
    {
        var policy=new AttendanceGradePolicy(true,5,0,4,1);
        Assert.Equal(2.5m,policy.Average([AttendanceState.Present,AttendanceState.Absent,AttendanceState.Justified,AttendanceState.Late,null,AttendanceState.NoClass]));
        Assert.Null(policy.Average([null,AttendanceState.NoClass]));
        Assert.Null((policy with{Enabled=false}).Average([AttendanceState.Present]));
    }
    [Theory]
    [InlineData(-0.01)] [InlineData(5.01)]
    public void Every_state_must_have_a_valid_grade_even_when_disabled(double value)
    {
        var invalid=(decimal)value;
        foreach(var policy in new[]{new AttendanceGradePolicy(false,invalid,0,0,0),new(false,0,invalid,0,0),new(false,0,0,invalid,0),new(false,0,0,0,invalid)})
            Assert.Throws<AcademicValidationException>(()=>policy.Validate());
    }
    [Fact]
    public void Grade_uses_only_active_weeks_and_does_not_change_percentage_or_grades()
    {
        var cut=new AcademicCut{Weeks=[new(2),new(7)],AttendanceGrading=new(true,5,0,5,1),Attendance=[new("s",2,AttendanceState.Justified),new("s",7,AttendanceState.Late),new("s",9,AttendanceState.Absent)]};
        var index=new AcademicCutReadIndex(cut);
        Assert.Equal(3m,index.AttendanceGrade("s"));
        index.ApplyAttendance("s",7,AttendanceState.Present);Assert.Equal(5m,index.AttendanceGrade("s"));
        Assert.Null(index.AttendanceGrade("other"));Assert.Empty(cut.Grades);
        var summary=AttendanceSummary.From([AttendanceState.Justified,AttendanceState.Late]);Assert.Equal(100m,summary.Percentage);
    }
}
