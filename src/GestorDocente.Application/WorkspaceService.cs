using System.Text.Json;
using GestorDocente.Domain;

namespace GestorDocente.Application;

public sealed class WorkspaceService
{
    private readonly IWorkspaceStore store;
    private bool configurationSaving;
    public AcademicWorkspace Current { get; private set; }
    public event Action? Changed;
    public WorkspaceService(IWorkspaceStore store)
    {
        this.store=store;
        var loaded=store.Load(); Current=loaded ?? DemoWorkspaceFactory.Create();
        if(Current.SchemaVersion is not (1 or 2) || !Current.Cuts.Select(x=>x.Number).Order().SequenceEqual(new[]{1,2,3}))
            throw new AcademicValidationException("La versión o los cortes de la materia no son compatibles. Los datos no se reemplazaron.");
        var migrating=Current.SchemaVersion==1;
        if(migrating)
        {
            Current=Clone(Current);
            foreach(var group in Current.Cuts.SelectMany(c=>c.Groups.Concat(c.RemovedGroups)))
                group.Mode=group.Activities.Count>0 ? EvaluationMode.SubActivities : EvaluationMode.Direct;
            Current.SchemaVersion=2;
        }
        foreach(var cut in Current.Cuts)if(cut.Groups.Count>0)AcademicRules.ValidateGroups(cut.Groups,true);
        if(loaded is null || migrating)store.Save(Current);
    }
    public AcademicCut Cut(int number) => Current.Cuts.SingleOrDefault(x=>x.Number==number) ?? throw new AcademicValidationException("Selecciona un corte entre 1 y 3.");
    public EvaluationActivity Activity(int number,string id) => Cut(number).Groups.SelectMany(x=>x.Activities).Single(x=>x.Id==id);
    public static T Clone<T>(T value) => JsonSerializer.Deserialize<T>(JsonSerializer.Serialize(value))!;
    private void Change(int number,Action<AcademicCut> action)
    {
        EnsureWritable();
        _=Cut(number);
        var copy=Clone(Current); action(copy.Cuts.Single(x=>x.Number==number));
        store.Save(copy); Current=copy; Changed?.Invoke();
    }
    private void EnsureWritable()
    {
        if(configurationSaving)throw new AcademicValidationException("Espera a que termine de guardarse la configuración antes de modificar la materia.");
    }
    private void StudentExists(string id)
    {
        if(!Current.Students.Any(x=>x.Id==id)) throw new AcademicValidationException("El estudiante no pertenece al grupo.");
    }
    public void ImportStudents(IReadOnlyList<StudentImportCandidate> candidates)
    {
        EnsureWritable();
        if(candidates.Count==0)throw new AcademicValidationException("Selecciona al menos un estudiante nuevo.");
        if(candidates.Count+Current.Students.Count>5000)throw new AcademicValidationException("La materia admite hasta 5000 estudiantes.");
        var seen=Current.Students.Select(s=>s.Document.Trim()).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var additions=new List<Student>();
        foreach(var candidate in candidates)
        {
            var doc=candidate.Document.Trim();var name=candidate.Name.Trim();
            if(doc.Length is 0 or >80 || name.Length is 0 or >240)throw new AcademicValidationException("Cada estudiante requiere documento y nombre válidos.");
            if(!seen.Add(doc))throw new AcademicValidationException("Hay documentos repetidos o ya registrados. Actualiza la vista previa.");
            additions.Add(new(Guid.NewGuid().ToString("N"),doc,name));
        }
        var copy=Clone(Current);copy.Students.AddRange(additions);store.Save(copy);Current=copy;Changed?.Invoke();
    }
    public void SetGrade(int number,string studentId,string activityId,decimal? value)
    {
        StudentExists(studentId);
        if(!Cut(number).Groups.Any(g=>g.Mode==EvaluationMode.Direct ? g.Id==activityId : g.Activities.Any(a=>a.Id==activityId)))
            throw new AcademicValidationException("La actividad o subactividad no está activa en este corte.");
        if(value is not null) AcademicRules.ValidateGrade(value.Value);
        Change(number,cut=> {cut.Grades.RemoveAll(x=>x.StudentId==studentId && x.ActivityId==activityId); if(value is not null) cut.Grades.Add(new(studentId,activityId,value.Value));});
    }
    public void ConfigureGroups(int number,List<EvaluationGroup> groups)
    {
        EnsureWritable();
        var copy=PrepareGroups(Current,number,groups);
        store.Save(copy);Current=copy;Changed?.Invoke();
    }
    public async Task ConfigureGroupsAsync(int number,List<EvaluationGroup> groups)
    {
        EnsureWritable();
        var source=Current;var configuration=Clone(groups);
        configurationSaving=true;
        try
        {
            // Source is stable while writes are guarded. Clone/prepare and persist off the UI
            // thread; publish the detached result on the caller's synchronization context.
            var copy=await Task.Run(()=>{var next=PrepareGroups(source,number,configuration);store.Save(next);return next;});
            Current=copy;
        }
        finally{configurationSaving=false;}
        Changed?.Invoke();
    }
    private static AcademicWorkspace PrepareGroups(AcademicWorkspace source,int number,List<EvaluationGroup> groups)
    {
        AcademicRules.ValidateGroups(groups,true);
        var copy=Clone(source);var cut=copy.Cuts.SingleOrDefault(c=>c.Number==number) ?? throw new AcademicValidationException("Selecciona un corte entre 1 y 3.");
        foreach(var previous in cut.Groups)
        {
            var replacement=groups.SingleOrDefault(g=>g.Id==previous.Id);
            if(replacement is null)cut.RemovedGroups.Add(Clone(previous));
            else
            {
                TransferMode(copy,cut,previous,replacement);
                foreach(var activity in previous.Activities.Where(a=>!replacement.Activities.Any(b=>b.Id==a.Id)))
                    if(!cut.RemovedActivities.Any(a=>a.Activity.Id==activity.Id))cut.RemovedActivities.Add(new(previous.Id,Clone(activity)));
            }
        }
        cut.RemovedGroups.RemoveAll(g=>groups.Any(n=>n.Id==g.Id));
        cut.RemovedActivities.RemoveAll(a=>groups.SelectMany(g=>g.Activities).Any(n=>n.Id==a.Activity.Id));
        cut.Groups=Clone(groups);
        return copy;
    }
    private static void TransferMode(AcademicWorkspace workspace,AcademicCut cut,EvaluationGroup previous,EvaluationGroup next)
    {
        if(previous.Mode==next.Mode)return;
        if(next.Mode==EvaluationMode.SubActivities)
        {
            if(next.Activities.Count>0)CopyDirectGrades(cut,previous.Id,next.Activities[0].Id);
        }
        else
        {
            var grades=workspace.Students.Select(s=>(s.Id,Value:GradeCalculator.Group(cut,previous,s.Id))).ToList();
            cut.Grades.RemoveAll(g=>g.ActivityId==next.Id);
            foreach(var grade in grades)if(grade.Value is {} value)cut.Grades.Add(new(grade.Id,next.Id,value));
        }
    }
    private static void CopyDirectGrades(AcademicCut cut,string groupId,string subactivityId)
    {
        var direct=cut.Grades.Where(g=>g.ActivityId==groupId).ToList();
        cut.Grades.RemoveAll(g=>g.ActivityId==subactivityId);
        foreach(var grade in direct)cut.Grades.Add(new(grade.StudentId,subactivityId,grade.Value));
    }
    public void AddWeek(int number,WeekKind kind=WeekKind.Class) => Change(number,cut=> {
        AcademicRules.ValidateWeekCount(cut.Weeks.Count);if(!Enum.IsDefined(kind))throw new AcademicValidationException("Selecciona semana de clase o actividades.");
        var next=cut.Weeks.Concat(cut.RemovedWeeks).Select(w=>w.Number).DefaultIfEmpty(0).Max()+1;cut.Weeks.Add(new(next,kind));
    });
    public void RemoveWeek(int number,int week)=>Change(number,cut=>{
        var item=cut.Weeks.SingleOrDefault(w=>w.Number==week) ?? throw new AcademicValidationException("La semana no está activa.");cut.Weeks.Remove(item);cut.RemovedWeeks.Add(item);
    });
    public void RestoreWeek(int number,int week)=>Change(number,cut=>{
        AcademicRules.ValidateWeekCount(cut.Weeks.Count);var item=cut.RemovedWeeks.Single(w=>w.Number==week);cut.RemovedWeeks.Remove(item);cut.Weeks.Add(item);cut.Weeks=cut.Weeks.OrderBy(w=>w.Number).ToList();
    });
    public void RemoveActivity(int number,string id)=>Change(number,cut=>{
        var group=cut.Groups.Single(g=>g.Activities.Any(a=>a.Id==id));var item=group.Activities.Single(a=>a.Id==id);group.Activities.Remove(item);cut.RemovedActivities.Add(new(group.Id,item));
    });
    public void RestoreActivity(int number,string id)=>Change(number,cut=>{
        var item=cut.RemovedActivities.Single(x=>x.Activity.Id==id);var group=cut.Groups.SingleOrDefault(g=>g.Id==item.GroupId) ?? throw new AcademicValidationException("Recupera primero el grupo de esta actividad.");
        if(group.Mode==EvaluationMode.Direct)
        {
            var first=new EvaluationActivity{Name=$"{group.Name} 1"};
            CopyDirectGrades(cut,group.Id,first.Id);group.Activities.Add(first);group.Mode=EvaluationMode.SubActivities;
        }
        group.Activities.Add(item.Activity);cut.RemovedActivities.Remove(item);
        AcademicRules.ValidateGroups(cut.Groups,true);
    });
    public void SetAttendance(int number,string studentId,int week,AttendanceState? state)
    {
        StudentExists(studentId);
        if(!Cut(number).Weeks.Any(x=>x.Number==week)) throw new AcademicValidationException("La semana no existe en este corte.");
        if(state is not null && (!Enum.IsDefined(state.Value) || state==AttendanceState.NoClass)) throw new AcademicValidationException("Selecciona P, A, J o T; para períodos sin clase, omite la semana.");
        Change(number,cut=> {cut.Attendance.RemoveAll(x=>x.StudentId==studentId && x.WeekNumber==week);if(state is not null)cut.Attendance.Add(new(studentId,week,state.Value));});
    }
}
