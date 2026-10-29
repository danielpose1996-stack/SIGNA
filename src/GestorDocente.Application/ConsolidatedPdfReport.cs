using System.Globalization;
namespace GestorDocente.Application;

public interface IConsolidatedPdfExporter { byte[] Export(ConsolidatedPdfReport report); }
// Privacy boundary: no Student entities, documents, internal identifiers or contact details.
public sealed record ConsolidatedPdfCut(string Label,decimal Weight);
public sealed record ConsolidatedPdfStudent(string FullName,IReadOnlyList<string> Grades,string Final,string Minimum,string Status);
public sealed record ConsolidatedPdfReport(string Course,string Code,string Period,string Group,
    IReadOnlyList<ConsolidatedPdfCut> Cuts,IReadOnlyList<ConsolidatedPdfStudent> Students,bool WeightsPending);

public static class ConsolidatedPdfProjection
{
    public static ConsolidatedPdfReport Build(Domain.AcademicWorkspace workspace)
    {
        var index=new ConsolidatedReadIndex(workspace);var cuts=workspace.Cuts.OrderBy(c=>c.Number).ToArray();
        var culture=CultureInfo.GetCultureInfo("es-CO");
        string Grade(decimal? value)=>value?.ToString("0.00",culture) ?? "—";
        return new(workspace.CourseName,workspace.CourseCode,workspace.Period,workspace.GroupName,
            cuts.Select(c=>new ConsolidatedPdfCut(c.Label,c.Weight)).ToArray(),workspace.Students.Select(student=>
            {
                var result=index.For(student.Id);
                return new ConsolidatedPdfStudent(student.FullName,cuts.Select(c=>
                {
                    var grade=result.Cuts[c.Number];return grade.Completed ? Grade(grade.Value) : $"{Grade(grade.Value)} *";
                }).ToArray(),Grade(result.Final),Grade(result.Risk.DisplayMinimum),result.Risk.Label);
            }).ToArray(),workspace.CutWeightsPending);
    }
}
