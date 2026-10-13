using GestorDocente.Domain;

namespace GestorDocente.Application;

public sealed record CourseRevision(string Id,long Revision);
public sealed record SemesterDeletionPreview(AcademicSemester Semester,IReadOnlyList<CourseRevision> Courses,
    int StudentCount,int GradeCount,int AttendanceCount)
{
    public int CourseCount=>Courses.Count;
}
