using System.Text.Json;
using GestorDocente.Application;
using GestorDocente.Domain;
using Xunit;
namespace GestorDocente.Application.Tests;

public class ConsolidatedPdfCardsTests
{
    [Fact]
    public void Pdf_only_exports_emails_for_critical_and_delicate_students_in_ui_order()
    {
        var data=new AcademicWorkspace{Students=[new("s1","DOC-OCULTO","Alumno delicado","delicado@example.invalid","TEL-OCULTO"),
            new("s2","DOC-DOS","Alumno crítico","critico@example.invalid"),new("s3","DOC-TRES","Alumno normal","NO-EXPORTAR@example.invalid"),new("s4","DOC-CUATRO","Alumno pendiente")],Cuts=[
            new(){Number=1,Weight=30,Groups=[new(){Id="p",Name="Parcial",Weight=100}],Grades=[new("s1","p",2),new("s2","p",0),new("s3","p",5)]},
            new(){Number=2,Weight=30},new(){Number=3,Weight=40}]};
        var before=JsonSerializer.Serialize(data);var report=ConsolidatedPdfProjection.Build(data);
        Assert.Equal(new[]{"Alumno crítico","Alumno delicado"},report.RiskCards.Select(c=>c.FullName));
        Assert.Equal(new[]{"4,29","3,43"},report.RiskCards.Select(c=>c.Minimum));Assert.Equal("critico@example.invalid",report.RiskCards[0].Email);
        var json=JsonSerializer.Serialize(report);foreach(var secret in new[]{"DOC-OCULTO","TEL-OCULTO","NO-EXPORTAR@example.invalid"})Assert.DoesNotContain(secret,json);
        Assert.Equal(before,JsonSerializer.Serialize(data));
        data.Students[1]=data.Students[1] with{Email=""};Assert.Equal("Sin correo registrado",ConsolidatedPdfProjection.Build(data).RiskCards[0].EmailLabel);
        data.CutWeightsPending=true;data.Cuts[2].Weight=30;Assert.Empty(ConsolidatedPdfProjection.Build(data).RiskCards);
    }
}
