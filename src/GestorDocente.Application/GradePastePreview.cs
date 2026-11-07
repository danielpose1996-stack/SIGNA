using System.Globalization;
using GestorDocente.Domain;
namespace GestorDocente.Application;

public sealed record GradePasteRow(int Number,string StudentId,string FullName,string Input,decimal? Grade,string Error);

/// <summary>Lote inmutable de notas por estudiante para una columna editable.</summary>
public sealed class GradePastePreview
{
    internal AcademicWorkspace Source {get;}
    public int Cut {get;}
    public string TargetId {get;}
    public string TargetName {get;}
    public IReadOnlyList<GradePasteRow> Rows {get;}
    public int StudentCount {get;}
    public int InputCount {get;}
    public int ReplacedCount {get;}
    public bool CanSave=>StudentCount>0 && InputCount==StudentCount && Rows.All(r=>r.Error.Length==0 && r.Grade is not null);
    public string Validation=>StudentCount==0 ? "La tabla no tiene estudiantes visibles." : InputCount!=StudentCount ?
        $"Hay {InputCount} notas para {StudentCount} estudiantes visibles. Las cantidades deben coincidir exactamente." :
        Rows.Any(r=>r.Error.Length>0) ? "Corrige las filas señaladas. No se guardará ninguna nota hasta que todo el lote sea válido." :
        $"{StudentCount} notas listas, en el orden de la tabla. Se reemplazarán {ReplacedCount} notas existentes en esta columna.";

    private GradePastePreview(AcademicWorkspace source,int cut,string targetId,string targetName,List<GradePasteRow> rows,int studentCount,int inputCount,int replacedCount)
    {Source=source;Cut=cut;TargetId=targetId;TargetName=targetName;Rows=rows.AsReadOnly();StudentCount=studentCount;InputCount=inputCount;ReplacedCount=replacedCount;}

    public static GradePastePreview Build(AcademicWorkspace source,int number,string targetId,IReadOnlyList<string> orderedStudentIds,string text)
    {
        var (cut,targetName)=FindTarget(source,number,targetId);
        var students=source.Students.ToDictionary(s=>s.Id);
        if(orderedStudentIds.Count>5000 || orderedStudentIds.Distinct().Count()!=orderedStudentIds.Count || orderedStudentIds.Any(id=>!students.ContainsKey(id)))
            throw new AcademicValidationException("La lista de estudiantes cambió o contiene duplicados. Cierra y vuelve a abrir Pegar notas.");
        if(text.Length>100000)throw new AcademicValidationException("El texto pegado es demasiado largo. Usa una única columna de notas, hasta 5000 filas.");
        var normalized=text.Replace("\r\n","\n").Replace('\r','\n');
        if(normalized.EndsWith('\n'))normalized=normalized[..^1]; // Un único salto terminal del portapapeles, sin saltar filas internas.
        var lines=normalized.Length==0 ? Array.Empty<string>() : normalized.Split('\n');
        if(lines.Length>5000)throw new AcademicValidationException("Solo se admiten hasta 5000 notas por lote.");
        var rows=new List<GradePasteRow>();
        for(var i=0;i<Math.Max(lines.Length,orderedStudentIds.Count);i++)
        {
            var raw=i<lines.Length ? lines[i] : "";var input=raw.Trim();var id=i<orderedStudentIds.Count ? orderedStudentIds[i] : "";
            var error="";decimal? grade=null;
            if(id.Length==0)error="No hay estudiante para esta fila.";
            else if(raw.Contains('\t'))error="Pega una sola columna, con una nota por fila.";
            else if(input.Length==0)error="Falta una nota. Escribe 0 si deseas asignar cero.";
            else if(!decimal.TryParse(input.Replace(',','.'),NumberStyles.AllowDecimalPoint|NumberStyles.AllowLeadingSign,CultureInfo.InvariantCulture,out var value) || value is <0 or >5)
                error="Nota inválida: usa un valor entre 0,0 y 5,0.";
            else grade=value;
            rows.Add(new(i+1,id,id.Length>0 ? students[id].FullName : "—",input,grade,error));
        }
        var selected=orderedStudentIds.ToHashSet();
        var replaced=cut.Grades.Count(g=>g.ActivityId==targetId && selected.Contains(g.StudentId));
        return new(source,number,targetId,targetName,rows,orderedStudentIds.Count,lines.Length,replaced);
    }

    public static GradePastePreview BuildUniform(AcademicWorkspace source, int number, string targetId, decimal grade)
    {
        AcademicRules.ValidateGrade(grade);
        var (cut, targetName) = FindTarget(source, number, targetId);
        var input = grade.ToString(CultureInfo.InvariantCulture);
        var rows = source.Students.Select((student, index) =>
            new GradePasteRow(index + 1, student.Id, student.FullName, input, grade, "")).ToList();
        var replaced = cut.Grades.Count(entry => entry.ActivityId == targetId);
        return new(source, number, targetId, targetName, rows, rows.Count, rows.Count, replaced);
    }

    private static (AcademicCut Cut, string Name) FindTarget(AcademicWorkspace source, int number, string targetId)
    {
        var cut = source.Cuts.SingleOrDefault(c => c.Number == number)
            ?? throw new AcademicValidationException("El corte ya no existe.");
        foreach (var group in cut.Groups)
        {
            if (group.Mode == EvaluationMode.Direct && group.Id == targetId)
                return (cut, group.Name);
            if (group.Mode == EvaluationMode.Direct)
                continue;
            var activity = group.Activities.SingleOrDefault(item => item.Id == targetId);
            if (activity is not null)
                return (cut, $"{group.Name} · {activity.Name}");
        }
        throw new AcademicValidationException("Selecciona una actividad o subactividad editable del corte.");
    }
}
