using System.Text;
using System.Text.RegularExpressions;
using GestorDocente.Application;
using GestorDocente.Infrastructure;
using Xunit;

namespace GestorDocente.Infrastructure.Tests;
public class NotesPdfTests
{
    [Fact]
    public void Report_supports_many_rows_and_columns_with_repeated_context_and_safe_file_replacement()
    {
        var columns=Enumerable.Range(1,12).Select(i=>new NotesPdfColumn($"Actividad académica {i}","Trabajos de investigación",20,3)).ToArray();
        var report=new NotesPdfReport("Asignatura ficticia de investigación y desarrollo","FIC-101","2026-2","Nocturno",2,columns,
            Enumerable.Range(1,70).Select(i=>new NotesPdfStudent($"Estudiante ficticio {i:000} con apellidos extensos para revisar la tabla",columns.Select((_,c)=>(decimal?)(c%5)).ToArray(),3.45m)).ToArray());
        var bytes=new NotesPdfExporter().Export(report);Assert.StartsWith("%PDF-",Encoding.ASCII.GetString(bytes,0,5));
        Assert.True(Regex.Matches(Encoding.Latin1.GetString(bytes),@"/Type /Page\b").Count>=6);
        if(Environment.GetEnvironmentVariable("SIGNA_NOTES_PDF_PREVIEW") is {} folder){Directory.CreateDirectory(folder);File.WriteAllBytes(Path.Combine(folder,"notas-multipagina.pdf"),bytes);}
        var directory=Path.Combine(Path.GetTempPath(),Guid.NewGuid().ToString("N"));Directory.CreateDirectory(directory);
        try
        {
            var path=Path.Combine(directory,"notas.pdf");File.WriteAllText(path,"Reporte anterior");
            Assert.Throws<IOException>(()=>{ReportFile.Save(path,bytes);});Assert.Equal("Reporte anterior",File.ReadAllText(path));
            ReportFile.Save(path,bytes,true);Assert.Equal(bytes,File.ReadAllBytes(path));Assert.Single(Directory.GetFiles(directory));
        }
        finally{Directory.Delete(directory,true);}
    }
    [Fact]
    public void Current_cut_and_empty_report_export_without_contact_data_in_the_contract()
    {
        var data=DemoWorkspaceFactory.Create();data.Students[0]=data.Students[0] with{Document="DOC-NO-COMPARTIR",Email="privado@example.test",Phone="3001234567"};
        data.Cuts[0].Grades.RemoveAll(g=>g.StudentId==data.Students[0].Id && g.ActivityId=="t1");
        var report=NotesPdfProjection.Build(data,1);var exporter=new NotesPdfExporter();var bytes=exporter.Export(report);Assert.True(bytes.Length>2000);
        if(Environment.GetEnvironmentVariable("SIGNA_NOTES_PDF_PREVIEW") is {} folder){Directory.CreateDirectory(folder);File.WriteAllBytes(Path.Combine(folder,"notas-corte.pdf"),bytes);File.WriteAllBytes(Path.Combine(folder,"notas-sin-estudiantes.pdf"),exporter.Export(report with{Students=[]}));}
        Assert.DoesNotContain(typeof(NotesPdfStudent).GetProperties(),p=>p.Name is "Document" or "Email" or "Phone" or "StudentId");
    }
}
