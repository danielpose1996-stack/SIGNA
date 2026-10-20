using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using GestorDocente.Domain;

namespace GestorDocente.Application;

/// <summary>Comprueba el agregado completo antes de publicarlo o persistirlo.</summary>
public static class WorkspaceIntegrity
{
    public static void ValidateLegacyShape(AcademicWorkspace data)
    {
        if(data.SchemaVersion!=1 || data.Students is null || data.Cuts is null || data.Cuts.Count!=3)Invalid();
        foreach(var cut in data.Cuts)
        {
            if(cut is null || cut.Groups is null || cut.RemovedGroups is null || cut.RemovedActivities is null)Invalid();
            foreach(var group in cut.Groups.Concat(cut.RemovedGroups))
                if(group is null || group.Activities is null || group.Activities.Any(a=>a is null))Invalid();
        }
    }
    public static AcademicWorkspace Deserialize(string payload)
    {
        using var json=JsonDocument.Parse(payload);
        var root=json.RootElement;
        if(root.ValueKind!=JsonValueKind.Object ||
           !HasArray(root,"Students") || !HasArray(root,"Cuts"))Invalid();
        var version=root.TryGetProperty("SchemaVersion",out var value) && value.TryGetInt32(out var parsed) ? parsed : 0;
        if(version is <1 or >AcademicWorkspace.CurrentSchemaVersion)Invalid();
        if(version>=4 && (!root.TryGetProperty("CutWeightsPending",out var pending) || pending.ValueKind is not (JsonValueKind.True or JsonValueKind.False)))Invalid();
        if(version>=2)
            foreach(var cut in root.GetProperty("Cuts").EnumerateArray())
            {
                if(cut.ValueKind!=JsonValueKind.Object)Invalid();
                foreach(var key in new[]{"Groups","Grades","Weeks","Attendance","RemovedActivities","RemovedGroups","RemovedWeeks"})
                    if(!HasArray(cut,key))Invalid();
                if(version>=3 && (!cut.TryGetProperty("Weight",out var weight) || !weight.TryGetDecimal(out _)))Invalid();
                if(version>=6)
                {
                    if(!cut.TryGetProperty("AttendanceGrading",out var grading))Invalid();
                    if(grading.ValueKind!=JsonValueKind.Null)
                    {
                        if(grading.ValueKind!=JsonValueKind.Object || !grading.TryGetProperty("Enabled",out var enabled) || enabled.ValueKind is not (JsonValueKind.True or JsonValueKind.False))Invalid();
                        foreach(var key in new[]{"Present","Absent","Justified","Late"})
                            if(!grading.TryGetProperty(key,out var grade) || !grade.TryGetDecimal(out _))Invalid();
                    }
                }
                if(version>=7)
                {
                    foreach(var group in cut.GetProperty("Groups").EnumerateArray().Concat(cut.GetProperty("RemovedGroups").EnumerateArray()))
                    {
                        if(group.ValueKind!=JsonValueKind.Object || !group.TryGetProperty("UseWeightedAverage",out var weighted) ||
                           weighted.ValueKind is not (JsonValueKind.True or JsonValueKind.False) || !HasArray(group,"Activities"))Invalid();
                        foreach(var activity in group.GetProperty("Activities").EnumerateArray())ValidateActivityShape(activity);
                    }
                    foreach(var removed in cut.GetProperty("RemovedActivities").EnumerateArray())
                    {
                        if(removed.ValueKind!=JsonValueKind.Object)Invalid();
                        if(!removed.TryGetProperty("Activity",out var activity))Invalid();
                        ValidateActivityShape(activity);
                    }
                }
            }
        return JsonSerializer.Deserialize<AcademicWorkspace>(payload) ?? throw new AcademicValidationException("La materia almacenada está dañada.");
    }
    private static bool HasArray(JsonElement value,string key)=>
        value.TryGetProperty(key,out var array) && array.ValueKind==JsonValueKind.Array;
    private static void ValidateActivityShape(JsonElement activity)
    {
        if(activity.ValueKind!=JsonValueKind.Object || !activity.TryGetProperty("Weight",out var weight) ||
           weight.ValueKind!=JsonValueKind.Null && (weight.ValueKind!=JsonValueKind.Number || !weight.TryGetDecimal(out _)))Invalid();
    }
    public static void Validate(AcademicWorkspace data)
    {
        if(data.SchemaVersion is <2 or >AcademicWorkspace.CurrentSchemaVersion || data.Students is null || data.Cuts is null ||
           data.Students.Count>5000 || data.Cuts.Count<1 || (data.SchemaVersion<4 && data.Cuts.Count<3) || (data.SchemaVersion==2 && data.Cuts.Count!=3) ||
           !data.Cuts.Select(c=>c?.Number).Order().SequenceEqual(Enumerable.Range(1,data.Cuts.Count).Select(n=>(int?)n)))
            Invalid();
        if(data.SchemaVersion==3)AcademicRules.ValidateCutWeights(data.Cuts.Select(c=>c.Weight));
        if(data.SchemaVersion>=4)
        {
            var weights=data.Cuts.Select(c=>c.Weight).ToArray();
            AcademicRules.ValidateCutWeightValues(weights);
            if(data.CutWeightsPending!=(weights.Sum()!=100))Invalid();
        }

        var ids=new HashSet<string>(StringComparer.Ordinal);
        var documents=new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach(var student in data.Students)
        {
            if(student is null || string.IsNullOrWhiteSpace(student.Id) ||
               !ids.Add(student.Id))Invalid();
            StudentRules.Validate(student,data.SchemaVersion<5);
            if(student.Document.Length>0 && !documents.Add(student.Document.Trim()))Invalid();
        }
        foreach(var cut in data.Cuts)ValidateCut(cut,ids);
    }

    private static void ValidateCut(AcademicCut cut,HashSet<string> students)
    {
        cut.AttendanceGrading?.Validate();
        if(cut.Groups is null || cut.RemovedGroups is null || cut.RemovedActivities is null ||
           cut.Grades is null || cut.Weeks is null || cut.RemovedWeeks is null || cut.Attendance is null ||
           cut.Weeks.Count>9)Invalid();

        var targetIds=new HashSet<string>(StringComparer.Ordinal);
        var groupIds=new HashSet<string>(StringComparer.Ordinal);
        foreach(var group in cut.Groups.Concat(cut.RemovedGroups))
        {
            if(group is null || string.IsNullOrWhiteSpace(group.Id) || !groupIds.Add(group.Id) ||
               string.IsNullOrWhiteSpace(group.Name) || group.Weight is <=0 or >100 ||
               !Enum.IsDefined(group.Mode) || group.Activities is null ||
               group.Mode==EvaluationMode.Direct && group.Activities.Count>0)Invalid();
            if(!targetIds.Add(group.Id))Invalid();
            foreach(var activity in group.Activities)AddActivity(activity,targetIds);
        }
        foreach(var removed in cut.RemovedActivities)
        {
            if(removed is null || !groupIds.Contains(removed.GroupId))Invalid();
            AddActivity(removed.Activity,targetIds);
        }
        if(cut.Groups.Count>0)AcademicRules.ValidateGroups(cut.Groups,true);

        var weekIds=new HashSet<int>();
        foreach(var week in cut.Weeks.Concat(cut.RemovedWeeks))
            if(week is null || week.Number<1 || !Enum.IsDefined(week.Kind) || !weekIds.Add(week.Number))Invalid();

        var gradeKeys=new HashSet<(string,string)>();
        foreach(var grade in cut.Grades)
        {
            if(grade is null || !students.Contains(grade.StudentId) ||
               !targetIds.Contains(grade.ActivityId) || !gradeKeys.Add((grade.StudentId,grade.ActivityId)))Invalid();
            AcademicRules.ValidateGrade(grade.Value);
        }
        var attendanceKeys=new HashSet<(string,int)>();
        foreach(var entry in cut.Attendance)
            if(entry is null || !students.Contains(entry.StudentId) || !weekIds.Contains(entry.WeekNumber) ||
               !Enum.IsDefined(entry.State) || !attendanceKeys.Add((entry.StudentId,entry.WeekNumber)))Invalid();
    }

    private static void AddActivity(EvaluationActivity activity,HashSet<string> ids)
    {
        if(activity is null || string.IsNullOrWhiteSpace(activity.Id) ||
           string.IsNullOrWhiteSpace(activity.Name) || activity.Weight is <0 or >100 || !ids.Add(activity.Id))Invalid();
    }
    [DoesNotReturn]
    private static void Invalid()=>throw new AcademicValidationException("La materia contiene datos académicos inconsistentes. El archivo original no se modificó; recupera una copia válida antes de continuar.");
}
