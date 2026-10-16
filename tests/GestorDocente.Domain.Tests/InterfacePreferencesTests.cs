using GestorDocente.Domain;
using Xunit;

namespace GestorDocente.Domain.Tests;

public class InterfacePreferencesTests
{
    [Theory]
    [InlineData("#000000")]
    [InlineData("#FFFFFF")]
    [InlineData("#e1a627")]
    public void Custom_rgb_preserves_all_channels(string accent)=>new InterfacePreferences(TeacherName:"Docente ficticio",Accent:accent,ColorGrades:false).Validate();

    [Theory]
    [InlineData("")]
    [InlineData("red")]
    [InlineData("#123")]
    [InlineData("#FFFFFFFF")]
    [InlineData("#GG0000")]
    public void Invalid_rgb_is_rejected(string accent)=>Assert.Throws<AcademicValidationException>(()=>new InterfacePreferences(Accent:accent).Validate());

    [Fact]
    public void Name_has_a_bounded_single_line_and_no_name_is_allowed()
    {
        new InterfacePreferences().Validate();
        Assert.Throws<AcademicValidationException>(()=>new InterfacePreferences(TeacherName:new string('x',121)).Validate());
        Assert.Throws<AcademicValidationException>(()=>new InterfacePreferences(TeacherName:"Nombre\nOtro").Validate());
    }
}
