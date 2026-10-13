using System.ComponentModel;
using Avalonia.Controls;
using GestorDocente.Desktop.Controls;
using GestorDocente.Desktop.ViewModels;

namespace GestorDocente.Desktop.Views;

public partial class ConsolidatedView : UserControl
{
    private MainViewModel? model;
    private TableColumnLayout? layout;
    public ConsolidatedView(){InitializeComponent();DataContextChanged+=(_,_)=>Attach();ConsolidatedTable.SizeChanged+=(_,_)=>layout?.Refresh();}
    private void Attach()
    {
        if(model is not null)model.Consolidated.PropertyChanged-=Changed;
        model=DataContext as MainViewModel;
        if(model is not null){model.Consolidated.PropertyChanged+=Changed;BuildColumns();}
    }
    private void Changed(object? sender,PropertyChangedEventArgs e){if(e.PropertyName==nameof(ConsolidatedViewModel.Cuts))BuildColumns();}
    private void BuildColumns()
    {
        if(model is null)return;
        ConsolidatedTable.Columns.Clear();
        ConsolidatedTable.Columns.Add(TablePresentation.Text("#","Number",38));
        ConsolidatedTable.Columns.Add(TablePresentation.Text("Documento","Document",108));
        ConsolidatedTable.Columns.Add(TablePresentation.Text("Nombre del estudiante","FullName",240));
        foreach(var cut in model.Consolidated.Cuts)
            ConsolidatedTable.Columns.Add(TablePresentation.Text(cut.Header,$"CutGrades[{cut.Number}]",110));
        ConsolidatedTable.Columns.Add(TablePresentation.Text("Nota final\n100 %","Final",120));
        ConsolidatedTable.Columns.Add(TablePresentation.Text("Estado","Status",125));
        layout=new(ConsolidatedTable,model.Service.Current.Students);
    }
}
