using Avalonia.Controls;
using GestorDocente.Desktop.ViewModels;
namespace GestorDocente.Desktop.Views;

public partial class MainWindow : Window
{
    private MainViewModel? observed;
    public MainWindow() {InitializeComponent();Controls.WindowLayout.Configure(this);DataContextChanged+=(_,_)=>Attach();}
    private void Attach()
    {
        if(observed is not null) {observed.RubricRequested-=OpenRubric;observed.EvaluationRequested-=OpenEvaluation;observed.ConfigurationRequested-=OpenConfiguration;}
        observed=DataContext as MainViewModel;
        if(observed is not null) {observed.RubricRequested+=OpenRubric;observed.EvaluationRequested+=OpenEvaluation;observed.ConfigurationRequested+=OpenConfiguration;}
    }
    private async void OpenRubric(string activityId)
    {
        if(observed is not null)await ShowAcademicDialog(new RubricWindow {DataContext=new RubricEditorViewModel(observed.Service,observed.SelectedCut,activityId)});
    }
    private async void OpenEvaluation(string studentId,string activityId)
    {
        if(observed is not null)await ShowAcademicDialog(new EvaluationWindow {DataContext=new EvaluationViewModel(observed.Service,observed.SelectedCut,studentId,activityId)});
    }
    private async void OpenConfiguration()
    {
        if(observed is not null)await ShowAcademicDialog(new ConfigurationWindow {DataContext=new ConfigurationViewModel(observed.Service,observed.SelectedCut)});
    }
    public async Task ShowAcademicDialog(Window dialog)
    {
        dialog.MaxHeight=Math.Max(420,ClientSize.Height-30);
        dialog.MaxWidth=Math.Max(560,ClientSize.Width-40);
        dialog.WindowStartupLocation=WindowStartupLocation.CenterOwner;
        ModalOverlay.IsVisible=true;
        try {await dialog.ShowDialog(this);} finally {ModalOverlay.IsVisible=false;}
    }
}
