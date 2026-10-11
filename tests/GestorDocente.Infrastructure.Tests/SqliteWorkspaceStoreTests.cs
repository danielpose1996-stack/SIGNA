using GestorDocente.Application;
using GestorDocente.Infrastructure;
using GestorDocente.Domain;
using Microsoft.Data.Sqlite;
using Xunit;

namespace GestorDocente.Infrastructure.Tests;

public class SqliteWorkspaceStoreTests
{
    [Fact]
    public void Reopening_database_preserves_modes_grades_and_attendance()
    {
        var path=Path.Combine(Path.GetTempPath(),Guid.NewGuid()+".db");
        try {
            var service=new WorkspaceService(new SqliteWorkspaceStore(path));
            service.SetGrade(2,"s1","t2",4.75m);
            service.SetAttendance(2,"s1",1,AttendanceState.Late);
            var reopened=new WorkspaceService(new SqliteWorkspaceStore(path));
            Assert.Equal(4.75m,reopened.Cut(2).Grades.Single(x=>x.StudentId=="s1" && x.ActivityId=="t2").Value);
            Assert.Equal(AttendanceState.Late,reopened.Cut(2).Attendance.Single(x=>x.StudentId=="s1" && x.WeekNumber==1).State);
            Assert.Equal(EvaluationMode.SubActivities,reopened.Cut(2).Groups[0].Mode);
        } finally {SqliteConnection.ClearAllPools(); if(File.Exists(path))File.Delete(path);}
    }
    [Fact]
    public void Second_demo_instance_cannot_overwrite_first_instances_change()
    {
        var path=Path.Combine(Path.GetTempPath(),Guid.NewGuid()+".db");
        try
        {
            var first=new WorkspaceService(new SqliteWorkspaceStore(path));
            var second=new WorkspaceService(new SqliteWorkspaceStore(path));
            first.SetGrade(1,"s1","t1",4.8m);
            Assert.Throws<AcademicValidationException>(()=>second.SetGrade(1,"s1","t1",2m));
            Assert.Equal(4.8m,new WorkspaceService(new SqliteWorkspaceStore(path)).Cut(1).Grades.Single(x=>x.StudentId=="s1" && x.ActivityId=="t1").Value);
        }
        finally{SqliteConnection.ClearAllPools();if(File.Exists(path))File.Delete(path);}
    }
}
