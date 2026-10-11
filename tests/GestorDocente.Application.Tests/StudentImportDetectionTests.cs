using GestorDocente.Application;
using Xunit;
namespace GestorDocente.Application.Tests;

public class StudentImportDetectionTests
{
    [Fact]
    public void Headers_after_title_detect_document_and_all_name_columns()
    {
        var sheet=Sheet(Row(1,"Listado de clase"),Row(3,"Apellidos","Nombres","Número de identidad","Correo"),Row(4,"Prueba","Ana","00012345","ignorar@example.test"));
        var mapping=StudentImportDetector.Detect(sheet)!;
        Assert.Equal(3,mapping.HeaderRow);Assert.Equal(2,mapping.DocumentColumn);Assert.Equal(new[]{0,1},mapping.NameColumns);
        var preview=StudentImportPreview.Build(sheet,mapping.HeaderRow,mapping.DocumentColumn,mapping.NameColumns,[]);
        Assert.Equal("Prueba Ana",Assert.Single(preview).Name);
    }
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Headerless_roster_keeps_first_person_and_ignores_phone_email_and_small_numbers(bool title)
    {
        var first=title?2:1;
        var rows=new List<WorkbookRow>();if(title)rows.Add(Row(1,"Listado de estudiantes"));
        rows.Add(Row(first,"PRUEBA GÓMEZ, ANA MARÍA","00123456","1","1","","ignorar@example.test","3121234567"));
        rows.Add(Row(first+1,"PRUEBA RÍOS, LUIS","00123457","2","1","","otro@example.test","3121234568"));
        var sheet=new WorkbookSheet("Sheet1",rows);var mapping=StudentImportDetector.Detect(sheet)!;
        Assert.Equal(first-1,mapping.HeaderRow);Assert.Equal(1,mapping.DocumentColumn);Assert.Equal(new[]{0},mapping.NameColumns);
        var preview=StudentImportPreview.Build(sheet,mapping.HeaderRow,mapping.DocumentColumn,mapping.NameColumns,[]);
        Assert.Equal(2,preview.Count);Assert.Equal("00123456",preview[0].Document);Assert.Equal("PRUEBA GÓMEZ, ANA MARÍA",preview[0].Name);
    }
    [Fact]
    public void Document_before_name_is_detected_and_unrecognizable_sheet_returns_no_mapping()
    {
        var mapping=StudentImportDetector.Detect(Sheet(Row(1,"00123456","Ana Prueba","1"),Row(2,"00123457","Luis Prueba","2")))!;
        Assert.Equal(0,mapping.DocumentColumn);Assert.Equal(new[]{1},mapping.NameColumns);
        Assert.Null(StudentImportDetector.Detect(Sheet(Row(1,"Portada","Información"))));
    }
    [Fact]
    public void Invalid_rows_are_still_visible_in_detected_roster_and_not_importable()
    {
        var sheet=Sheet(Row(1,"Nombre","Cédula","Correo"),Row(2,"Ana Prueba","00123456","ignorado"),Row(3,"Luis Prueba","","otro"));
        var mapping=StudentImportDetector.Detect(sheet)!;
        var preview=StudentImportPreview.Build(sheet,mapping.HeaderRow,mapping.DocumentColumn,mapping.NameColumns,[]);
        Assert.Equal(2,preview.Count);Assert.False(preview[1].CanImport);
    }
    private static WorkbookSheet Sheet(params WorkbookRow[] rows)=>new("Listado",rows);
    private static WorkbookRow Row(int number,params string[] cells)=>new(number,cells.Select((v,i)=>(v,i)).ToDictionary(c=>c.i,c=>new WorkbookCell(c.v)));
}
