using System.Globalization;
using System.Text;

namespace GestorDocente.Application;

public sealed record CourseStudentEnrollment(string SemesterId,string CourseId,string CourseName,string CourseCode,string Group,string StudentId,string FullName,string Document="");
public sealed record StudentCourseMatch(string Id,string Name,string Code,string Group)
{
    public string GroupLabel=>string.IsNullOrWhiteSpace(Group) ? "Sin grupo" : Group;
}
public sealed record SemesterStudentMatch(string Key,string FullName,IReadOnlyList<StudentCourseMatch> Courses);

/// <summary>Detached read model: name filtering never opens a workspace or accesses storage.</summary>
public sealed class SemesterStudentIndex
{
    private readonly (SemesterStudentMatch Match,string[] Names)[] entries;
    public SemesterStudentIndex(string semesterId,IEnumerable<CourseStudentEnrollment> enrollments)
    {
        entries=enrollments.Where(e=>e.SemesterId==semesterId)
            .GroupBy(e=>string.IsNullOrWhiteSpace(e.Document)
                ? (Document:"",Course:e.CourseId,Student:e.StudentId)
                : (Document:e.Document.Trim().ToUpperInvariant(),Course:"",Student:""))
            .Select((group,i)=>(Match:new SemesterStudentMatch($"match-{i}",group.First().FullName,
                Array.AsReadOnly(group.DistinctBy(e=>e.CourseId).OrderBy(e=>e.CourseName,StringComparer.CurrentCultureIgnoreCase)
                    .ThenBy(e=>e.Group,StringComparer.CurrentCultureIgnoreCase)
                    .Select(e=>new StudentCourseMatch(e.CourseId,e.CourseName,e.CourseCode,e.Group)).ToArray())),
                Names:group.Select(e=>Normalize(e.FullName)).Distinct().ToArray()))
            .OrderBy(e=>e.Match.FullName,StringComparer.CurrentCultureIgnoreCase).ToArray();
    }
    public IReadOnlyList<SemesterStudentMatch> Find(string? query)
    {
        var terms=Normalize(query ?? "").Split((char[]?)null,StringSplitOptions.RemoveEmptyEntries);
        if(terms.Length==0)return [];
        return entries.Where(e=>e.Names.Any(name=>terms.All(term=>name.Contains(term,StringComparison.Ordinal))))
            .Select(e=>e.Match).ToArray();
    }
    private static string Normalize(string value)
    {
        var builder=new StringBuilder(value.Length);
        foreach(var ch in value.Normalize(NormalizationForm.FormD))
            if(CharUnicodeInfo.GetUnicodeCategory(ch)!=UnicodeCategory.NonSpacingMark)builder.Append(char.ToUpperInvariant(ch));
        return builder.ToString();
    }
}
