using GestorDocente.Application;
using GestorDocente.Domain;
namespace GestorDocente.Desktop.ViewModels;

public sealed record AttendanceConsolidatedRow(int Number,Student Student,IReadOnlyDictionary<int,int> Absences,int Total)
{
    public string FullName=>Student.FullName;
}

/// <summary>Read-only attendance projection; neither search nor edits query persistence.</summary>
public sealed class AttendanceConsolidatedViewModel:ObservableObject
{
    private AttendanceAbsenceReadIndex? index;
    private readonly List<AttendanceConsolidatedRow> allRows=[];
    private readonly Dictionary<string,int> positions=[];
    private readonly DeferredUiAction searchUpdate;
    private string search="",searchInput="";
    public AttendanceConsolidatedViewModel()=>searchUpdate=new(TimeSpan.FromMilliseconds(100),()=>Search=searchInput);
    public IReadOnlyList<CutColumn> Cuts{get;private set;}=[];
    public ObservableRangeCollection<AttendanceConsolidatedRow> Rows{get;}=[];
    public bool IsEmpty=>Rows.Count==0;
    public string Summary=>$"{allRows.Count} estudiantes · {Cuts.Count} cortes · Solo ausencias (A) de semanas activas";
    public string SearchInput{get=>searchInput;set{if(!Set(ref searchInput,value ?? ""))return;if(searchInput.Length==0){searchUpdate.Cancel();Search="";}else searchUpdate.Schedule();}}
    public string Search
    {
        get=>search;
        set{searchUpdate.Cancel();var next=value ?? "";if(searchInput!=next){searchInput=next;Raise(nameof(SearchInput));}if(Set(ref search,next))Filter();}
    }
    public void Refresh(AcademicWorkspace workspace)
    {
        index=new(workspace.Cuts);positions.Clear();allRows.Clear();
        Cuts=workspace.Cuts.OrderBy(c=>c.Number).Select(c=>new CutColumn(c.Number,c.Weight){Name=c.Name}).ToArray();
        foreach(var student in workspace.Students){positions.Add(student.Id,allRows.Count);allRows.Add(Build(student,allRows.Count+1));}
        Raise(nameof(Cuts));Raise(nameof(Summary));Filter();
    }
    public void ApplyAttendance(WorkspaceChange change)
    {
        if(index is null || change.StudentId is not {} id || change.Week is not {} week || !positions.TryGetValue(id,out var position))return;
        index.ApplyAttendance(change.Cut,id,week,change.Attendance);
        var row=Build(allRows[position].Student,position+1);allRows[position]=row;
        for(var i=0;i<Rows.Count;i++)if(Rows[i].Student.Id==id){Rows[i]=row;break;}
    }
    private AttendanceConsolidatedRow Build(Student student,int number)=>new(number,student,Cuts.ToDictionary(c=>c.Number,c=>index!.Count(c.Number,student.Id)),index!.Total(student.Id));
    private void Filter()
    {
        Rows.ReplaceAll(allRows.Where(r=>r.FullName.Contains(Search,StringComparison.OrdinalIgnoreCase) || r.Student.Document.Contains(Search,StringComparison.OrdinalIgnoreCase)));
        Raise(nameof(IsEmpty));
    }
}
