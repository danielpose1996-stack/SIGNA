using GestorDocente.Domain;

namespace GestorDocente.Application;

public sealed record CutPerformance(int Number,decimal? Average,decimal? Minimum,decimal? Maximum,int Complete,int Pending,bool Configured);
public sealed record CoursePerformance(string Id,string Name,string Code,string Period,string School,string Group,
    int StudentCount,IReadOnlyList<CutPerformance> Cuts);

public static class AcademicPerformanceProjection
{
    public static CoursePerformance Build(AcademicWorkspace workspace)
    {
        var ordered=workspace.Cuts.OrderBy(c=>c.Number).ToArray();
        var indexes=ordered.Select(c=>new AcademicCutReadIndex(c)).ToArray();
        var valuesByCut=indexes.Select(index=>workspace.Students.Select(s=>index?.Cut(s.Id)).ToArray()).ToArray();
        var cuts=new List<CutPerformance>(ordered.Length);
        for(var i=0;i<ordered.Length;i++)
        {
            var number=ordered[i].Number;var index=indexes[i];
            var configured=cutConfigurationValid(ordered[i]);
            var values=valuesByCut[i];
            var hasResults=configured && values.Length>0 && values.All(v=>v is not null);
            var complete=hasResults ? workspace.Students.Count(s=>!index.HasPendingGrades(s.Id)) : 0;
            cuts.Add(new(number,hasResults ? values.Average(v=>v!.Value) : null,
                hasResults ? values.Min(v=>v!.Value) : null,
                hasResults ? values.Max(v=>v!.Value) : null,complete,workspace.Students.Count-complete,configured));
        }
        return new(workspace.CourseId,workspace.CourseName,workspace.CourseCode,workspace.Period,
            workspace.School,workspace.GroupName,workspace.Students.Count,cuts);
    }
    private static bool cutConfigurationValid(AcademicCut cut)
    {
        if(cut.Groups.Count==0)return false;
        try{AcademicRules.ValidateGroups(cut.Groups,true);return true;}
        catch(AcademicValidationException){return false;}
    }
}
