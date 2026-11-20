using System.Text.Json;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using GestorDocente.Application;
using GestorDocente.Domain;
using GestorDocente.Desktop.Controls;
using GestorDocente.Desktop.ViewModels;
using GestorDocente.Desktop.Views;
using Xunit;

namespace GestorDocente.Acceptance.Tests;

public class ConsolidatedApprovalDisplayTests
{
    [AvaloniaFact]
    public async Task Completed_course_shows_passed_in_the_table_and_pdf_with_matching_color()
    {
        var data=new AcademicWorkspace{Students=[new("test","001","Estudiante de prueba")],Cuts=AcademicRules.DefaultCuts()};
        decimal[] grades=[2.3m,3.6m,3.07m];
        foreach(var cut in data.Cuts)
        {
            cut.Groups=[new(){Id=$"g{cut.Number}",Name="Actividad de prueba",Weight=100}];
            cut.Grades=[new("test",$"g{cut.Number}",grades[cut.Number-1])];
        }
        var before=JsonSerializer.Serialize(data);
        var model=new MainViewModel(new WorkspaceService(new Store(data)));
        var window=new MainWindow{DataContext=model,Width=1400,Height=850};window.Show();
        try
        {
            await model.OpenCourseAsync(null);model.Page="Nota consolidada";window.UpdateLayout();
            var row=Assert.Single(model.Consolidated.Rows);
            Assert.Equal("3,00",row.Final);
            Assert.Equal("Aprobado",row.Status);
            Assert.Equal(2.998m,row.FinalValue);
            Assert.Equal(ResultGrade.ColorFor(3m,true),ResultGrade.ColorFor(row.FinalValue,true));
            var report=ConsolidatedPdfProjection.Build(model.Service.Current);
            Assert.Equal("3,00",report.Students.Single().Final);
            Assert.Equal("Aprobado",report.Students.Single().Status);
            Assert.Empty(report.RiskCards);
            Assert.Equal(before,JsonSerializer.Serialize(model.Service.Current));
            if(Environment.GetEnvironmentVariable("SIGNA_056_PREVIEW") is {} folder)
            {
                Directory.CreateDirectory(folder);using var image=window.CaptureRenderedFrame();
                image!.Save(Path.Combine(folder,"consolidada-aprobada.png"));
            }
        }
        finally{window.Close();}
    }

    [Theory]
    [InlineData(2.998,3)]
    [InlineData(3.995,4)]
    [InlineData(4.595,4.6)]
    public void Result_color_matches_the_rounded_display(decimal actual,decimal shown)
    {
        Assert.Equal(ResultGrade.ColorFor(shown,true),ResultGrade.ColorFor(actual,true));
    }

    private sealed class Store(AcademicWorkspace data):IWorkspaceStore
    {
        public AcademicWorkspace? Load()=>data;
        public void Save(AcademicWorkspace workspace)=>throw new InvalidOperationException("Esta verificación es de lectura.");
    }
}
