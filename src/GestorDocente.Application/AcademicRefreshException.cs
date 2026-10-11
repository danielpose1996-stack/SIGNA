namespace GestorDocente.Application;

/// <summary>La escritura ya se confirmó; falló uno o más observadores de la vista.</summary>
public sealed class AcademicRefreshException(Exception inner,object? committedEntity=null)
    : Exception("Los datos se guardaron, pero falló la actualización de la pantalla. Pulsa Actualizar para volver a leerlos.",inner)
{
    public object? CommittedEntity {get;}=committedEntity;
}
