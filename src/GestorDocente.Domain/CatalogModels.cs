namespace GestorDocente.Domain;

public sealed record AcademicSemester(string Id, int Year, int Term)
{
    public string Name => $"{Year}-{Term}";
    public override string ToString() => Name;
}
public sealed record CourseRecord(string Id,string SemesterId,string Code,string Name,string School,string Group,int StudentCount=0,int CutCount=3)
{
    // Metadata de lectura para reutilizar proyecciones; no forma parte del payload académico.
    public long Revision{get;init;}=-1;
}
