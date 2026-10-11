using Avalonia.Controls;
using Avalonia.Interactivity;

namespace GestorDocente.Desktop.Views;

public partial class BackupPasswordWindow:Window
{
    private readonly bool confirmation;
    public BackupPasswordWindow():this("Contraseña del respaldo",false){}
    public BackupPasswordWindow(string title,bool confirmation)
    {
        InitializeComponent();Controls.WindowLayout.Configure(this);
        this.confirmation=confirmation;Title=title;Heading.Text=title;
        ConfirmLabel.IsVisible=confirmation;ConfirmInput.IsVisible=confirmation;
    }
    private void Cancel(object? sender,RoutedEventArgs e)=>Close(null);
    private void Accept(object? sender,RoutedEventArgs e)
    {
        var password=PasswordInput.Text ?? "";
        if(password.Length<12 || string.IsNullOrWhiteSpace(password))
        {ErrorText.Text="Escribe una contraseña de al menos 12 caracteres.";return;}
        if(confirmation && password!=ConfirmInput.Text)
        {ErrorText.Text="Las contraseñas no coinciden.";return;}
        PasswordInput.Text="";ConfirmInput.Text="";Close(password);
    }
}
