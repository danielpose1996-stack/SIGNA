using System.Collections.ObjectModel;
using GestorDocente.Application;
using GestorDocente.Domain;

namespace GestorDocente.Desktop.ViewModels;

public sealed record ActivityCard(string Id,string Name,string WeightLabel,string RubricLabel,RelayCommand OpenCommand);
public sealed record GroupCard(string Name,string WeightLabel,string TypeLabel,IReadOnlyList<ActivityCard> Activities);
public sealed class MainViewModel : ObservableObject
{
    public WorkspaceService Service {get;}
    private int selectedCut=1;
    private string page="Notas",search="",message="",saveStatus="Datos ficticios · cambios guardados solo en la demostración";
    private AttendanceRowViewModel? selectedStudent;
    public event Action<string>? RubricRequested;
    public event Action<string,string>? EvaluationRequested;
    public event Action? ConfigurationRequested;
    public int SelectedCut
    {
        get=>selectedCut;
        set {_=Service.Cut(value);if(Set(ref selectedCut,value)){Rebuild();Raise(nameof(CutLabel));}}
    }
    public string CutLabel=>$"Corte {SelectedCut}";
    public string Page {get=>page;set {if(Set(ref page,value)){Raise(nameof(IsNotes));Raise(nameof(IsAttendance));}}}
    public bool IsNotes=>Page=="Notas";
    public bool IsAttendance=>Page=="Asistencias";
    public string Search {get=>search;set {if(Set(ref search,value))Rebuild();}}
    public string Message {get=>message;set=>Set(ref message,value);}
    public string SaveStatus {get=>saveStatus;private set=>Set(ref saveStatus,value);}
    public string CourseName=>Service.Current.CourseName;
    public string CourseInfo=>$"Código: {Service.Current.CourseCode}    |    Grupo: {Service.Current.GroupName}    |    Periodo: {Service.Current.Period}";
    public string StudentCount=>Service.Current.Students.Count.ToString();
    public string CourseAverage {get;private set;}="—";
    public bool HasRows=>GradeRows.Count>0;
    public bool IsEmpty=>!HasRows;
    public ObservableCollection<GradeRowViewModel> GradeRows {get;}=[];
    public ObservableCollection<AttendanceRowViewModel> AttendanceRows {get;}=[];
    public ObservableCollection<GroupCard> Groups {get;}=[];
    public ObservableCollection<AttendanceWeek> Weeks {get;}=[];
    public bool CanAddWeek=>Weeks.Count<9;
    public AttendanceRowViewModel? SelectedStudent {get=>selectedStudent;set=>Set(ref selectedStudent,value);}
    public RelayCommand NavigateCommand {get;}
    public RelayCommand CutCommand {get;}
    public RelayCommand AddWeekCommand {get;}
    public RelayCommand ConfigureCommand {get;}
    public RelayCommand DismissMessageCommand {get;}
    public MainViewModel(WorkspaceService service)
    {
        Service=service;
        NavigateCommand=new(parameter=> {
            if(parameter is "Notas" or "Asistencias")Page=(string)parameter;
            else if(parameter is "Rúbricas") {Page="Notas";Message="Elige una actividad en sus tarjetas para construir o editar la rúbrica.";}
            else Message="Esta sección pertenece a un incremento posterior. Puedes trabajar en Notas y Asistencias.";
        });
        CutCommand=new(parameter=> {if(int.TryParse(parameter?.ToString(),out var number))SelectedCut=number;});
        AddWeekCommand=new(_=>Try(()=>service.AddWeek(SelectedCut)),_=>CanAddWeek);
        ConfigureCommand=new(_=>ConfigurationRequested?.Invoke());
        DismissMessageCommand=new(_=>Message="");
        service.Changed+=()=> {SaveStatus="Cambios guardados en la base local de demostración";Rebuild();};
        Rebuild();
    }
    public void Try(Action action)
    {
        try {action();Message="";} catch(Exception ex) when(ex is AcademicValidationException or IOException or System.Data.Common.DbException) {Message=ex is AcademicValidationException ? ex.Message : "No se guardó el cambio. Revisa el acceso a la base de demostración y reintenta.";}
    }
    private void Rebuild()
    {
        var selectedId=SelectedStudent?.Student.Id;
        var cut=Service.Cut(SelectedCut);
        Groups.Clear();
        foreach(var group in cut.Groups)Groups.Add(new(group.Name,$"{group.Weight:0.##} %",group.IsPartial ? "Calificación única" : "Actividades ponderadas",group.Activities.Select(a=>new ActivityCard(a.Id,a.Name,$"{a.Weight:0.##} % del grupo",a.Rubric is null ? "Sin rúbrica" : "Con rúbrica",new(_=>RubricRequested?.Invoke(a.Id)))).ToList()));
        Weeks.Clear();foreach(var week in cut.Weeks)Weeks.Add(week);
        GradeRows.Clear();AttendanceRows.Clear();
        int i=0;
        foreach(var student in Service.Current.Students)
        {
            i++;
            if(!student.FullName.Contains(Search,StringComparison.OrdinalIgnoreCase) && !student.Document.Contains(Search,StringComparison.OrdinalIgnoreCase))continue;
            GradeRows.Add(new(Service,SelectedCut,student,i,(s,a)=>EvaluationRequested?.Invoke(s,a)));
            AttendanceRows.Add(new(Service,SelectedCut,student,i));
        }
        SelectedStudent=AttendanceRows.FirstOrDefault(x=>x.Student.Id==selectedId) ?? AttendanceRows.FirstOrDefault();
        var totals=Service.Current.Students.Select(s=>GradeCalculator.Cut(cut,s.Id)).ToList();
        CourseAverage=totals.Count>0 && totals.All(x=>x is not null) ? AcademicFormatting.Grade(totals.Average(x=>x!.Value)) : "Pendiente";
        Raise(nameof(CourseAverage));Raise(nameof(HasRows));Raise(nameof(IsEmpty));Raise(nameof(CanAddWeek));
        Raise(nameof(Groups));Raise(nameof(Weeks));AddWeekCommand.Refresh();
    }
}
