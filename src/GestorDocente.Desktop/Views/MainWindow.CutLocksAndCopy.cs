using GestorDocente.Application;
using GestorDocente.Desktop.ViewModels;

namespace GestorDocente.Desktop.Views;

public partial class MainWindow
{
    private async void ToggleCutLock(int number)
    {
        if(observed is not {} model || model.FileBusy || model.Service.IsSaving)return;
        try
        {
            using var operation=model.BeginCutConfiguration();
            var source=model.Service.Current;var cut=model.Service.Cut(number);
            var title=cut.IsLocked ? "Desbloquear corte" : "Bloquear corte";
            var message=cut.IsLocked ? $"¿Desbloquear «{cut.Label}» para corregir sus notas, actividades y asistencias?" :
                $"¿Bloquear «{cut.Label}»? Sus notas, actividades y asistencias quedarán protegidas. Podrás consultar los datos, generar reportes y desbloquearlo cuando necesites corregirlos.";
            ModalOverlay.IsVisible=true;
            try
            {
                if(await new ConfirmWindow(message,title){Title=title,Width=570,Height=285}.ShowDialog<bool>(this))
                    await model.Service.SetCutLockedAsync(number,!cut.IsLocked,source);
            }
            finally{ModalOverlay.IsVisible=false;}
        }
        catch(Exception ex){model.Message=ex is AcademicRefreshException ? "El candado se guardó, pero falló el refresco. Pulsa Actualizar." : SafeError(ex);}
    }

    private async void CopyGradesToClipboard(string? targetId)
    {
        if(observed is not {} model)return;
        try
        {
            var text=model.GetGradeClipboardText(targetId);
            if(Clipboard is null)throw new GestorDocente.Domain.AcademicValidationException("El portapapeles no está disponible. Intenta copiar nuevamente.");
            await Clipboard.SetTextAsync(text);
            model.Message="Notas copiadas al portapapeles. Puedes pegarlas con Ctrl+V.";
        }
        catch(Exception ex){model.Message=ex is GestorDocente.Domain.AcademicValidationException ? ex.Message :
            $"No se pudieron copiar las notas al portapapeles ({SafeErrorCode.From(ex)}). Intenta nuevamente.";}
    }
}
