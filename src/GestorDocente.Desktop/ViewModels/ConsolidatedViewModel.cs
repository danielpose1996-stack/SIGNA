using GestorDocente.Application;
using GestorDocente.Domain;

namespace GestorDocente.Desktop.ViewModels;

public sealed record CutColumn(int Number,decimal Weight,bool IsSelected=false)
{
    public string Label=>$"Corte {Number}";
    public string Header=>$"{Label}\n{Weight:0.##} %";
}
public sealed record ConsolidatedStudentRow(int Number,Student Student,IReadOnlyDictionary<int,string> CutGrades,decimal? FinalValue,bool Pending)
{
    public string Document=>Student.Document;
    public string FullName=>Student.FullName;
    public string Final=>AcademicFormatting.Grade(FinalValue);
    public string Status=>FinalValue is null ? "Configurar porcentajes" : Pending ? "Con pendientes" : "Completo";
}

/// <summary>Read projection shared by all cuts; search never reads persistence.</summary>
public sealed class ConsolidatedViewModel : ObservableObject
{
    private readonly Dictionary<int,AcademicCutReadIndex> indexes=[];
    private readonly Dictionary<string,int> positions=[];
    private readonly List<ConsolidatedStudentRow> allRows=[];
    private string search="";
    public string Search{get=>search;set{if(Set(ref search,value ?? ""))Filter();}}
    public IReadOnlyList<CutColumn> Cuts{get;private set;}=[];
    public ObservableRangeCollection<ConsolidatedStudentRow> Rows{get;}=[];
    public bool IsEmpty=>Rows.Count==0;
    public bool WeightsPending{get;private set;}
    public string CalculationNote=>WeightsPending ? $"Nota final pendiente: los porcentajes suman {Cuts.Sum(c=>c.Weight):0.##} %. Usa Configurar porcentajes para completar el 100 %." : "Escala 0,0–5,0 · Las notas pendientes y los cortes sin configuración aportan 0,0 al cálculo; los datos originales se conservan.";
    public string WeightsLabel=>string.Join(" · ",Cuts.Select(c=>$"{c.Label}: {c.Weight:0.##} %"));
    public void Refresh(AcademicWorkspace workspace)
    {
        Rows.Clear();allRows.Clear();positions.Clear();indexes.Clear();
        Cuts=workspace.Cuts.OrderBy(c=>c.Number).Select(c=>new CutColumn(c.Number,c.Weight)).ToArray();
        WeightsPending=workspace.CutWeightsPending;
        foreach(var cut in workspace.Cuts)indexes.Add(cut.Number,new(cut));
        foreach(var student in workspace.Students)
        {
            positions.Add(student.Id,allRows.Count);allRows.Add(Build(student,allRows.Count+1));
        }
        Raise(nameof(Cuts));Raise(nameof(WeightsLabel));Raise(nameof(WeightsPending));Raise(nameof(CalculationNote));Filter();
    }
    public void ApplyGrade(WorkspaceChange change)
    {
        if(change.StudentId is not {} id || change.ActivityId is not {} activity ||
           !indexes.TryGetValue(change.Cut,out var index) || !positions.TryGetValue(id,out var position))return;
        index.ApplyGrade(id,activity,change.Grade);
        var row=Build(allRows[position].Student,position+1);allRows[position]=row;
        for(var i=0;i<Rows.Count;i++)if(Rows[i].Student.Id==id){Rows[i]=row;break;}
    }
    private ConsolidatedStudentRow Build(Student student,int number)
    {
        bool pending=false;Dictionary<int,string> values=[];List<(decimal? Grade,decimal Weight)> results=[];
        foreach(var cut in Cuts)
        {
            var index=indexes[cut.Number];var value=index.Cut(student.Id);
            values.Add(cut.Number,AcademicFormatting.Grade(value));
            results.Add((value,cut.Weight));
            pending|=value is null || index.HasPendingGrades(student.Id);
        }
        return new(number,student,values,GradeCalculator.Final(results,WeightsPending),pending);
    }
    private void Filter()
    {
        Rows.ReplaceAll(allRows.Where(r=>r.FullName.Contains(Search,StringComparison.OrdinalIgnoreCase) || r.Document.Contains(Search,StringComparison.OrdinalIgnoreCase)));
        Raise(nameof(IsEmpty));
    }
}
