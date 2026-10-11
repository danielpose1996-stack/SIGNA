using System.Text.Json;
using GestorDocente.Domain;

namespace GestorDocente.Application;

public sealed class WorkspaceService
{
    private readonly IWorkspaceStore store;
    public AcademicWorkspace Current { get; private set; }
    public event Action? Changed;
    public WorkspaceService(IWorkspaceStore store)
    {
        this.store=store;
        var loaded=store.Load(); Current=loaded ?? DemoWorkspaceFactory.Create();
        if(!Current.IsDemo || Current.SchemaVersion!=1 || !Current.Cuts.Select(x=>x.Number).Order().SequenceEqual(new[]{1,2,3}))
            throw new AcademicValidationException("La base no corresponde a esta demostración o su versión no es compatible. No se reemplazó.");
        foreach(var cut in Current.Cuts) AcademicRules.ValidateGroups(cut.Groups);
        if(loaded is null) store.Save(Current);
    }
    public AcademicCut Cut(int number) => Current.Cuts.SingleOrDefault(x=>x.Number==number) ?? throw new AcademicValidationException("Selecciona un corte entre 1 y 3.");
    public EvaluationActivity Activity(int number,string id) => Cut(number).Groups.SelectMany(x=>x.Activities).Single(x=>x.Id==id);
    public static T Clone<T>(T value) => JsonSerializer.Deserialize<T>(JsonSerializer.Serialize(value))!;
    private void Change(int number,Action<AcademicCut> action)
    {
        _=Cut(number);
        var copy=Clone(Current); action(copy.Cuts.Single(x=>x.Number==number));
        store.Save(copy); Current=copy; Changed?.Invoke();
    }
    private void StudentExists(string id)
    {
        if(!Current.Students.Any(x=>x.Id==id)) throw new AcademicValidationException("El estudiante no pertenece al grupo.");
    }
    public void SetGrade(int number,string studentId,string activityId,decimal? value)
    {
        StudentExists(studentId); _=Activity(number,activityId);
        if(value is not null) AcademicRules.ValidateGrade(value.Value);
        Change(number,cut=> {cut.Grades.RemoveAll(x=>x.StudentId==studentId && x.ActivityId==activityId); if(value is not null) cut.Grades.Add(new(studentId,activityId,value.Value));});
    }
    public void ConfigureGroups(int number,List<EvaluationGroup> groups)
    {
        AcademicRules.ValidateGroups(groups);
        var ids=groups.SelectMany(x=>x.Activities).Select(x=>x.Id).ToHashSet();
        if(Cut(number).Grades.Any(x=>!ids.Contains(x.ActivityId))) throw new AcademicValidationException("No se pueden eliminar actividades que tienen calificaciones. Conserva sus datos.");
        Change(number,cut=>cut.Groups=Clone(groups));
    }
    public void SaveActivity(int number,EvaluationActivity draft)
    {
        Change(number,cut=> {
            var group=cut.Groups.Single(x=>x.Activities.Any(a=>a.Id==draft.Id));
            var index=group.Activities.FindIndex(x=>x.Id==draft.Id); group.Activities[index]=Clone(draft);
            AcademicRules.ValidateGroups(cut.Groups);
        });
    }
    public void Evaluate(int number,string studentId,string activityId,Dictionary<string,decimal> scores)
    {
        StudentExists(studentId);
        var rubric=Activity(number,activityId).Rubric ?? throw new AcademicValidationException("La actividad no tiene rúbrica.");
        var grade=GradeCalculator.Rubric(rubric,scores);
        Change(number,cut=> {cut.Grades.RemoveAll(x=>x.StudentId==studentId && x.ActivityId==activityId);cut.Grades.Add(new(studentId,activityId,grade,new(Clone(rubric),new(scores))));});
    }
    public void AddWeek(int number) => Change(number,cut=> {AcademicRules.ValidateWeekCount(cut.Weeks.Count);cut.Weeks.Add(new(cut.Weeks.Count==0 ? 1 : cut.Weeks.Max(x=>x.Number)+1));});
    public void SetAttendance(int number,string studentId,int week,AttendanceState? state)
    {
        StudentExists(studentId);
        if(!Cut(number).Weeks.Any(x=>x.Number==week)) throw new AcademicValidationException("La semana no existe en este corte.");
        if(state is not null && (!Enum.IsDefined(state.Value) || state==AttendanceState.NoClass)) throw new AcademicValidationException("Selecciona P, A, J o T; para períodos sin clase, omite la semana.");
        Change(number,cut=> {cut.Attendance.RemoveAll(x=>x.StudentId==studentId && x.WeekNumber==week);if(state is not null)cut.Attendance.Add(new(studentId,week,state.Value));});
    }
}
