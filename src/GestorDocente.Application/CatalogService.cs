using GestorDocente.Domain;

namespace GestorDocente.Application;

public interface ICatalogStore
{
    IReadOnlyList<AcademicSemester> Semesters();
    IReadOnlyList<CourseRecord> Courses(string semesterId);
    void AddSemester(AcademicSemester semester);
    void AddCourse(CourseRecord course,AcademicWorkspace workspace);
    void UpdateCourse(CourseRecord original,CourseRecord updated);
    void DeleteCourse(string courseId);
    SemesterDeletionPreview InspectSemesterDeletion(string semesterId);
    void DeleteSemester(SemesterDeletionPreview confirmed);
    IWorkspaceStore OpenCourse(string id);
}

public sealed class CatalogService(ICatalogStore store)
{
    public IReadOnlyList<AcademicSemester> Semesters=>store.Semesters();
    public event Action? Changed;
    public IReadOnlyList<CourseRecord> Courses(string semesterId)=>store.Courses(semesterId);
    public AcademicSemester CreateSemester(int year,int term)
    {
        var semester=CreateSemesterCore(year,term);Publish(semester);return semester;
    }
    public async Task<AcademicSemester> CreateSemesterAsync(int year,int term)
    {
        var semester=await Task.Run(()=>CreateSemesterCore(year,term));Publish(semester);return semester;
    }
    private AcademicSemester CreateSemesterCore(int year,int term)
    {
        if(year is <1000 or >9999 || term is <1 or >2)throw new AcademicValidationException("Escribe un año de cuatro dígitos y un semestre 1 o 2.");
        if(Semesters.Any(s=>s.Year==year && s.Term==term))throw new AcademicValidationException("Este semestre ya existe. Selecciónalo en la lista.");
        var semester=new AcademicSemester(Guid.NewGuid().ToString("N"),year,term);store.AddSemester(semester);return semester;
    }
    public CourseRecord CreateCourse(string semesterId,string code,string name,string school,string group)
    {
        var course=CreateCourseCore(semesterId,code,name,school,group);Publish(course);return course;
    }
    public async Task<CourseRecord> CreateCourseAsync(string semesterId,string code,string name,string school,string group)
    {
        var course=await Task.Run(()=>CreateCourseCore(semesterId,code,name,school,group));Publish(course);return course;
    }
    private CourseRecord CreateCourseCore(string semesterId,string code,string name,string school,string group)
    {
        var semester=Semesters.SingleOrDefault(s=>s.Id==semesterId) ?? throw new AcademicValidationException("Crea o selecciona primero un semestre.");
        foreach(var (value,label) in new[]{(code,"código"),(name,"nombre de asignatura"),(school,"escuela")})
            if(string.IsNullOrWhiteSpace(value))throw new AcademicValidationException($"El {label} es obligatorio.");
        code=code.Trim();name=name.Trim();school=school.Trim();group=group.Trim();
        if(Courses(semesterId).Any(c=>string.Equals(c.Code,code,StringComparison.OrdinalIgnoreCase) && string.Equals(c.Group,group,StringComparison.OrdinalIgnoreCase)))
            throw new AcademicValidationException("Ya existe este código y grupo en el semestre. Para otra sección, indica un grupo diferente.");
        var course=new CourseRecord(Guid.NewGuid().ToString("N"),semesterId,code,name,school,group);
        var workspace=new AcademicWorkspace {IsDemo=false,CourseId=course.Id,SemesterId=semesterId,CourseName=name,CourseCode=code,School=school,GroupName=group,Period=semester.Name,Cuts=AcademicRules.DefaultCuts()};
        store.AddCourse(course,workspace);return course;
    }
    public CourseRecord UpdateCourse(CourseRecord original,string semesterId,string code,string name,string school,string group)
    {
        var updated=UpdateCourseCore(original,semesterId,code,name,school,group);Publish(updated);return updated;
    }
    public async Task<CourseRecord> UpdateCourseAsync(CourseRecord original,string semesterId,string code,string name,string school,string group)
    {
        var updated=await Task.Run(()=>UpdateCourseCore(original,semesterId,code,name,school,group));Publish(updated);return updated;
    }
    private CourseRecord UpdateCourseCore(CourseRecord original,string semesterId,string code,string name,string school,string group)
    {
        if(string.IsNullOrWhiteSpace(original.Id) || original.StudentCount<0)
            throw new AcademicValidationException("Selecciona una materia válida para editar.");
        if(!Semesters.Any(s=>s.Id==semesterId))throw new AcademicValidationException("Selecciona un semestre existente.");
        foreach(var (value,label) in new[]{(code,"código"),(name,"nombre de asignatura"),(school,"escuela")})
            if(string.IsNullOrWhiteSpace(value))throw new AcademicValidationException($"El {label} es obligatorio.");
        code=code.Trim();name=name.Trim();school=school.Trim();group=group.Trim();
        if(Courses(semesterId).Any(c=>c.Id!=original.Id &&
            string.Equals(c.Code,code,StringComparison.OrdinalIgnoreCase) &&
            string.Equals(c.Group,group,StringComparison.OrdinalIgnoreCase)))
            throw new AcademicValidationException("Ya existe este código y grupo en el semestre. Indica un grupo diferente.");
        var updated=original with{SemesterId=semesterId,Code=code,Name=name,School=school,Group=group};
        store.UpdateCourse(original,updated);return updated;
    }
    public void DeleteCourse(string courseId)
    {
        if(string.IsNullOrWhiteSpace(courseId))throw new AcademicValidationException("Selecciona una materia válida.");
        store.DeleteCourse(courseId);Publish(courseId);
    }
    public async Task DeleteCourseAsync(string courseId)
    {
        if(string.IsNullOrWhiteSpace(courseId))throw new AcademicValidationException("Selecciona una materia válida.");
        await Task.Run(()=>store.DeleteCourse(courseId));Publish(courseId);
    }
    public SemesterDeletionPreview InspectSemesterDeletion(string semesterId)
    {
        if(string.IsNullOrWhiteSpace(semesterId))throw new AcademicValidationException("Selecciona un semestre válido.");
        return store.InspectSemesterDeletion(semesterId);
    }
    public Task<SemesterDeletionPreview> InspectSemesterDeletionAsync(string semesterId)=>Task.Run(()=>InspectSemesterDeletion(semesterId));
    public void DeleteSemester(SemesterDeletionPreview confirmed)
    {
        store.DeleteSemester(confirmed);Publish(confirmed);
    }
    public async Task DeleteSemesterAsync(SemesterDeletionPreview confirmed)
    {
        await Task.Run(()=>store.DeleteSemester(confirmed));Publish(confirmed);
    }
    private void Publish(object created)
    {
        if(Changed is not { } changed)return;
        List<Exception> failures=[];
        foreach(Action handler in changed.GetInvocationList())try{handler();}catch(Exception ex){failures.Add(ex);}
        if(failures.Count>0)throw new AcademicRefreshException(new AggregateException(failures),created);
    }
    public WorkspaceService Open(string courseId)=>new(store.OpenCourse(courseId));
}
