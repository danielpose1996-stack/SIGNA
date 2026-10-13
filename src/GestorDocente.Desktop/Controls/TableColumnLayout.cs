using Avalonia.Controls;
using GestorDocente.Domain;

namespace GestorDocente.Desktop.Controls;

/// <summary>Ajusta las columnas fijas sin reconstruir las filas ni consultar la base de datos.</summary>
public sealed class TableColumnLayout
{
    private readonly DataGrid table;
    private readonly double nameWidth,documentWidth;
    public TableColumnLayout(DataGrid table,IEnumerable<Student> students)
    {
        this.table=table;
        var roster=students.ToArray();
        nameWidth=Math.Clamp(TablePresentation.MeasureWidth(roster.Select(s=>s.FullName),12)+24,240,440);
        documentWidth=Math.Clamp(TablePresentation.MeasureWidth(roster.Select(s=>s.Document),12)+24,110,190);
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
