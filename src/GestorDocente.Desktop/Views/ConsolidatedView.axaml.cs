using System.ComponentModel;
using Avalonia.Controls;
using GestorDocente.Desktop.Controls;
using GestorDocente.Desktop.ViewModels;

namespace GestorDocente.Desktop.Views;

public partial class ConsolidatedView : UserControl
{
    private MainViewModel? model;
    private TableColumnLayout? layout;
    private bool? compact;
    public ConsolidatedView(){InitializeComponent();DataContextChanged+=(_,_)=>Attach();ConsolidatedTable.SizeChanged+=(_,_)=>layout?.Refresh();SizeChanged+=(_,_)=>ArrangeCompact();}
    private void ArrangeCompact()
    {
        var next=Bounds.Height<520;if(compact==next)return;compact=next;
        RiskExpander.IsExpanded=!next;
        CalculationDescription.IsVisible=!next;CompactCalculationDescription.IsVisible=next;
        WeightsSummary.MaxLines=next ? 2 : 0;WeightsSummary.TextTrimming=next ? Avalonia.Media.TextTrimming.CharacterEllipsis : Avalonia.Media.TextTrimming.None;
    }
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
            ConsolidatedTable.Columns.Add(TablePresentation.CompletedGrade(cut.Header,cut.Number,model.Appearance));
        ConsolidatedTable.Columns.Add(TablePresentation.Text("Nota mínima","Minimum",110));
        ConsolidatedTable.Columns.Add(TablePresentation.Grade("Nota final\n100 %","FinalValue",120,model.Appearance));
        ConsolidatedTable.Columns.Add(TablePresentation.Text("Estado académico","Status",175));
        layout=new(ConsolidatedTable,model.Service.Current.Students);
    }
}
