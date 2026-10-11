using System.Text.Json;
using GestorDocente.Application;
using GestorDocente.Domain;
using Xunit;
namespace GestorDocente.Application.Tests;
public class EvaluationModesTests
{
    [Fact]
    public void Direct_to_subactivities_preserves_grade_in_first_new_subactivity()
    {
        var service=Service();service.ConfigureGroups(1,[new(){Id="g",Name="Parcial",Weight=100}]);
        service.SetGrade(1,"s1","g",4.5m);
        service.ConfigureGroups(1,[new(){Id="g",Name="Parcial",Weight=100,Mode=EvaluationMode.SubActivities,Activities=[new(){Id="a",Name="Parte 1"},new(){Id="b",Name="Parte 2"}]}]);
        Assert.Equal(4.5m,service.Cut(1).Grades.Single(g=>g.ActivityId=="a" && g.StudentId=="s1").Value);
        Assert.Null(GradeCalculator.Cut(service.Cut(1),"s1"));service.SetGrade(1,"s1","b",3.5m);Assert.Equal(4m,GradeCalculator.Cut(service.Cut(1),"s1"));
    }
    [Fact]
    public void Subactivities_to_direct_preserves_average_and_removed_records()
    {
        var service=Service();service.SetGrade(1,"s1","t1",4);service.SetGrade(1,"s1","t2",3);service.SetGrade(1,"s1","t3",5);
        var groups=WorkspaceService.Clone(service.Cut(1).Groups);groups[0].Mode=EvaluationMode.Direct;groups[0].Activities.Clear();service.ConfigureGroups(1,groups);
        Assert.Equal(4m,GradeCalculator.Group(service.Cut(1),service.Cut(1).Groups[0],"s1"));
        Assert.Equal(3,service.Cut(1).RemovedActivities.Count);
        Assert.Equal(3m,service.Cut(1).Grades.Single(g=>g.ActivityId=="t2" && g.StudentId=="s1").Value);
        service.RestoreActivity(1,"t2");Assert.Equal(EvaluationMode.SubActivities,service.Cut(1).Groups[0].Mode);
        Assert.Equal(3.5m,GradeCalculator.Group(service.Cut(1),service.Cut(1).Groups[0],"s1"));
    }
    [Fact]
    public void Incomplete_subactivities_to_direct_does_not_reuse_old_direct_grade()
    {
        var service=Service();service.ConfigureGroups(1,[new(){Id="g",Name="Trabajo",Weight=100}]);service.SetGrade(1,"s1","g",5);
        service.ConfigureGroups(1,[new(){Id="g",Name="Trabajo",Weight=100,Mode=EvaluationMode.SubActivities,Activities=[new(){Id="a",Name="A"},new(){Id="b",Name="B"}]}]);
        service.ConfigureGroups(1,[new(){Id="g",Name="Trabajo",Weight=100}]);Assert.Null(GradeCalculator.Cut(service.Cut(1),"s1"));Assert.Equal(2,service.Cut(1).RemovedActivities.Count);
    }
    [Fact]
    public void Legacy_workspace_migrates_numbers_ids_attendance_and_removed_records_atomically()
    {
        var legacy=Legacy();var before=JsonSerializer.Serialize(legacy);var store=new MemoryStore(legacy);var service=new WorkspaceService(store);
        Assert.Equal(2,service.Current.SchemaVersion);Assert.Equal(EvaluationMode.SubActivities,service.Cut(1).Groups[0].Mode);
        Assert.Equal(4.2m,service.Cut(1).Grades.Single(g=>g.ActivityId=="a").Value);Assert.Single(service.Cut(1).Attendance);
        Assert.Single(service.Cut(1).RemovedGroups);Assert.Equal(1,store.Saves);
        Assert.DoesNotContain("Rubric",JsonSerializer.Serialize(service.Current));Assert.DoesNotContain("Assessment",JsonSerializer.Serialize(service.Current));
        Assert.Equal(before,JsonSerializer.Serialize(legacy));
    }
    [Fact]
    public void Failed_legacy_migration_leaves_original_unchanged()
    {
        var legacy=Legacy();var before=JsonSerializer.Serialize(legacy);var store=new MemoryStore(legacy){Fail=true};
        Assert.Throws<IOException>(()=>new WorkspaceService(store));Assert.Equal(before,JsonSerializer.Serialize(store.Data));
    }
    private static AcademicWorkspace Legacy()=>JsonSerializer.Deserialize<AcademicWorkspace>("""
        {"SchemaVersion":1,"IsDemo":true,"Students":[{"Id":"s1","Document":"001","FullName":"Estudiante ficticio"}],
         "Cuts":[{"Number":1,"Groups":[{"Id":"g","Name":"Talleres","Weight":100,"Aggregation":0,"IsPartial":true,"Activities":[{"Id":"a","Name":"Taller 1","Weight":100,"Rubric":{"Criteria":[]}}]}],
         "Grades":[{"StudentId":"s1","ActivityId":"a","Value":4.2,"Assessment":{"Scores":{}}}],
         "Weeks":[{"Number":1}],"Attendance":[{"StudentId":"s1","WeekNumber":1,"State":0}],
         "RemovedGroups":[{"Id":"old","Name":"Anterior","Weight":100,"Activities":[{"Id":"old-a","Name":"Anterior 1"}]}]},
         {"Number":2},{"Number":3}]}
        """)!;
    private static WorkspaceService Service()=>new(new MemoryStore(DemoWorkspaceFactory.Create()));
    private sealed class MemoryStore(AcademicWorkspace data):IWorkspaceStore
    {
        public AcademicWorkspace Data=data;public bool Fail;public int Saves;
        public AcademicWorkspace? Load()=>Data;
        public void Save(AcademicWorkspace value){if(Fail)throw new IOException();Data=WorkspaceService.Clone(value);Saves++;}
    }
}
