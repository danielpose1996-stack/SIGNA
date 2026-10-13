using GestorDocente.Domain;

namespace GestorDocente.Application;
public interface IInterfacePreferencesStore
{
    InterfacePreferences Load();
    void Save(InterfacePreferences preferences);
}
public sealed class InterfacePreferencesService
{
    private readonly IInterfacePreferencesStore store;
    private bool saving;
    public InterfacePreferences Current{get;private set;}=new();
    public string Warning{get;private set;}="";
    public InterfacePreferencesService(IInterfacePreferencesStore store)
    {
        this.store=store;
        try{var loaded=store.Load();loaded.Validate();Current=loaded;}
        catch(Exception ex){Warning=ex is AcademicValidationException ? ex.Message : $"No se pudo cargar la apariencia ({ex.GetType().Name}). Se usa la predeterminada; los datos no se borraron.";}
    }
    public async Task SaveAsync(InterfacePreferences preferences)
    {
        if(saving)throw new AcademicValidationException("Espera a que termine de guardarse la apariencia.");
        preferences.Validate();saving=true;
        try{await Task.Run(()=>store.Save(preferences));Current=preferences;Warning="";}
        finally{saving=false;}
    }
}
public sealed class MemoryInterfacePreferencesStore:IInterfacePreferencesStore
{
    private InterfacePreferences current=new();
    public InterfacePreferences Load()=>current;
    public void Save(InterfacePreferences preferences){preferences.Validate();current=preferences;}
}
