using Avalonia.Controls;
using Avalonia.Interactivity;
using GestorDocente.Application;
using GestorDocente.Desktop.ViewModels;
namespace GestorDocente.Desktop.Views;

public partial class CutManagementWindow:Window
{
    private CutManagementViewModel? model;
    public CutManagementWindow()
    {
        InitializeComponent();
        DataContextChanged+=(_,_)=>{if(model is not null){model.DeleteRequested-=Delete;model.LockRequested-=ToggleLock;}model=DataContext as CutManagementViewModel;if(model is not null){model.DeleteRequested+=Delete;model.LockRequested+=ToggleLock;}};
        Closing+=(_,e)=>{if(model?.IsBusy==true)e.Cancel=true;};
        Closed+=(_,_)=>{if(model is not null){model.DeleteRequested-=Delete;model.LockRequested-=ToggleLock;}};
    }
    private void CloseEditor(object? sender,RoutedEventArgs e)=>Close();
    private async void ToggleLock(int number)
    {
        if(model?.CanModify!=true)return;
        try
        {
            var source=model.Service.Current;var cut=model.Service.Cut(number);
            var action=cut.IsLocked ? "Desbloquear" : "Bloquear";
            var text=cut.IsLocked ? $"¿Desbloquear «{cut.Label}» para corregir sus notas y asistencias?" :
                $"¿Bloquear «{cut.Label}» para proteger sus notas, actividades y asistencias? Podrás consultar los datos, generar reportes y desbloquearlo posteriormente.";
            if(await new ConfirmWindow(text,action+" corte"){Width=550,Height=280}.ShowDialog<bool>(this))
                await model.ToggleLockAsync(number,source);
        }
        catch(Exception ex){model.ReportError(ex);}
    }
    private async void SaveNames(object? sender,RoutedEventArgs e){if(model is not null)await model.SaveAsync();}
    private async void ConfigureWeights(object? sender,RoutedEventArgs e)
    {
        if(model?.CanModify!=true)return;
        try{await ShowWeights();}catch(Exception ex){model.ReportError(ex);}
    }
    private async Task ShowWeights()
    {
        if(model is null)return;
        await new CutWeightsWindow{DataContext=new CutWeightsViewModel(model.Service),MaxHeight=Math.Max(400,ClientSize.Height-20)}.ShowDialog(this);
        model?.RefreshWeights();
    }
    private async void Delete(CutDeletionPreview preview)
    {
        if(model is null)return;
        try
        {
            var confirmed=await new ConfirmWindow(CutDeletionMessage.For(preview),"Eliminar corte",scrollable:true){Width=620,Height=400}.ShowDialog<bool>(this);
            if(await model.DeleteAsync(preview,confirmed))await ShowWeights();
        }
        catch(Exception ex){model.ReportError(ex);}
    }
}
internal static class CutDeletionMessage
{
    public static string For(CutDeletionPreview p)=>$"¿Eliminar definitivamente «{p.Label}» de «{p.CourseName}»?\n\nSe eliminarán sus actividades ({p.ActivityCount}), subactividades ({p.SubactivityCount}), notas ({p.GradeCount}), semanas ({p.WeekCount}) y registros de asistencia ({p.AttendanceCount}), incluidos los retirados. Esta acción no se puede deshacer.\n\nLos cortes restantes se renumerarán y conservarán sus porcentajes. Deberás revisar que sumen 100 %. No puedes eliminar el último corte de la materia.";
}
