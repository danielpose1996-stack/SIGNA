using GestorDocente.Application;
using GestorDocente.Domain;
using Xunit;
namespace GestorDocente.Application.Tests;
public class StudentContactTests
{
    [Fact]
    public void Contact_schema_upgrade_preserves_pending_cut_weights_and_existing_records()
    {
        var original=DemoWorkspaceFactory.Create();original.SchemaVersion=4;
        original.Cuts.RemoveAt(1);original.Cuts[1].Number=2;original.CutWeightsPending=true;
        var before=System.Text.Json.JsonSerializer.Serialize(original);
        var migrated=WorkspaceMigration.Upgrade(original);
        Assert.Equal(AcademicWorkspace.CurrentSchemaVersion,migrated.SchemaVersion);
        Assert.True(migrated.CutWeightsPending);Assert.Equal(new decimal[]{30,40},migrated.Cuts.Select(c=>c.Weight));
        Assert.Equal(original.Students,migrated.Students);Assert.Equal(original.Cuts[0].Grades,migrated.Cuts[0].Grades);
        Assert.Equal(original.Cuts[1].Attendance,migrated.Cuts[1].Attendance);
        Assert.Equal(before,System.Text.Json.JsonSerializer.Serialize(original));
    }

    [Fact]
    public async Task Manual_students_only_require_name_and_empty_documents_are_independent()
    {
        var service=new WorkspaceService(new Store());
        await service.AddStudentAsync("Estudiante ficticio");await service.AddStudentAsync("Otro estudiante ficticio");
        var students=service.Current.Students.TakeLast(2).ToArray();Assert.All(students,s=>Assert.Empty(s.Document));Assert.NotEqual(students[0].Id,students[1].Id);
        await Assert.ThrowsAsync<AcademicValidationException>(()=>service.AddStudentAsync(" "));
        await service.AddStudentAsync("Con contactos","FICTICIO-11","ficticio@example.com","+57 300 000 0000");
        Assert.Equal("+57 300 000 0000",service.Current.Students.Last().Phone);
        await Assert.ThrowsAsync<AcademicValidationException>(()=>service.AddStudentAsync("Duplicado","FICTICIO-11"));
    }
    [Fact]
    public async Task Reimporting_contacts_preserves_identity_grades_and_existing_nonempty_contacts()
    {
        var service=new WorkspaceService(new Store());var original=service.Current.Students[0];var grades=service.Cut(1).Grades.ToArray();
        await service.ImportStudentsAsync([new(original.Document,"Nombre del Excel", "ficticio@example.com","3000000000")]);
        var next=service.Current.Students[0];Assert.Equal(original.Id,next.Id);Assert.Equal(original.FullName,next.FullName);Assert.Equal("ficticio@example.com",next.Email);Assert.Equal(grades,service.Cut(1).Grades);
        await service.ImportStudentsAsync([new(original.Document,original.FullName,"otro@example.com","")]);Assert.Equal("3000000000",service.Current.Students[0].Phone);
    }
    [Fact]
    public void Excel_detects_and_preserves_optional_contact_columns()
    {
        var sheet=new WorkbookSheet("Estudiantes",[new(1,new Dictionary<int,WorkbookCell>{{0,new("Documento")},{1,new("Nombre completo")},{2,new("Correo electrónico")},{3,new("Celular")}}),new(2,new Dictionary<int,WorkbookCell>{{0,new("001")},{1,new("Estudiante ficticio")},{2,new("ficticio@example.com")},{3,new("+57 3000000000")}})]);
        var mapping=StudentImportDetector.Detect(sheet)!;Assert.Equal(2,mapping.EmailColumn);Assert.Equal(3,mapping.PhoneColumn);
        var row=StudentImportPreview.Build(sheet,1,0,[1],[],mapping.EmailColumn,mapping.PhoneColumn).Single();Assert.True(row.CanImport);Assert.Equal("ficticio@example.com",row.Email);Assert.Equal("+57 3000000000",row.Phone);
    }
    private sealed class Store:IWorkspaceStore{public AcademicWorkspace? Load()=>DemoWorkspaceFactory.Create();public void Save(AcademicWorkspace data){} }
}
