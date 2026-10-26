using GestorDocente.Domain;

namespace GestorDocente.Application;

/// <summary>A confirmation applies to exactly one committed course snapshot, never a filtered view.</summary>
public sealed class StudentListDeletionPreview
{
    internal AcademicWorkspace Source{get;}
    public string CourseName=>Source.CourseName;
    public string CourseCode=>Source.CourseCode;
    public string Period=>Source.Period;
    public string Group=>Source.GroupName;
    public int StudentCount{get;}
    public int GradeCount{get;}
    public int AttendanceCount{get;}
    internal StudentListDeletionPreview(AcademicWorkspace source)
    {
        Source=source;StudentCount=source.Students.Count;
        GradeCount=source.Cuts.Sum(c=>c.Grades.Count);
        AttendanceCount=source.Cuts.Sum(c=>c.Attendance.Count);
    }
}
