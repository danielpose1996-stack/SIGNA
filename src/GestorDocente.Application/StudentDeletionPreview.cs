using GestorDocente.Domain;

namespace GestorDocente.Application;
public sealed class StudentDeletionPreview
{
    internal AcademicWorkspace Source{get;}
    public Student Student{get;}
    public int GradeCount{get;}
    public int AttendanceCount{get;}
    internal StudentDeletionPreview(AcademicWorkspace source,Student student)
    {
        Source=source;Student=student;
        GradeCount=source.Cuts.Sum(c=>c.Grades.Count(g=>g.StudentId==student.Id));
        AttendanceCount=source.Cuts.Sum(c=>c.Attendance.Count(a=>a.StudentId==student.Id));
    }
}
