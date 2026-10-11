using GestorDocente.Application;
using GestorDocente.Infrastructure;
using GestorDocente.Domain;
using Microsoft.Data.Sqlite;
using Xunit;

namespace GestorDocente.Infrastructure.Tests;

public class SqliteWorkspaceStoreTests
{
    [Fact]
    public void Reopening_database_preserves_grades_rubrics_and_attendance()
    {
        var path=Path.Combine(Path.GetTempPath(),Guid.NewGuid()+".db");
        try {
            var service=new WorkspaceService(new SqliteWorkspaceStore(path));
            service.SetGrade(2,"s1","t2",4.75m);
            service.SetAttendance(2,"s1",1,AttendanceState.Late);
            var reopened=new WorkspaceService(new SqliteWorkspaceStore(path));
            Assert.Equal(4.75m,reopened.Cut(2).Grades.Single(x=>x.StudentId=="s1" && x.ActivityId=="t2").Value);
            Assert.Equal(AttendanceState.Late,reopened.Cut(2).Attendance.Single(x=>x.StudentId=="s1" && x.WeekNumber==1).State);
            Assert.NotNull(reopened.Activity(2,"t1").Rubric);
        } finally {SqliteConnection.ClearAllPools(); if(File.Exists(path))File.Delete(path);}
    }
}
