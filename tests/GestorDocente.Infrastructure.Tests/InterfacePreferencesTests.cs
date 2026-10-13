using GestorDocente.Application;
using GestorDocente.Domain;
using GestorDocente.Infrastructure;
using Microsoft.Data.Sqlite;
using Xunit;

namespace GestorDocente.Infrastructure.Tests;
public class InterfacePreferencesTests
{
    [Fact]
    public async Task Preferences_persist_and_are_included_in_portable_encrypted_backup()
    {
        var directory=Path.Combine(Path.GetTempPath(),Guid.NewGuid().ToString("N"));Directory.CreateDirectory(directory);
        try
        {
            var source=Path.Combine(directory,"source.db");var catalog=new CatalogService(new SqliteCatalogStore(source));var semester=catalog.CreateSemester(2026,1);
            var first=catalog.CreateCourse(semester.Id,"A","Materia ficticia A","Escuela","");var second=catalog.CreateCourse(semester.Id,"B","Materia ficticia B","Escuela","");
            var a=catalog.Open(first.Id);var b=catalog.Open(second.Id);await a.AddStudentAsync("Nombre ficticio","123");await b.AddStudentAsync("Nombre ficticio","123");
            await a.ConfigureGroupsAsync(1,[new(){Id="p",Name="Parcial",Weight=100}]);await a.SetGradeAsync(1,a.Current.Students[0].Id,"p",4);
            var bBefore=System.Text.Json.JsonSerializer.Serialize(b.Current);
            await a.EditStudentAsync(a.Current.Students[0],"Nombre editado","","","3000000000");await a.DeleteStudentAsync(a.PreviewStudentDeletion(a.Current.Students[0].Id),true);
            Assert.Equal(bBefore,System.Text.Json.JsonSerializer.Serialize(catalog.Open(second.Id).Current));Assert.Empty(catalog.Open(first.Id).Current.Students);
            var preferences=new SqliteInterfacePreferencesStore(source);Assert.Equal(new InterfacePreferences(),preferences.Load());
            var expected=new InterfacePreferences(InterfacePalette.Violet,ProfileAvatar.Book);preferences.Save(expected);
            Assert.Equal(expected,new SqliteInterfacePreferencesStore(source).Load());
            var archive=Path.Combine(directory,"backup.aulabk");var target=Path.Combine(directory,"target.db");var password="Contraseña ficticia de pruebas";
            new EncryptedLocalBackupService(source).Create(archive,password);new EncryptedLocalBackupService(target).Restore(archive,password);
            Assert.Equal(expected,new SqliteInterfacePreferencesStore(target).Load());Assert.Equal(2,new CatalogService(new SqliteCatalogStore(target)).Courses(semester.Id).Count);
        }
        finally{Directory.Delete(directory,true);}
    }
    [Fact]
    public void Invalid_preferences_are_reported_without_overwriting_or_blocking_academic_data()
    {
        var directory=Path.Combine(Path.GetTempPath(),Guid.NewGuid().ToString("N"));Directory.CreateDirectory(directory);
        try
        {
            var path=Path.Combine(directory,"data.db");_=new SqliteCatalogStore(path);var store=new SqliteInterfacePreferencesStore(path);
            store.Save(new());Assert.Throws<AcademicValidationException>(()=>store.Save(new((InterfacePalette)99)));
            using(var db=new SqliteConnection($"Data Source={path};Pooling=false"))
            {db.Open();using var command=db.CreateCommand();command.CommandText="PRAGMA ignore_check_constraints=ON; UPDATE interface_preferences SET palette='NoExiste'";command.ExecuteNonQuery();}
            var before=File.ReadAllBytes(path);var service=new InterfacePreferencesService(store);
            Assert.Equal(new InterfacePreferences(),service.Current);Assert.NotEmpty(service.Warning);Assert.Equal(before,File.ReadAllBytes(path));
            Assert.Throws<AcademicValidationException>(()=>new EncryptedLocalBackupService(path).Create(Path.Combine(directory,"invalid.aulabk"),"Contraseña ficticia larga"));
        }
        finally{Directory.Delete(directory,true);}
    }
}
