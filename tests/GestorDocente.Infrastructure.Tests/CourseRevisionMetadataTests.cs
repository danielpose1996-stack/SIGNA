using GestorDocente.Application;
using GestorDocente.Infrastructure;
using Xunit;

namespace GestorDocente.Infrastructure.Tests;

public class CourseRevisionMetadataTests
{
    [Fact]
    public async Task Read_only_revision_metadata_changes_after_writes_and_keeps_other_courses_unchanged()
    {
        var path=Path.Combine(Path.GetTempPath(),"signa-040-"+Guid.NewGuid().ToString("N")+".db");
        try
        {
            var catalog=new CatalogService(new SqliteCatalogStore(path));var semester=catalog.CreateSemester(2026,2);
            var first=catalog.CreateCourse(semester.Id,"F-1","Materia ficticia","Escuela","A");
            var other=catalog.CreateCourse(semester.Id,"F-2","Otra materia ficticia","Escuela","B");
            Assert.All(catalog.Courses(semester.Id),c=>Assert.Equal(0,c.Revision));
            await catalog.Open(first.Id).AddStudentAsync("Estudiante ficticio");
            var metadata=catalog.Courses(semester.Id);Assert.Equal(1,metadata.Single(c=>c.Id==first.Id).Revision);Assert.Equal(0,metadata.Single(c=>c.Id==other.Id).Revision);
            Assert.Equal(metadata,catalog.Courses(semester.Id));
            await catalog.UpdateCourseAsync(metadata.Single(c=>c.Id==first.Id),semester.Id,"F-1","Nombre editado","Escuela","A");
            var edited=catalog.Courses(semester.Id).Single(c=>c.Id==first.Id);Assert.Equal(2,edited.Revision);Assert.Equal(1,edited.StudentCount);
            Assert.Equal("Estudiante ficticio",Assert.Single(catalog.Open(first.Id).Current.Students).FullName);
        }
        finally{if(File.Exists(path))File.Delete(path);}
    }
}
