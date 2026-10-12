using GestorDocente.Application;
using GestorDocente.Domain;
using Xunit;

namespace GestorDocente.Application.Tests;

public class ReportProjectionTests
{
    [Fact]
    public void Notes_and_attendance_keep_values_and_pending_state()
    {
        var data=DemoWorkspaceFactory.Create();
        var cut=data.Cuts[0];
        var student=data.Students[0];
        var notes=AcademicReportProjection.Build(data,1,AcademicReportKind.Notes);
        var noteRow=notes.Rows[0];
        Assert.Equal(student.Document,noteRow[1]);
        Assert.Equal(cut.Grades.Single(g=>g.StudentId==student.Id && g.ActivityId=="t1").Value,noteRow[3]);
        Assert.Equal(GradeCalculator.Cut(cut,student.Id),noteRow[^2]);
        Assert.Equal("Completo",noteRow[^1]);

        cut.Attendance.RemoveAll(a=>a.StudentId==student.Id && a.WeekNumber==cut.Weeks[0].Number);
        var attendance=AcademicReportProjection.Build(data,1,AcademicReportKind.Attendance);
        var attendanceRow=attendance.Rows[0];
        Assert.Equal("Sin diligenciar",attendanceRow[3]);
        Assert.Equal("Pendiente de política",attendanceRow[^1]);
        Assert.Equal(1,attendanceRow[^2]);
    }
}
