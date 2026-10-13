using System.Text;
using GestorDocente.Application;
using GestorDocente.Domain;
using GestorDocente.Infrastructure;
using Xunit;

namespace GestorDocente.Infrastructure.Tests;

public class AttendancePdfTests
{
    [Fact]
    public void Dated_weeks_export_with_and_without_dates_including_continuation_pages()
    {
        var data=DemoWorkspaceFactory.Create();var cut=data.Cuts[0];
        cut.Weeks=Enumerable.Range(1,9).Select(n=>new AttendanceWeek(n,WeekKind.Activities,n==9 ? null : new DateOnly(2026,10,n))).ToList();
        var report=AttendancePdfProjection.Build(data,1,"s1");
        Assert.Equal(new DateOnly(2026,10,1),report.Lines[0].Date);Assert.Null(report.Lines[8].Date);
        var pdf=new AttendancePdfExporter().Export(data,1,"s1");Assert.StartsWith("%PDF-",Encoding.ASCII.GetString(pdf,0,5));
        if(Environment.GetEnvironmentVariable("AULA_DATED_PDF_PREVIEW") is {Length:>0} preview)File.WriteAllBytes(preview,pdf);
    }
    [Fact]
    public void Individual_pdf_is_generated_for_selected_cut_and_student_even_without_weeks()
    {
        var data=DemoWorkspaceFactory.Create();var student=data.Students[0];
        data.Cuts[0].Weeks=Enumerable.Range(1,9).Select(n=>new AttendanceWeek(n,n%2==0 ? WeekKind.Activities : WeekKind.Class)).ToList();
        data.Cuts[0].Attendance.RemoveAll(a=>a.StudentId==student.Id);
        data.Cuts[0].Attendance.AddRange([new(student.Id,1,AttendanceState.Present),new(student.Id,2,AttendanceState.Absent),
            new(student.Id,3,AttendanceState.Justified),new(student.Id,4,AttendanceState.Late),new(student.Id,5,AttendanceState.NoClass)]);
        var exporter=new AttendancePdfExporter();
        var pdf=exporter.Export(data,1,student.Id);
        Assert.True(pdf.Length>1000);Assert.StartsWith("%PDF-",Encoding.ASCII.GetString(pdf,0,5));
        if(Environment.GetEnvironmentVariable("AULA_PDF_PREVIEW") is {Length:>0} preview)
            File.WriteAllBytes(preview,pdf);
        data.Cuts[1].Weeks.Clear();data.Cuts[1].Attendance.Clear();
        var empty=exporter.Export(data,2,student.Id);
        Assert.True(empty.Length>1000);Assert.StartsWith("%PDF-",Encoding.ASCII.GetString(empty,0,5));
        Assert.Throws<AcademicValidationException>(()=>exporter.Export(data,1,"otro-estudiante"));
    }
}
