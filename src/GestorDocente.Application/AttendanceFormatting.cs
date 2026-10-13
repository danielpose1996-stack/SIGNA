using System.Globalization;
using GestorDocente.Domain;

namespace GestorDocente.Application;

public static class AttendanceFormatting
{
    private static readonly CultureInfo Culture=CultureInfo.GetCultureInfo("es-CO");
    public static string Percentage(decimal? value)=>value is { } percentage ? percentage.ToString("0.##",Culture)+" %" : "—";
    public static string Status(AttendanceClassification classification)=>classification switch
    {
        AttendanceClassification.Excellent=>"Excelente",
        AttendanceClassification.Good=>"Bueno",
        AttendanceClassification.Regular=>"Regular",
        AttendanceClassification.Low=>"Bajo",
        _=>"Sin registros"
    };
}
