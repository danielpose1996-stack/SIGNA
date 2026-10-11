using GestorDocente.Domain;
namespace GestorDocente.Application;

public sealed record WorkbookCell(string Text,bool IsFormula=false);
public sealed record WorkbookRow(int Number,IReadOnlyDictionary<int,WorkbookCell> Cells);
public sealed record WorkbookSheet(string Name,IReadOnlyList<WorkbookRow> Rows);
public sealed record StudentWorkbook(IReadOnlyList<WorkbookSheet> Sheets);
public interface IStudentWorkbookReader {StudentWorkbook Read(Stream input);}
public enum AcademicReportKind {Notes,Attendance}
public interface IAcademicReportExporter {byte[] Export(AcademicWorkspace workspace,int cut,AcademicReportKind kind);}
public sealed record StudentImportCandidate(string Document,string Name);
public sealed record StudentImportRow(int Row,string Document,string Name,string Status,bool CanImport);

public static class StudentImportPreview
{
    public static IReadOnlyList<StudentImportRow> Build(WorkbookSheet sheet,int headerRow,int documentColumn,IReadOnlyList<int> nameColumns,IReadOnlyList<Student> existing)
    {
        if(headerRow<0 || !sheet.Rows.Any(r=>r.Number>headerRow))throw new AcademicValidationException("Selecciona la primera fila que contiene estudiantes.");
        if(documentColumn<0 || nameColumns.Count==0 || nameColumns.Contains(documentColumn))throw new AcademicValidationException("Selecciona documento y al menos una columna de nombre distinta.");
        var seen=new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var known=existing.ToDictionary(s=>s.Document.Trim(),StringComparer.OrdinalIgnoreCase);
        var result=new List<StudentImportRow>();
        foreach(var row in sheet.Rows.Where(r=>r.Number>headerRow))
        {
            var doc=row.Cells.GetValueOrDefault(documentColumn)?.Text.Trim() ?? "";
            var name=string.Join(" ",nameColumns.Select(c=>row.Cells.GetValueOrDefault(c)?.Text.Trim() ?? "").Where(v=>v.Length>0));
            if(doc.Length==0 && name.Length==0)continue;
            var status="Nuevo";
            if(new[]{documentColumn}.Concat(nameColumns).Any(c=>row.Cells.GetValueOrDefault(c)?.IsFormula==true))status="Fórmula: usa valores directos";
            else if(doc.Length==0 || name.Length==0)status="Falta documento o nombre";
            else if(doc.Length>80 || name.Length>240)status="Documento o nombre demasiado largo";
            else if(known.TryGetValue(doc,out var student))status=student.FullName==name ? "Ya pertenece a esta materia" : "Documento existente con otro nombre";
            else if(!seen.Add(doc))status="Documento repetido en el archivo";
            result.Add(new(row.Number,doc,name,status,status=="Nuevo"));
        }
        return result;
    }
}
