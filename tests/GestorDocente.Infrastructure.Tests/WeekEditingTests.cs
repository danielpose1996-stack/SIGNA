using GestorDocente.Application;
using GestorDocente.Domain;
using GestorDocente.Infrastructure;
using Xunit;

namespace GestorDocente.Infrastructure.Tests;
public class WeekEditingTests
{
    [Fact]
    public async Task Reopening_backup_and_stale_writer_preserve_week_identity_and_edited_details()
    {
        var directory=Path.Combine(Path.GetTempPath(),Guid.NewGuid().ToString("N"));Directory.CreateDirectory(directory);
        try
        {
            var path=Path.Combine(directory,"weeks.db");var catalog=new CatalogService(new SqliteCatalogStore(path));
            var semester=catalog.CreateSemester(2026,2);var course=catalog.CreateCourse(semester.Id,"F01","Materia ficticia","Escuela","");
            var service=catalog.Open(course.Id);await service.AddStudentAsync("Estudiante ficticio");
            await service.AddWeekAsync(1,markAllPresent:true);await service.AddWeekAsync(1);await service.AddWeekAsync(1);
            var student=service.Current.Students[0].Id;await service.SetAttendanceAsync(1,student,2,AttendanceState.Late);
            var stale=catalog.Open(course.Id);await service.RemoveWeekAsync(1,1);
            await service.EditWeekAsync(1,2,WeekKind.Activities,new DateOnly(2026,10,14));
            await Assert.ThrowsAsync<AcademicValidationException>(()=>stale.EditWeekAsync(1,2,WeekKind.Class,null));
            var reopened=catalog.Open(course.Id);Assert.Equal(new[]{1,2},reopened.Cut(1).DisplayWeeks.Select(w=>w.DisplayNumber));
            var report=AttendancePdfProjection.Build(reopened.Current,1,student);Assert.Equal("T - Tarde",report.Lines[0].State);Assert.Equal(new DateOnly(2026,10,14),report.Lines[0].Date);
            var archive=Path.Combine(directory,"copy.aulabk");var restored=Path.Combine(directory,"restored.db");const string password="Contraseña ficticia de prueba";
            new EncryptedLocalBackupService(path).Create(archive,password);new EncryptedLocalBackupService(restored).Restore(archive,password);
            var copy=new CatalogService(new SqliteCatalogStore(restored)).Open(course.Id);
            await copy.RestoreWeekAsync(1,1);Assert.Equal(new[]{1,2,3},copy.Cut(1).DisplayWeeks.Select(w=>w.DisplayNumber));
            Assert.Equal(AttendanceState.Present,copy.Cut(1).Attendance.Single(a=>a.WeekNumber==1).State);
            Assert.Equal(AttendanceState.Late,copy.Cut(1).Attendance.Single(a=>a.WeekNumber==2).State);
        }
        finally{Directory.Delete(directory,true);}
    }
}
