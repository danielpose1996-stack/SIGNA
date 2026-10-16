using GestorDocente.Domain;

namespace GestorDocente.Application;

public interface INotesPdfExporter
{
    byte[] Export(NotesPdfReport report);
}

// Deliberate privacy boundary: no Student entity, internal IDs or contact fields.
public sealed record NotesPdfColumn(string Name,string GroupName,decimal Weight,int ShareCount=0,bool IsAverage=false)
{
    public string ShareLabel=>ShareCount>0 ? $"1/{ShareCount} del grupo" : IsAverage ? "Promedio simple" : "Nota directa";
    public string WeightLabel=>$"{Weight.ToString("0.##",System.Globalization.CultureInfo.GetCultureInfo("es-CO"))} % del corte";
    public string Header=>$"{Name}\n{(Name==GroupName ? "" : GroupName+" · ")}{WeightLabel}\n{ShareLabel}";
}
public sealed record NotesPdfStudent(string FullName,IReadOnlyList<decimal?> Grades,decimal? Total);
public sealed record NotesPdfReport(string Course,string Code,string Period,string Group,int Cut,
    IReadOnlyList<NotesPdfColumn> Columns,IReadOnlyList<NotesPdfStudent> Students);

public static class NotesPdfProjection
{
    public static NotesPdfReport Build(AcademicWorkspace workspace,int cutNumber)
    {
        var cut=workspace.Cuts.SingleOrDefault(c=>c.Number==cutNumber)
            ?? throw new AcademicValidationException("Selecciona un corte válido para el reporte de notas.");
        var index=new AcademicCutReadIndex(cut);
        List<NotesPdfColumn> columns=[];List<Func<string,decimal?>> values=[];
        foreach(var group in cut.Groups)
        {
            if(group.Mode==EvaluationMode.Direct)
            {
                columns.Add(new(group.Name,group.Name,group.Weight));
                values.Add(student=>index.Grade(student,group.Id));
            }
            else
            {
                foreach(var activity in group.Activities)
                {
                    columns.Add(new(activity.Name,group.Name,group.Weight,group.Activities.Count));
                    values.Add(student=>index.Grade(student,activity.Id));
                }
                columns.Add(new($"Promedio {group.Name}",group.Name,group.Weight,IsAverage:true));
                values.Add(student=>index.Group(group,student));
            }
        }
        return new(workspace.CourseName,workspace.CourseCode,workspace.Period,workspace.GroupName,cutNumber,columns,
            workspace.Students.Select(student=>new NotesPdfStudent(student.FullName,values.Select(get=>get(student.Id)).ToArray(),index.Cut(student.Id))).ToArray());
    }
}
