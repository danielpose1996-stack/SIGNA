using GestorDocente.Domain;
namespace GestorDocente.Application;
public interface IWorkspaceStore
{
    AcademicWorkspace? Load();
    void Save(AcademicWorkspace workspace);
}
