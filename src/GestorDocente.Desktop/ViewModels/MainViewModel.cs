using System.Collections.ObjectModel;
using GestorDocente.Application;
using GestorDocente.Domain;

namespace GestorDocente.Desktop.ViewModels;

public sealed record GroupCard(string Name,string WeightLabel,string TypeLabel,RelayCommand ConfigureCommand);
public enum NavigationSection { Home, Courses, Academic, Backups }
public sealed class MainViewModel : ObservableObject
{
    public WorkspaceService Service {get;private set;}
    public CatalogService? Catalog {get;}
    public IStudentWorkbookReader? WorkbookReader {get;}
    public IAcademicReportExporter? ReportExporter {get;}
    public ILocalBackupService? LocalBackup {get;}
    private readonly Dictionary<string,(int Cut,string Page,string Search,string? Student)> courseStates=[];
    public HomeViewModel Home {get;}
    private NavigationSection section=NavigationSection.Home;
    private int selectedCut=1;
    private string page="Notas",search="",message="",saveStatus="Datos ficticios · cambios guardados solo en la demostración";
    private AttendanceRowViewModel? selectedStudent;
    public event Action? ConfigurationRequested;
    public event Action<bool>? CatalogEditorRequested;
    public event Action? WeekEditorRequested;
    public event Action? RecoveryRequested;
    public event Action? ImportRequested;
    public event Action? ExportRequested;
    public event Action? BackupRequested;
    public event Action? RestoreRequested;
    private bool fileBusy;
    public bool FileBusy{get=>fileBusy;set{if(Set(ref fileBusy,value)){ImportStudentsCommand.Refresh();ExportReportCommand.Refresh();CreateBackupCommand.Refresh();RestoreBackupCommand.Refresh();NavigateCommand.Refresh();OpenCourseCommand.Refresh();CutCommand.Refresh();ConfigureCommand.Refresh();}}}
    public RelayCommand ImportStudentsCommand{get;}
    public RelayCommand ExportReportCommand{get;}
    public RelayCommand CreateBackupCommand{get;}
    public RelayCommand RestoreBackupCommand{get;}
    private string backupStatus="Elige dónde guardar una copia cifrada. Conservarás la contraseña para restaurarla.";
    public string BackupStatus{get=>backupStatus;set=>Set(ref backupStatus,value);}
    public bool CanUseLocalBackup=>LocalBackup is not null;
    public bool IsDemo=>Service.Current.IsDemo;
    public string StorageLabel=>IsDemo ? "DEMOSTRACIÓN · DATOS FICTICIOS" : "DATOS LOCALES";
    public bool HasOpenCourse=>!string.IsNullOrWhiteSpace(Service.Current.CourseName);
    private bool detailVisible;
    public bool DetailVisible {get=>detailVisible;set=>Set(ref detailVisible,value);}
    public bool DetailHidden=>!DetailVisible;
    public RelayCommand ToggleDetailCommand {get;}
    public RelayCommand NewSemesterCommand {get;}
    public RelayCommand NewCourseCommand {get;}
    public RelayCommand RecoveryCommand {get;}
    public RelayCommand ReloadCourseCommand {get;}
    public int SelectedCut
    {
        get=>selectedCut;
        set {_=Service.Cut(value);if(Set(ref selectedCut,value)){Rebuild();Raise(nameof(CutLabel));}}
    }
    public string CutLabel=>$"Corte {SelectedCut}";
    public NavigationSection Section
    {
        get=>section;
        private set
        {
            if(!Set(ref section,value))return;
            foreach(var name in new[] {nameof(IsHome),nameof(IsCourses),nameof(IsHomeOrCourses),nameof(IsBackups),
                nameof(IsAcademicNavigation),nameof(IsCourseContext),nameof(IsGlobalContext),
                nameof(IsNotes),nameof(IsAttendance),nameof(PageTitle),nameof(PageSubtitle)})Raise(name);
        }
    }
    // La última pestaña académica se mantiene independiente de las páginas globales.
    public string Page
    {
        get=>page;
        set
        {
            if(value is not ("Notas" or "Asistencias"))throw new ArgumentException("Selecciona Notas o Asistencias.",nameof(value));
            Section=NavigationSection.Academic;
            if(Set(ref page,value)){Raise(nameof(IsNotes));Raise(nameof(IsAttendance));}
        }
    }
    public bool IsHome=>Section==NavigationSection.Home;
    public bool IsCourses=>Section==NavigationSection.Courses;
    public bool IsHomeOrCourses=>IsHome || IsCourses;
    public bool IsBackups=>Section==NavigationSection.Backups;
    public bool IsAcademicNavigation=>Section==NavigationSection.Academic;
    public bool IsCourseContext=>IsAcademicNavigation;
    public bool IsGlobalContext=>!IsCourseContext;
    public bool IsNotes=>IsCourseContext && Page=="Notas";
    public bool IsAttendance=>IsCourseContext && Page=="Asistencias";
    public string PageTitle=>Section switch
    {
        NavigationSection.Home=>"Inicio", NavigationSection.Courses=>"Mis Materias",
        NavigationSection.Backups=>"Respaldos", _=>CourseName
    };
    public string PageSubtitle=>Section switch
    {
        NavigationSection.Home=>"Abre una materia y continúa tu trabajo académico.",
        NavigationSection.Courses=>"Selecciona una materia para trabajar en sus notas y asistencias.",
        NavigationSection.Backups=>"Conserva una copia de tu trabajo y recupérala cuando la necesites.", _=>""
    };
    public string Search {get=>search;set {if(Set(ref search,value))Rebuild();}}
    public string Message {get=>message;set=>Set(ref message,value);}
    public string SaveStatus {get=>saveStatus;private set=>Set(ref saveStatus,value);}
    public string CourseName=>Service.Current.CourseName;
    public string CourseInfo=>$"Código: {Service.Current.CourseCode}   ·   {Service.Current.Period}   ·   {Service.Current.School}{(Service.Current.GroupName.Length>0 ? $"   ·   Grupo {Service.Current.GroupName}" : "")}";
    public string StudentCount=>Service.Current.Students.Count.ToString();
    public string CourseAverage {get;private set;}="—";
    public bool HasRows=>GradeRows.Count>0;
    public bool IsEmpty=>!HasRows;
    public string EmptyTableMessage=>Service.Current.Students.Count==0 ? "Esta materia todavía no tiene estudiantes. Usa Importar Excel para agregar el listado." : "No hay estudiantes que coincidan con la búsqueda.";
    public ObservableRangeCollection<GradeRowViewModel> GradeRows {get;}=[];
    public ObservableRangeCollection<AttendanceRowViewModel> AttendanceRows {get;}=[];
    public ObservableCollection<GroupCard> Groups {get;}=[];
    public ObservableCollection<AttendanceWeek> Weeks {get;}=[];
    public bool CanAddWeek=>Weeks.Count<9;
    public AttendanceRowViewModel? SelectedStudent
    {
        get=>selectedStudent;
        set
        {
            // Las tablas pueden enviar null al refrescar sus elementos; el detalle
            // conserva su selección mientras existan estudiantes visibles.
            if(value is null && AttendanceRows.Count>0)return;
            Set(ref selectedStudent,value);
        }
    }
    public RelayCommand NavigateCommand {get;}
    public RelayCommand OpenCourseCommand {get;}
    public RelayCommand CutCommand {get;}
    public RelayCommand AddWeekCommand {get;}
    public RelayCommand ManageWeeksCommand {get;}
    public RelayCommand ConfigureCommand {get;}
    public RelayCommand DismissMessageCommand {get;}
    public MainViewModel(WorkspaceService service,CatalogService? catalog=null,IStudentWorkbookReader? workbookReader=null,IAcademicReportExporter? reportExporter=null,ILocalBackupService? localBackup=null)
    {
        Service=service;
        Catalog=catalog;
        WorkbookReader=workbookReader;ReportExporter=reportExporter;LocalBackup=localBackup;
        if(localBackup is null)BackupStatus="Los respaldos se habilitan en el catálogo local, fuera de la demostración.";
        ImportStudentsCommand=new(_=>ImportRequested?.Invoke(),_=>HasOpenCourse && WorkbookReader is not null && !FileBusy);
        ExportReportCommand=new(_=>ExportRequested?.Invoke(),_=>HasOpenCourse && ReportExporter is not null && !FileBusy);
        CreateBackupCommand=new(_=>BackupRequested?.Invoke(),_=>CanUseLocalBackup && !FileBusy);
        RestoreBackupCommand=new(_=>RestoreRequested?.Invoke(),_=>CanUseLocalBackup && !FileBusy);
        Home=new(service,catalog);
        OpenCourseCommand=new(parameter=>Try(()=>OpenCourse(parameter as CourseSummary)),_=>!FileBusy && (HasOpenCourse || Home.HasCourses));
        NewSemesterCommand=new(_=>CatalogEditorRequested?.Invoke(true),_=>Catalog is not null);
        NewCourseCommand=new(_=>CatalogEditorRequested?.Invoke(false),_=>Home.CanCreateCourse);
        ReloadCourseCommand=new(_=>Try(()=>ReloadCourse()),_=>Catalog is not null && HasOpenCourse);
        ToggleDetailCommand=new(_=>{DetailVisible=!DetailVisible;Raise(nameof(DetailHidden));});
        RecoveryCommand=new(_=>RecoveryRequested?.Invoke());
        Home.PropertyChanged+=(_,_)=>{OpenCourseCommand.Refresh();NewCourseCommand.Refresh();};
        NavigateCommand=new(parameter=> {
            if(parameter is "Inicio")Section=NavigationSection.Home;
            else if(parameter is "Materias")Section=NavigationSection.Courses;
            else if(parameter is "Respaldos")Section=NavigationSection.Backups;
            else if(parameter is "Académico")OpenCourseCommand.Execute(null);
            else if(parameter is "Notas" or "Asistencias") {if(HasOpenCourse)Page=(string)parameter;}
            else if(parameter is "Estudiantes" && HasOpenCourse)ImportStudentsCommand.Execute(null);
            else Message="Esta sección pertenece a un incremento posterior. Puedes trabajar en Notas y Asistencias.";
        },_=>!FileBusy);
        CutCommand=new(parameter=> {if(int.TryParse(parameter?.ToString(),out var number))SelectedCut=number;},_=>!FileBusy);
        AddWeekCommand=new(_=>WeekEditorRequested?.Invoke(),_=>CanAddWeek);
        ManageWeeksCommand=new(_=>WeekEditorRequested?.Invoke());
        ConfigureCommand=new(_=>ConfigurationRequested?.Invoke(),_=>!FileBusy);
        DismissMessageCommand=new(_=>Message="");
        Service.ChangedDetailed+=OnChanged;
        Service.RefreshFailed+=OnRefreshFailed;
        SaveStatus=IsDemo ? "Cambios guardados solo en la demostración" : "Guardado en este equipo · sin cuenta requerida";
        Rebuild();
    }
    private void OnChanged(WorkspaceChange change)
    {
        SaveStatus=IsDemo ? "Cambios guardados en la demostración" : "Cambios guardados en este equipo";
        if(change.Cut==SelectedCut && change.StudentId is { } studentId && change.Kind is WorkspaceChangeKind.Grade or WorkspaceChangeKind.Attendance)
        {
            var index=new AcademicCutReadIndex(Service.Cut(SelectedCut));
            if(change.Kind==WorkspaceChangeKind.Grade)
            {
                var position=GradeRows.ToList().FindIndex(r=>r.Student.Id==studentId);
                if(position>=0)
                {
                    var prior=GradeRows[position];
                    GradeRows[position]=new GradeRowViewModel(Service,SelectedCut,prior.Student,prior.Number,index);
                }
                UpdateCourseAverage(index);
            }
            else
            {
                var position=AttendanceRows.ToList().FindIndex(r=>r.Student.Id==studentId);
                if(position>=0)
                {
                    var prior=AttendanceRows[position];
                    var updated=new AttendanceRowViewModel(Service,SelectedCut,prior.Student,prior.Number,index);
                    AttendanceRows[position]=updated;
                    if(SelectedStudent?.Student.Id==studentId)SelectedStudent=updated;
                }
            }
            return;
        }
        if(change.Kind is WorkspaceChangeKind.Structure or WorkspaceChangeKind.Students || change.Cut==SelectedCut)Rebuild();
        if(change.Kind==WorkspaceChangeKind.Students){if(Catalog is not null)Home.Refresh();OpenCourseCommand.Refresh();}
    }
    private void OnRefreshFailed(AcademicRefreshException error)
    {
        SaveStatus="Guardado confirmado · actualización de pantalla fallida";
        Message=error.Message;
    }
    private void OpenCourse(CourseSummary? course)
    {
        if(Catalog is not null)
        {
            var id=course?.Id ?? (HasOpenCourse ? Service.Current.CourseId : Home.Courses.FirstOrDefault()?.Id);
            if(string.IsNullOrEmpty(id))return;
            if(Service.Current.CourseId!=id)
            {
                if(HasOpenCourse)courseStates[Service.Current.CourseId]=(SelectedCut,Page,Search,SelectedStudent?.Student.Id);
                var next=Catalog.Open(id);Service.ChangedDetailed-=OnChanged;Service.RefreshFailed-=OnRefreshFailed;
                Service=next;Service.ChangedDetailed+=OnChanged;Service.RefreshFailed+=OnRefreshFailed;
                var saved=courseStates.GetValueOrDefault(id,(1,"Notas","",null));
                selectedCut=saved.Cut;page=saved.Page;search=saved.Search;selectedStudent=null;Message="";
                Rebuild();if(saved.Student is not null)SelectedStudent=AttendanceRows.FirstOrDefault(r=>r.Student.Id==saved.Student) ?? SelectedStudent;
                foreach(var name in new[]{nameof(SelectedCut),nameof(CutLabel),nameof(Search),nameof(IsDemo),nameof(StorageLabel),nameof(CourseName),nameof(CourseInfo),nameof(StudentCount),nameof(HasOpenCourse),nameof(PageTitle)})Raise(name);
                SaveStatus="Materia abierta · datos guardados en este equipo";ReloadCourseCommand.Refresh();ImportStudentsCommand.Refresh();ExportReportCommand.Refresh();
            }
        }
        if(HasOpenCourse)Section=NavigationSection.Academic;
    }
    private void ReloadCourse()
    {
        if(Catalog is null || !HasOpenCourse)return;var next=Catalog.Open(Service.Current.CourseId);
        Service.ChangedDetailed-=OnChanged;Service.RefreshFailed-=OnRefreshFailed;Service=next;
        Service.ChangedDetailed+=OnChanged;Service.RefreshFailed+=OnRefreshFailed;Rebuild();Message="";SaveStatus="Materia actualizada desde el archivo local";
    }
    public void Try(Action action)
    {
        try {action();Message="";} catch(Exception ex) {Message=ex is AcademicRefreshException or AcademicValidationException ? ex.Message : $"No se completó la operación ({SafeErrorCode.From(ex)}). Revisa tus datos locales y reintenta.";}
    }
    private void Rebuild()
    {
        var selectedId=SelectedStudent?.Student.Id;
        var cut=Service.Cut(SelectedCut);
        var index=new AcademicCutReadIndex(cut);
        Groups.Clear();
        foreach(var group in cut.Groups)Groups.Add(new(group.Name,$"{group.Weight:0.##} %",group.Mode==EvaluationMode.Direct ? "Nota directa" : $"Promedio simple · {group.Activities.Count} subactividades",ConfigureCommand));
        Weeks.Clear();foreach(var week in cut.Weeks)Weeks.Add(week);
        // Remove the old row contexts before rebuilding columns. Publish new rows only
        // after both tables have their matching grade/week schema.
        GradeRows.Clear();AttendanceRows.Clear();
        Raise(nameof(Groups));Raise(nameof(Weeks));
        var grades=new List<GradeRowViewModel>();var attendance=new List<AttendanceRowViewModel>();
        int i=0;
        foreach(var student in Service.Current.Students)
        {
            i++;
            if(!student.FullName.Contains(Search,StringComparison.OrdinalIgnoreCase) && !student.Document.Contains(Search,StringComparison.OrdinalIgnoreCase))continue;
            grades.Add(new(Service,SelectedCut,student,i,index));
            attendance.Add(new(Service,SelectedCut,student,i,index));
        }
        GradeRows.ReplaceAll(grades);AttendanceRows.ReplaceAll(attendance);
        SelectedStudent=AttendanceRows.FirstOrDefault(x=>x.Student.Id==selectedId) ?? AttendanceRows.FirstOrDefault();
        UpdateCourseAverage(index);
        Raise(nameof(HasRows));Raise(nameof(IsEmpty));Raise(nameof(CanAddWeek));
        Raise(nameof(EmptyTableMessage));Raise(nameof(StudentCount));
        AddWeekCommand.Refresh();
    }
    private void UpdateCourseAverage(AcademicCutReadIndex index)
    {
        var totals=Service.Current.Students.Select(s=>index.Cut(s.Id)).ToList();
        CourseAverage=totals.Count>0 && totals.All(x=>x is not null) ? AcademicFormatting.Grade(totals.Average(x=>x!.Value)) : "Pendiente";
        Raise(nameof(CourseAverage));
    }
}
