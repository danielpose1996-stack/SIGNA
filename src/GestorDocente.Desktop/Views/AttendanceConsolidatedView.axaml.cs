using System.ComponentModel;
using Avalonia.Controls;
using GestorDocente.Desktop.Controls;
using GestorDocente.Desktop.ViewModels;
namespace GestorDocente.Desktop.Views;

public partial class AttendanceConsolidatedView:UserControl
{
    private MainViewModel? model;
    private double nameWidth=280;
    public AttendanceConsolidatedView()
    {
        InitializeComponent();DataContextChanged+=(_,_)=>Attach();AttendanceConsolidatedTable.SizeChanged+=(_,_)=>FitName();
    }
    private void Attach()
    {
        if(model is not null)model.AttendanceConsolidated.PropertyChanged-=Changed;
        model=DataContext as MainViewModel;
        if(model is not null){model.AttendanceConsolidated.PropertyChanged+=Changed;BuildColumns();}
    }
    private void Changed(object? sender,PropertyChangedEventArgs e){if(e.PropertyName==nameof(AttendanceConsolidatedViewModel.Cuts))BuildColumns();}
    private void BuildColumns()
    {
        if(model is null)return;
        var table=AttendanceConsolidatedTable;table.Columns.Clear();table.Columns.Add(TablePresentation.Text("#","Number",40));
        nameWidth=Math.Clamp(TablePresentation.MeasureWidth(model.Service.Current.Students.Select(s=>s.FullName),12)+24,240,440);
        table.Columns.Add(TablePresentation.Text("Nombre completo","FullName",nameWidth));
        foreach(var cut in model.AttendanceConsolidated.Cuts)
        {
            var header=$"{cut.Label}\nAusencias (A)";table.Columns.Add(TablePresentation.Text(header,$"Absences[{cut.Number}]",TablePresentation.HeaderWidth(header)));
        }
        table.Columns.Add(TablePresentation.Text("Total de faltas","Total",125));FitName();
    }
    private void FitName()
    {
        var table=AttendanceConsolidatedTable;if(table.Columns.Count<2)return;
        var available=table.Bounds.Width>0 ? table.Bounds.Width : 900;
        var width=Math.Min(nameWidth,Math.Clamp(available*.42,220,440));
        if(Math.Abs(table.Columns[1].Width.Value-width)>.5)table.Columns[1].Width=new(width);
    }
}
