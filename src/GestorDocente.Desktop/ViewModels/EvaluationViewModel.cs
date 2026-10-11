using System.Collections.ObjectModel;
using GestorDocente.Application;
using GestorDocente.Domain;

namespace GestorDocente.Desktop.ViewModels;

public sealed class CriterionScore : ObservableObject
{
    public string Id {get;init;}="";
    public string Name {get;init;}="";
    public string Description {get;init;}="";
    public string WeightLabel {get;init;}="";
    private decimal? score;
    public decimal? Score {get=>score;set=>Set(ref score,value);}
}
public sealed class EvaluationViewModel : ObservableObject
{
    private readonly WorkspaceService service;
    private readonly int cut;
    private readonly string studentId,activityId;
    private string error="",result="Completa todos los criterios.";
    public string Title {get;}
    public string StudentName {get;}
    public string Error {get=>error;private set=>Set(ref error,value);}
    public string Result {get=>result;private set=>Set(ref result,value);}
    public ObservableCollection<CriterionScore> Scores {get;}
    public EvaluationViewModel(WorkspaceService service,int cut,string studentId,string activityId)
    {
        this.service=service;this.cut=cut;this.studentId=studentId;this.activityId=activityId;
        var activity=service.Activity(cut,activityId);Title=activity.Name;StudentName=service.Current.Students.Single(x=>x.Id==studentId).FullName;
        var previous=service.Cut(cut).Grades.SingleOrDefault(x=>x.StudentId==studentId && x.ActivityId==activityId)?.Assessment;
        Scores=new(activity.Rubric!.Criteria.Select(x=>new CriterionScore {Id=x.Id,Name=x.Name,Description=x.Description,WeightLabel=$"{x.Weight:0.##} %",Score=previous is not null && previous.Scores.TryGetValue(x.Id,out var saved) ? saved : null}));
        foreach(var score in Scores)score.PropertyChanged+=(_,_)=>UpdateResult();
        UpdateResult();
    }
    private Dictionary<string,decimal> Values()
    {
        if(Scores.Any(x=>x.Score is null))throw new AcademicValidationException("Completa todos los criterios (0,0 a 5,0).");
        return Scores.ToDictionary(x=>x.Id,x=>x.Score!.Value);
    }
    private void UpdateResult()
    {
        try {Result="Nota de la actividad: "+AcademicFormatting.Grade(GradeCalculator.Rubric(service.Activity(cut,activityId).Rubric!,Values()));}
        catch(AcademicValidationException ex) {Result=ex.Message;}
    }
    public bool Save()
    {
        try {service.Evaluate(cut,studentId,activityId,Values());Error="";return true;}
        catch(Exception ex) when(ex is AcademicValidationException or IOException or System.Data.Common.DbException) {Error=ex is AcademicValidationException ? ex.Message : "No se guardó la evaluación.";return false;}
    }
}
