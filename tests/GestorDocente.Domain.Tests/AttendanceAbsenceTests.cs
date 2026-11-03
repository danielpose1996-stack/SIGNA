using GestorDocente.Domain;
using Xunit;
namespace GestorDocente.Domain.Tests;

public class AttendanceAbsenceTests
{
    [Fact]
    public void Only_absent_records_in_active_class_and_activity_weeks_count()
    {
        var cut=new AcademicCut{Number=1,Weeks=[new(1),new(2,WeekKind.Activities),new(3),new(4),new(5),new(6),new(7)],RemovedWeeks=[new(8)],Attendance=[
            new("s",1,AttendanceState.Absent),new("s",2,AttendanceState.Absent),new("s",3,AttendanceState.Present),new("s",4,AttendanceState.Justified),
            new("s",5,AttendanceState.Late),new("s",6,AttendanceState.NoClass),new("s",8,AttendanceState.Absent)]};
        var index=new AttendanceAbsenceReadIndex([cut,new(){Number=2,Weeks=[new(1)],Attendance=[new("s",1,AttendanceState.Absent)]}]);
        Assert.Equal(2,index.Count(1,"s"));Assert.Equal(1,index.Count(2,"s"));Assert.Equal(3,index.Total("s"));Assert.Equal(0,index.Total("another"));
        Assert.Equal(7,cut.Attendance.Count);
    }
    [Fact]
    public void Incremental_edits_remove_and_add_exactly_one_absence_without_mutating_records()
    {
        var cut=new AcademicCut{Number=4,Weeks=[new(5)],Attendance=[new("s",5,AttendanceState.Absent)]};var index=new AttendanceAbsenceReadIndex([cut]);
        index.ApplyAttendance(4,"s",5,AttendanceState.Absent);Assert.Equal(1,index.Total("s"));
        index.ApplyAttendance(4,"s",5,AttendanceState.Justified);Assert.Equal(0,index.Total("s"));
        index.ApplyAttendance(4,"s",5,AttendanceState.Absent);index.ApplyAttendance(4,"s",5,null);Assert.Equal(0,index.Total("s"));
        index.ApplyAttendance(4,"s",500,AttendanceState.Absent);Assert.Equal(0,index.Total("s"));
        Assert.Equal(AttendanceState.Absent,cut.Attendance[0].State);
    }
}
