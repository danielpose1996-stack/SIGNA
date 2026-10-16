using GestorDocente.Domain;
namespace GestorDocente.Application;

public sealed record WorkbookCell(string Text,bool IsFormula=false);
public sealed record WorkbookRow(int Number,IReadOnlyDictionary<int,WorkbookCell> Cells);
public sealed record WorkbookSheet(string Name,IReadOnlyList<WorkbookRow> Rows);
public sealed record StudentWorkbook(IReadOnlyList<WorkbookSheet> Sheets,string ReadNotice="");
public interface IStudentWorkbookReader {StudentWorkbook Read(Stream input);}
public static class StudentWorkbookFormats
{
    public static bool IsSupportedFileName(string name)=>Path.GetExtension(name).ToLowerInvariant() is ".xls" or ".xlsx" or ".xlsm" or ".xlsb";
}
public enum AcademicReportKind {Notes,Attendance}
public interface IAcademicReportExporter {byte[] Export(AcademicWorkspace workspace,int cut,AcademicReportKind kind);}
public sealed record StudentImportCandidate(string Document,string Name,string Email="",string Phone="");
public sealed record StudentImportRow(int Row,string Document,string Name,string Status,bool CanImport,string Email="",string Phone="");

public static class StudentImportPreview
{
    public static IReadOnlyList<StudentImportRow> Build(WorkbookSheet sheet,int headerRow,int documentColumn,IReadOnlyList<int> nameColumns,IReadOnlyList<Student> existing,int? emailColumn=null,int? phoneColumn=null)
    {
        if(headerRow<0 || !sheet.Rows.Any(r=>r.Number>headerRow))throw new AcademicValidationException("Selecciona la primera fila que contiene estudiantes.");
        if(documentColumn<0 || nameColumns.Count==0 || nameColumns.Contains(documentColumn))throw new AcademicValidationException("Selecciona documento y al menos una columna de nombre distinta.");
        var seen=new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var known=existing.Where(s=>!string.IsNullOrWhiteSpace(s.Document)).ToDictionary(s=>s.Document.Trim(),StringComparer.OrdinalIgnoreCase);
        var result=new List<StudentImportRow>();
        foreach(var row in sheet.Rows.Where(r=>r.Number>headerRow))
        {
            var doc=row.Cells.GetValueOrDefault(documentColumn)?.Text.Trim() ?? "";
            var name=string.Join(" ",nameColumns.Select(c=>row.Cells.GetValueOrDefault(c)?.Text.Trim() ?? "").Where(v=>v.Length>0));
            var email=emailColumn is {} e ? row.Cells.GetValueOrDefault(e)?.Text.Trim() ?? "" : "";
            var phone=phoneColumn is {} p ? row.Cells.GetValueOrDefault(p)?.Text.Trim() ?? "" : "";
            if(doc.Length==0 && name.Length==0)continue;
            var status="Nuevo";
            if(new[]{documentColumn}.Concat(nameColumns).Concat(new[]{emailColumn,phoneColumn}.OfType<int>()).Any(c=>row.Cells.GetValueOrDefault(c)?.IsFormula==true))status="Fórmula: usa valores directos";
            else if(doc.Length==0 || name.Length==0)status="Falta documento o nombre";
            else if(doc.Length>80 || name.Length>240)status="Documento o nombre demasiado largo";
            else if(!seen.Add(doc))status="Documento repetido en el archivo";
            else
            {
                try{StudentRules.Validate(new("preview",doc,name,email,phone));}
                catch(AcademicValidationException ex){status=ex.Message;}
                if(status=="Nuevo" && known.TryGetValue(doc,out var student))status=email.Length>0 && email!=student.Email || phone.Length>0 && phone!=student.Phone ? "Actualizar contactos" : student.FullName==name ? "Ya pertenece a esta materia" : "Documento existente con otro nombre";
            }
            result.Add(new(row.Number,doc,name,status,status is "Nuevo" or "Actualizar contactos",email,phone));
        }
        return result;
    }
}
