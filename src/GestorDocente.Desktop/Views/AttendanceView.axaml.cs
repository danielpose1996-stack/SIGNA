using System.ComponentModel;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using GestorDocente.Desktop.Controls;
using GestorDocente.Desktop.ViewModels;
namespace GestorDocente.Desktop.Views;
public partial class AttendanceView : UserControl
{
    private MainViewModel? observed;
    private TableColumnLayout? columnLayout;
    private GestorDocente.Domain.AcademicCut? columnSource;
    public AttendanceView(){InitializeComponent();DataContextChanged+=(_,_)=>Attach();AttendanceTable.SizeChanged+=(_,_)=>columnLayout?.Refresh();}
    private void Attach(){if(observed is not null)observed.PropertyChanged-=Changed;observed=DataContext as MainViewModel;if(observed is not null){observed.PropertyChanged+=Changed;BuildColumns();}}
    private void Changed(object? sender,PropertyChangedEventArgs e){if(e.PropertyName is nameof(MainViewModel.Weeks) or nameof(MainViewModel.AttendanceGradingEnabled))BuildColumns();}
    private void BuildColumns()
    {
        if(observed is null)return;
        var source=observed.Service.Cut(observed.SelectedCut);
        if(ReferenceEquals(columnSource,source))return;
        columnSource=source;
        AttendanceTable.Columns.Clear();
        AttendanceTable.Columns.Add(Text("#","Number",38));AttendanceTable.Columns.Add(Text("Documento","Document",108));var name=Text("Nombre completo","FullName",215);name.Header=TablePresentation.NameHeader(observed);AttendanceTable.Columns.Add(name);
        foreach(var week in observed.Weeks)
        {
            var number=week.Number;
            var title=week.ShortLabel;
            AttendanceTable.Columns.Add(new DataGridTemplateColumn {Header=TablePresentation.Header(title),Width=new(TablePresentation.HeaderWidth(title,125,165)),CellTemplate=new FuncDataTemplate<AttendanceRowViewModel>((row,_)=> {
                var combo=new ComboBox {DataContext=row!.Cells[number],ItemsSource=AttendanceCellViewModel.Options,Padding=new(6,2),MinHeight=28,Height=30,HorizontalAlignment=Avalonia.Layout.HorizontalAlignment.Stretch};
                combo.ItemTemplate=new FuncDataTemplate<AttendanceOption>((option,_)=>new TextBlock {Text=option!.Label,FontSize=12});
                combo.Bind(ComboBox.SelectedItemProperty,new Binding(nameof(AttendanceCellViewModel.Selected)){Mode=BindingMode.TwoWay});
                combo.Bind(ComboBox.IsEnabledProperty,new Binding(nameof(AttendanceCellViewModel.CanEdit)));
                combo.Bind(ComboBox.BackgroundProperty,new Binding(nameof(AttendanceCellViewModel.Tone)));
                combo.Bind(ToolTip.TipProperty,new Binding(nameof(AttendanceCellViewModel.Error)));
                Avalonia.Automation.AutomationProperties.SetName(combo,$"Estado de asistencia, semana {week.DisplayNumber}");
                return combo;
            })});
        }
        AttendanceTable.Columns.Add(Text("P + T / evaluables","Total",120));AttendanceTable.Columns.Add(Text("% asistencia","Percentage",105));AttendanceTable.Columns.Add(Text("Estado","Status",110));
        if(observed.AttendanceGradingEnabled)AttendanceTable.Columns.Add(Text("Nota de asistencia","AttendanceGrade",120));
        columnLayout=new(AttendanceTable,observed.Service.Current.Students);
    }
    private static DataGridTemplateColumn Text(string title,string path,double width)=>TablePresentation.Text(title,path,width);
}
