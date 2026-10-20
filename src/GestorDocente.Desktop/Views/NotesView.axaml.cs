using System.ComponentModel;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using GestorDocente.Desktop.Controls;
using GestorDocente.Desktop.ViewModels;
using GestorDocente.Domain;
namespace GestorDocente.Desktop.Views;

public partial class NotesView : UserControl
{
    private MainViewModel? observed;
    private TableColumnLayout? columnLayout;
    public NotesView()
    {
        InitializeComponent();DataContextChanged+=(_,_)=>Attach();GradesTable.SizeChanged+=(_,_)=>columnLayout?.Refresh();
        StudentActionsBar.SizeChanged+=(_,_)=>ArrangeActions();
    }
    private void ArrangeActions()
    {
        SetActionClass("compact",StudentActionsBar.Bounds.Width<1040);
        SetActionClass("tight",StudentActionsBar.Bounds.Width<720);
    }
    private void SetActionClass(string name,bool active)
    {
        if(active){if(!StudentActionsBar.Classes.Contains(name))StudentActionsBar.Classes.Add(name);}
        else StudentActionsBar.Classes.Remove(name);
    }
    private void Attach()
    {
        if(observed is not null)observed.PropertyChanged-=Changed;
        observed=DataContext as MainViewModel;
        if(observed is not null){observed.PropertyChanged+=Changed;BuildColumns();}
    }
    private void Changed(object? sender,PropertyChangedEventArgs e) {if(e.PropertyName==nameof(MainViewModel.Groups))BuildColumns();}
    private void BuildColumns()
    {
        if(observed is null)return;
        GradesTable.Columns.Clear();
        GradesTable.Columns.Add(Text("#","Number",40));
        GradesTable.Columns.Add(Text("Documento","Document",112));
        var name=Text("Nombre completo","FullName",230);name.Header=TablePresentation.NameHeader(observed);GradesTable.Columns.Add(name);
        GradesTable.Columns.Add(new DataGridTemplateColumn{Header=TablePresentation.Header("Detalles"),Width=new(100),CellTemplate=new FuncDataTemplate<GradeRowViewModel>((row,_)=>new Button{Content="Ver detalles",FontSize=11,Padding=new(5,3),Margin=new(3,2),Command=observed.ViewStudentDetailsCommand,CommandParameter=row!.Student})});
        foreach(var group in observed.Service.Cut(observed.SelectedCut).Groups)
        {
        if(group.Mode==EvaluationMode.Direct)
        {
            var id=group.Id;
            var title=$"{group.Name}\n{group.Weight:0.##} %";
            GradesTable.Columns.Add(new DataGridTemplateColumn {Header=TablePresentation.GradeHeader(title,id,observed),Width=new(TablePresentation.HeaderWidth(title)),CellTemplate=new FuncDataTemplate<GradeRowViewModel>((row,_)=>new GradeEditor {DataContext=row!.Cells[id]})});
            continue;
        }
        foreach(var activity in group.Activities)
        {
            var id=activity.Id;
            var title=$"{activity.Name}\n{group.Name}{(group.UseWeightedAverage ? $" · {activity.Weight:0.############################} % interno" : "")}";
            GradesTable.Columns.Add(new DataGridTemplateColumn {Header=TablePresentation.GradeHeader(title,id,observed),Width=new(TablePresentation.HeaderWidth(title)),CellTemplate=new FuncDataTemplate<GradeRowViewModel>((row,_)=>new GradeEditor {DataContext=row!.Cells[id]})});
        }
        var averageTitle=$"Promedio{(group.UseWeightedAverage ? " ponderado" : "")} {group.Name}\n{group.Weight:0.##} %";
        GradesTable.Columns.Add(Text(averageTitle,$"GroupTotals[{group.Id}]",TablePresentation.HeaderWidth(averageTitle)));
        }
        GradesTable.Columns.Add(TablePresentation.Grade("Nota del\ncorte","TotalValue",105,observed.Appearance));
        GradesTable.Columns.Add(Text("Estado","Completion",100));
        columnLayout=new(GradesTable,observed.Service.Current.Students);
    }
    private static DataGridTemplateColumn Text(string title,string path,double width)=>TablePresentation.Text(title,path,width);
}
