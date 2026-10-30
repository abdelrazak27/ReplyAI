using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
namespace ParoleRP {
 // This DTO is persisted only inside a DPAPI-encrypted vault, never in workspace exports.
 public sealed class ApiConnection {
  public string Id {get;set;} public string Name {get;set;} public string Provider {get;set;}
  public string Model {get;set;} public string QuotaGroup {get;set;} public string Key {get;set;} public bool Enabled {get;set;}
  public ApiConnection(){Id=Guid.NewGuid().ToString("N");Enabled=true;}
 }
 public static class OperationRoutes {
  public const string RephraseModel="openai/gpt-oss-120b";
  public static List<ApiConnection> Prepare(List<ApiConnection> connections,bool rephrase){return connections.Select(c=>new ApiConnection{Id=c.Id,Name=c.Name,Provider=c.Provider,Model=rephrase?RephraseModel:c.Model,QuotaGroup=c.QuotaGroup,Key=c.Key,Enabled=c.Enabled}).ToList();}
 }
 public sealed class ConnectionVault {
  readonly string path;
  public ConnectionVault(string root){path=Path.Combine(root,"connections.protected");}
  public List<ApiConnection> Load(){if(!File.Exists(path))return new List<ApiConnection>();try{var bytes=ProtectedData.Unprotect(File.ReadAllBytes(path),null,DataProtectionScope.CurrentUser);var list=DataFiles.Json().Deserialize<List<ApiConnection>>(Encoding.UTF8.GetString(bytes));if(list==null)throw new Exception();list.RemoveAll(c=>c!=null&&c.Provider=="Google");Validate(list);return list;}catch{throw new Exception("Les connexions de secours ne peuvent pas être lues sur ce compte Windows. Le fichier n'a pas été écrasé.");}}
  public static void Validate(List<ApiConnection> list){if(list==null||list.Count>8)throw new Exception("Ajoutez au maximum huit connexions de secours.");var ids=new HashSet<string>();var keys=new HashSet<string>();foreach(var c in list){if(c==null||!Regex.IsMatch(c.Id??"",@"^[a-f0-9]{32}$")||!ids.Add(c.Id))throw new Exception("Identifiant de connexion invalide.");if(String.IsNullOrWhiteSpace(c.Name)||c.Name.Length>60||c.Name.Any(Char.IsControl))throw new Exception("Donnez un nom de 1 à 60 caractères à la connexion.");if(c.Provider!="Groq")throw new Exception("Fournisseur inconnu.");if((c.Model??"").Length>100||!Regex.IsMatch(c.Model??"",@"^[A-Za-z0-9][A-Za-z0-9._-]*(/[A-Za-z0-9][A-Za-z0-9._-]*)?$"))throw new Exception("Identifiant de modèle invalide.");if(c.Provider=="Groq"&&!new[]{"qwen/qwen3.8-27b","openai/gpt-oss-120b","openai/gpt-oss-20b"}.Contains(c.Model))throw new Exception("Choisissez un modèle Groq pris en charge : Qwen 3.8 27B, GPT-OSS 120B ou GPT-OSS 20B.");if(String.IsNullOrWhiteSpace(c.QuotaGroup)||c.QuotaGroup.Length>80||c.QuotaGroup.Any(Char.IsControl))throw new Exception("Indiquez le compte Groq qui partage ce quota (80 caractères maximum).");if(String.IsNullOrEmpty(c.Key)||c.Key.Length<15||c.Key.Length>300||c.Key.Any(Char.IsWhiteSpace))throw new Exception("La clé API semble incomplète.");if(!keys.Add(c.Key))throw new Exception("Cette clé est déjà présente dans les connexions de secours.");}}
  public void RemoveRetiredConnections(){if(!File.Exists(path))return;var raw=DataFiles.Json().Deserialize<List<ApiConnection>>(Encoding.UTF8.GetString(ProtectedData.Unprotect(File.ReadAllBytes(path),null,DataProtectionScope.CurrentUser)));if(raw==null)throw new Exception("Coffre de connexions invalide.");if(raw.RemoveAll(c=>c!=null&&c.Provider=="Google")>0)Save(raw);}
  public void Save(List<ApiConnection> list){Validate(list);Directory.CreateDirectory(Path.GetDirectoryName(path));var bytes=ProtectedData.Protect(Encoding.UTF8.GetBytes(DataFiles.Json().Serialize(list)),null,DataProtectionScope.CurrentUser);string temp=path+".tmp-"+Guid.NewGuid().ToString("N");try{File.WriteAllBytes(temp,bytes);if(File.Exists(path))File.Replace(temp,path,null);else File.Move(temp,path);}finally{if(File.Exists(temp))File.Delete(temp);}}
  public List<ApiConnection> Routes(LocalStore store,Preferences settings){var routes=new List<ApiConnection>{new ApiConnection{Name="Connexion principale",Provider=settings.Provider,Model=settings.Model,QuotaGroup=settings.QuotaGroup??"principal",Key=store.GetKey()}};if(settings.FallbackEnabled)routes.AddRange(Load().Where(c=>c.Enabled));return routes.GroupBy(c=>c.Provider+"\n"+c.Model+"\n"+c.Key).Select(g=>g.First()).ToList();}
 }
}
