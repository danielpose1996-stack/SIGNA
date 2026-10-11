using Microsoft.Data.Sqlite;

namespace GestorDocente.Desktop.ViewModels;

/// <summary>Identificación técnica sin incluir parámetros SQL, rutas ni datos personales.</summary>
public static class SafeErrorCode
{
    public static string From(Exception error)=>error is SqliteException sqlite ?
        $"SqliteException · SQLite {sqlite.SqliteErrorCode}/{sqlite.SqliteExtendedErrorCode}" :
        error.GetType().Name;
}
