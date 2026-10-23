using System.Collections.ObjectModel;
using GestorDocente.Application;
using GestorDocente.Domain;

namespace GestorDocente.Desktop.ViewModels;

public sealed record GroupCard(string Name,string WeightLabel,string TypeLabel,RelayCommand ConfigureCommand,string Id="");
public enum NavigationSection { Home, Courses, Academic, Backups }
public sealed partial class MainViewModel : ObservableObject
{
    public WorkspaceService Service {get;private set;}
    public CatalogService? Catalog {get;}
    public CourseParameters? CopiedParameters {get;private set;}
    public string CopiedParametersSource {get;private set;}="";
    public RelayCommand CopyCourseParametersCommand {get;}
    public IStudentWorkbookReader? WorkbookReader {get;}
    public IAcademicReportExporter? ReportExporter {get;}
    public IAttendancePdfExporter? AttendancePdfExporter {get;}
    public INotesPdfExporter? NotesPdfExporter {get;}
    public ILocalBackupService? LocalBackup {get;}
    public AppearanceViewModel Appearance{get;}
    public UpdatesViewModel Updates{get;}
    public RelayCommand UpdatesCommand{get;}
    public event Action? UpdatesRequested;
    public RelayCommand PersonalizeCommand{get;}
    public event Action? AppearanceRequested;
    private readonly Dictionary<string,(int Cut,string Page,string Search,string? Student)> courseStates=[];
    private readonly List<GradeRowViewModel> allGradeRows=[];
    private readonly List<AttendanceRowViewModel> allAttendanceRows=[];
    private readonly Dictionary<string,int> studentPositions=[];
    private AcademicCutReadIndex? readIndex;
    private decimal?[] courseTotals=[];
    private decimal courseTotalSum;
    private int completedStudents;
    private AttendanceSummary groupAttendanceSummary=new(0,0,0,0);
    public AttendanceSummary GroupAttendanceSummary=>groupAttendanceSummary;
    public decimal? GroupAttendancePercentage=>groupAttendanceSummary.Percentage;
    public string GroupAttendanceLabel=>GroupAttendancePercentage is null ? "Sin registros" : AttendanceFormatting.Percentage(GroupAttendancePercentage);
    private sealed record CourseProjection(AcademicCutReadIndex Index,List<GradeRowViewModel> Grades,List<AttendanceRowViewModel> Attendance,Dictionary<string,int> Positions,decimal?[] Totals);
    public HomeViewModel Home {get;}
    public DashboardViewModel Dashboard {get;}
    public ConsolidatedViewModel Consolidated {get;}=new();
    public IReadOnlyList<CutColumn> Cuts=>Service.Current.Cuts.OrderBy(c=>c.Number).Select(c=>new CutColumn(c.Number,c.Weight,c.Number==SelectedCut)).ToArray();
    public string SelectedCutWeight=>$"Peso en nota final: {Service.Cut(SelectedCut).Weight:0.##} %";
    private NavigationSection section=NavigationSection.Home;
    private int selectedCut=1;
    private string page="Notas",search="",message="",saveStatus="Datos ficticios · cambios guardados solo en la demostración";
    private AttendanceRowViewModel? selectedStudent;
    public event Action? ConfigurationRequested;
    public event Action? CutWeightsRequested;
    public event Action<int>? CutDeletionRequested;
    public event Action<bool>? CatalogEditorRequested;
    public event Action? WeekEditorRequested;
    public event Action? RecoveryRequested;
    public event Action? ImportRequested;
    public event Action? StudentEditorRequested;
    public event Action<Student>? StudentDetailsRequested;
    public RelayCommand AddStudentCommand{get;}
    public RelayCommand ViewStudentDetailsCommand{get;}
    public event Action? ExportRequested;
    public event Action? AttendancePdfRequested;
    public event Action? NotesPdfRequested;
    public event Action<string>? GradePasteRequested;
    public event Action? AttendanceGradingRequested;
    public RelayCommand PasteGradesCommand{get;}
    public RelayCommand ConfigureAttendanceGradingCommand{get;}
    public RelayCommand SortStudentsCommand{get;}
    private bool? nameDescending;
    public string NameHeaderLabel=>nameDescending switch{true=>"Nombre completo · Z–A",false=>"Nombre completo · A–Z",_=>"Nombre completo ↕"};
    public bool AttendanceGradingEnabled=>Service.Cut(SelectedCut).AttendanceGrading?.Enabled==true;
    public string AttendanceGradingLabel=>AttendanceGradingEnabled ? "Nota de asistencia: activa" : "Activar nota de asistencia";
    public event Action<CourseSummary>? CourseDeletionRequested;
    public event Action<CourseSummary>? CourseEditionRequested;
    public event Action<AcademicSemester>? SemesterDeletionRequested;
    public event Action? BackupRequested;
    public event Action? RestoreRequested;
    private bool fileBusy;
    public bool FileBusy{get=>fileBusy;set{if(Set(ref fileBusy,value)){SortStudentsCommand.Refresh();PasteGradesCommand.Refresh();ConfigureAttendanceGradingCommand.Refresh();PersonalizeCommand.Refresh();UpdatesCommand.Refresh();ImportStudentsCommand.Refresh();AddStudentCommand.Refresh();ViewStudentDetailsCommand.Refresh();ExportReportCommand.Refresh();ExportAttendancePdfCommand.Refresh();ExportNotesPdfCommand.Refresh();CreateBackupCommand.Refresh();RestoreBackupCommand.Refresh();ChooseBackupFolderCommand.Refresh();ClearLocalDataCommand.Refresh();NavigateCommand.Refresh();OpenCourseCommand.Refresh();CopyCourseParametersCommand.Refresh();EditCourseCommand.Refresh();DeleteCourseCommand.Refresh();DeleteSemesterCommand.Refresh();NewSemesterCommand.Refresh();NewCourseCommand.Refresh();ReloadCourseCommand.Refresh();CutCommand.Refresh();ConfigureCommand.Refresh();AddCutCommand.Refresh();ConfigureCutWeightsCommand.Refresh();DeleteCutCommand.Refresh();Raise(nameof(CanDeleteCut));}}}
    public RelayCommand ImportStudentsCommand{get;}
    public RelayCommand ExportReportCommand{get;}
    public RelayCommand ExportAttendancePdfCommand{get;}
    public RelayCommand ExportNotesPdfCommand{get;}
    public RelayCommand CreateBackupCommand{get;}
    public RelayCommand RestoreBackupCommand{get;}
    private string backupStatus="Elige una carpeta una sola vez. Las copias nuevas se guardan sin contraseña.";
    public string BackupStatus{get=>backupStatus;set=>Set(ref backupStatus,value);}
    public bool CanUseLocalBackup=>LocalBackup is not null;
    public bool IsDemo=>Service.Current.IsDemo;
    public string StorageLabel=>IsDemo ? "DEMOSTRACIÓN · DATOS FICTICIOS" : "DATOS LOCALES";
    public bool HasOpenCourse=>!string.IsNullOrWhiteSpace(Service.Current.CourseName);
    private bool detailVisible;
    public bool DetailVisible {get=>detailVisible;set{if(Set(ref detailVisible,value))Raise(nameof(DetailHidden));}}
    public bool DetailHidden=>!DetailVisible;
    public RelayCommand ToggleDetailCommand {get;}
    public RelayCommand NewSemesterCommand {get;}
    public RelayCommand NewCourseCommand {get;}
    public RelayCommand DeleteCourseCommand {get;}
    public RelayCommand EditCourseCommand {get;}
    public RelayCommand DeleteSemesterCommand {get;}
    public RelayCommand RecoveryCommand {get;}
    public RelayCommand ReloadCourseCommand {get;}
    public int SelectedCut
    {
        get=>selectedCut;
        set {_=Service.Cut(value);if(Set(ref selectedCut,value)){Rebuild();Raise(nameof(CutLabel));Raise(nameof(SelectedCutWeight));Raise(nameof(Cuts));}}
    }
    public string CutLabel=>$"Corte {SelectedCut}";
    public NavigationSection Section
    {
        get=>section;
        private set
        {
            if(!Set(ref section,value))return;
            if(value==NavigationSection.Home)_=Dashboard.RefreshAsync();
            else Dashboard.Suspend();
            if(value==NavigationSection.Courses && Catalog is not null && Home.Semesters.Count==0)_=Home.RefreshAsync();
            foreach(var name in new[] {nameof(IsHome),nameof(IsCourses),nameof(IsBackups),
                nameof(IsAcademicNavigation),nameof(IsCourseContext),nameof(IsGlobalContext),
                nameof(IsNotes),nameof(IsAttendance),nameof(IsConsolidated),nameof(PageTitle),nameof(PageSubtitle)})Raise(name);
        }
    }
    // La última pestaña académica se mantiene independiente de las páginas globales.
    public string Page
    {
        get=>page;
        set
        {
            if(value is not ("Notas" or "Asistencias" or "Nota consolidada"))throw new ArgumentException("Selecciona una pestaña académica válida.",nameof(value));
            Section=NavigationSection.Academic;
            if(Set(ref page,value)){Raise(nameof(IsNotes));Raise(nameof(IsAttendance));Raise(nameof(IsConsolidated));}
        }
    }
    public bool IsHome=>Section==NavigationSection.Home;
    public bool IsCourses=>Section==NavigationSection.Courses;
    public bool IsBackups=>Section==NavigationSection.Backups;
    public bool IsAcademicNavigation=>Section==NavigationSection.Academic;
    public bool IsCourseContext=>IsAcademicNavigation;
    public bool IsGlobalContext=>!IsCourseContext;
    public bool IsNotes=>IsCourseContext && Page=="Notas";
    public bool IsAttendance=>IsCourseContext && Page=="Asistencias";
    public bool IsConsolidated=>IsCourseContext && Page=="Nota consolidada";
    public string PageTitle=>Section switch
    {
        NavigationSection.Home=>"Inicio", NavigationSection.Courses=>"Mis Materias",
        NavigationSection.Backups=>"Respaldos", _=>CourseName
    };
    public string PageSubtitle=>Section switch
    {
        NavigationSection.Home=>"Estadísticas de notas por materia y por cada corte académico.",
        NavigationSection.Courses=>"Selecciona una materia para trabajar en sus notas y asistencias.",
        NavigationSection.Backups=>"Conserva una copia de tu trabajo y recupérala cuando la necesites.", _=>""
    };
    private string searchInput="";
    private readonly DeferredUiAction searchUpdate;
    public string SearchInput
    {
        get=>searchInput;
        set
        {
            if(!Set(ref searchInput,value ?? ""))return;
            if(searchInput.Length==0){searchUpdate.Cancel();Search="";}else searchUpdate.Schedule();
        }
    }
    public void FlushSearch()=>searchUpdate.Flush();
    public string Search
    {
        get=>search;
        set
        {
            searchUpdate.Cancel();var next=value ?? "";
            if(searchInput!=next){searchInput=next;Raise(nameof(SearchInput));}
            if(Set(ref search,next))ApplySearchFilter();
        }
    }
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
            if(Set(ref selectedStudent,value))ExportAttendancePdfCommand.Refresh();
        }
    }
    public RelayCommand NavigateCommand {get;}
    public RelayCommand OpenCourseCommand {get;}
    public RelayCommand CutCommand {get;}
    public RelayCommand AddCutCommand {get;}
    public RelayCommand DeleteCutCommand {get;}
    public bool CanDeleteCut=>HasOpenCourse && Service.Current.Cuts.Count>1 && !FileBusy && !Service.IsSaving;
    public string DeleteCutHint=>Service.Current.Cuts.Count<=1 ? "No puedes eliminar el último corte de la materia." : "Eliminar corte: requiere confirmación y elimina sus notas y asistencias.";
    public RelayCommand ConfigureCutWeightsCommand {get;}
    public RelayCommand AddWeekCommand {get;}
    public RelayCommand ManageWeeksCommand {get;}
    public RelayCommand ConfigureCommand {get;}
    public string? ConfigurationGroupId {get;private set;}
    public RelayCommand DismissMessageCommand {get;}
    public MainViewModel(WorkspaceService service,CatalogService? catalog=null,IStudentWorkbookReader? workbookReader=null,IAcademicReportExporter? reportExporter=null,ILocalBackupService? localBackup=null,IAttendancePdfExporter? attendancePdfExporter=null,InterfacePreferencesService? appearance=null,INotesPdfExporter? notesPdfExporter=null,IApplicationUpdater? updater=null,IUpdateSafetyCheck? updateSafety=null,BackupPreferencesService? backupPreferences=null,ILocalDataResetService? dataReset=null,AutomaticBackupCoordinator? automaticBackups=null)
    {
        Service=service;
        searchUpdate=new(TimeSpan.FromMilliseconds(100),()=>Search=searchInput);
        SortStudentsCommand=new(_=>{FlushSearch();nameDescending=nameDescending is false;Raise(nameof(NameHeaderLabel));ApplySearchFilter();},_=>!FileBusy);
        PasteGradesCommand=new(value=>{FlushSearch();if(value is string id && GradeRows.Count>0 && !FileBusy && !Service.IsSaving)GradePasteRequested?.Invoke(id);},_=>HasOpenCourse && GradeRows.Count>0 && !FileBusy && !Service.IsSaving);
        ConfigureAttendanceGradingCommand=new(_=>AttendanceGradingRequested?.Invoke(),_=>HasOpenCourse && !FileBusy && !Service.IsSaving);
        Appearance=new(appearance ?? new InterfacePreferencesService(new MemoryInterfacePreferencesStore()));
        AutomaticBackups=automaticBackups;
        if(automaticBackups is not null)automaticBackups.Changed+=OnAutomaticBackupChanged;
        Updates=new(updater,updateSafety,BeginUpdateMaintenance,PrepareAutomaticBackupForUpdateAsync);
        Updates.PropertyChanged+=(_,e)=>{if(e.PropertyName==nameof(Updates.CanUseApplication))Raise(nameof(CanInteractWithWorkspace));};
        UpdatesCommand=new(_=>UpdatesRequested?.Invoke(),_=>!FileBusy);
        PersonalizeCommand=new(_=>AppearanceRequested?.Invoke(),_=>!FileBusy && !Service.IsSaving);
        Catalog=catalog;
        WorkbookReader=workbookReader;ReportExporter=reportExporter;LocalBackup=localBackup;AttendancePdfExporter=attendancePdfExporter;NotesPdfExporter=notesPdfExporter;
        BackupPreferences=backupPreferences ?? new(new MemoryBackupPreferencesStore());DataReset=dataReset;
        ChooseBackupFolderCommand=new(_=>BackupFolderRequested?.Invoke(),_=>CanUseLocalBackup && !FileBusy);
        ClearLocalDataCommand=new(_=>LocalDataResetRequested?.Invoke(),_=>DataReset is not null && !FileBusy && !Service.IsSaving);
        if(localBackup is null)BackupStatus="Los respaldos se habilitan en el catálogo local, fuera de la demostración.";
        ImportStudentsCommand=new(_=>ImportRequested?.Invoke(),_=>HasOpenCourse && WorkbookReader is not null && !FileBusy);
        AddStudentCommand=new(_=>StudentEditorRequested?.Invoke(),_=>HasOpenCourse && !FileBusy && !Service.IsSaving);
        ViewStudentDetailsCommand=new(value=>{if(value is Student student)StudentDetailsRequested?.Invoke(student);},_=>HasOpenCourse && !FileBusy);
        ExportReportCommand=new(_=>ExportRequested?.Invoke(),_=>HasOpenCourse && ReportExporter is not null && !FileBusy);
        ExportAttendancePdfCommand=new(_=>AttendancePdfRequested?.Invoke(),_=>HasOpenCourse && SelectedStudent is not null && AttendancePdfExporter is not null && !FileBusy);
        ExportNotesPdfCommand=new(_=>NotesPdfRequested?.Invoke(),_=>HasOpenCourse && NotesPdfExporter is not null && !FileBusy && !Service.IsSaving);
        CreateBackupCommand=new(_=>BackupRequested?.Invoke(),_=>CanUseLocalBackup && !string.IsNullOrWhiteSpace(BackupFolder) && !FileBusy && !Service.IsSaving);
        RestoreBackupCommand=new(_=>RestoreRequested?.Invoke(),_=>CanUseLocalBackup && !FileBusy);
        Home=new(service,catalog,loadImmediately:false);
        Dashboard=new(catalog,()=>Service.Current,loadImmediately:false);
        OpenCourseCommand=new(parameter=>_ = OpenCourseAsync(parameter as CourseSummary),_=>!FileBusy && (HasOpenCourse || Home.HasCourses));
        NewSemesterCommand=new(_=>CatalogEditorRequested?.Invoke(true),_=>Catalog is not null && !FileBusy);
        NewCourseCommand=new(_=>CatalogEditorRequested?.Invoke(false),_=>Home.CanCreateCourse && !FileBusy);
        DeleteSemesterCommand=new(_=>{if(Home.SelectedSemester is { } semester)SemesterDeletionRequested?.Invoke(semester);},
            _=>Catalog is not null && Home.SelectedSemester is not null && !FileBusy);
        EditCourseCommand=new(value=>{if(value is CourseSummary course)CourseEditionRequested?.Invoke(course);},_=>Catalog is not null && !FileBusy);
        CopyCourseParametersCommand=new(value=>{if(value is CourseSummary course)_=CopyCourseParametersAsync(course);},
            value=>Catalog is not null && value is CourseSummary {CanDelete:true} && !FileBusy && !Service.IsSaving);
        DeleteCourseCommand=new(value=>{if(value is CourseSummary course)CourseDeletionRequested?.Invoke(course);},_=>Catalog is not null && !FileBusy);
        ReloadCourseCommand=new(_=>_ = ReloadCourseAsync(),_=>Catalog is not null && HasOpenCourse && !FileBusy);
        ToggleDetailCommand=new(_=>{DetailVisible=!DetailVisible;Raise(nameof(DetailHidden));});
        RecoveryCommand=new(_=>RecoveryRequested?.Invoke());
        Home.PropertyChanged+=(_,_)=>{OpenCourseCommand.Refresh();NewCourseCommand.Refresh();DeleteSemesterCommand.Refresh();};
        NavigateCommand=new(parameter=> {
            if(parameter is "Inicio")Section=NavigationSection.Home;
            else if(parameter is "Materias")Section=NavigationSection.Courses;
            else if(parameter is "Respaldos")Section=NavigationSection.Backups;
            else if(parameter is "Académico")OpenCourseCommand.Execute(null);
            else if(parameter is "Reportes" && HasOpenCourse){Page="Asistencias";DetailVisible=true;Raise(nameof(DetailHidden));}
            else if(parameter is "Notas" or "Asistencias" or "Nota consolidada") {if(HasOpenCourse)Page=(string)parameter;}
            else Message="Esta sección pertenece a un incremento posterior. Puedes trabajar en Notas y Asistencias.";
        },_=>!FileBusy);
        CutCommand=new(parameter=> {if(int.TryParse(parameter?.ToString(),out var number))_=SelectCutAsync(number);},_=>!FileBusy && !Service.IsSaving);
        AddCutCommand=new(_=>_ = AddCutAsync(),_=>HasOpenCourse && !FileBusy && !Service.IsSaving);
        DeleteCutCommand=new(value=>{if(CanDeleteCut && value is int number && Service.Current.Cuts.Any(c=>c.Number==number))CutDeletionRequested?.Invoke(number);},value=>CanDeleteCut && value is int number && Service.Current.Cuts.Any(c=>c.Number==number));
        ConfigureCutWeightsCommand=new(_=>CutWeightsRequested?.Invoke(),_=>HasOpenCourse && !FileBusy && !Service.IsSaving);
        AddWeekCommand=new(_=>WeekEditorRequested?.Invoke(),_=>CanAddWeek);
        ManageWeeksCommand=new(_=>WeekEditorRequested?.Invoke());
        ConfigureCommand=new(group=>{ConfigurationGroupId=group as string;ConfigurationRequested?.Invoke();},_=>!FileBusy);
        DismissMessageCommand=new(_=>Message="");
        Service.ChangedDetailed+=OnChanged;
        Service.RefreshFailed+=OnRefreshFailed;
        SaveStatus=IsDemo ? "Cambios guardados solo en la demostración" : "Guardado en este equipo · sin cuenta requerida";
        Rebuild();
        Consolidated.Refresh(Service.Current);
    }
    public async Task AddCutAsync()
    {
        if(FileBusy || Service.IsSaving)return;
        FileBusy=true;var before=Service.Current;
        try
        {
            var number=await Service.AddCutAsync();SelectedCut=number;
            Message=$"Corte {number} creado. Su peso inicial es 0 %; puedes ajustarlo en Nota consolidada → Configurar porcentajes.";
            if(Catalog is not null)await Home.RefreshAsync();else Home.Refresh();
        }
        catch(Exception ex){Message=Service.Current!=before || ex is AcademicRefreshException ? "El corte se guardó, pero falló el refresco. Pulsa Actualizar." : ex is AcademicValidationException ? ex.Message : $"No se agregó el corte ({SafeErrorCode.From(ex)}). Reintenta.";}
        finally{FileBusy=false;}
    }
    public async Task DeleteCutAsync(CutDeletionPreview preview,bool confirmed=false)
    {
        if(!confirmed || FileBusy)return;
        FileBusy=true;var before=Service.Current;
        try
        {
            await Service.DeleteCutAsync(preview,true);
            Message=$"Corte {preview.Number} eliminado. Los cortes restantes se renumeraron y conservan sus porcentajes. Revisa la ponderación para completar el 100 %.";
            if(Catalog is not null)await Home.RefreshAsync();else Home.Refresh();
            FileBusy=false;
            CutWeightsRequested?.Invoke();
        }
        catch(Exception ex)
        {
            Message=Service.Current!=before || ex is AcademicRefreshException ? "El corte se eliminó, pero falló el refresco. Pulsa Actualizar y revisa los porcentajes." :
                ex is AcademicValidationException ? ex.Message : $"No se eliminó el corte ({SafeErrorCode.From(ex)}). Los datos se conservaron; reintenta.";
        }
        finally{FileBusy=false;}
    }
    private void OnChanged(WorkspaceChange change)
    {
        SaveStatus=IsDemo ? "Cambios guardados en la demostración" : "Cambios guardados en este equipo";
        if(change.Kind==WorkspaceChangeKind.AttendanceGrading)
        {
            if(change.Cut==SelectedCut)
            {
                // La política informativa solo reemplaza las filas de asistencia; conserva borradores y columnas de Notas.
                readIndex=new(Service.Cut(SelectedCut));
                for(var i=0;i<allAttendanceRows.Count;i++)
                {
                    var prior=allAttendanceRows[i];allAttendanceRows[i]=new(Service,SelectedCut,prior.Student,prior.Number,readIndex);
                }
                ApplySearchFilter();Raise(nameof(AttendanceGradingEnabled));Raise(nameof(AttendanceGradingLabel));
            }
            return;
        }
        if(change.RemovedCut is {} removed)
        {
            selectedCut=selectedCut>removed ? selectedCut-1 : Math.Min(selectedCut,Service.Current.Cuts.Count);
            Raise(nameof(SelectedCut));Raise(nameof(CutLabel));
        }
        if(change.Kind==WorkspaceChangeKind.Grade)Consolidated.ApplyGrade(change);
        else if(change.Kind==WorkspaceChangeKind.GradesBatch)Consolidated.Refresh(Service.Current);
        else if(change.Kind is WorkspaceChangeKind.Structure or WorkspaceChangeKind.Students)
        {
            Consolidated.Refresh(Service.Current);Raise(nameof(Cuts));Raise(nameof(SelectedCutWeight));Raise(nameof(CanDeleteCut));Raise(nameof(DeleteCutHint));DeleteCutCommand.Refresh();
        }
        if(change.Cut==SelectedCut && change.StudentId is { } studentId && change.Kind is WorkspaceChangeKind.Grade or WorkspaceChangeKind.Attendance)
        {
            if(readIndex is null){Rebuild();return;}
            var index=readIndex;
            if(change.Kind==WorkspaceChangeKind.Grade)
            {
                if(change.ActivityId is null){Rebuild();return;}
                index.ApplyGrade(studentId,change.ActivityId,change.Grade);
                if(studentPositions.TryGetValue(studentId,out var position))
                {
                    var previousTotal=courseTotals[position];
                    var nextTotal=index.Cut(studentId);
                    courseTotals[position]=nextTotal;
                    if(previousTotal is { } old){courseTotalSum-=old;completedStudents--;}
                    if(nextTotal is { } current){courseTotalSum+=current;completedStudents++;}
                    var prior=allGradeRows[position];
                    var updated=new GradeRowViewModel(Service,SelectedCut,prior.Student,prior.Number,index);
                    allGradeRows[position]=updated;
                    var visiblePosition=GradeRows.ToList().FindIndex(r=>r.Student.Id==studentId);
                    if(visiblePosition>=0)GradeRows[visiblePosition]=updated;
                }
                PublishCourseAverage();
            }
            else
            {
                if(change.Week is null){Rebuild();return;}
                index.ApplyAttendance(studentId,change.Week.Value,change.Attendance);
                if(studentPositions.TryGetValue(studentId,out var position))
                {
                    var selectedId=SelectedStudent?.Student.Id;
                    var prior=allAttendanceRows[position];
                    var updated=new AttendanceRowViewModel(Service,SelectedCut,prior.Student,prior.Number,index);
                    UpdateGroupAttendance(prior.Summary,updated.Summary);
                    allAttendanceRows[position]=updated;
                    var visiblePosition=AttendanceRows.ToList().FindIndex(r=>r.Student.Id==studentId);
                    if(visiblePosition>=0)AttendanceRows[visiblePosition]=updated;
                    // DataGrid puede seleccionar la fila siguiente al reemplazar un elemento.
                    // Restaurar la selección previa después de publicar la fila persistida.
                    if(selectedId is not null && studentPositions.TryGetValue(selectedId,out var selectedPosition))
                        SelectedStudent=allAttendanceRows[selectedPosition];
                }
            }
            return;
        }
        if(change.Kind is WorkspaceChangeKind.Structure or WorkspaceChangeKind.Students || change.Cut==SelectedCut)Rebuild();
        if(change.Kind==WorkspaceChangeKind.Students){if(Catalog is not null)_ = Home.RefreshAsync();OpenCourseCommand.Refresh();}
    }
    private void OnRefreshFailed(AcademicRefreshException error)
    {
        SaveStatus="Guardado confirmado · actualización de pantalla fallida";
        Message=error.Message;
    }
    private IDisposable BeginUpdateMaintenance()
    {
        if(FileBusy || Service.IsSaving)throw new AcademicValidationException("Espera a que termine la operación o el guardado antes de actualizar SIGNA.");
        if(allGradeRows.Any(r=>r.HasPendingEdits) || allAttendanceRows.Any(r=>r.HasPendingEdits))
            throw new AcademicValidationException("Hay notas o asistencias pendientes de guardar. Revísalas y guárdalas antes de actualizar SIGNA.");
        var pause=AutomaticBackups?.Pause();FileBusy=true;
        return new UpdateMaintenance(()=>{FileBusy=false;pause?.Dispose();});
    }
    private sealed class UpdateMaintenance(Action release):IDisposable{public void Dispose()=>release();}
    public async Task CopyCourseParametersAsync(CourseSummary course)
    {
        if(Catalog is null || !course.CanDelete || FileBusy || Service.IsSaving)return;
        FileBusy=true;
        try
        {
            var parameters=await Catalog.CopyParametersAsync(course.Id);
            CopiedParameters=parameters;CopiedParametersSource=$"{course.Name} · {course.Period}";
            Raise(nameof(CopiedParameters));Raise(nameof(CopiedParametersSource));
            Message=$"Parámetros copiados de «{course.Name}» ({parameters.Cuts.Count} cortes). Puedes cambiar de semestre y elegir Crear materia → Pegar parámetros. No se copiaron estudiantes ni registros. La copia estará disponible hasta cerrar SIGNA.";
        }
        catch(Exception ex){Message=ex is AcademicValidationException ? ex.Message : $"No se pudieron copiar los parámetros ({SafeErrorCode.From(ex)}). Revisa el acceso al archivo local e intenta de nuevo.";}
        finally{FileBusy=false;}
    }
    public async Task OpenCourseAsync(CourseSummary? course)
    {
        if(FileBusy)return;
        FlushSearch();
        try
        {
            if(Catalog is not null)
            {
                var id=course?.Id ?? (HasOpenCourse ? Service.Current.CourseId : Home.Courses.FirstOrDefault()?.Id);
                if(string.IsNullOrEmpty(id))return;
                if(Service.Current.CourseId!=id)
                {
                    var saved=courseStates.GetValueOrDefault(id,(1,"Notas","",null));
                    FileBusy=true;SaveStatus="Abriendo materia…";
                    var (next,projection)=await Task.Run(()=>
                    {
                        var loaded=Catalog.Open(id);
                        return (loaded,BuildProjection(loaded,saved.Cut));
                    });
                    if(Service.IsSaving)throw new AcademicValidationException("Espera a que termine de guardarse la materia actual antes de abrir otra.");
                    if(HasOpenCourse)courseStates[Service.Current.CourseId]=(SelectedCut,Page,Search,SelectedStudent?.Student.Id);
                    SwitchCourse(next,saved,projection);
                }
            }
            if(HasOpenCourse)Section=NavigationSection.Academic;
            Message="";
        }
        catch(Exception ex){Message=ex is AcademicRefreshException or AcademicValidationException ? ex.Message : $"No se pudo abrir la materia ({SafeErrorCode.From(ex)}). Revisa el archivo local y reintenta.";}
        finally{if(FileBusy){FileBusy=false;if(SaveStatus=="Abriendo materia…")SaveStatus="Materia anterior conservada · revisa el aviso";}}
    }
    public async Task DeleteCourseAsync(CourseSummary course)
    {
        if(Catalog is null || !course.CanDelete || FileBusy)return;
        if(Service.IsSaving){Message="Espera a que termine de guardarse la materia antes de eliminarla.";return;}
        FileBusy=true;
        var deleted=false;
        try
        {
            await Catalog.DeleteCourseAsync(course.Id);
            deleted=true;
            courseStates.Remove(course.Id);
            if(Service.Current.CourseId==course.Id)
            {
                var empty=new WorkspaceService(new EmptyWorkspaceStore());
                SwitchCourse(empty,(1,"Notas","",null),BuildProjection(empty,1));
                Section=NavigationSection.Courses;
            }
            await Home.RefreshAsync();
            Message=$"Se eliminó «{course.Name}» y sus datos de este equipo.";
            SaveStatus="Catálogo actualizado en este equipo";
        }
        catch(Exception ex)
        {
            Message=deleted || ex is AcademicRefreshException
                ? $"La materia se eliminó, pero falló la actualización de la pantalla ({SafeErrorCode.From(ex)}). Pulsa Actualizar en Mis Materias."
                : ex is AcademicValidationException ? ex.Message : $"No se pudo eliminar la materia ({SafeErrorCode.From(ex)}). Actualiza e inténtalo de nuevo.";
        }
        finally{FileBusy=false;}
    }
    public async Task RefreshEditedCourseAsync(string courseId,string semesterId)
    {
        if(Catalog is null)return;
        if(Service.Current.CourseId==courseId)
        {
            if(Service.IsSaving)throw new AcademicValidationException("Espera a que termine el guardado de la materia antes de editarla.");
            var saved=(SelectedCut,Page,Search,SelectedStudent?.Student.Id);
            var (next,projection)=await Task.Run(()=>
            {
                var loaded=Catalog.Open(courseId);
                return (loaded,BuildProjection(loaded,saved.SelectedCut));
            });
            SwitchCourse(next,saved,projection);
        }
        await Home.RefreshAsync();
        var selected=Home.Semesters.FirstOrDefault(s=>s.Id==semesterId);
        if(selected is not null && Home.SelectedSemester?.Id!=semesterId)
        {
            Home.SelectedSemester=selected;
            await Home.RefreshAsync();
        }
        Message="Materia editada y guardada en este equipo.";
    }
    public async Task DeleteSemesterAsync(SemesterDeletionPreview confirmed)
    {
        if(Catalog is null || FileBusy)return;
        if(Service.IsSaving){Message="Espera a que termine de guardarse la materia antes de eliminar el semestre.";return;}
        FileBusy=true;var committed=false;string? refreshWarning=null;
        try
        {
            try{await Catalog.DeleteSemesterAsync(confirmed);committed=true;}
            catch(AcademicRefreshException ex){committed=true;refreshWarning=ex.Message;}
            foreach(var course in confirmed.Courses)courseStates.Remove(course.Id);
            if(Service.Current.SemesterId==confirmed.Semester.Id)
            {
                var empty=new WorkspaceService(new EmptyWorkspaceStore());
                SwitchCourse(empty,(1,"Notas","",null),BuildProjection(empty,1));
                Section=NavigationSection.Courses;
            }
            await Home.RefreshAsync();
            Message=refreshWarning ?? $"Se eliminó el semestre {confirmed.Semester.Name} y sus {confirmed.CourseCount} materias de este equipo.";
            SaveStatus="Catálogo actualizado en este equipo";
        }
        catch(Exception ex)
        {
            Message=committed ? $"El semestre se eliminó, pero falló la actualización de pantalla ({SafeErrorCode.From(ex)}). Vuelve a abrir Mis Materias." :
                ex is AcademicValidationException ? ex.Message : $"No se pudo eliminar el semestre ({SafeErrorCode.From(ex)}). Los cambios se revirtieron; actualiza e inténtalo de nuevo.";
        }
        finally{FileBusy=false;}
    }
    private void SwitchCourse(WorkspaceService next,(int Cut,string Page,string Search,string? Student) saved,CourseProjection projection)
    {
        Service.ChangedDetailed-=OnChanged;Service.RefreshFailed-=OnRefreshFailed;
        Service=next;Service.ChangedDetailed+=OnChanged;Service.RefreshFailed+=OnRefreshFailed;
        searchUpdate.Cancel();selectedCut=saved.Cut;page=saved.Page;search=saved.Search;searchInput=saved.Search;selectedStudent=null;Raise(nameof(SearchInput));
        ShowProjection(projection);
        Consolidated.Refresh(Service.Current);Raise(nameof(Cuts));Raise(nameof(SelectedCutWeight));Raise(nameof(IsConsolidated));Raise(nameof(CanDeleteCut));Raise(nameof(DeleteCutHint));DeleteCutCommand.Refresh();
        if(saved.Student is not null)SelectedStudent=AttendanceRows.FirstOrDefault(r=>r.Student.Id==saved.Student) ?? SelectedStudent;
        foreach(var name in new[]{nameof(SelectedCut),nameof(CutLabel),nameof(Search),nameof(IsDemo),nameof(StorageLabel),nameof(CourseName),nameof(CourseInfo),nameof(StudentCount),nameof(HasOpenCourse),nameof(PageTitle),nameof(IsNotes),nameof(IsAttendance)})Raise(name);
        SaveStatus="Materia abierta · datos guardados en este equipo";
        ReloadCourseCommand.Refresh();PersonalizeCommand.Refresh();UpdatesCommand.Refresh();ImportStudentsCommand.Refresh();AddStudentCommand.Refresh();ViewStudentDetailsCommand.Refresh();ExportReportCommand.Refresh();ExportAttendancePdfCommand.Refresh();ExportNotesPdfCommand.Refresh();
    }
    private async Task ReloadCourseAsync()
    {
        if(Catalog is null || !HasOpenCourse || FileBusy)return;
        FileBusy=true;SaveStatus="Actualizando materia…";
        try
        {
            var id=Service.Current.CourseId;
            var saved=(SelectedCut,Page,Search,SelectedStudent?.Student.Id);
            var (next,projection)=await Task.Run(()=>
            {
                var loaded=Catalog.Open(id);
                return (loaded,BuildProjection(loaded,saved.SelectedCut));
            });
            if(Service.IsSaving)throw new AcademicValidationException("Espera a que termine el guardado antes de actualizar la materia.");
            SwitchCourse(next,saved,projection);Message="";SaveStatus="Materia actualizada desde el archivo local";
        }
        catch(Exception ex){Message=ex is AcademicValidationException ? ex.Message : $"No se pudo actualizar la materia ({SafeErrorCode.From(ex)}). Reintenta.";}
        finally{FileBusy=false;if(SaveStatus=="Actualizando materia…")SaveStatus="Materia anterior conservada · revisa el aviso";}
    }
    public void Try(Action action)
    {
        try {action();Message="";} catch(Exception ex) {Message=ex is AcademicRefreshException or AcademicValidationException ? ex.Message : $"No se completó la operación ({SafeErrorCode.From(ex)}). Revisa tus datos locales y reintenta.";}
    }
    private void Rebuild()
    {
        ShowProjection(BuildProjection(Service,SelectedCut));
    }
    public async Task SelectCutAsync(int number)
    {
        if(FileBusy || Service.IsSaving || number==SelectedCut)return;
        FileBusy=true;var service=Service;var source=service.Current;var previousStatus=SaveStatus;SaveStatus="Abriendo corte…";
        try
        {
            FlushSearch();_=service.Cut(number);
            var projection=await Task.Run(()=>BuildProjection(service,number));
            if(Service!=service || service.Current!=source)throw new AcademicValidationException("La materia cambió mientras se abría el corte. Vuelve a seleccionarlo.");
            selectedCut=number;ShowProjection(projection);
            foreach(var name in new[]{nameof(SelectedCut),nameof(CutLabel),nameof(SelectedCutWeight),nameof(Cuts)})Raise(name);
            Message="";
        }
        catch(Exception ex){Message=ex is AcademicValidationException ? ex.Message : $"No se pudo abrir el corte ({SafeErrorCode.From(ex)}). Se conserva el corte anterior.";}
        finally{FileBusy=false;if(SaveStatus=="Abriendo corte…")SaveStatus=previousStatus;}
    }
    private static CourseProjection BuildProjection(WorkspaceService service,int selectedCut)
    {
        var index=new AcademicCutReadIndex(service.Cut(selectedCut));
        var grades=new List<GradeRowViewModel>(service.Current.Students.Count);
        var attendance=new List<AttendanceRowViewModel>(service.Current.Students.Count);
        var positions=new Dictionary<string,int>(service.Current.Students.Count);
        var totals=new decimal?[service.Current.Students.Count];
        var i=0;
        foreach(var student in service.Current.Students)
        {
            positions.Add(student.Id,i);
            grades.Add(new(service,selectedCut,student,i+1,index));
            attendance.Add(new(service,selectedCut,student,i+1,index));
            totals[i]=index.Cut(student.Id);
            i++;
        }
        return new(index,grades,attendance,positions,totals);
    }
    private void ShowProjection(CourseProjection projection)
    {
        var cut=Service.Cut(SelectedCut);
        readIndex=projection.Index;
        Groups.Clear();
        foreach(var group in cut.Groups)Groups.Add(new(group.Name,$"{group.Weight:0.##} %",group.Mode==EvaluationMode.Direct ? "Nota directa" : $"Promedio {(group.UseWeightedAverage ? "ponderado" : "simple")} · {group.Activities.Count} subactividades",ConfigureCommand,group.Id));
        Weeks.Clear();foreach(var week in cut.DisplayWeeks)Weeks.Add(week);
        // Remove the old row contexts before rebuilding columns. Publish new rows only
        // after both tables have their matching grade/week schema.
        GradeRows.Clear();AttendanceRows.Clear();
        Raise(nameof(AttendanceGradingEnabled));Raise(nameof(AttendanceGradingLabel));
        Raise(nameof(Groups));Raise(nameof(Weeks));
        allGradeRows.Clear();allAttendanceRows.Clear();studentPositions.Clear();
        allGradeRows.AddRange(projection.Grades);allAttendanceRows.AddRange(projection.Attendance);
        groupAttendanceSummary=new(allAttendanceRows.Sum(r=>r.Summary.Present),allAttendanceRows.Sum(r=>r.Summary.Absent),
            allAttendanceRows.Sum(r=>r.Summary.Justified),allAttendanceRows.Sum(r=>r.Summary.Late));
        PublishGroupAttendance();
        foreach(var (id,position) in projection.Positions)studentPositions.Add(id,position);
        ApplySearchFilter();
        UpdateCourseAverage(projection.Totals);
        Raise(nameof(CanAddWeek));Raise(nameof(StudentCount));
        AddWeekCommand.Refresh();
    }
    private void ApplySearchFilter()
    {
        var selectedId=SelectedStudent?.Student.Id;
        var grades=new List<GradeRowViewModel>();
        var attendance=new List<AttendanceRowViewModel>();
        for(var i=0;i<allGradeRows.Count;i++)
        {
            var student=allGradeRows[i].Student;
            if(!student.FullName.Contains(Search,StringComparison.OrdinalIgnoreCase) && !student.Document.Contains(Search,StringComparison.OrdinalIgnoreCase))continue;
            grades.Add(allGradeRows[i]);
            attendance.Add(allAttendanceRows[i]);
        }
        if(nameDescending is {} descending)
        {
            var comparer=StringComparer.Create(System.Globalization.CultureInfo.GetCultureInfo("es-CO"),true);
            grades=descending ? grades.OrderByDescending(r=>r.FullName,comparer).ThenBy(r=>r.Number).ToList() : grades.OrderBy(r=>r.FullName,comparer).ThenBy(r=>r.Number).ToList();
            var attendanceById=attendance.ToDictionary(r=>r.Student.Id);
            attendance=grades.Select(r=>attendanceById[r.Student.Id]).ToList();
        }
        if(!GradeRows.SequenceEqual(grades))GradeRows.ReplaceAll(grades);
        if(!AttendanceRows.SequenceEqual(attendance))AttendanceRows.ReplaceAll(attendance);
        SelectedStudent=attendance.FirstOrDefault(x=>x.Student.Id==selectedId) ?? attendance.FirstOrDefault();
        Raise(nameof(HasRows));Raise(nameof(IsEmpty));Raise(nameof(EmptyTableMessage));
        PasteGradesCommand.Refresh();
    }
    private void UpdateCourseAverage(decimal?[] totals)
    {
        courseTotals=totals;
        courseTotalSum=courseTotals.Where(x=>x is not null).Sum(x=>x!.Value);
        completedStudents=courseTotals.Count(x=>x is not null);
        PublishCourseAverage();
    }
    private void PublishCourseAverage()
    {
        CourseAverage=courseTotals.Length>0 && completedStudents==courseTotals.Length ? AcademicFormatting.Grade(courseTotalSum/courseTotals.Length) : "Pendiente";
        Raise(nameof(CourseAverage));
    }
    private void UpdateGroupAttendance(AttendanceSummary previous,AttendanceSummary next)
    {
        groupAttendanceSummary=new(groupAttendanceSummary.Present-previous.Present+next.Present,
            groupAttendanceSummary.Absent-previous.Absent+next.Absent,
            groupAttendanceSummary.Justified-previous.Justified+next.Justified,
            groupAttendanceSummary.Late-previous.Late+next.Late);
        PublishGroupAttendance();
    }
    private void PublishGroupAttendance()
    {
        Raise(nameof(GroupAttendanceSummary));Raise(nameof(GroupAttendancePercentage));Raise(nameof(GroupAttendanceLabel));
    }
}
