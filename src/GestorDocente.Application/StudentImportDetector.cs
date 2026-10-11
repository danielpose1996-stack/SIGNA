using System.Globalization;
using System.Text;
namespace GestorDocente.Application;

public sealed record StudentImportMapping(int HeaderRow,int DocumentColumn,IReadOnlyList<int> NameColumns);

/// <summary>Suggests a roster layout; the teacher sees the complete preview before saving.</summary>
public static class StudentImportDetector
{
    public static StudentImportMapping? Detect(WorkbookSheet sheet)
    {
        var rows=sheet.Rows.OrderBy(r=>r.Number).ToList();
        foreach(var row in rows.Take(50))
        {
            var names=row.Cells.Where(c=>IsNameHeader(c.Value.Text)).Select(c=>c.Key).Order().ToList();
            var documents=row.Cells.Where(c=>IsDocumentHeader(c.Value.Text)).Select(c=>c.Key).ToList();
            if(documents.Count==1 && names.Count>0 && !names.Contains(documents[0]))return new(row.Number,documents[0],names);
        }
        foreach(var row in rows.Take(50))
        {
            var matches=(from name in row.Cells.Where(c=>LooksLikeName(c.Value))
                         from document in row.Cells.Where(c=>LooksLikeDocument(c.Value))
                         where Math.Abs(name.Key-document.Key)==1
                         select (Name:name.Key,Document:document.Key)).ToList();
            var following=rows.Where(r=>r.Number>=row.Number).Take(20).ToList();
            matches=matches.Where(m=>following.Count(r=>LooksLikeName(r.Cells.GetValueOrDefault(m.Name)) && LooksLikeDocument(r.Cells.GetValueOrDefault(m.Document)))>=Math.Ceiling(following.Count*0.6)).ToList();
            if(matches.Count==1)return new(row.Number-1,matches[0].Document,[matches[0].Name]);
        }
        return null;
    }
    public static bool IsNameHeader(string value)
    {
        var text=Normalize(value);
        return text.Contains("nombre") || text.Contains("apellido") || text is "estudiante" or "alumno" or "student name" or "full name";
    }
    public static bool IsDocumentHeader(string value)
    {
        var text=Normalize(value);
        return text.Contains("documento") || text.Contains("identificacion") || text.Contains("identidad") || text.Contains("cedula") || text is "cc" or "ced" or "dni" or "document" or "id";
    }
    private static bool LooksLikeName(WorkbookCell? cell)
    {
        if(cell is null || cell.IsFormula)return false;
        var text=cell.Text.Trim();
        return text.Length is >=5 and <=240 && text.All(c=>char.IsLetter(c) || char.IsWhiteSpace(c) || c is ',' or '.' or '-' or '\'' or '’') && text.Split([' ', ','],StringSplitOptions.RemoveEmptyEntries).Length>=2;
    }
    private static bool LooksLikeDocument(WorkbookCell? cell)
    {
        if(cell is null || cell.IsFormula)return false;
        var text=cell.Text.Trim();
        return text.Length is >=5 and <=20 && text.Any(char.IsDigit) && text.All(c=>char.IsLetterOrDigit(c) || c=='-');
    }
    private static string Normalize(string value)=>string.Concat(value.ToLowerInvariant().Normalize(NormalizationForm.FormD).Where(c=>CharUnicodeInfo.GetUnicodeCategory(c)!=UnicodeCategory.NonSpacingMark)).Trim().TrimEnd('.',':');
}
