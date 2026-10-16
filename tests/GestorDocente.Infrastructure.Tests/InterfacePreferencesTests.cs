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
            var expected=new InterfacePreferences("#F1C20A",ProfileAvatar.Book,"Docente ficticio",false);preferences.Save(expected);
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
            store.Save(new());Assert.Throws<AcademicValidationException>(()=>store.Save(new(Accent:"#INVALID")));
            using(var db=new SqliteConnection($"Data Source={path};Pooling=false"))
            {db.Open();using var command=db.CreateCommand();command.CommandText="PRAGMA ignore_check_constraints=ON; UPDATE interface_preferences SET custom_accent='NoExiste'";command.ExecuteNonQuery();}
            var before=File.ReadAllBytes(path);var service=new InterfacePreferencesService(store);
            Assert.Equal(new InterfacePreferences(),service.Current);Assert.NotEmpty(service.Warning);Assert.Equal(before,File.ReadAllBytes(path));
            Assert.Throws<AcademicValidationException>(()=>new EncryptedLocalBackupService(path).Create(Path.Combine(directory,"invalid.aulabk"),"Contraseña ficticia larga"));
        }
        finally{Directory.Delete(directory,true);}
    }
    [Fact]
    public void Legacy_preferences_load_without_writes_and_extend_atomically_only_when_saved()
    {
        var directory=Path.Combine(Path.GetTempPath(),Guid.NewGuid().ToString("N"));Directory.CreateDirectory(directory);
        try
        {
            var path=Path.Combine(directory,"legacy.db");_=new SqliteCatalogStore(path);
            using var db=new SqliteConnection($"Data Source={path};Pooling=false");db.Open();
            using var command=db.CreateCommand();command.CommandText="""
                CREATE TABLE interface_preferences(id INTEGER PRIMARY KEY CHECK(id=1),schema_version INTEGER NOT NULL CHECK(schema_version=1),palette TEXT NOT NULL,avatar TEXT NOT NULL);
                INSERT INTO interface_preferences VALUES(1,1,'Emerald','Person');
                CREATE TRIGGER reject_preference_update BEFORE UPDATE ON interface_preferences BEGIN SELECT RAISE(ABORT,'Fallo ficticio'); END;
                """;command.ExecuteNonQuery();
            db.Close();var store=new SqliteInterfacePreferencesStore(path);var bytes=File.ReadAllBytes(path);
            Assert.Equal(new InterfacePreferences("#007A66",ProfileAvatar.Person),store.Load());Assert.Equal(bytes,File.ReadAllBytes(path));
            db.Open();
            var expected=new InterfacePreferences("#123456",ProfileAvatar.Graduation,"Docente de prueba",false);
            Assert.Throws<SqliteException>(()=>store.Save(expected));
            command.CommandText="SELECT count(*) FROM pragma_table_info('interface_preferences')";Assert.Equal(4L,command.ExecuteScalar());
            Assert.Equal(new InterfacePreferences("#007A66",ProfileAvatar.Person),store.Load());
            command.CommandText="DROP TRIGGER reject_preference_update";command.ExecuteNonQuery();
            store.Save(expected);Assert.Equal(expected,new SqliteInterfacePreferencesStore(path).Load());
            command.CommandText="PRAGMA user_version";Assert.Equal(1L,command.ExecuteScalar());
        }
        finally{Directory.Delete(directory,true);}
    }
}
