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
        var states=cut.Weeks.Select(w=>cut.Attendance.SingleOrDefault(a=>a.StudentId==student.Id && a.WeekNumber==w.Number)?.State);
        Assert.Equal(AttendanceSummary.From(states).Percentage,attendanceRow[^1]);
        Assert.Equal(1,attendanceRow[^2]);
    }
    [Fact]
    public void Notes_report_keeps_missing_cells_blank_while_exporting_zero_based_totals()
    {
        var data=DemoWorkspaceFactory.Create();
        var student=data.Students[0];
        data.Cuts[0].Groups=[new(){Id="g",Name="Talleres",Weight=40,Mode=EvaluationMode.SubActivities,
            Activities=[new(){Id="a",Name="Taller 1"},new(){Id="b",Name="Taller 2"}]},
            new(){Id="p",Name="Parcial",Weight=60}];
        data.Cuts[0].Grades=[new(student.Id,"a",4m)];
        var report=AcademicReportProjection.Build(data,1,AcademicReportKind.Notes);
        var row=report.Rows[0];
        Assert.Null(row[4]);
        Assert.Equal(2m,row[5]);
        Assert.Null(row[6]);
        Assert.Equal(0.8m,row[7]);
        Assert.Equal("Pendiente",row[8]);
        Assert.Single(data.Cuts[0].Grades);
    }
}
