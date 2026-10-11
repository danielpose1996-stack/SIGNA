using System.ComponentModel;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using GestorDocente.Desktop.Controls;
using GestorDocente.Desktop.ViewModels;
namespace GestorDocente.Desktop.Views;

public partial class NotesView : UserControl
{
    private MainViewModel? observed;
    public NotesView() {InitializeComponent();DataContextChanged+=(_,_)=>Attach();}
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
        GradesTable.Columns.Add(Text("Nombre del estudiante","FullName",230));
        foreach(var group in observed.Service.Cut(observed.SelectedCut).Groups)
        foreach(var activity in group.Activities)
        {
            var id=activity.Id;
            GradesTable.Columns.Add(new DataGridTemplateColumn {Header=TablePresentation.Header($"{activity.Name}\n{activity.Weight:0.##} % grupo"),Width=new(98),CellTemplate=new FuncDataTemplate<GradeRowViewModel>((row,_)=>new GradeEditor {DataContext=row!.Cells[id]})});
        }
        GradesTable.Columns.Add(Text("Nota corte\n100 %","Total",100));
        GradesTable.Columns.Add(Text("Estado","Completion",100));
    }
    private static DataGridTemplateColumn Text(string title,string path,double width)=>TablePresentation.Text(title,path,width);
}
