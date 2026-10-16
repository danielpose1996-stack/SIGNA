namespace GestorDocente.Domain;

/// <summary>Valoración opcional independiente de los porcentajes y las notas académicas.</summary>
public sealed record AttendanceGradePolicy(bool Enabled,decimal Present,decimal Absent,decimal Justified,decimal Late)
{
    public void Validate()
    {
        foreach(var value in new[]{Present,Absent,Justified,Late})AcademicRules.ValidateGrade(value);
    }
    public decimal? Average(IEnumerable<AttendanceState?> states)
    {
        Validate();if(!Enabled)return null;
        decimal sum=0;var count=0;
        foreach(var state in states)
        {
            decimal? value=state switch
            {
                AttendanceState.Present=>Present,AttendanceState.Absent=>Absent,
                AttendanceState.Justified=>Justified,AttendanceState.Late=>Late,_=>null
            };
            if(value is {} grade){sum+=grade;count++;}
        }
        return count==0 ? null : sum/count;
    }
}
