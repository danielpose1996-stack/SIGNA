using System.Text;
using System.Text.RegularExpressions;
using GestorDocente.Application;
using GestorDocente.Domain;
using GestorDocente.Infrastructure;
using Xunit;
namespace GestorDocente.Infrastructure.Tests;

public class ConsolidatedPdfRiskCardsTests
{
    [Fact]
    public void Risk_cards_add_paginated_pages_without_changing_the_original_table()
    {
        var report=new ConsolidatedPdfReport("Asignatura ficticia de seguimiento académico","FIC-049","2026-2","Nocturno",
            [new("Primer período",30),new("Segundo período",30),new("Cierre",40)],
            [new("Estudiante ficticia uno",["2,00","0,00 *","0,00 *"],"0,60","3,43","Delicado")],false);
        var exporter=new ConsolidatedPdfExporter();var tableOnly=exporter.Export(report);var before=Pages(tableOnly);
        var cards=Enumerable.Range(1,37).Select(i=>new ConsolidatedPdfRiskCard($"Estudiante ficticio {i:00} con apellidos extensos para revisar el contenido completo",
            i%7==0 ? "" : $"estudiante.ficticio.{i:00}.correo.extenso.para.verificacion@example.invalid",i<=18 ? "4,29" : "3,43",i<=18 ? AcademicRisk.Critical : AcademicRisk.Delicate)).ToArray();
        var withCards=exporter.Export(report with{Students=cards.Select(Student).ToArray(),RiskCards=cards});Assert.True(Pages(withCards)>=before+4);
        Assert.StartsWith("%PDF-",Encoding.ASCII.GetString(withCards,0,5));
        if(Environment.GetEnvironmentVariable("SIGNA_049_PREVIEW") is {} folder)
        {
            Directory.CreateDirectory(folder);File.WriteAllBytes(Path.Combine(folder,"consolidado-tarjetas-multipagina.pdf"),withCards);
            var sample=cards.Take(1).Concat(cards.Skip(18).Take(1)).ToArray();
            File.WriteAllBytes(Path.Combine(folder,"consolidado-tarjetas-ejemplo.pdf"),exporter.Export(report with{Students=sample.Select(Student).ToArray(),RiskCards=sample}));
        }
    }
    private static ConsolidatedPdfStudent Student(ConsolidatedPdfRiskCard card)=>new(card.FullName,
        [card.Risk==AcademicRisk.Critical ? "0,00" : "2,00","0,00 *","0,00 *"],card.Risk==AcademicRisk.Critical ? "0,00" : "0,60",card.Minimum,card.Status);
    private static int Pages(byte[] bytes)=>Regex.Matches(Encoding.Latin1.GetString(bytes),@"/Type /Page\b").Count;
}
