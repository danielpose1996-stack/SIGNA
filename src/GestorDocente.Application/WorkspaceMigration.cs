using GestorDocente.Domain;

namespace GestorDocente.Application;

/// <summary>Transforms a detached snapshot; persistence remains the caller's atomic operation.</summary>
public static class WorkspaceMigration
{
    public static AcademicWorkspace Upgrade(AcademicWorkspace source)
    {
        if(source.SchemaVersion is <1 or >AcademicWorkspace.CurrentSchemaVersion || source.Cuts is null ||
           (source.SchemaVersion<3 && !source.Cuts.Select(c=>c?.Number).Order().SequenceEqual(new int?[]{1,2,3})))
            throw new AcademicValidationException("La versión o los cortes de la materia no son compatibles. Los datos no se reemplazaron.");
        if(source.SchemaVersion==AcademicWorkspace.CurrentSchemaVersion){WorkspaceIntegrity.Validate(source);return source;}
        if(source.SchemaVersion==1)WorkspaceIntegrity.ValidateLegacyShape(source);
        else WorkspaceIntegrity.Validate(source);
        var next=WorkspaceService.Clone(source);
        // Earlier versions always used a simple average. Do not revive legacy rubric weights.
        foreach(var cut in next.Cuts)
        {
            foreach(var group in cut.Groups.Concat(cut.RemovedGroups))
            {
                group.UseWeightedAverage=false;
                foreach(var activity in group.Activities)activity.Weight=null;
            }
            foreach(var removed in cut.RemovedActivities)
                if(removed?.Activity is {} activity)activity.Weight=null;
        }
        if(next.SchemaVersion==1)
            foreach(var group in next.Cuts.SelectMany(c=>c.Groups.Concat(c.RemovedGroups)))
                group.Mode=group.Activities.Count>0 ? EvaluationMode.SubActivities : EvaluationMode.Direct;
        if(source.SchemaVersion<3)foreach(var cut in next.Cuts)cut.Weight=AcademicRules.DefaultCutWeight(cut.Number);
        next.SchemaVersion=AcademicWorkspace.CurrentSchemaVersion;
        if(source.SchemaVersion<4)next.CutWeightsPending=false;
        WorkspaceIntegrity.Validate(next);
        return next;
    }
}
