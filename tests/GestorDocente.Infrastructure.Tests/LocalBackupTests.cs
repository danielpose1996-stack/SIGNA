using GestorDocente.Application;
using GestorDocente.Domain;
using GestorDocente.Infrastructure;
using Xunit;

namespace GestorDocente.Infrastructure.Tests;

public class LocalBackupTests
{
    private const string Password="contraseña docente ficticia muy larga";

    [Fact]
    public void Encrypted_online_backup_restores_course_grades_and_attendance()
    {
        WithFiles((source,target,archive)=>
        {
            var original=Seed(source);
            var backup=new EncryptedLocalBackupService(source);
            var created=backup.Create(archive,Password);
            Assert.Equal(archive,created.Path);
            Assert.DoesNotContain("Estudiante ficticio",System.Text.Encoding.UTF8.GetString(File.ReadAllBytes(archive)),StringComparison.Ordinal);
            var destination=new CatalogService(new SqliteCatalogStore(target));destination.CreateSemester(2027,1);
            var restored=new EncryptedLocalBackupService(target).Restore(archive,Password);
            Assert.True(File.Exists(restored.PreviousBackupPath));
            Assert.Equal(0,new EncryptedLocalBackupService(target).Inspect(restored.PreviousBackupPath,Password).CourseCount);
            var reopened=new CatalogService(new SqliteCatalogStore(target)).Open(original.CourseId);
            Assert.Equal("0001",reopened.Current.Students.Single().Document);
            Assert.Equal(4.5m,GradeCalculator.Cut(reopened.Cut(1),reopened.Current.Students.Single().Id));
            Assert.Equal(AttendanceState.Present,reopened.Cut(1).Attendance.Single().State);
        });
    }

    [Theory]
    [InlineData("wrong-password")]
    [InlineData("truncated")]
    [InlineData("modified")]
    public void Invalid_backup_never_replaces_current_database(string fault)
    {
        WithFiles((source,target,archive)=>
        {
            Seed(source);var destination=new CatalogService(new SqliteCatalogStore(target));destination.CreateSemester(2027,1);
            new EncryptedLocalBackupService(source).Create(archive,Password);
            var bytes=File.ReadAllBytes(archive);
            if(fault=="truncated")File.WriteAllBytes(archive,bytes[..^20]);
            if(fault=="modified"){bytes[bytes.Length/2]^=0x55;File.WriteAllBytes(archive,bytes);}
            var before=File.ReadAllBytes(target);
            Assert.Throws<AcademicValidationException>(()=>new EncryptedLocalBackupService(target).Restore(archive,fault=="wrong-password" ? "contraseña equivocada" : Password));
            Assert.Equal(before,File.ReadAllBytes(target));
        });
    }

    [Fact]
    public void Existing_backup_destination_is_never_overwritten()
    {
        WithFiles((source,_,archive)=>
        {
            Seed(source);File.WriteAllText(archive,"archivo anterior");
            Assert.Throws<AcademicValidationException>(()=>new EncryptedLocalBackupService(source).Create(archive,Password));
            Assert.Equal("archivo anterior",File.ReadAllText(archive));
        });
    }

    [Fact]
    public void Multi_chunk_backup_is_authenticated_and_readable()
    {
        WithFiles((source,_,archive)=>
        {
            var seeded=Seed(source);var catalog=new CatalogService(new SqliteCatalogStore(source));var service=catalog.Open(seeded.CourseId);
            service.ConfigureGroups(1,[new(){Id="p",Name="Trabajo extenso",Weight=100,Mode=EvaluationMode.SubActivities,
                Activities=[new(){Id="sub",Name="Entrega",Description=new string('x',2*1024*1024)}]}]);
            var backup=new EncryptedLocalBackupService(source);
            backup.Create(archive,Password);
            Assert.True(new FileInfo(archive).Length>1024*1024);
            Assert.Equal(1,backup.Inspect(archive,Password).CourseCount);
        });
    }

    [Fact]
    public void Corrupt_current_file_is_preserved_encrypted_before_valid_restore()
    {
        WithFiles((source,target,archive)=>
        {
            var course=Seed(source);new EncryptedLocalBackupService(source).Create(archive,Password);
            File.WriteAllText(target,"Archivo SQLite dañado de prueba");
            var restored=new EncryptedLocalBackupService(target).Restore(archive,Password);
            Assert.True(restored.PreviousCopyWasRaw);
            Assert.True(File.Exists(restored.PreviousBackupPath));
            Assert.DoesNotContain("dañado",System.Text.Encoding.UTF8.GetString(File.ReadAllBytes(restored.PreviousBackupPath)),StringComparison.Ordinal);
            Assert.Single(new CatalogService(new SqliteCatalogStore(target)).Open(course.CourseId).Current.Students);
        });
    }

    private static (string CourseId,string StudentId) Seed(string path)
    {
        var catalog=new CatalogService(new SqliteCatalogStore(path));var semester=catalog.CreateSemester(2026,1);
        var course=catalog.CreateCourse(semester.Id,"BK","Materia ficticia","Escuela","");
        var service=catalog.Open(course.Id);service.ImportStudents([new("0001","Estudiante ficticio")]);
        var student=service.Current.Students.Single().Id;
        service.ConfigureGroups(1,[new(){Id="p",Name="Parcial",Weight=100}]);
        service.SetGrade(1,student,"p",4.5m);service.AddWeek(1);service.SetAttendance(1,student,1,AttendanceState.Present);
        return(course.Id,student);
    }
    private static void WithFiles(Action<string,string,string> test)
    {
        var folder=Path.Combine(Path.GetTempPath(),"aula-backup-tests",Guid.NewGuid().ToString("N"));Directory.CreateDirectory(folder);
        try{test(Path.Combine(folder,"source.db"),Path.Combine(folder,"target.db"),Path.Combine(folder,"backup.aulabk"));}
        finally{Directory.Delete(folder,true);}
    }
}
