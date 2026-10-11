using GestorDocente.Domain;
namespace GestorDocente.Application;

public static class DemoWorkspaceFactory
{
    public static AcademicWorkspace Create()
    {
        var names=new[]{"Ana Torres Ruiz","Carlos López Gómez","Daniela Ruiz Martínez","Esteban Ríos Medina","Laura Sánchez Gómez","Miguel Vargas León","Natalia Herrera Castro","Sebastián Pérez Díaz","Valentina Castro Moreno","Juan Pineda Álvarez"};
        var workspace=new AcademicWorkspace { Students=names.Select((name,i)=>new Student($"s{i+1}",$"DEMO-{i+1:000}",name)).ToList() };
        for(int number=1;number<=3;number++)
        {
            var cut=new AcademicCut { Number=number, Groups=[
                new() { Id="t",Name="Talleres",Weight=30,Activities=[Activity("t1","Taller 1",33,true),Activity("t2","Taller 2",33,false),Activity("t3","Taller 3",34,true)] },
                new() { Id="q",Name="Quices",Weight=20,Activities=[Activity("q1","Quiz 1",50,false),Activity("q2","Quiz 2",50,true)] },
                new() { Id="p",Name="Parcial",Weight=30,IsPartial=true,Activities=[Activity("p1","Parcial",100,false)] },
                new() { Id="project",Name="Proyecto",Weight=20,Activities=[Activity("project1","Proyecto",100,true)] }
            ],Weeks=[new(1),new(2),new(3)] };
            for(int s=0;s<names.Length;s++)
            {
                int a=0;
                foreach(var activity in cut.Groups.SelectMany(x=>x.Activities))
                {
                    var grade=2.7m+((s*7+a*3+number)%22)/10m;
                    cut.Grades.Add(new($"s{s+1}",activity.Id,grade)); a++;
                }
                for(int week=1;week<=3;week++)
                {
                    if(s==8 && week==3) continue;
                    var state=(s+week)%7==0 ? AttendanceState.Absent : s==1 && week==2 ? AttendanceState.Justified : s==7 && week==2 ? AttendanceState.Late : AttendanceState.Present;
                    cut.Attendance.Add(new($"s{s+1}",week,state));
                }
            }
            workspace.Cuts.Add(cut);
        }
        return workspace;
    }
    private static EvaluationActivity Activity(string id,string name,decimal weight,bool rubric) => new() {Id=id,Name=name,Weight=weight,Description="Actividad de demostración de programación orientada a objetos.",Rubric=rubric ? DefaultRubric() : null};
    public static RubricDefinition DefaultRubric() => new() {
        Criteria=[new() {Name="Análisis del problema",Description="Identifica y comprende los requerimientos del problema.",Weight=25},new() {Name="Desarrollo de la solución",Description="Implementa una solución correcta y eficiente.",Weight=35},new() {Name="Presentación y orden",Description="Código claro, estructurado y legible.",Weight=20},new() {Name="Documentación",Description="Explica las decisiones y documenta la solución.",Weight=20}],
        Levels=[new() {Name="Excelente",Lower=90,Upper=100,Description="Cumple de manera sobresaliente los criterios."},new() {Name="Bueno",Lower=70,Upper=90,Description="Cumple adecuadamente la mayoría de criterios."},new() {Name="Aceptable",Lower=50,Upper=70,Description="Cumple parcialmente los criterios establecidos."},new() {Name="Insuficiente",Lower=0,Upper=50,Description="Requiere mejorar los criterios evaluados."}]
    };
}
