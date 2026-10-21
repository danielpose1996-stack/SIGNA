using Avalonia.Controls;
using GestorDocente.Domain;
using System.Runtime.CompilerServices;

namespace GestorDocente.Desktop.Controls;

/// <summary>Ajusta las columnas fijas sin reconstruir las filas ni consultar la base de datos.</summary>
public sealed class TableColumnLayout
{
    private readonly DataGrid table;
    private readonly double nameWidth,documentWidth;
    private sealed record Measurements(double Name,double Document);
    private static readonly ConditionalWeakTable<List<Student>,Measurements> Cache=new();
    private static Measurements Measure(IEnumerable<Student> students)
    {
        var roster=students as IReadOnlyList<Student> ?? students.ToArray();
        return new(Math.Clamp(TablePresentation.MeasureWidth(roster.Select(s=>s.FullName),12)+24,240,440),
            Math.Clamp(TablePresentation.MeasureWidth(roster.Select(s=>s.Document),12)+24,110,190));
    }
    public TableColumnLayout(DataGrid table,IEnumerable<Student> students)
    {
        this.table=table;
        var measurement=students is List<Student> roster ? Cache.GetValue(roster,key=>Measure(key)) : Measure(students);
        nameWidth=measurement.Name;documentWidth=measurement.Document;
        table.RowHeight=double.NaN;table.ColumnHeaderHeight=double.NaN;
        Refresh();
    }
    public void Refresh()
    {
        if(table.Columns.Count<3)return;
        var width=table.Bounds.Width>0 ? table.Bounds.Width : 900;
        SetWidth(table.Columns[1],Math.Min(documentWidth,Math.Clamp(width*.2,110,190)));
        SetWidth(table.Columns[2],Math.Min(nameWidth,Math.Clamp(width*.42,200,440)));
    }
    private static void SetWidth(DataGridColumn column,double width)
    {
        if(Math.Abs(column.Width.Value-width)>.5)column.Width=new DataGridLength(width);
    }
}
