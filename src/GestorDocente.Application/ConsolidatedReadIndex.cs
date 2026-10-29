using GestorDocente.Domain;
namespace GestorDocente.Application;

public sealed record ConsolidatedCutGrade(decimal? Value,bool Completed);
public sealed record ConsolidatedStanding(IReadOnlyDictionary<int,ConsolidatedCutGrade> Cuts,decimal? Final,AcademicRiskResult Risk);

/// <summary>One in-memory projection for the consolidated screen and its privacy-filtered report.</summary>
public sealed class ConsolidatedReadIndex
{
    private readonly (int Number,decimal Weight)[] cuts;
    private readonly Dictionary<int,AcademicCutReadIndex> indexes;
    private readonly bool weightsPending;
    public ConsolidatedReadIndex(AcademicWorkspace workspace)
    {
        cuts=workspace.Cuts.OrderBy(c=>c.Number).Select(c=>(c.Number,c.Weight)).ToArray();
        indexes=workspace.Cuts.ToDictionary(c=>c.Number,c=>new AcademicCutReadIndex(c));weightsPending=workspace.CutWeightsPending;
    }
    public void ApplyGrade(WorkspaceChange change)
    {
        if(change.StudentId is {} student && change.ActivityId is {} activity && indexes.TryGetValue(change.Cut,out var index))
            index.ApplyGrade(student,activity,change.Grade);
    }
    public ConsolidatedStanding For(string student)
    {
        Dictionary<int,ConsolidatedCutGrade> grades=[];List<CutProgress> progress=[];
        foreach(var cut in cuts)
        {
            var index=indexes[cut.Number];var grade=index.Cut(student);var complete=grade is not null && !index.HasPendingGrades(student);
            grades.Add(cut.Number,new(grade,complete));progress.Add(new(grade,cut.Weight,complete));
        }
        return new(grades,GradeCalculator.Final(progress.Select(c=>(c.Grade,c.Weight)),weightsPending),AcademicRiskCalculator.Calculate(progress,weightsPending));
    }
}
