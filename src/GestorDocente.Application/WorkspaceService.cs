using System.Text.Json;
using GestorDocente.Domain;

namespace GestorDocente.Application;

public enum WorkspaceChangeKind { Structure, Grade, Attendance, Students }
public sealed record WorkspaceChange(WorkspaceChangeKind Kind,int Cut=0,string? StudentId=null,string? ActivityId=null,int? Week=null,decimal? Grade=null,AttendanceState? Attendance=null,int? RemovedCut=null);

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
        var migrating=candidate.SchemaVersion<AcademicWorkspace.CurrentSchemaVersion;
        candidate=WorkspaceMigration.Upgrade(candidate);
        if(loaded is null || migrating)store.Save(candidate);
        Current=candidate;
    }
    public AcademicCut Cut(int number) => Current.Cuts.SingleOrDefault(x=>x.Number==number) ?? throw new AcademicValidationException("Selecciona un corte existente en esta materia.");
    public async Task<int> AddCutAsync()
    {
        var number=Current.Cuts.Max(c=>c.Number)+1;
        await ChangeWorkspaceAsync(next=>next.Cuts.Add(new(){Number=number,Weight=0}));
        return number;
    }
    public CutDeletionPreview PreviewCutDeletion(int number)
    {
        EnsureWritable();return new(Current,Cut(number));
    }
    public Task DeleteCutAsync(CutDeletionPreview preview,bool confirmed=false)
    {
        if(!confirmed)throw new AcademicValidationException("Confirma la eliminación del corte antes de continuar.");
        if(!ReferenceEquals(preview.Source,Current))
            throw new AcademicValidationException("La materia cambió después de mostrar la confirmación. Revisa de nuevo el corte que deseas eliminar.");
        if(Current.Cuts.Count<=1)throw new AcademicValidationException("No puedes eliminar el último corte de una materia.");
        return ChangeWorkspaceAsync(next=>
        {
            var cut=next.Cuts.SingleOrDefault(c=>c.Number==preview.Number)
                ?? throw new AcademicValidationException("El corte ya no existe. Actualiza la materia.");
            next.Cuts.Remove(cut);
            next.Cuts=next.Cuts.OrderBy(c=>c.Number).ToList();
            for(var i=0;i<next.Cuts.Count;i++)next.Cuts[i].Number=i+1;
            next.CutWeightsPending=next.Cuts.Sum(c=>c.Weight)!=100;
        },new(WorkspaceChangeKind.Structure,RemovedCut:preview.Number));
    }
    public Task ConfigureCutWeightsAsync(IReadOnlyDictionary<int,decimal> weights)
    {
        var draft=weights.ToDictionary(p=>p.Key,p=>p.Value);
        return ChangeWorkspaceAsync(next=>
        {
            if(!next.Cuts.Select(c=>c.Number).Order().SequenceEqual(draft.Keys.Order()))
                throw new AcademicValidationException("Configura los porcentajes de todos los cortes existentes. Vuelve a abrir la configuración.");
            AcademicRules.ValidateCutWeights(draft.Values);
            foreach(var cut in next.Cuts)cut.Weight=draft[cut.Number];
            next.CutWeightsPending=false;
        });
    }
    private async Task ChangeWorkspaceAsync(Action<AcademicWorkspace> action,WorkspaceChange? change=null)
    {
        EnsureWritable();var source=Current;configurationSaving=true;
        try
        {
            var next=await Task.Run(()=>{var copy=Clone(source);action(copy);WorkspaceIntegrity.Validate(copy);store.Save(copy);return copy;});
            Current=next;
        }
        finally{configurationSaving=false;}
        Publish(change ?? new(WorkspaceChangeKind.Structure));
    }
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
        if(candidates.Count>5000)throw new AcademicValidationException("La materia admite hasta 5000 estudiantes.");
        var seen=new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var copy=Clone(source);
        var existingPositions=copy.Students.Select((s,i)=>(Student:s,Index:i)).Where(p=>!string.IsNullOrWhiteSpace(p.Student.Document)).ToDictionary(p=>p.Student.Document.Trim(),p=>p.Index,StringComparer.OrdinalIgnoreCase);
        foreach(var candidate in candidates)
        {
            var doc=candidate.Document.Trim();var name=candidate.Name.Trim();
            var student=new Student(Guid.NewGuid().ToString("N"),doc,name,candidate.Email.Trim(),candidate.Phone.Trim());StudentRules.Validate(student);
            if(doc.Length>0 && !seen.Add(doc))throw new AcademicValidationException("Hay documentos repetidos en el archivo. Actualiza la vista previa.");
            var existing=doc.Length>0 && existingPositions.TryGetValue(doc,out var position) ? position : -1;
            if(existing>=0)
            {
                if(student.Email.Length==0 && student.Phone.Length==0)throw new AcademicValidationException("El documento ya está registrado. Usa otro documento o actualiza sus contactos.");
                var prior=copy.Students[existing];copy.Students[existing]=prior with {Email=student.Email.Length>0 ? student.Email : prior.Email,Phone=student.Phone.Length>0 ? student.Phone : prior.Phone};
            }
            else copy.Students.Add(student);
        }
        if(copy.Students.Count>5000)throw new AcademicValidationException("La materia admite hasta 5000 estudiantes.");
        WorkspaceIntegrity.Validate(copy);return copy;
    }
    public Task AddStudentAsync(string name,string document="",string email="",string phone="")
    {
        if(document.Trim().Length>0 && Current.Students.Any(s=>string.Equals(s.Document,document.Trim(),StringComparison.OrdinalIgnoreCase)))throw new AcademicValidationException("Este documento ya pertenece a la materia.");
        return ImportStudentsAsync([new(document,name,email,phone)]);
    }
    public Task EditStudentAsync(Student original,string name,string document="",string email="",string phone="")
    {
        return ChangeWorkspaceAsync(next=>
        {
            var index=next.Students.FindIndex(s=>s.Id==original.Id);
            if(index<0 || next.Students[index]!=original)
                throw new AcademicValidationException("La información del estudiante cambió. Cierra y vuelve a abrir sus detalles antes de editar.");
            var edited=original with{FullName=name.Trim(),Document=document.Trim(),Email=email.Trim(),Phone=phone.Trim()};
            StudentRules.Validate(edited);
            if(edited.Document.Length>0 && next.Students.Any(s=>s.Id!=original.Id && string.Equals(s.Document.Trim(),edited.Document,StringComparison.OrdinalIgnoreCase)))
                throw new AcademicValidationException("Este documento ya pertenece a otro estudiante de la materia.");
            next.Students[index]=edited;
        },new(WorkspaceChangeKind.Students,StudentId:original.Id));
    }
    public StudentDeletionPreview PreviewStudentDeletion(string id)
    {
        EnsureWritable();var student=Current.Students.SingleOrDefault(s=>s.Id==id)
            ?? throw new AcademicValidationException("El estudiante ya no pertenece a esta materia.");
        return new(Current,student);
    }
    public Task DeleteStudentAsync(StudentDeletionPreview preview,bool confirmed=false)
    {
        if(!confirmed)throw new AcademicValidationException("Confirma la eliminación del estudiante antes de continuar.");
        if(!ReferenceEquals(preview.Source,Current))
            throw new AcademicValidationException("La materia cambió después de la confirmación. Revisa de nuevo el estudiante que deseas eliminar.");
        return ChangeWorkspaceAsync(next=>
        {
            if(next.Students.RemoveAll(s=>s.Id==preview.Student.Id)!=1)
                throw new AcademicValidationException("El estudiante ya no pertenece a esta materia.");
            foreach(var cut in next.Cuts)
            {
                cut.Grades.RemoveAll(g=>g.StudentId==preview.Student.Id);
                cut.Attendance.RemoveAll(a=>a.StudentId==preview.Student.Id);
            }
        },new(WorkspaceChangeKind.Students,StudentId:preview.Student.Id));
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
        Change(number,cut=> {cut.Grades.RemoveAll(x=>x.StudentId==studentId && x.ActivityId==activityId); if(value is not null) cut.Grades.Add(new(studentId,activityId,value.Value));},new(WorkspaceChangeKind.Grade,number,studentId,ActivityId:activityId,Grade:value));
    }
    public Task SetGradeAsync(int number,string studentId,string activityId,decimal? value)
    {
        StudentExists(studentId);
        if(!Cut(number).Groups.Any(g=>g.Mode==EvaluationMode.Direct ? g.Id==activityId : g.Activities.Any(a=>a.Id==activityId)))
            throw new AcademicValidationException("La actividad o subactividad no está activa en este corte.");
        if(value is not null)AcademicRules.ValidateGrade(value.Value);
        return ChangeAsync(number,cut=>{cut.Grades.RemoveAll(x=>x.StudentId==studentId && x.ActivityId==activityId);if(value is not null)cut.Grades.Add(new(studentId,activityId,value.Value));},
            new(WorkspaceChangeKind.Grade,number,studentId,ActivityId:activityId,Grade:value),
            cut=>cut.Grades.SingleOrDefault(x=>x.StudentId==studentId && x.ActivityId==activityId)?.Value==value);
    }
    private async Task ChangeAsync(int number,Action<AcademicCut> action,WorkspaceChange change,Func<AcademicCut,bool>? unchanged=null)
    {
        EnsureWritable();_=Cut(number);var source=Current;configurationSaving=true;
        try
        {
            var result=await Task.Run(()=>
            {
                if(unchanged?.Invoke(source.Cuts.Single(x=>x.Number==number))==true)return (Next:source,Changed:false);
                var next=Clone(source);action(next.Cuts.Single(x=>x.Number==number));WorkspaceIntegrity.Validate(next);store.Save(next);
                return (Next:next,Changed:true);
            });
            if(!result.Changed)return;
            Current=result.Next;
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
        var copy=Clone(source);var cut=copy.Cuts.SingleOrDefault(c=>c.Number==number) ?? throw new AcademicValidationException("Selecciona un corte existente en esta materia.");
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
            var index=new AcademicCutReadIndex(cut);
            // El cero de una celda pendiente solo participa en cálculos; no se materializa al cambiar de modalidad.
            var grades=workspace.Students.Select(s=>(s.Id,Value:index.HasPendingGrades(previous,s.Id) ? null : index.Group(previous,s.Id))).ToList();
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
    public void AddWeek(int number,WeekKind kind=WeekKind.Class,DateOnly? date=null,bool markAllPresent=false)=>
        Change(number,cut=>CreateWeek(cut,kind,date,markAllPresent,Current.Students),new(WorkspaceChangeKind.Structure,number));
    public Task AddWeekAsync(int number,WeekKind kind=WeekKind.Class,DateOnly? date=null,bool markAllPresent=false)=>
        ChangeAsync(number,cut=>CreateWeek(cut,kind,date,markAllPresent,Current.Students),new(WorkspaceChangeKind.Structure,number));
    private static void CreateWeek(AcademicCut cut,WeekKind kind,DateOnly? date,bool markAllPresent,IReadOnlyList<Student> students)
    {
        AcademicRules.ValidateWeekCount(cut.Weeks.Count);if(!Enum.IsDefined(kind))throw new AcademicValidationException("Selecciona semana de clase o actividades.");
        var next=cut.Weeks.Concat(cut.RemovedWeeks).Select(w=>w.Number).DefaultIfEmpty(0).Max()+1;cut.Weeks.Add(new(next,kind,date));
        if(markAllPresent)cut.Attendance.AddRange(students.Select(student=>new AttendanceEntry(student.Id,next,AttendanceState.Present)));
    }
    public Task EditWeekAsync(int number,int week,WeekKind kind,DateOnly? date)=>ChangeAsync(number,cut=>
    {
        if(!Enum.IsDefined(kind))throw new AcademicValidationException("Selecciona semana de clase o actividades.");
        var position=cut.Weeks.FindIndex(w=>w.Number==week);
        if(position<0)throw new AcademicValidationException("La semana ya no está activa. Actualiza la materia.");
        cut.Weeks[position]=cut.Weeks[position] with {Kind=kind,Date=date};
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
        Change(number,cut=> {cut.Attendance.RemoveAll(x=>x.StudentId==studentId && x.WeekNumber==week);if(state is not null)cut.Attendance.Add(new(studentId,week,state.Value));},new(WorkspaceChangeKind.Attendance,number,studentId,Week:week,Attendance:state));
    }
    public Task SetAttendanceAsync(int number,string studentId,int week,AttendanceState? state)
    {
        StudentExists(studentId);
        if(!Cut(number).Weeks.Any(x=>x.Number==week))throw new AcademicValidationException("La semana no existe en este corte.");
        if(state is not null && (!Enum.IsDefined(state.Value) || state==AttendanceState.NoClass))throw new AcademicValidationException("Selecciona P, A, J o T; para períodos sin clase, omite la semana.");
        return ChangeAsync(number,cut=>{cut.Attendance.RemoveAll(x=>x.StudentId==studentId && x.WeekNumber==week);if(state is not null)cut.Attendance.Add(new(studentId,week,state.Value));},
            new(WorkspaceChangeKind.Attendance,number,studentId,Week:week,Attendance:state),
            cut=>cut.Attendance.SingleOrDefault(x=>x.StudentId==studentId && x.WeekNumber==week)?.State==state);
    }
}
