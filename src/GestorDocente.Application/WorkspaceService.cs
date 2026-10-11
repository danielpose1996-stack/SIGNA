using System.Text.Json;
using GestorDocente.Domain;

namespace GestorDocente.Application;

public enum WorkspaceChangeKind { Structure, Grade, Attendance, Students }
public sealed record WorkspaceChange(WorkspaceChangeKind Kind,int Cut=0,string? StudentId=null);

public sealed class WorkspaceService
{
    private readonly IWorkspaceStore store;
    private bool configurationSaving;
    public bool IsSaving=>configurationSaving;
    public AcademicWorkspace Current { get; private set; }
    public event Action? Changed;
    public event Action<WorkspaceChange>? ChangedDetailed;
    public event Action<AcademicRefreshException>? RefreshFailed;
    public WorkspaceService(IWorkspaceStore store)
    {
        this.store=store;
        var loaded=store.Load(); var candidate=loaded ?? DemoWorkspaceFactory.Create();
        if(candidate.SchemaVersion is not (1 or 2) || candidate.Cuts is null || !candidate.Cuts.Select(x=>x?.Number).Order().SequenceEqual(new int?[]{1,2,3}))
            throw new AcademicValidationException("La versión o los cortes de la materia no son compatibles. Los datos no se reemplazaron.");
        var migrating=candidate.SchemaVersion==1;
        if(migrating)
        {
            WorkspaceIntegrity.ValidateLegacyShape(candidate);
            candidate=Clone(candidate);
            foreach(var group in candidate.Cuts.SelectMany(c=>c.Groups.Concat(c.RemovedGroups)))
                group.Mode=group.Activities.Count>0 ? EvaluationMode.SubActivities : EvaluationMode.Direct;
            candidate.SchemaVersion=2;
        }
        WorkspaceIntegrity.Validate(candidate);
        if(loaded is null || migrating)store.Save(candidate);
        Current=candidate;
    }
    public AcademicCut Cut(int number) => Current.Cuts.SingleOrDefault(x=>x.Number==number) ?? throw new AcademicValidationException("Selecciona un corte entre 1 y 3.");
    public EvaluationActivity Activity(int number,string id) => Cut(number).Groups.SelectMany(x=>x.Activities).Single(x=>x.Id==id);
    public static T Clone<T>(T value) => JsonSerializer.Deserialize<T>(JsonSerializer.Serialize(value))!;
    private void Change(int number,Action<AcademicCut> action,WorkspaceChange change)
    {
        EnsureWritable();
        _=Cut(number);
        var copy=Clone(Current); action(copy.Cuts.Single(x=>x.Number==number));
        WorkspaceIntegrity.Validate(copy);
        store.Save(copy); Current=copy; Publish(change);
    }
    private void Publish(WorkspaceChange change)
    {
        List<Exception> failures=[];
        if(ChangedDetailed is { } detailed)
            foreach(Action<WorkspaceChange> handler in detailed.GetInvocationList())
                try{handler(change);}catch(Exception ex){failures.Add(ex);}
        if(Changed is { } general)
            foreach(Action handler in general.GetInvocationList())
                try{handler();}catch(Exception ex){failures.Add(ex);}
        if(failures.Count>0)
        {
            var problem=new AcademicRefreshException(new AggregateException(failures));
            if(RefreshFailed is { } refresh)
                foreach(Action<AcademicRefreshException> handler in refresh.GetInvocationList())
                    try{handler(problem);}catch(Exception ex){failures.Add(ex);}
            throw problem;
        }
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
        var copy=PrepareStudents(Current,candidates);store.Save(copy);Current=copy;Publish(new(WorkspaceChangeKind.Students));
    }
    private static AcademicWorkspace PrepareStudents(AcademicWorkspace source,IReadOnlyList<StudentImportCandidate> candidates)
    {
        if(candidates.Count==0)throw new AcademicValidationException("Selecciona al menos un estudiante nuevo.");
        if(candidates.Count+source.Students.Count>5000)throw new AcademicValidationException("La materia admite hasta 5000 estudiantes.");
        var seen=source.Students.Select(s=>s.Document.Trim()).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var additions=new List<Student>();
        foreach(var candidate in candidates)
        {
            var doc=candidate.Document.Trim();var name=candidate.Name.Trim();
            if(doc.Length is 0 or >80 || name.Length is 0 or >240)throw new AcademicValidationException("Cada estudiante requiere documento y nombre válidos.");
            if(!seen.Add(doc))throw new AcademicValidationException("Hay documentos repetidos o ya registrados. Actualiza la vista previa.");
            additions.Add(new(Guid.NewGuid().ToString("N"),doc,name));
        }
        var copy=Clone(source);copy.Students.AddRange(additions);WorkspaceIntegrity.Validate(copy);return copy;
    }
    public async Task ImportStudentsAsync(IReadOnlyList<StudentImportCandidate> candidates)
    {
        EnsureWritable();var source=Current;var batch=candidates.ToArray();configurationSaving=true;
        try
        {
            var copy=await Task.Run(()=>{var next=PrepareStudents(source,batch);store.Save(next);return next;});
            Current=copy;
        }
        finally{configurationSaving=false;}
        Publish(new(WorkspaceChangeKind.Students));
    }
    public void SetGrade(int number,string studentId,string activityId,decimal? value)
    {
        StudentExists(studentId);
        if(!Cut(number).Groups.Any(g=>g.Mode==EvaluationMode.Direct ? g.Id==activityId : g.Activities.Any(a=>a.Id==activityId)))
            throw new AcademicValidationException("La actividad o subactividad no está activa en este corte.");
        if(value is not null) AcademicRules.ValidateGrade(value.Value);
        if(Cut(number).Grades.SingleOrDefault(x=>x.StudentId==studentId && x.ActivityId==activityId)?.Value==value)return;
        Change(number,cut=> {cut.Grades.RemoveAll(x=>x.StudentId==studentId && x.ActivityId==activityId); if(value is not null) cut.Grades.Add(new(studentId,activityId,value.Value));},new(WorkspaceChangeKind.Grade,number,studentId));
    }
    public Task SetGradeAsync(int number,string studentId,string activityId,decimal? value)
    {
        StudentExists(studentId);
        if(!Cut(number).Groups.Any(g=>g.Mode==EvaluationMode.Direct ? g.Id==activityId : g.Activities.Any(a=>a.Id==activityId)))
            throw new AcademicValidationException("La actividad o subactividad no está activa en este corte.");
        if(value is not null)AcademicRules.ValidateGrade(value.Value);
        if(Cut(number).Grades.SingleOrDefault(x=>x.StudentId==studentId && x.ActivityId==activityId)?.Value==value)return Task.CompletedTask;
        return ChangeAsync(number,cut=>{cut.Grades.RemoveAll(x=>x.StudentId==studentId && x.ActivityId==activityId);if(value is not null)cut.Grades.Add(new(studentId,activityId,value.Value));},new(WorkspaceChangeKind.Grade,number,studentId));
    }
    private async Task ChangeAsync(int number,Action<AcademicCut> action,WorkspaceChange change)
    {
        EnsureWritable();_=Cut(number);var source=Current;configurationSaving=true;
        try
        {
            var copy=await Task.Run(()=>{var next=Clone(source);action(next.Cuts.Single(x=>x.Number==number));WorkspaceIntegrity.Validate(next);store.Save(next);return next;});
            Current=copy;
        }
        finally{configurationSaving=false;}
        Publish(change);
    }
    public void ConfigureGroups(int number,List<EvaluationGroup> groups)
    {
        EnsureWritable();
        var copy=PrepareGroups(Current,number,groups);
        WorkspaceIntegrity.Validate(copy);store.Save(copy);Current=copy;Publish(new(WorkspaceChangeKind.Structure,number));
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
            var copy=await Task.Run(()=>{var next=PrepareGroups(source,number,configuration);WorkspaceIntegrity.Validate(next);store.Save(next);return next;});
            Current=copy;
        }
        finally{configurationSaving=false;}
        Publish(new(WorkspaceChangeKind.Structure,number));
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
    },new(WorkspaceChangeKind.Structure,number));
    public Task AddWeekAsync(int number,WeekKind kind=WeekKind.Class)=>ChangeAsync(number,cut=>{
        AcademicRules.ValidateWeekCount(cut.Weeks.Count);if(!Enum.IsDefined(kind))throw new AcademicValidationException("Selecciona semana de clase o actividades.");
        var next=cut.Weeks.Concat(cut.RemovedWeeks).Select(w=>w.Number).DefaultIfEmpty(0).Max()+1;cut.Weeks.Add(new(next,kind));
    },new(WorkspaceChangeKind.Structure,number));
    public void RemoveWeek(int number,int week)=>Change(number,cut=>{
        var item=cut.Weeks.SingleOrDefault(w=>w.Number==week) ?? throw new AcademicValidationException("La semana no está activa.");cut.Weeks.Remove(item);cut.RemovedWeeks.Add(item);
    },new(WorkspaceChangeKind.Structure,number));
    public Task RemoveWeekAsync(int number,int week)=>ChangeAsync(number,cut=>{
        var item=cut.Weeks.SingleOrDefault(w=>w.Number==week) ?? throw new AcademicValidationException("La semana no está activa.");cut.Weeks.Remove(item);cut.RemovedWeeks.Add(item);
    },new(WorkspaceChangeKind.Structure,number));
    public void RestoreWeek(int number,int week)=>Change(number,cut=>{
        AcademicRules.ValidateWeekCount(cut.Weeks.Count);var item=cut.RemovedWeeks.Single(w=>w.Number==week);cut.RemovedWeeks.Remove(item);cut.Weeks.Add(item);cut.Weeks=cut.Weeks.OrderBy(w=>w.Number).ToList();
    },new(WorkspaceChangeKind.Structure,number));
    public Task RestoreWeekAsync(int number,int week)=>ChangeAsync(number,cut=>{
        AcademicRules.ValidateWeekCount(cut.Weeks.Count);var item=cut.RemovedWeeks.SingleOrDefault(w=>w.Number==week) ?? throw new AcademicValidationException("La semana retirada no existe.");
        cut.RemovedWeeks.Remove(item);cut.Weeks.Add(item);cut.Weeks=cut.Weeks.OrderBy(w=>w.Number).ToList();
    },new(WorkspaceChangeKind.Structure,number));
    public void RemoveActivity(int number,string id)=>Change(number,cut=>{
        var group=cut.Groups.Single(g=>g.Activities.Any(a=>a.Id==id));var item=group.Activities.Single(a=>a.Id==id);group.Activities.Remove(item);cut.RemovedActivities.Add(new(group.Id,item));
    },new(WorkspaceChangeKind.Structure,number));
    public void RestoreActivity(int number,string id)=>Change(number,cut=>{
        var item=cut.RemovedActivities.Single(x=>x.Activity.Id==id);var group=cut.Groups.SingleOrDefault(g=>g.Id==item.GroupId) ?? throw new AcademicValidationException("Recupera primero el grupo de esta actividad.");
        if(group.Mode==EvaluationMode.Direct)
        {
            var first=new EvaluationActivity{Name=$"{group.Name} 1"};
            CopyDirectGrades(cut,group.Id,first.Id);group.Activities.Add(first);group.Mode=EvaluationMode.SubActivities;
        }
        group.Activities.Add(item.Activity);cut.RemovedActivities.Remove(item);
        AcademicRules.ValidateGroups(cut.Groups,true);
    },new(WorkspaceChangeKind.Structure,number));
    public Task RestoreActivityAsync(int number,string id)=>ChangeAsync(number,cut=>{
        var item=cut.RemovedActivities.SingleOrDefault(x=>x.Activity.Id==id) ?? throw new AcademicValidationException("La subactividad retirada no existe.");
        var group=cut.Groups.SingleOrDefault(g=>g.Id==item.GroupId) ?? throw new AcademicValidationException("Recupera primero el grupo de esta actividad.");
        if(group.Mode==EvaluationMode.Direct)
        {
            var first=new EvaluationActivity{Name=$"{group.Name} 1"};
            CopyDirectGrades(cut,group.Id,first.Id);group.Activities.Add(first);group.Mode=EvaluationMode.SubActivities;
        }
        group.Activities.Add(item.Activity);cut.RemovedActivities.Remove(item);
        AcademicRules.ValidateGroups(cut.Groups,true);
    },new(WorkspaceChangeKind.Structure,number));
    public void SetAttendance(int number,string studentId,int week,AttendanceState? state)
    {
        StudentExists(studentId);
        if(!Cut(number).Weeks.Any(x=>x.Number==week)) throw new AcademicValidationException("La semana no existe en este corte.");
        if(state is not null && (!Enum.IsDefined(state.Value) || state==AttendanceState.NoClass)) throw new AcademicValidationException("Selecciona P, A, J o T; para períodos sin clase, omite la semana.");
        if(Cut(number).Attendance.SingleOrDefault(x=>x.StudentId==studentId && x.WeekNumber==week)?.State==state)return;
        Change(number,cut=> {cut.Attendance.RemoveAll(x=>x.StudentId==studentId && x.WeekNumber==week);if(state is not null)cut.Attendance.Add(new(studentId,week,state.Value));},new(WorkspaceChangeKind.Attendance,number,studentId));
    }
    public Task SetAttendanceAsync(int number,string studentId,int week,AttendanceState? state)
    {
        StudentExists(studentId);
        if(!Cut(number).Weeks.Any(x=>x.Number==week))throw new AcademicValidationException("La semana no existe en este corte.");
        if(state is not null && (!Enum.IsDefined(state.Value) || state==AttendanceState.NoClass))throw new AcademicValidationException("Selecciona P, A, J o T; para períodos sin clase, omite la semana.");
        if(Cut(number).Attendance.SingleOrDefault(x=>x.StudentId==studentId && x.WeekNumber==week)?.State==state)return Task.CompletedTask;
        return ChangeAsync(number,cut=>{cut.Attendance.RemoveAll(x=>x.StudentId==studentId && x.WeekNumber==week);if(state is not null)cut.Attendance.Add(new(studentId,week,state.Value));},new(WorkspaceChangeKind.Attendance,number,studentId));
    }
}
