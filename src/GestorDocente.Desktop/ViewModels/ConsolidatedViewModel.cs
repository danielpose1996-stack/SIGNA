using GestorDocente.Application;
using GestorDocente.Domain;

namespace GestorDocente.Desktop.ViewModels;

public sealed record CutColumn(int Number,decimal Weight,bool IsSelected=false)
{
    public string Name{get;init;}="";
    public string Label=>string.IsNullOrWhiteSpace(Name) ? $"Corte {Number}" : Name;
    public string Header=>$"{Label}\n{Weight:0.##} %";
}
public sealed record ConsolidatedStudentRow(int Number,Student Student,IReadOnlyDictionary<int,string> CutGrades,decimal? FinalValue,bool Pending)
{
    public IReadOnlyDictionary<int,decimal?> CutValues {get;init;}=new Dictionary<int,decimal?>();
    public IReadOnlyDictionary<int,bool> CutCompletion {get;init;}=new Dictionary<int,bool>();
    public AcademicRiskResult Risk{get;init;}=new(null,AcademicRisk.NoCompletedCuts);
    public decimal? MinimumValue=>Risk.DisplayMinimum;
    public string Minimum=>AcademicFormatting.Grade(MinimumValue);
    public string Email=>string.IsNullOrWhiteSpace(Student.Email) ? "Sin correo registrado" : Student.Email;
    public string MinimumLabel=>$"Necesita {Minimum} en los cortes pendientes";
    public string StatusTone=>Risk.Risk switch {AcademicRisk.Critical or AcademicRisk.Unreachable=>"#A32B37",AcademicRisk.Delicate=>"#8A5800",AcademicRisk.Assured=>"#176345",_=>"#304563"};
    public string Document=>Student.Document;
    public string FullName=>Student.FullName;
    public string Final=>AcademicFormatting.Grade(FinalValue);
    public string Status=>Risk.Label;
}

/// <summary>Read projection shared by all cuts; search never reads persistence.</summary>
public sealed class ConsolidatedViewModel : ObservableObject
{
    private ConsolidatedReadIndex? index;
    private readonly Dictionary<string,int> positions=[];
    private readonly List<ConsolidatedStudentRow> allRows=[];
    private string search="";
    private string searchInput="";
    private readonly DeferredUiAction searchUpdate;
    public ConsolidatedViewModel()=>searchUpdate=new(TimeSpan.FromMilliseconds(100),()=>Search=searchInput);
    public string SearchInput{get=>searchInput;set{if(!Set(ref searchInput,value ?? ""))return;if(searchInput.Length==0){searchUpdate.Cancel();Search="";}else searchUpdate.Schedule();}}
    public string Search{get=>search;set{searchUpdate.Cancel();var next=value ?? "";if(searchInput!=next){searchInput=next;Raise(nameof(SearchInput));}if(Set(ref search,next))Filter();}}
    public IReadOnlyList<CutColumn> Cuts{get;private set;}=[];
    public ObservableRangeCollection<ConsolidatedStudentRow> Rows{get;}=[];
    public ObservableRangeCollection<ConsolidatedStudentRow> RiskStudents{get;}=[];
    public bool HasRiskStudents=>RiskStudents.Count>0;
    public string RiskSummary=>$"Atención académica · {RiskStudents.Count} estudiantes";
    public bool IsEmpty=>Rows.Count==0;
    public bool WeightsPending{get;private set;}
    public string CalculationNote=>WeightsPending ? $"Nota final pendiente: los porcentajes suman {Cuts.Sum(c=>c.Weight):0.##} %. Usa Configurar porcentajes para completar el 100 %." : "Aprobación: 3,0 · Un corte se completa al registrar todas sus notas (incluido 0,0). Nota mínima: promedio ponderado requerido en los cortes pendientes, redondeado hacia arriba a 2 decimales. La nota final conserva los vacíos como 0,0 solo en el cálculo.";
    public string WeightsLabel=>string.Join(" · ",Cuts.Select(c=>$"{c.Label}: {c.Weight:0.##} %"));
    public void Refresh(AcademicWorkspace workspace)
    {
        Rows.Clear();allRows.Clear();positions.Clear();index=new(workspace);
        Cuts=workspace.Cuts.OrderBy(c=>c.Number).Select(c=>new CutColumn(c.Number,c.Weight){Name=c.Name}).ToArray();
        WeightsPending=workspace.CutWeightsPending;
        foreach(var student in workspace.Students)
        {
            positions.Add(student.Id,allRows.Count);allRows.Add(Build(student,allRows.Count+1));
        }
        Raise(nameof(Cuts));Raise(nameof(WeightsLabel));Raise(nameof(WeightsPending));Raise(nameof(CalculationNote));Filter();UpdateRisk();
    }
    public void ApplyGrade(WorkspaceChange change)
    {
        if(change.StudentId is not {} id || change.ActivityId is not {} activity ||
           index is null || !Cuts.Any(c=>c.Number==change.Cut) || !positions.TryGetValue(id,out var position))return;
        index.ApplyGrade(change);
        var row=Build(allRows[position].Student,position+1);allRows[position]=row;
        for(var i=0;i<Rows.Count;i++)if(Rows[i].Student.Id==id){Rows[i]=row;break;}
        UpdateRisk();
    }
    private ConsolidatedStudentRow Build(Student student,int number)
    {
        var result=index!.For(student.Id);
        var values=result.Cuts.ToDictionary(c=>c.Key,c=>AcademicFormatting.Grade(c.Value.Value)+(c.Value.Completed ? "" : " · pendiente"));
        return new(number,student,values,result.Final,result.Cuts.Values.Any(c=>!c.Completed))
        {CutValues=result.Cuts.ToDictionary(c=>c.Key,c=>c.Value.Value),CutCompletion=result.Cuts.ToDictionary(c=>c.Key,c=>c.Value.Completed),Risk=result.Risk};
    }
    private void UpdateRisk()
    {
        RiskStudents.ReplaceAll(allRows.Where(r=>r.Risk.Risk is AcademicRisk.Critical or AcademicRisk.Delicate)
            .OrderBy(r=>r.Risk.Risk==AcademicRisk.Critical ? 0 : 1).ThenByDescending(r=>r.Risk.Minimum).ThenBy(r=>r.FullName,StringComparer.CurrentCultureIgnoreCase));
        Raise(nameof(HasRiskStudents));Raise(nameof(RiskSummary));
    }
    private void Filter()
    {
        Rows.ReplaceAll(allRows.Where(r=>r.FullName.Contains(Search,StringComparison.OrdinalIgnoreCase) || r.Document.Contains(Search,StringComparison.OrdinalIgnoreCase)));
        Raise(nameof(IsEmpty));
    }
}
