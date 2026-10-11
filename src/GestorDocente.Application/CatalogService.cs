using GestorDocente.Domain;

namespace GestorDocente.Application;

public interface ICatalogStore
{
    IReadOnlyList<AcademicSemester> Semesters();
    IReadOnlyList<CourseRecord> Courses(string semesterId);
    void AddSemester(AcademicSemester semester);
    void AddCourse(CourseRecord course,AcademicWorkspace workspace);
    IWorkspaceStore OpenCourse(string id);
}

public sealed class CatalogService(ICatalogStore store)
{
    public IReadOnlyList<AcademicSemester> Semesters=>store.Semesters();
    public event Action? Changed;
    public IReadOnlyList<CourseRecord> Courses(string semesterId)=>store.Courses(semesterId);
    public AcademicSemester CreateSemester(int year,int term)
    {
        if(year is <1000 or >9999 || term is <1 or >2)throw new AcademicValidationException("Escribe un año de cuatro dígitos y un semestre 1 o 2.");
        if(Semesters.Any(s=>s.Year==year && s.Term==term))throw new AcademicValidationException("Este semestre ya existe. Selecciónalo en la lista.");
        var semester=new AcademicSemester(Guid.NewGuid().ToString("N"),year,term);store.AddSemester(semester);Changed?.Invoke();return semester;
    }
    public CourseRecord CreateCourse(string semesterId,string code,string name,string school,string group)
    {
        var semester=Semesters.SingleOrDefault(s=>s.Id==semesterId) ?? throw new AcademicValidationException("Crea o selecciona primero un semestre.");
        foreach(var (value,label) in new[]{(code,"código"),(name,"nombre de asignatura"),(school,"escuela")})
            if(string.IsNullOrWhiteSpace(value))throw new AcademicValidationException($"El {label} es obligatorio.");
        code=code.Trim();name=name.Trim();school=school.Trim();group=group.Trim();
        if(Courses(semesterId).Any(c=>string.Equals(c.Code,code,StringComparison.OrdinalIgnoreCase) && string.Equals(c.Group,group,StringComparison.OrdinalIgnoreCase)))
            throw new AcademicValidationException("Ya existe este código y grupo en el semestre. Para otra sección, indica un grupo diferente.");
        var course=new CourseRecord(Guid.NewGuid().ToString("N"),semesterId,code,name,school,group);
        var workspace=new AcademicWorkspace {IsDemo=false,CourseId=course.Id,SemesterId=semesterId,CourseName=name,CourseCode=code,School=school,GroupName=group,Period=semester.Name,Cuts=Enumerable.Range(1,3).Select(n=>new AcademicCut{Number=n}).ToList()};
        store.AddCourse(course,workspace);Changed?.Invoke();return course;
    }
    public WorkspaceService Open(string courseId)=>new(store.OpenCourse(courseId));
}
