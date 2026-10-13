using GestorDocente.Application;
using GestorDocente.Domain;
using Xunit;

namespace GestorDocente.Application.Tests;

public class AttendancePdfProjectionTests
{
    [Fact]
    public void Individual_report_contains_selected_student_cut_and_approved_percentage()
    {
        var workspace=DemoWorkspaceFactory.Create();var student=workspace.Students[0];
        var cut=workspace.Cuts[0];cut.Weeks=[new(1),new(2,WeekKind.Activities),new(3),new(4),new(5),new(6)];
        cut.Attendance=[new(student.Id,1,AttendanceState.Present),new(student.Id,2,AttendanceState.Absent),
            new(student.Id,3,AttendanceState.Justified),new(student.Id,4,AttendanceState.Late),
            new(student.Id,5,AttendanceState.NoClass),new(workspace.Students[1].Id,6,AttendanceState.Absent)];
        var report=AttendancePdfProjection.Build(workspace,1,student.Id);
        Assert.Equal(student.FullName,report.Student);Assert.Equal(student.Document,report.Document);
        Assert.Equal(workspace.CourseName,report.Course);Assert.Equal(1,report.Cut);
        Assert.Equal(["P - Presente","A - Ausente","J - Justificada","T - Tarde","Sin clase","Sin diligenciar"],report.Lines.Select(l=>l.State));
        Assert.Equal("Actividades",report.Lines[1].Kind);
        Assert.Equal(new AttendanceSummary(1,1,1,1),report.Summary);Assert.Equal(1,report.Unfilled);
        Assert.Equal(200m/3,report.Percentage);Assert.Equal("Regular",report.Status);
        Assert.DoesNotContain(report.Lines,l=>l.State.Contains("%"));
        Assert.Throws<AcademicValidationException>(()=>AttendancePdfProjection.Build(workspace,1,"ajeno"));
    }
}
