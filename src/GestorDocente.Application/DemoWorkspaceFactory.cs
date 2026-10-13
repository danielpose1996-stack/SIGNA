using GestorDocente.Domain;
namespace GestorDocente.Application;
public static class DemoWorkspaceFactory
{
    public static AcademicWorkspace Create()
    {
        var names=new[]{"Ana Torres Ruiz","Carlos López Gómez","Daniela Ruiz Martínez","Esteban Ríos Medina","Laura Sánchez Gómez","Miguel Vargas León","Natalia Herrera Castro","Sebastián Pérez Díaz","Valentina Castro Moreno","Juan Pineda Álvarez"};
        var workspace=new AcademicWorkspace{Students=names.Select((name,i)=>new Student($"s{i+1}",$"DEMO-{i+1:000}",name)).ToList()};
        for(int number=1;number<=3;number++)
        {
            var cut=new AcademicCut{Number=number,Weight=AcademicRules.DefaultCutWeight(number),Groups=[
                new(){Id="t",Name="Talleres",Weight=30,Mode=EvaluationMode.SubActivities,Activities=[Activity("t1","Taller 1"),Activity("t2","Taller 2"),Activity("t3","Taller 3")]},
                new(){Id="q",Name="Quices",Weight=20,Mode=EvaluationMode.SubActivities,Activities=[Activity("q1","Quiz 1"),Activity("q2","Quiz 2")]},
                new(){Id="p",Name="Parcial",Weight=30},
                new(){Id="project",Name="Proyecto",Weight=20}
            ],Weeks=[new(1),new(2),new(3)]};
            for(int s=0;s<names.Length;s++)
            {
                int a=0;
                foreach(var id in cut.Groups.SelectMany(g=>g.Mode==EvaluationMode.Direct ? new[]{g.Id} : g.Activities.Select(v=>v.Id)))
                {
                    cut.Grades.Add(new($"s{s+1}",id,2.7m+((s*7+a*3+number)%22)/10m));a++;
                }
                for(int week=1;week<=3;week++)
                {
                    if(s==8 && week==3)continue;
                    var state=(s+week)%7==0 ? AttendanceState.Absent : s==1 && week==2 ? AttendanceState.Justified : s==7 && week==2 ? AttendanceState.Late : AttendanceState.Present;
                    cut.Attendance.Add(new($"s{s+1}",week,state));
                }
            }
            workspace.Cuts.Add(cut);
        }
        return workspace;
    }
    private static EvaluationActivity Activity(string id,string name)=>new(){Id=id,Name=name,Description="Subactividad de demostración."};
}
