using GestorDocente.Domain;
namespace GestorDocente.Application;

public sealed record ReportColumn(string Title,double Width);
public sealed record AcademicReport(string Sheet,string Title,string Context,string Note,IReadOnlyList<ReportColumn> Columns,IReadOnlyList<IReadOnlyList<object?>> Rows);
public static class AcademicReportProjection
{
    public static AcademicReport Build(AcademicWorkspace workspace,int number,AcademicReportKind kind)
    {
        var cut=workspace.Cuts.SingleOrDefault(c=>c.Number==number) ?? throw new AcademicValidationException("Selecciona un corte válido.");
        var readIndex=new AcademicCutReadIndex(cut);
        var columns=new List<ReportColumn>{new("#",7),new("Documento",20),new("Nombre del estudiante",38)};
        if(kind==AcademicReportKind.Notes)
        {
            foreach(var group in cut.Groups){foreach(var activity in group.Activities)columns.Add(new($"{group.Name}\n{activity.Name}",22));columns.Add(new($"{(group.Mode==EvaluationMode.Direct ? "Nota" : "Promedio")} {group.Name}\n{group.Weight:0.##} %",22));}
            columns.Add(new("Nota del corte",18));columns.Add(new("Estado",18));
        }
        else
        {
            foreach(var week in cut.DisplayWeeks)columns.Add(new(week.Label,22));
            foreach(var title in new[]{"Presentes","Ausentes","Justificadas","Tardanzas","Registrados","Sin diligenciar","% asistencia"})columns.Add(new(title,19));
        }
        var rows=new List<IReadOnlyList<object?>>();int index=0;
        foreach(var student in workspace.Students)
        {
            var row=new List<object?>{++index,student.Document,student.FullName};
            if(kind==AcademicReportKind.Notes)
            {
                foreach(var group in cut.Groups)
                {
                    foreach(var activity in group.Activities)row.Add(readIndex.Grade(student.Id,activity.Id));
                    row.Add(group.Mode==EvaluationMode.Direct
                        ? readIndex.Grade(student.Id,group.Id)
                        : readIndex.Group(group,student.Id));
                }
                var total=readIndex.Cut(student.Id);row.Add(total);row.Add(total is null || readIndex.HasPendingGrades(student.Id) ? "Pendiente" : "Completo");
            }
            else
            {
                var states=cut.Weeks.Select(w=>readIndex.Attendance(student.Id,w.Number)).ToList();
                row.AddRange(states.Select(s=>(object?)(s switch{AttendanceState.Present=>"P",AttendanceState.Absent=>"A",AttendanceState.Justified=>"J",AttendanceState.Late=>"T",AttendanceState.NoClass=>"Sin clase",_=>"Sin diligenciar"})));
                var summary=AttendanceSummary.From(states);row.AddRange(new object?[]{summary.Present,summary.Absent,summary.Justified,summary.Late,summary.Recorded,states.Count(s=>s is null),summary.Percentage});
            }
            rows.Add(row);
        }
        return new(kind==AcademicReportKind.Notes ? "Notas" : "Asistencias",$"{workspace.CourseName} · Corte {number}",
            $"Código: {workspace.CourseCode} · Período: {workspace.Period} · Escuela: {workspace.School} · Grupo: {workspace.GroupName}",
            kind==AcademicReportKind.Notes ? "Celdas vacías: pendientes y con valor 0,0 solo en el cálculo. La nota final ponderada está disponible en Nota consolidada." : "Asistencia: (P + T) / (P + T + A) × 100. J, sin clase y pendientes se excluyen del denominador.",columns,rows);
    }
}
