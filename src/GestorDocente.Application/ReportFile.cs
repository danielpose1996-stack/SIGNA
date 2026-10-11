namespace GestorDocente.Application;

public static class ReportFile
{
    // Archivo temporal adyacente: no destruir un reporte anterior ante fallo de escritura.
    public static void Save(string destination,byte[] bytes,bool overwrite=false)
    {
        var path=Path.GetFullPath(destination);var temporary=Path.Combine(Path.GetDirectoryName(path)!,"."+Guid.NewGuid().ToString("N")+".tmp");
        try{using(var output=new FileStream(temporary,FileMode.CreateNew,FileAccess.Write,FileShare.None)){output.Write(bytes);output.Flush(true);}File.Move(temporary,path,overwrite);}
        finally{if(File.Exists(temporary))File.Delete(temporary);}
    }
}
