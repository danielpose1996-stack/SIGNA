using System.Collections.ObjectModel;
using GestorDocente.Application;
using GestorDocente.Domain;

namespace GestorDocente.Desktop.ViewModels;

public sealed class RubricEditorViewModel : ObservableObject
{
    private readonly WorkspaceService service;
    private readonly int cut;
    private readonly EvaluationActivity draft;
    private string name,description,error="";
    private decimal weight;
    public string Name {get=>name;set=>Set(ref name,value);}
    public string Description {get=>description;set=>Set(ref description,value);}
    public decimal Weight {get=>weight;set=>Set(ref weight,value);}
    public string GroupLabel {get;}
    public bool IsPartial {get;}
    public string RubricState {get;}
    public string Error {get=>error;private set=>Set(ref error,value);}
    public ObservableCollection<RubricCriterion> Criteria {get;}
    public ObservableCollection<RubricLevel> Levels {get;}
    public RelayCommand AddCriterionCommand {get;}
    public RelayCommand AddLevelCommand {get;}
    public RubricEditorViewModel(WorkspaceService service,int cut,string activityId)
    {
        this.service=service;this.cut=cut;draft=WorkspaceService.Clone(service.Activity(cut,activityId));
        var group=service.Cut(cut).Groups.Single(x=>x.Activities.Any(a=>a.Id==activityId));
        GroupLabel=$"Grupo: {group.Name} · {group.Weight:0.##} % del corte";IsPartial=group.IsPartial;
        RubricState=draft.Rubric is null ? "Nueva rúbrica" : "Con rúbrica";
        name=draft.Name;description=draft.Description;weight=draft.Weight;
        var rubric=draft.Rubric ?? DemoWorkspaceFactory.DefaultRubric();
        Criteria=new(rubric.Criteria);Levels=new(rubric.Levels);
        AddCriterionCommand=new(_=>Criteria.Add(new() {Name="Nuevo criterio",Weight=1}));
        AddLevelCommand=new(_=>Levels.Add(new() {Name="Nuevo nivel",Lower=0,Upper=100}));
    }
    public RubricDefinition Definition()=>new() {Criteria=Criteria.ToList(),Levels=Levels.ToList()};
    public bool Save()
    {
        try {draft.Name=Name;draft.Description=Description;draft.Weight=Weight;draft.Rubric=Definition();AcademicRules.ValidateRubric(draft.Rubric);service.SaveActivity(cut,draft);Error="";return true;}
        catch(Exception ex) when(ex is AcademicValidationException or IOException or System.Data.Common.DbException) {Error=ex is AcademicValidationException ? ex.Message : "No se guardó la rúbrica. Reintenta.";return false;}
    }
    public string Preview()
    {
        try {
            var rubric=Definition();AcademicRules.ValidateRubric(rubric);Error="";
            return $"{Name}\n{GroupLabel}\n\nCRITERIOS\n"+string.Join("\n",Criteria.Select(x=>$"{x.Name} · {x.Weight:0.##} %\n{x.Description}"))+"\n\nNIVELES\n"+string.Join("\n",Levels.OrderByDescending(x=>x.Lower).Select(x=>$"{x.Name}: [{x.Lower:0.##}, {x.Upper:0.##}{(x.Upper==100 ? "]" : ")")} %\n{x.Description}"));
        } catch(AcademicValidationException ex) {Error=ex.Message;return "";}
    }
}
