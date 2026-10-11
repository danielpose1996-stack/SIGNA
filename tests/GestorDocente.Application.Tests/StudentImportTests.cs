using GestorDocente.Application;
using GestorDocente.Domain;
using Xunit;
namespace GestorDocente.Application.Tests;
public class StudentImportTests
{
    [Fact]
    public void Append_preserves_existing_students_and_grades_and_ignores_other_columns()
    {
        var store=new MemoryStore();var service=new WorkspaceService(store);var before=WorkspaceService.Clone(service.Current);
        service.ImportStudents([new("000012","Alumna ficticia")]);
        Assert.Equal(before.Students.Count+1,service.Current.Students.Count);Assert.Equal("000012",service.Current.Students.Last().Document);
        Assert.Equal(before.Cuts[0].Grades,service.Current.Cuts[0].Grades);Assert.Equal(before.Students[0],service.Current.Students[0]);
        Assert.Throws<AcademicValidationException>(()=>service.ImportStudents([new("000012","Otra persona")]));
    }
    [Fact]
    public void Invalid_duplicate_or_failed_batch_does_not_partially_import()
    {
        var store=new MemoryStore();var service=new WorkspaceService(store);var count=service.Current.Students.Count;
        Assert.Throws<AcademicValidationException>(()=>service.ImportStudents([new("A","Alumna"),new("A","Alumno")]));
        Assert.Throws<AcademicValidationException>(()=>service.ImportStudents([new("A","Alumna"),new("B","")]));
        store.Fail=true;Assert.Throws<IOException>(()=>service.ImportStudents([new("A","Alumna")]));Assert.Equal(count,service.Current.Students.Count);
    }
    [Fact]
    public void Mapping_reports_bad_rows_duplicates_and_existing_documents()
    {
        var sheet=new WorkbookSheet("Listado",[new(1,new Dictionary<int,WorkbookCell>{{0,new("Documento")},{1,new("Nombres")},{2,new("Apellidos")},{3,new("Correo")}}),
            new(2,new Dictionary<int,WorkbookCell>{{0,new("001")},{1,new("Ana")},{2,new("Prueba")},{3,new("ignorado")}}),
            new(3,new Dictionary<int,WorkbookCell>{{0,new("001")},{1,new("Ana")},{2,new("Prueba")}}),
            new(4,new Dictionary<int,WorkbookCell>{{0,new("DEMO-001")},{1,new("Otro")}}),
            new(5,new Dictionary<int,WorkbookCell>{{0,new("002",true)},{1,new("Formula")}})]);
        var rows=StudentImportPreview.Build(sheet,1,0,[1,2],DemoWorkspaceFactory.Create().Students);
        Assert.Equal("Ana Prueba",rows[0].Name);Assert.True(rows[0].CanImport);Assert.False(rows[1].CanImport);Assert.False(rows[2].CanImport);Assert.False(rows[3].CanImport);
    }
    private sealed class MemoryStore:IWorkspaceStore
    {
        private AcademicWorkspace data=DemoWorkspaceFactory.Create();public bool Fail{get;set;}
        public AcademicWorkspace? Load()=>WorkspaceService.Clone(data);
        public void Save(AcademicWorkspace value){if(Fail)throw new IOException();data=WorkspaceService.Clone(value);}
    }
}
