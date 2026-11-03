using System.Globalization;
using GestorDocente.Domain;
namespace GestorDocente.Application;

public interface IConsolidatedPdfExporter { byte[] Export(ConsolidatedPdfReport report); }
// Privacy boundary: no Student entities, documents, IDs or phones. Email only in risk cards.
public sealed record ConsolidatedPdfCut(string Label,decimal Weight);
public sealed record ConsolidatedPdfStudent(string FullName,IReadOnlyList<string> Grades,string Final,string Minimum,string Status);
public sealed record ConsolidatedPdfRiskCard(string FullName,string Email,string Minimum,AcademicRisk Risk)
{
    public string EmailLabel=>string.IsNullOrWhiteSpace(Email) ? "Sin correo registrado" : Email;
    public string Status=>new AcademicRiskResult(null,Risk).Label;
    public string MinimumLabel=>$"Necesita {Minimum} en los cortes pendientes";
}
public sealed record ConsolidatedPdfReport(string Course,string Code,string Period,string Group,
    IReadOnlyList<ConsolidatedPdfCut> Cuts,IReadOnlyList<ConsolidatedPdfStudent> Students,bool WeightsPending)
{
    public IReadOnlyList<ConsolidatedPdfRiskCard> RiskCards{get;init;}=[];
}

public static class ConsolidatedPdfProjection
{
    public static ConsolidatedPdfReport Build(Domain.AcademicWorkspace workspace)
    {
        var index=new ConsolidatedReadIndex(workspace);var cuts=workspace.Cuts.OrderBy(c=>c.Number).ToArray();
        var culture=CultureInfo.GetCultureInfo("es-CO");
        string Grade(decimal? value)=>value?.ToString("0.00",culture) ?? "—";
        var summaries=workspace.Students.Select(student=>(Student:student,Standing:index.For(student.Id))).ToArray();
        return new(workspace.CourseName,workspace.CourseCode,workspace.Period,workspace.GroupName,
            cuts.Select(c=>new ConsolidatedPdfCut(c.Label,c.Weight)).ToArray(),summaries.Select(item=>
            {
                var result=item.Standing;
                return new ConsolidatedPdfStudent(item.Student.FullName,cuts.Select(c=>
                {
                    var grade=result.Cuts[c.Number];return grade.Completed ? Grade(grade.Value) : $"{Grade(grade.Value)} *";
                }).ToArray(),Grade(result.Final),Grade(result.Risk.DisplayMinimum),result.Risk.Label);
            }).ToArray(),workspace.CutWeightsPending)
        {
            RiskCards=summaries.Where(s=>s.Standing.Risk.Risk is AcademicRisk.Critical or AcademicRisk.Delicate)
                .OrderBy(s=>s.Standing.Risk.Risk==AcademicRisk.Critical ? 0 : 1).ThenByDescending(s=>s.Standing.Risk.Minimum)
                .ThenBy(s=>s.Student.FullName,StringComparer.CurrentCultureIgnoreCase)
                .Select(s=>new ConsolidatedPdfRiskCard(s.Student.FullName,s.Student.Email,Grade(s.Standing.Risk.DisplayMinimum),s.Standing.Risk.Risk)).ToArray()
        };
    }
}
