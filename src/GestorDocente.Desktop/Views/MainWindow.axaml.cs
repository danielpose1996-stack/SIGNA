using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;
using GestorDocente.Application;
using GestorDocente.Domain;
using GestorDocente.Desktop.ViewModels;
namespace GestorDocente.Desktop.Views;

public partial class MainWindow : Window
{
    private MainViewModel? observed;
    public MainWindow()
    {
        InitializeComponent();Controls.WindowLayout.Configure(this);DataContextChanged+=(_,_)=>Attach();
        Closing+=(_,e)=>{if(observed is {IsMaintainingLocalData:true} model){e.Cancel=true;model.BackupStatus="Espera a que termine la operación de datos antes de cerrar SIGNA.";}};
        Opened+=(_,_)=>{if(observed?.IsHome==true)_=observed.Dashboard.RefreshAsync();if(observed is {} model)_=model.Updates.CheckAsync(automatic:true);};
        Closed+=(_,_)=>{observed?.Dashboard.Suspend();observed?.Updates.Cancel();};
    }
    private void Attach()
    {
        if(observed is not null){observed.UpdatesRequested-=OpenUpdates;observed.Updates.RestartRequested-=RestartAfterUpdate;}
        if(observed is not null){observed.GradePasteRequested-=OpenGradePaste;observed.AttendanceGradingRequested-=OpenAttendanceGrading;}
        if(observed is not null)observed.AppearanceRequested-=OpenAppearance;
        if(observed is not null)observed.SemesterDeletionRequested-=DeleteSemester;
        if(observed is not null) {observed.PropertyChanged-=OnModelPropertyChanged;observed.ConfigurationRequested-=OpenConfiguration;observed.CutWeightsRequested-=OpenCutWeights;observed.CutDeletionRequested-=DeleteCut;observed.CatalogEditorRequested-=OpenCatalog;observed.CourseEditionRequested-=EditCourse;observed.CourseDeletionRequested-=DeleteCourse;observed.WeekEditorRequested-=OpenWeeks;observed.RecoveryRequested-=OpenRecovery;observed.ImportRequested-=OpenImport;observed.StudentEditorRequested-=AddStudent;observed.StudentDetailsRequested-=ViewStudentDetails;observed.ExportRequested-=ExportReport;observed.AttendancePdfRequested-=ExportAttendancePdf;observed.NotesPdfRequested-=ExportNotesPdf;observed.BackupRequested-=CreateBackup;observed.RestoreRequested-=RestoreBackup;observed.BackupFolderRequested-=ChooseBackupFolder;observed.LocalDataResetRequested-=ClearLocalData;}
        observed=DataContext as MainViewModel;
        if(observed is not null){observed.UpdatesRequested+=OpenUpdates;observed.Updates.RestartRequested+=RestartAfterUpdate;}
        if(observed is not null){observed.GradePasteRequested+=OpenGradePaste;observed.AttendanceGradingRequested+=OpenAttendanceGrading;}
        if(observed is not null)observed.AppearanceRequested+=OpenAppearance;
        if(observed is not null)observed.SemesterDeletionRequested+=DeleteSemester;
        if(observed is not null) {observed.PropertyChanged+=OnModelPropertyChanged;observed.ConfigurationRequested+=OpenConfiguration;observed.CutWeightsRequested+=OpenCutWeights;observed.CutDeletionRequested+=DeleteCut;observed.CatalogEditorRequested+=OpenCatalog;observed.CourseEditionRequested+=EditCourse;observed.CourseDeletionRequested+=DeleteCourse;observed.WeekEditorRequested+=OpenWeeks;observed.RecoveryRequested+=OpenRecovery;observed.ImportRequested+=OpenImport;observed.StudentEditorRequested+=AddStudent;observed.StudentDetailsRequested+=ViewStudentDetails;observed.ExportRequested+=ExportReport;observed.AttendancePdfRequested+=ExportAttendancePdf;observed.NotesPdfRequested+=ExportNotesPdf;observed.BackupRequested+=CreateBackup;observed.RestoreRequested+=RestoreBackup;observed.BackupFolderRequested+=ChooseBackupFolder;observed.LocalDataResetRequested+=ClearLocalData;}
        MinWidth=observed?.IsCourses==true ? 780 : 1100;
    }
    private void OnModelPropertyChanged(object? sender,System.ComponentModel.PropertyChangedEventArgs e)
    {
        if(e.PropertyName==nameof(MainViewModel.Section))MinWidth=observed?.IsCourses==true ? 780 : 1100;
    }
    private async void OpenUpdates()
    {
        if(observed is not {} model || model.FileBusy)return;
        try
        {
            _=model.Updates.CheckAsync();
            await ShowAcademicDialog(new UpdatesWindow{DataContext=model.Updates});
        }
        catch(Exception ex){model.Message=SafeError(ex);}
    }
    private static void RestartAfterUpdate()
    {
        if(Avalonia.Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop)
            throw new AcademicValidationException("No se pudo cerrar SIGNA de forma segura para reiniciar. Intenta de nuevo desde la aplicación instalada.");
        desktop.Shutdown();
    }
    private async void OpenConfiguration()
    {
        if(observed is null)return;
        try
        {
            var model=new ConfigurationViewModel(observed.Service,observed.SelectedCut);
            if(observed.ConfigurationGroupId is {} id && model.Groups.FirstOrDefault(g=>g.Id==id) is {} group)model.SelectedGroup=group;
            await ShowAcademicDialog(new ConfigurationWindow{DataContext=model});
        }
        catch(Exception ex){observed.Message=SafeError(ex);}
    }
    private async void OpenAppearance()
    {
        if(observed is not {} model || model.FileBusy || model.Service.IsSaving)return;
        try{model.Appearance.ResetDraft();await ShowAcademicDialog(new AppearanceWindow{DataContext=model.Appearance});}
        catch(Exception ex){model.Message=SafeError(ex);}
    }
    private async void OpenCutWeights()
    {
        if(observed is null)return;
        try{await ShowAcademicDialog(new CutWeightsWindow{DataContext=new CutWeightsViewModel(observed.Service)});}
        catch(Exception ex){observed.Message=SafeError(ex);}
    }
    private async void DeleteCut(int number)
    {
        if(observed is not {} model || model.FileBusy || model.Service.IsSaving)return;
        model.FileBusy=true;
        try
        {
            var preview=model.Service.PreviewCutDeletion(number);
            if(await ConfirmCutDeletionAsync(preview))
            {
                model.FileBusy=false;await model.DeleteCutAsync(preview,true);
            }
        }
        catch(Exception ex){model.Message=SafeError(ex);}
        finally{model.FileBusy=false;}
    }
    public async Task<bool> ConfirmCutDeletionAsync(CutDeletionPreview preview)
    {
        var text=$"¿Eliminar definitivamente el Corte {preview.Number} de «{preview.CourseName}»?\n\nSe eliminarán sus actividades ({preview.ActivityCount}), subactividades ({preview.SubactivityCount}), notas ({preview.GradeCount}), semanas ({preview.WeekCount}) y registros de asistencia ({preview.AttendanceCount}), incluidos los retirados. Esta acción no se puede deshacer.\n\nLos cortes restantes se renumerarán y conservarán sus porcentajes. Deberás revisar que sumen 100 %. No puedes eliminar el último corte de la materia.";
        ModalOverlay.IsVisible=true;
        try{return await new ConfirmWindow(text,"Eliminar corte",scrollable:true){Title=$"Eliminar Corte {preview.Number}",Width=640,Height=385}.ShowDialog<bool>(this);}
        finally{ModalOverlay.IsVisible=false;}
    }
    private async void OpenCatalog(bool semester)
    {
        if(observed?.Catalog is not {} catalog)return;
        var vm=new CatalogEditorViewModel(catalog,observed.Home.SelectedSemester,semester,
            availableParameters:observed.CopiedParameters,parametersSource:observed.CopiedParametersSource);
        try
        {
            await ShowAcademicDialog(new CatalogEditorWindow{DataContext=vm});
            if(!string.IsNullOrWhiteSpace(vm.SavedSemesterId))
            {
                await observed.Home.RefreshAsync();
                observed.Home.SelectedSemester=observed.Home.Semesters.FirstOrDefault(s=>s.Id==vm.SavedSemesterId);
                if(!string.IsNullOrWhiteSpace(vm.Error))observed.Message=vm.Error;
            }
        }
        catch(Exception ex){observed.Message=!string.IsNullOrWhiteSpace(vm.SavedSemesterId) ?
            $"El registro se guardó, pero falló el refresco ({SafeErrorCode.From(ex)}). Pulsa Actualizar." : SafeError(ex);}
    }
    private async void DeleteCourse(CourseSummary course)
    {
        if(observed is not {} model || model.FileBusy)return;
        try
        {
            var question=$"¿Eliminar definitivamente «{course.Name}» ({course.Code}, {course.Period})? Se eliminarán sus estudiantes, notas y asistencias locales. Esta acción no se puede deshacer; crea un respaldo antes si deseas conservarlos.";
            if(await new ConfirmWindow(question,"Eliminar materia"){Width=560,Height=250}.ShowDialog<bool>(this))await model.DeleteCourseAsync(course);
        }
        catch(Exception ex){model.Message=SafeError(ex);}
    }
    private async void EditCourse(CourseSummary course)
    {
        if(observed is not {} model || model.Catalog is not {} catalog || model.FileBusy)return;
        try
        {
            if(model.Service.Current.CourseId==course.Id && model.Service.IsSaving)
                throw new AcademicValidationException("Espera a que termine de guardarse la materia antes de editarla.");
            var original=await Task.Run(()=>catalog.Courses(model.Home.SelectedSemester?.Id ?? "").FirstOrDefault(c=>c.Id==course.Id));
            if(original is null)throw new AcademicValidationException("La materia ya no está en este período. Actualiza la lista.");
            var editor=new CatalogEditorViewModel(catalog,model.Home.SelectedSemester,false,original);
            await ShowAcademicDialog(new CatalogEditorWindow{DataContext=editor});
            if(!string.IsNullOrEmpty(editor.SavedCourseId))
            {
                await model.RefreshEditedCourseAsync(editor.SavedCourseId,editor.SavedSemesterId);
                if(!string.IsNullOrWhiteSpace(editor.Error))model.Message=editor.Error;
            }
        }
        catch(Exception ex){model.Message=SafeError(ex);}
    }
    private async void DeleteSemester(AcademicSemester semester)
    {
        if(observed is not {} model || model.Catalog is not {} catalog || model.FileBusy)return;
        model.FileBusy=true;
        try
        {
            if(model.Service.IsSaving)throw new AcademicValidationException("Espera a que termine el guardado antes de eliminar un semestre.");
            var preview=await catalog.InspectSemesterDeletionAsync(semester.Id);
            var confirmed=await ConfirmSemesterDeletionAsync(preview);
            if(confirmed){model.FileBusy=false;await model.DeleteSemesterAsync(preview);}
        }
        catch(Exception ex){model.Message=SafeError(ex);}
        finally{ModalOverlay.IsVisible=false;model.FileBusy=false;}
    }
    public async Task<bool> ConfirmSemesterDeletionAsync(SemesterDeletionPreview preview)
    {
        var contents=preview.CourseCount==0 ? "Este semestre no contiene materias ni registros asociados." :
            $"Materias: {preview.CourseCount}. Registros de estudiantes: {preview.StudentCount}. Notas: {preview.GradeCount}. Asistencias: {preview.AttendanceCount}. Todos esos datos se eliminarán permanentemente.";
        var message=$"¿Eliminar el semestre {preview.Semester.Name}?\n\n{contents}\n\nEsta acción no se puede deshacer. Crea un respaldo antes si deseas conservar la información. Los otros semestres se conservan.";
        ModalOverlay.IsVisible=true;
        try{return await new ConfirmWindow(message,"Eliminar semestre"){Title=$"Eliminar semestre {preview.Semester.Name}",Width=620,Height=330}.ShowDialog<bool>(this);}
        finally{ModalOverlay.IsVisible=false;}
    }
    private async void OpenWeeks()
    {
        if(observed is null)return;
        try{await ShowAcademicDialog(new WeeksWindow{DataContext=new WeeksViewModel(observed.Service,observed.SelectedCut)});}
        catch(Exception ex){observed.Message=SafeError(ex);}
    }
    private async void OpenGradePaste(string targetId)
    {
        if(observed is not {} model || model.FileBusy || model.Service.IsSaving)return;
        model.FileBusy=true;
        try{await ShowAcademicDialog(new GradePasteWindow{DataContext=new GradePasteViewModel(model,targetId)});}
        catch(Exception ex){model.Message=SafeError(ex);}
        finally{model.FileBusy=false;}
    }
    private async void OpenAttendanceGrading()
    {
        if(observed is not {} model || model.FileBusy || model.Service.IsSaving)return;
        model.FileBusy=true;
        try{await ShowAcademicDialog(new AttendanceGradingWindow{DataContext=new AttendanceGradingViewModel(model.Service,model.SelectedCut)});}
        catch(Exception ex){model.Message=SafeError(ex);}
        finally{model.FileBusy=false;}
    }
    private async void OpenRecovery()
    {
        if(observed is null)return;
        try
        {
            var model=new RecoveryViewModel(observed.Service,observed.SelectedCut);
            var dialog=new RecoveryWindow{DataContext=model};
            model.GroupRequested+=async group=>{
                try{await new ConfigurationWindow{DataContext=new ConfigurationViewModel(observed.Service,observed.SelectedCut,group)}.ShowDialog(dialog);model.Refresh();}
                catch(Exception ex){model.ReportError(SafeError(ex));}
            };
            model.SubactivityRequested+=async activity=>{
                try{await new ConfigurationWindow{DataContext=new ConfigurationViewModel(observed.Service,observed.SelectedCut,restoreActivity:activity)}.ShowDialog(dialog);model.Refresh();}
                catch(Exception ex){model.ReportError(SafeError(ex));}
            };
            await ShowAcademicDialog(dialog);
        }
        catch(Exception ex){observed.Message=SafeError(ex);}
    }
    public async Task ShowAcademicDialog(Window dialog)
    {
        dialog.MaxHeight=Math.Max(420,ClientSize.Height-30);
        dialog.MaxWidth=Math.Max(560,ClientSize.Width-40);
        dialog.WindowStartupLocation=WindowStartupLocation.CenterOwner;
        ModalOverlay.IsVisible=true;
        try {await dialog.ShowDialog(this);} finally {ModalOverlay.IsVisible=false;}
    }
    private static FilePickerFileType ExcelType=>new("Libro de Excel (.xlsx)"){Patterns=["*.xlsx"],MimeTypes=["application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"]};
    private static FilePickerFileType ExcelImportType=>new("Libros de Excel (.xls, .xlsx, .xlsm, .xlsb)"){Patterns=["*.xls","*.xlsx","*.xlsm","*.xlsb"],MimeTypes=["application/vnd.ms-excel","application/vnd.openxmlformats-officedocument.spreadsheetml.sheet","application/vnd.ms-excel.sheet.macroEnabled.12","application/vnd.ms-excel.sheet.binary.macroEnabled.12"]};
    private static FilePickerFileType PdfType=>new("Documento PDF (.pdf)"){Patterns=["*.pdf"],MimeTypes=["application/pdf"]};
    private static FilePickerFileType BackupType=>new("Respaldo de SIGNA (.aulabk)"){Patterns=["*.aulabk"]};
    private async Task<string?> RequestBackupPassword(string title,bool confirmation)
    {
        ModalOverlay.IsVisible=true;
        try{return await new BackupPasswordWindow(title,confirmation).ShowDialog<string?>(this);}
        finally{ModalOverlay.IsVisible=false;}
    }
    private async void ChooseBackupFolder()
    {
        if(observed is not {} model || model.FileBusy)return;
        model.FileBusy=true;
        try
        {
            var folders=await StorageProvider.OpenFolderPickerAsync(new(){Title="Carpeta para los respaldos de SIGNA",AllowMultiple=false});
            if(folders.Count==0)return;
            using var folder=folders[0];var path=folder.TryGetLocalPath() ?? throw new AcademicValidationException("Elige una carpeta local disponible.");
            await model.SetBackupFolderAsync(path);
            model.BackupStatus="Carpeta guardada. Generar respaldo creará un archivo nuevo aquí, sin pedir ubicación ni contraseña.";
        }
        catch(Exception ex){model.BackupStatus=SafeError(ex);}
        finally{model.FileBusy=false;}
    }
    private async void CreateBackup()
    {
        if(observed is not {} model || model.LocalBackup is not {} backup || model.FileBusy)return;
        try
        {
            using var maintenance=model.BeginLocalDataMaintenance();
            var folder=model.BackupFolder ?? throw new AcademicValidationException("Elige primero la carpeta para tus respaldos.");
            model.BackupStatus="Generando y verificando el respaldo…";
            var result=await Task.Run(()=>backup.CreateInFolder(folder));
            model.BackupStatus=$"Respaldo completo verificado: {result.Path} · {result.SemesterCount} semestres, {result.CourseCount} materias, {result.StudentCount} estudiantes.";
        }
        catch(Exception ex){model.BackupStatus=SafeError(ex);}
    }
    private async void RestoreBackup()
    {
        if(observed is not {} model || model.LocalBackup is not {} backup || model.FileBusy)return;
        var replaced=false;
        try
        {
            using var maintenance=model.BeginLocalDataMaintenance();
            var files=await StorageProvider.OpenFilePickerAsync(new(){Title="Elegir respaldo de SIGNA",AllowMultiple=false,FileTypeFilter=[BackupType]});
            if(files.Count==0)return;
            using var file=files[0];var path=file.TryGetLocalPath() ?? throw new AcademicValidationException("Elige un archivo local de respaldo.");
            string? password=null;
            if(await Task.Run(()=>backup.RequiresPassword(path)))
            {
                password=await RequestBackupPassword("Contraseña del respaldo antiguo",false);
                if(password is null)return;
            }
            model.BackupStatus="Verificando el archivo y los datos…";
            var inspection=await Task.Run(()=>backup.Inspect(path,password));
            var question=$"Se reemplazará toda la información local con {inspection.SemesterCount} semestres, {inspection.CourseCount} materias y {inspection.StudentCount} estudiantes del respaldo, incluyendo contactos, cortes, porcentajes, actividades, notas y asistencias. Antes se conservará una copia preventiva sin contraseña. Cierra otras instancias del programa. ¿Restaurar ahora?";
            if(!await new ConfirmWindow(question,"Restaurar respaldo",scrollable:true){Width=620,Height=300}.ShowDialog<bool>(this))
            {model.BackupStatus="Restauración cancelada. Los datos actuales se conservan.";return;}
            model.BackupStatus="Guardando copia preventiva y restaurando…";
            var result=await Task.Run(()=>backup.Restore(path,password));replaced=true;
            var next=await App.CreateProductionViewModelAsync();DataContext=next;next.NavigateCommand.Execute("Respaldos");
            next.BackupStatus=result.PreviousCopyWasRaw ?
                $"Restauración completa. El archivo anterior dañado se conservó en {result.PreviousBackupPath}; puede no ser una base válida." :
                $"Restauración completa: {result.SemesterCount} semestres, {result.CourseCount} materias y {result.StudentCount} estudiantes. Copia anterior sin contraseña: {result.PreviousBackupPath}.";
            next.Message="Respaldo restaurado. Selecciona una materia para continuar.";
        }
        catch(Exception ex)
        {
            if(replaced)ShowReloadFailure("El respaldo se restauró",ex);
            else model.BackupStatus=SafeError(ex);
        }
    }
    private async void ClearLocalData()
    {
        if(observed is not {} model || model.DataReset is not {} reset || model.FileBusy)return;
        var cleared=false;
        try
        {
            using var maintenance=model.BeginLocalDataMaintenance();
            var question="Se eliminarán de este equipo todos los semestres, materias, estudiantes, calificaciones, asistencias y configuraciones, incluida la carpeta preferida de respaldos. SIGNA quedará como recién instalado. Todos tus archivos de respaldo se conservarán, estén donde estén; podrás restaurarlos después. Los archivos de actualización no se modificarán. Esta operación no se puede deshacer sin un respaldo. ¿Limpiar los datos de este equipo?";
            if(!await new ConfirmWindow(question,"Limpiar datos",scrollable:true){Width=640,Height=350}.ShowDialog<bool>(this))
            {model.BackupStatus="Limpieza cancelada. Todos los datos se conservan.";return;}
            model.BackupStatus="Limpiando los datos de este equipo…";
            var result=await Task.Run(()=>reset.Reset(confirmed:true));cleared=true;
            var next=await App.CreateProductionViewModelAsync();DataContext=next;next.NavigateCommand.Execute("Respaldos");
            next.BackupStatus=string.IsNullOrEmpty(result.Warning) ? "Datos locales eliminados. Tus respaldos se conservaron. Puedes empezar de nuevo o restaurar una copia." : result.Warning;
        }
        catch(Exception ex)
        {
            if(cleared)ShowReloadFailure("Los datos locales se limpiaron",ex);
            else model.BackupStatus=SafeError(ex);
        }
    }
    private void ShowReloadFailure(string operation,Exception error)
    {
        DataContext=null;
        Content=new TextBlock{Margin=new(30),TextWrapping=Avalonia.Media.TextWrapping.Wrap,Text=operation+", pero no se pudo volver a abrir el catálogo. Cierra y abre SIGNA antes de escribir nuevos datos. Error: "+SafeError(error)};
    }
    private async void OpenImport()
    {
        if(observed is not {} model || model.WorkbookReader is null || model.FileBusy)return;
        model.FileBusy=true;var service=model.Service;
        try
        {
            var files=await StorageProvider.OpenFilePickerAsync(new(){Title="Importar listado de estudiantes",AllowMultiple=false,FileTypeFilter=[ExcelImportType]});
            if(files.Count==0)return;using var file=files[0];
            if(!StudentWorkbookFormats.IsSupportedFileName(file.Name))throw new AcademicValidationException("Selecciona un libro .xls, .xlsx, .xlsm o .xlsb.");
            model.Message="Leyendo Excel…";
            await using var input=file.TryGetLocalPath() is {} localPath ? StudentWorkbookFile.OpenRead(localPath) : await file.OpenReadAsync();
            var workbook=await Task.Run(()=>model.WorkbookReader.Read(input));
            var import=new StudentImportViewModel(service,workbook,file.Name);model.Message="";
            await ShowAcademicDialog(new StudentImportWindow{DataContext=import});
            if(import.ImportedCount>0)model.Message=string.IsNullOrWhiteSpace(import.Error) ?
                $"Se importaron {import.ImportedCount} estudiantes. Los datos anteriores se conservaron." : import.Error;
        }
        catch(Exception ex){model.Message=SafeError(ex);}
        finally{model.FileBusy=false;}
    }
    private async void AddStudent()
    {
        if(observed is not {} model || model.FileBusy || model.Service.IsSaving)return;
        try{await ShowAcademicDialog(new StudentEditorWindow{DataContext=new StudentEditorViewModel(model.Service)});}
        catch(Exception ex){model.Message=SafeError(ex);}
    }
    private async void ViewStudentDetails(Student student)
    {
        if(observed is not {} model)return;
        try{await ShowAcademicDialog(new StudentDetailsWindow{DataContext=new StudentDetailsViewModel(student,model.Service)});}
        catch(Exception ex){model.Message=SafeError(ex);}
    }
    private async void ExportReport()
    {
        if(observed is not {} model || model.ReportExporter is null || model.FileBusy)return;
        model.FileBusy=true;
        try
        {
            var cut=model.SelectedCut;var kind=model.IsAttendance ? AcademicReportKind.Attendance : AcademicReportKind.Notes;
            var course=model.Service.Current;
            var code=new string(course.CourseCode.Select(c=>Path.GetInvalidFileNameChars().Contains(c) ? '_' : c).ToArray());
            using var file=await StorageProvider.SaveFilePickerAsync(new(){Title="Exportar reporte Excel del corte",SuggestedFileName=$"{code}-{course.Period}-corte-{cut}-{kind}.xlsx",DefaultExtension="xlsx",FileTypeChoices=[ExcelType]});
            if(file is null)return;
            var path=file.TryGetLocalPath() ?? throw new AcademicValidationException("Selecciona una carpeta local para el reporte.");
            if(!path.EndsWith(".xlsx",StringComparison.OrdinalIgnoreCase))throw new AcademicValidationException("El reporte debe guardarse con extensión .xlsx.");
            var overwrite=File.Exists(path);if(overwrite && !await new ConfirmWindow("El archivo ya existe. ¿Reemplazar este reporte con los datos actuales?","Reemplazar reporte").ShowDialog<bool>(this))return;
            model.Message="Generando reporte Excel…";
            var source=model.Service.Current;
            var bytes=await Task.Run(()=>model.ReportExporter.Export(WorkspaceService.Clone(source),cut,kind));
            await Task.Run(()=>ReportFile.Save(path,bytes,overwrite));model.Message=$"Reporte Excel guardado: {file.Name}. Incluye toda la lista del corte {cut}.";
        }
        catch(Exception ex){model.Message=SafeError(ex);}
        finally{model.FileBusy=false;}
    }
    private async void ExportNotesPdf()
    {
        if(observed is not {} model || model.NotesPdfExporter is not {} exporter || model.FileBusy)return;
        if(model.Service.IsSaving){model.Message="Espera a que termine de guardarse la nota y vuelve a generar el reporte.";return;}
        model.FileBusy=true;
        try
        {
            var cut=model.SelectedCut;var course=model.Service.Current;
            var code=new string(course.CourseCode.Select(c=>Path.GetInvalidFileNameChars().Contains(c) ? '_' : c).ToArray());
            using var file=await StorageProvider.SaveFilePickerAsync(new(){Title="Guardar reporte de notas del corte",SuggestedFileName=$"notas-{code}-corte-{cut}.pdf",DefaultExtension="pdf",FileTypeChoices=[PdfType]});
            if(file is null)return;
            var path=file.TryGetLocalPath() ?? throw new AcademicValidationException("Selecciona una carpeta local para el PDF.");
            if(!path.EndsWith(".pdf",StringComparison.OrdinalIgnoreCase))throw new AcademicValidationException("El reporte debe guardarse con extensión .pdf.");
            var overwrite=File.Exists(path);
            if(overwrite && !await new ConfirmWindow("El archivo ya existe. ¿Reemplazar este reporte con las notas actuales?","Reemplazar reporte").ShowDialog<bool>(this))return;
            if(model.Service.IsSaving)throw new AcademicValidationException("Espera a que termine de guardarse la nota y vuelve a generar el reporte.");
            model.Message="Generando reporte PDF de notas…";
            var source=model.Service.Current;
            var bytes=await Task.Run(()=>exporter.Export(NotesPdfProjection.Build(WorkspaceService.Clone(source),cut)));
            await Task.Run(()=>ReportFile.Save(path,bytes,overwrite));
            model.Message=$"Reporte PDF guardado: {file.Name}. Incluye todos los estudiantes del corte {cut}, identificados solo por nombre.";
        }
        catch(Exception ex){model.Message=SafeError(ex);}
        finally{model.FileBusy=false;}
    }
    private async void ExportAttendancePdf()
    {
        if(observed is not {} model || model.AttendancePdfExporter is not {} exporter || model.SelectedStudent is not {} selected || model.FileBusy)return;
        model.FileBusy=true;
        try
        {
            var cut=model.SelectedCut;var studentId=selected.Student.Id;var course=model.Service.Current;
            var code=new string(course.CourseCode.Select(c=>Path.GetInvalidFileNameChars().Contains(c) ? '_' : c).ToArray());
            var document=new string(selected.Document.Select(c=>Path.GetInvalidFileNameChars().Contains(c) ? '_' : c).ToArray());
            using var file=await StorageProvider.SaveFilePickerAsync(new(){Title="Guardar reporte individual de asistencia",SuggestedFileName=$"asistencia-{code}-corte-{cut}-{document}.pdf",DefaultExtension="pdf",FileTypeChoices=[PdfType]});
            if(file is null)return;
            var path=file.TryGetLocalPath() ?? throw new AcademicValidationException("Selecciona una carpeta local para el PDF.");
            if(!path.EndsWith(".pdf",StringComparison.OrdinalIgnoreCase))throw new AcademicValidationException("El reporte debe guardarse con extensión .pdf.");
            var overwrite=File.Exists(path);
            if(overwrite && !await new ConfirmWindow("El archivo ya existe. ¿Reemplazar este reporte con los datos actuales?","Reemplazar reporte").ShowDialog<bool>(this))return;
            model.Message="Generando reporte PDF…";
            var snapshot=WorkspaceService.Clone(course);
            var bytes=await Task.Run(()=>exporter.Export(snapshot,cut,studentId));
            await Task.Run(()=>ReportFile.Save(path,bytes,overwrite));
            model.Message=$"Reporte PDF guardado: {file.Name}. Estudiante: {selected.FullName}, corte {cut}.";
        }
        catch(Exception ex){model.Message=SafeError(ex);}
        finally{model.FileBusy=false;}
    }
    private static string SafeError(Exception ex)=>ex is AcademicValidationException or AcademicRefreshException ? ex.Message :
        $"La operación no se completó ({SafeErrorCode.From(ex)}). Revisa el archivo local e intenta de nuevo.";
}
