using GestorDocente.Application;
using Xunit;

namespace GestorDocente.Application.Tests;

public class SemesterStudentIndexTests
{
    [Fact]
    public void Name_search_is_scoped_to_one_semester_and_collects_all_courses_by_document()
    {
        var index=new SemesterStudentIndex("current",[
            new("current","a","Gerencia","G-1","Nocturno","s1","Mariana López Torres","F-01"),
            new("current","b","Trabajo de grado","T-1","Diurno","other-id","Mariana Lopez Torres","F-01"),
            new("previous","old","Materia anterior","OLD","","s1","Mariana López Torres","F-01"),
            new("current","c","Otra materia","O-1","","s2","Carlos Pérez","F-02")]);
        var result=Assert.Single(index.Find("  LOPEZ  marIANA "));
        Assert.Equal(new[]{"a","b"},result.Courses.Select(c=>c.Id));
        Assert.Equal("Mariana López Torres",result.FullName);
        Assert.Empty(index.Find("sin coincidencias"));Assert.Empty(index.Find(" "));
        Assert.Single(index.Find("pÉr"));
    }

    [Fact]
    public void Homonyms_and_missing_documents_are_not_merged_and_duplicate_enrollment_is_not_repeated()
    {
        CourseStudentEnrollment[] rows=[
            new("term","a","Materia A","A","","a1","Alex de prueba","F-01"),
            new("term","b","Materia B","B","","b1","Alex de prueba","F-02"),
            new("term","c","Materia C","C","","c1","Alex de prueba"),
            new("term","d","Materia D","D","","d1","Alex de prueba")];
        var results=new SemesterStudentIndex("term",rows.Append(rows[0])).Find("alex");
        Assert.Equal(4,results.Count);Assert.All(results,r=>Assert.Single(r.Courses));
        Assert.Equal(4,results.Select(r=>r.Key).Distinct().Count());
    }
}
