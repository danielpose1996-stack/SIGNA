using System.Text.Json.Nodes;
using GestorDocente.Application;
using GestorDocente.Domain;
using Microsoft.Data.Sqlite;

namespace GestorDocente.Infrastructure;

public sealed partial class SqliteCatalogStore
{
    private void NormalizeCourseMetadata(SqliteConnection db,string path)
    {
        var ids=new List<string>();
        using(var read=db.CreateCommand())
        {
            read.CommandText="SELECT id,code,name,school,group_name FROM courses";
            using var rows=read.ExecuteReader();
            while(rows.Read())
                if(Enumerable.Range(1,4).Any(i=>rows.GetString(i)!=CourseText.Upper(rows.GetString(i))))
                    ids.Add(rows.GetString(0));
        }
        if(ids.Count==0)return;
        var backup=Path.Combine(Path.GetDirectoryName(path)!,$"signa-antes-mayusculas-{DateTime.UtcNow:yyyyMMdd-HHmmss}-{Guid.NewGuid():N}.db");
        new EncryptedLocalBackupService(path).Snapshot(backup);
        _=SqliteBackupValidator.Validate(backup);
        using var transaction=db.BeginTransaction();
        foreach(var id in ids)
        {
            string payload;long revision;string[] original;
            using(var read=db.CreateCommand())
            {
                read.Transaction=transaction;
                read.CommandText="SELECT c.code,c.name,c.school,c.group_name,w.payload,w.revision FROM courses c JOIN course_workspaces w ON w.course_id=c.id WHERE c.id=$id";
                read.Parameters.AddWithValue("$id",id);
                using var row=read.ExecuteReader();
                if(!row.Read())throw new AcademicValidationException("El catálogo cambió durante la normalización. Vuelve a abrir SIGNA.");
                original=Enumerable.Range(0,4).Select(row.GetString).ToArray();payload=row.GetString(4);revision=row.GetInt64(5);
            }
            var workspace=WorkspaceIntegrity.Deserialize(payload);
            if(workspace.CourseId!=id || !original.SequenceEqual(new[]{workspace.CourseCode,workspace.CourseName,workspace.School,workspace.GroupName}))
                throw new AcademicValidationException("Los datos de la materia no coinciden con el catálogo. No se aplicó la normalización.");
            _=WorkspaceMigration.Upgrade(workspace);
            var upper=original.Select(CourseText.Upper).ToArray();
            if(original.SequenceEqual(upper))continue;
            var json=JsonNode.Parse(payload)!;
            foreach(var (key,value) in new[]{("CourseCode",upper[0]),("CourseName",upper[1]),("School",upper[2]),("GroupName",upper[3])})
                json[key]=value;
            using var update=db.CreateCommand();update.Transaction=transaction;
            update.CommandText="UPDATE courses SET code=$code,name=$name,school=$school,group_name=$group WHERE id=$id; UPDATE course_workspaces SET payload=$payload,revision=revision+1 WHERE course_id=$id AND revision=$revision";
            foreach(var (key,value) in new[]{("$id",id),("$code",upper[0]),("$name",upper[1]),("$school",upper[2]),("$group",upper[3]),("$payload",json.ToJsonString())})
                update.Parameters.AddWithValue(key,value);
            update.Parameters.AddWithValue("$revision",revision);
            try
            {
                if(update.ExecuteNonQuery()!=2)throw new AcademicValidationException("La materia cambió durante la normalización. No se modificaron los datos.");
            }
            catch(SqliteException ex) when(ex.SqliteExtendedErrorCode==2067)
            {
                throw new AcademicValidationException("Hay materias cuyos códigos y grupos coinciden al convertirlos a mayúsculas. Revisa esos registros antes de reintentar. Las materias originales se conservaron.",ex);
            }
        }
        transaction.Commit();changes?.Saved();
    }
}
