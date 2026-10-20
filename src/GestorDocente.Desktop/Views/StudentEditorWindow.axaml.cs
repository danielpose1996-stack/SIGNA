using Avalonia.Controls;
using Avalonia.Interactivity;
using System.ComponentModel;
using GestorDocente.Desktop.ViewModels;
namespace GestorDocente.Desktop.Views;
public partial class StudentEditorWindow:Window
{
    private StudentEditorViewModel? observed;
    private bool? lastBulkMode;
    public StudentEditorWindow()
    {
        InitializeComponent();
        DataContextChanged+=(_,_)=>ObserveModel();
        Opened+=(_,_)=>ResizeForMode(force:true);
        Closed+=(_,_)=>{if(observed is not null)observed.PropertyChanged-=OnModelChanged;};
        Closing+=(_,e)=>{if(DataContext is StudentEditorViewModel{IsSaving:true})e.Cancel=true;};
    }
    private void ObserveModel()
    {
        if(observed is not null)observed.PropertyChanged-=OnModelChanged;
        observed=DataContext as StudentEditorViewModel;lastBulkMode=null;
        if(observed is not null)observed.PropertyChanged+=OnModelChanged;
        ResizeForMode();
    }
    private void OnModelChanged(object? sender,PropertyChangedEventArgs e)
    {
        if(e.PropertyName==nameof(StudentEditorViewModel.IsBulk))ResizeForMode();
    }
    private void ResizeForMode(bool force=false)
    {
        if(observed is null || !force && lastBulkMode==observed.IsBulk)return;
        lastBulkMode=observed.IsBulk;
        Width=Math.Min(MaxWidth,observed.IsBulk ? 1060 : 490);
        Height=Math.Min(MaxHeight,observed.IsBulk ? 780 : observed.CanUseBulk ? 600 : 535);
    }
    private void Cancel(object? sender,RoutedEventArgs e)=>Close();
    private async void Save(object? sender,RoutedEventArgs e){if(DataContext is StudentEditorViewModel model && await model.SaveAsync())Close();}
}
