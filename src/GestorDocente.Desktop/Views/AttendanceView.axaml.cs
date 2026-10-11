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
    public AttendanceView(){InitializeComponent();DataContextChanged+=(_,_)=>Attach();}
    private void Attach(){if(observed is not null)observed.PropertyChanged-=Changed;observed=DataContext as MainViewModel;if(observed is not null){observed.PropertyChanged+=Changed;BuildColumns();}}
    private void Changed(object? sender,PropertyChangedEventArgs e){if(e.PropertyName==nameof(MainViewModel.Weeks))BuildColumns();}
    private void BuildColumns()
    {
        if(observed is null)return;
        AttendanceTable.Columns.Clear();
        AttendanceTable.Columns.Add(Text("#","Number",38));AttendanceTable.Columns.Add(Text("Documento","Document",108));AttendanceTable.Columns.Add(Text("Nombre del estudiante","FullName",215));
        foreach(var week in observed.Weeks)
        {
            var number=week.Number;
            AttendanceTable.Columns.Add(new DataGridTemplateColumn {Header=TablePresentation.Header(week.ShortLabel),Width=new(82),CellTemplate=new FuncDataTemplate<AttendanceRowViewModel>((row,_)=> {
                var combo=new ComboBox {DataContext=row!.Cells[number],ItemsSource=AttendanceCellViewModel.Options,Padding=new(6,2),MinHeight=28,Height=30,HorizontalAlignment=Avalonia.Layout.HorizontalAlignment.Stretch};
                combo.ItemTemplate=new FuncDataTemplate<AttendanceOption>((option,_)=>new TextBlock {Text=option!.Label,FontSize=12});
                combo.Bind(ComboBox.SelectedItemProperty,new Binding(nameof(AttendanceCellViewModel.Selected)){Mode=BindingMode.TwoWay});
                combo.Bind(ComboBox.IsEnabledProperty,new Binding(nameof(AttendanceCellViewModel.CanEdit)));
                combo.Bind(ComboBox.BackgroundProperty,new Binding(nameof(AttendanceCellViewModel.Tone)));
                combo.Bind(ToolTip.TipProperty,new Binding(nameof(AttendanceCellViewModel.Error)));
                Avalonia.Automation.AutomationProperties.SetName(combo,$"Estado de asistencia, semana {number}");
                return combo;
            })});
        }
        AttendanceTable.Columns.Add(Text("P / registrados","Total",100));AttendanceTable.Columns.Add(Text("% asistencia","Percentage",95));AttendanceTable.Columns.Add(Text("Estado","Status",95));
    }
    private static DataGridTemplateColumn Text(string title,string path,double width)=>TablePresentation.Text(title,path,width);
}
