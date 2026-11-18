namespace GestorDocente.Domain;

/// <summary>Solo para metadatos textuales de materias; nunca para IDs ni datos personales.</summary>
public static class CourseText
{
    public static string Upper(string? value)=>(value ?? "").ToUpperInvariant();
}
