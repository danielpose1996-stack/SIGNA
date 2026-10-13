using System.Globalization;
using System.Text;
namespace GestorDocente.Application;

public sealed record StudentImportMapping(int HeaderRow,int DocumentColumn,IReadOnlyList<int> NameColumns,int? EmailColumn=null,int? PhoneColumn=null);

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
            if(documents.Count==1 && names.Count>0 && !names.Contains(documents[0]))return new(row.Number,documents[0],names,Contact(row,"email"),Contact(row,"phone"));
        }
        foreach(var row in rows.Take(50))
        {
            var matches=(from name in row.Cells.Where(c=>LooksLikeName(c.Value))
                         from document in row.Cells.Where(c=>LooksLikeDocument(c.Value))
                         where Math.Abs(name.Key-document.Key)==1
                         select (Name:name.Key,Document:document.Key)).ToList();
            var following=rows.Where(r=>r.Number>=row.Number).Take(20).ToList();
            matches=matches.Where(m=>following.Count(r=>LooksLikeName(r.Cells.GetValueOrDefault(m.Name)) && LooksLikeDocument(r.Cells.GetValueOrDefault(m.Document)))>=Math.Ceiling(following.Count*0.6)).ToList();
            if(matches.Count==1)
            {
                var match=matches[0];
                var columns=following.SelectMany(r=>r.Cells.Keys).Distinct().Where(c=>c!=match.Document && c!=match.Name).ToArray();
                var emails=columns.Where(c=>following.Count(r=>LooksLikeEmail(r.Cells.GetValueOrDefault(c)))>=Math.Ceiling(following.Count*0.6)).ToArray();
                var phones=columns.Where(c=>following.Count(r=>LooksLikePhone(r.Cells.GetValueOrDefault(c)))>=Math.Ceiling(following.Count*0.6)).ToArray();
                return new(row.Number-1,match.Document,[match.Name],emails.Length==1 ? emails[0] : null,phones.Length==1 ? phones[0] : null);
            }
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
    private static int? Contact(WorkbookRow row,string kind)
    {
        var candidates=row.Cells.Where(c=>kind=="email" ? IsEmailHeader(c.Value.Text) : IsPhoneHeader(c.Value.Text)).Select(c=>(int?)c.Key).ToArray();
        return candidates.Length==1 ? candidates[0] : null;
    }
    public static bool IsEmailHeader(string value){var text=Normalize(value);return text.Contains("correo") || text.Contains("email") || text.Contains("e-mail");}
    public static bool IsPhoneHeader(string value){var text=Normalize(value);return text.Contains("celular") || text.Contains("telefono") || text is "movil" or "phone" or "mobile";}
    private static bool LooksLikeEmail(WorkbookCell? cell)=>cell is {IsFormula:false} && System.Net.Mail.MailAddress.TryCreate(cell.Text.Trim(),out var mail) && mail.Address.Contains('@') && mail.Address==cell.Text.Trim();
    private static bool LooksLikePhone(WorkbookCell? cell)=>cell is {IsFormula:false} && cell.Text.Count(char.IsDigit) is >=7 and <=15 && cell.Text.All(c=>char.IsDigit(c) || char.IsWhiteSpace(c) || c is '+' or '-' or '(' or ')');
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
