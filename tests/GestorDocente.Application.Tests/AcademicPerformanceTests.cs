using GestorDocente.Application;
using GestorDocente.Domain;
using Xunit;

namespace GestorDocente.Application.Tests;

public class AcademicPerformanceTests
{
    [Fact]
    public void Three_cut_dashboard_preserves_course_metrics_and_average_of_pending_zeros()
    {
        var data=DemoWorkspaceFactory.Create();data.Students=data.Students.Take(2).ToList();
        data.Cuts[0].Groups=[new(){Id="a",Name="Trabajo",Weight=100}];
        data.Cuts[0].Grades=[new(data.Students[0].Id,"a",4m)];
        data.Cuts[1].Groups=[new(){Id="b",Name="Parcial",Weight=100}];
        data.Cuts[1].Grades=[];data.Cuts[2].Groups=[];
        var dashboard=AcademicPerformanceProjection.Build(data);
        Assert.Equal(3,dashboard.Cuts.Count);
        Assert.Equal(2m,dashboard.Cuts[0].Average);
        Assert.Equal(0m,dashboard.Cuts[0].Minimum);Assert.Equal(4m,dashboard.Cuts[0].Maximum);
        Assert.Equal(1,dashboard.Cuts[0].Complete);Assert.Equal(1,dashboard.Cuts[0].Pending);
        Assert.Equal(0m,dashboard.Cuts[1].Average);Assert.Null(dashboard.Cuts[2].Average);
        Assert.Equal(2,dashboard.StudentCount);
    }
}
