using GestorDocente.Domain;
using Xunit;

namespace GestorDocente.Domain.Tests;

public class AttendancePolicyTests
{
    [Fact]
    public void Late_counts_as_presence_and_justified_pending_and_no_class_are_excluded()
    {
        var summary=AttendanceSummary.From([AttendanceState.Present,AttendanceState.Late,AttendanceState.Absent,
            AttendanceState.Justified,AttendanceState.NoClass,null]);
        Assert.Equal(2,summary.Attended);Assert.Equal(3,summary.Evaluable);
        Assert.Equal(200m/3,summary.Percentage);
        Assert.Equal(AttendanceClassification.Regular,summary.Classification);
        Assert.Equal(1,summary.Justified);Assert.Equal(4,summary.Recorded);
    }
    [Theory]
    [InlineData(4,0,0,AttendanceClassification.Excellent)]
    [InlineData(2,1,1,AttendanceClassification.Good)]
    [InlineData(1,1,2,AttendanceClassification.Regular)]
    [InlineData(1,0,3,AttendanceClassification.Low)]
    [InlineData(0,0,1,AttendanceClassification.Low)]
    [InlineData(0,0,0,AttendanceClassification.NoRecords)]
    public void Classification_uses_exact_boundaries(int present,int late,int absent,AttendanceClassification expected)
    {
        var summary=new AttendanceSummary(present,absent,2,late);
        Assert.Equal(expected,summary.Classification);
        if(expected==AttendanceClassification.NoRecords)Assert.Null(summary.Percentage);
    }
}
