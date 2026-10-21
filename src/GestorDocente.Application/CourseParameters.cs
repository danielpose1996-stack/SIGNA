using GestorDocente.Domain;

namespace GestorDocente.Application;

/// <summary>Instantánea de configuración con lista permitida; nunca contiene registros personales.</summary>
public sealed class CourseParameters
{
    public IReadOnlyList<CourseCutParameters> Cuts {get;}
    public bool CutWeightsPending {get;}

    private CourseParameters(AcademicWorkspace source)
    {
        CutWeightsPending=source.CutWeightsPending;
        Cuts=source.Cuts.OrderBy(c=>c.Number).Select(c=>new CourseCutParameters(c.Number,c.Weight,
            c.Groups.Select(g=>new CourseActivityParameters(g.Name,g.Weight,g.Mode,g.UseWeightedAverage,
                g.Activities.Select(a=>new CourseSubactivityParameters(a.Name,a.Weight)).ToList().AsReadOnly())).ToList().AsReadOnly(),
            c.AttendanceGrading is {Enabled:true} policy ? policy with {} : null)).ToList().AsReadOnly();
    }

    public static CourseParameters Capture(AcademicWorkspace source)
    {
        WorkspaceIntegrity.Validate(source);
        return new(source);
    }

    /// <summary>Materializa entidades nuevas en cada creación, sin reutilizar IDs ni colecciones.</summary>
    public List<AcademicCut> CreateCuts()=>Cuts.Select(c=>new AcademicCut
    {
        Number=c.Number,Weight=c.Weight,AttendanceGrading=c.AttendanceGrading is {} policy ? policy with {} : null,
        Groups=c.Groups.Select(g=>new EvaluationGroup
        {
            Name=g.Name,Weight=g.Weight,Mode=g.Mode,UseWeightedAverage=g.UseWeightedAverage,
            Activities=g.Subactivities.Select(a=>new EvaluationActivity{Name=a.Name,Weight=a.Weight}).ToList()
        }).ToList()
    }).ToList();
}

public sealed record CourseCutParameters(int Number,decimal Weight,IReadOnlyList<CourseActivityParameters> Groups,AttendanceGradePolicy? AttendanceGrading);
public sealed record CourseActivityParameters(string Name,decimal Weight,EvaluationMode Mode,bool UseWeightedAverage,IReadOnlyList<CourseSubactivityParameters> Subactivities);
public sealed record CourseSubactivityParameters(string Name,decimal? Weight);
