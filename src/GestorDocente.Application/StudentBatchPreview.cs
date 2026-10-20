using GestorDocente.Domain;

namespace GestorDocente.Application;

public sealed record StudentBatchRow(int Number,string FullName,string Document,string Email,string Phone,string Error);

/// <summary>Revisión inmutable de columnas alineadas. Nunca actualiza matrículas existentes.</summary>
public sealed class StudentBatchPreview
{
    internal AcademicWorkspace Source {get;}
    public IReadOnlyList<StudentBatchRow> Rows {get;}
    public string Validation {get;}
    public bool CanSave {get;}

    private StudentBatchPreview(AcademicWorkspace source,List<StudentBatchRow> rows,string problem)
    {
        Source=source;Rows=rows.AsReadOnly();
        CanSave=problem.Length==0 && rows.Count>0 && rows.All(r=>r.Error.Length==0);
        Validation=problem.Length>0 ? problem : rows.Count==0 ? "Pega al menos un nombre completo." :
            !CanSave ? "Corrige las filas señaladas y vuelve a revisar. No se guardará ningún estudiante hasta que todo el lote sea válido." :
            $"{rows.Count} estudiantes listos para agregar. Revisa que cada documento y contacto corresponda al nombre de su fila.";
    }

    public static StudentBatchPreview Build(AcademicWorkspace source,string names,string documents="",string emails="",string phones="")
    {
        var columns=new[]{Lines(names,240,"nombres"),Lines(documents,80,"documentos"),Lines(emails,254,"correos"),Lines(phones,40,"celulares")};
        var labels=new[]{"Nombres","Documentos","Correos","Celulares"};
        var count=columns.Max(c=>c.Length);var problem="";
        for(var c=1;c<columns.Length;c++)
            if(columns[c].Length>0 && columns[c].Length!=columns[0].Length)
                problem+=$"{labels[c]}: {columns[c].Length} filas para {columns[0].Length} nombres. Deja una línea vacía donde falte un dato, sin cambiar el orden. ";
        if(source.Students.Count+count>5000)problem+="La materia admite hasta 5000 estudiantes, incluidos los ya registrados.";
        var existing=source.Students.Where(s=>s.Document.Length>0).Select(s=>s.Document.Trim()).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var duplicated=columns[1].Where(d=>d.Length>0).GroupBy(d=>d,StringComparer.OrdinalIgnoreCase).Where(g=>g.Count()>1).Select(g=>g.Key).ToHashSet(StringComparer.OrdinalIgnoreCase);
        List<StudentBatchRow> rows=[];
        for(var i=0;i<count;i++)
        {
            var values=columns.Select(c=>i<c.Length ? c[i] : "").ToArray();var error="";
            if(values.Any(v=>v.Any(char.IsControl)))error="Pega una sola columna en cada campo, sin tabuladores ni datos adicionales en la misma línea.";
            else
            {
                try{StudentRules.Validate(new("preview",values[1],values[0],values[2],values[3]));}
                catch(AcademicValidationException ex){error=ex.Message;}
            }
            if(values[1].Length>0 && (existing.Contains(values[1]) || duplicated.Contains(values[1])))
                error+=(error.Length>0 ? " " : "")+(existing.Contains(values[1]) ? "El documento ya está registrado en esta materia." : "El documento está repetido en esta lista.");
            rows.Add(new(i+1,values[0],values[1],values[2],values[3],error));
        }
        return new(source,rows,problem.Trim());
    }

    private static string[] Lines(string text,int fieldLimit,string label)
    {
        if(text.Length>(fieldLimit+2)*5000)
            throw new AcademicValidationException($"La columna de {label} es demasiado larga. Usa hasta 5000 filas y respeta el tamaño máximo de cada campo.");
        if(string.IsNullOrWhiteSpace(text))return [];
        var normalized=text.Replace("\r\n","\n").Replace('\r','\n');
        if(normalized.EndsWith('\n'))normalized=normalized[..^1]; // Un salto final del portapapeles; conservar todas las filas interiores.
        var lines=normalized.Split('\n');
        if(lines.Length>5000)throw new AcademicValidationException("Solo se admiten hasta 5000 estudiantes por lote.");
        return lines.Select(line=>line.Trim(' ','\u00a0')).ToArray();
    }
}
