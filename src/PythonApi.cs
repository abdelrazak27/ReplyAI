using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
namespace ParoleRP {
 public sealed class GroqApi : IDisposable {
  public string Provider {get;set;}
  readonly string executable;
  public GroqApi():this(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"engine-v1.9","parole-engine.exe")){}
  public GroqApi(string enginePath){Provider="Groq";executable=Path.GetFullPath(enginePath);}
  readonly SemaphoreSlim gate=new SemaphoreSlim(1,1);Process worker;bool disposed;
  void Start(){if(worker!=null&&!worker.HasExited)return;string path=executable;if(!File.Exists(path))throw new Exception("Le moteur Python est absent. Extrayez le dossier complet de Reply AI avant de lancer l’application.");var info=new ProcessStartInfo(path){UseShellExecute=false,CreateNoWindow=true,RedirectStandardInput=true,RedirectStandardOutput=true,RedirectStandardError=true,StandardOutputEncoding=new UTF8Encoding(false),StandardErrorEncoding=new UTF8Encoding(false),WorkingDirectory=Path.GetDirectoryName(path)};worker=Process.Start(info);worker.ErrorDataReceived+=(s,e)=>{};worker.BeginErrorReadLine();}
  void Stop(){try{if(worker!=null){if(!worker.HasExited)worker.Kill();worker.Dispose();}}catch{}worker=null;}
  async Task<Dictionary<string,object>> Request(object payload,string key,string model){await gate.WaitAsync();try{if(disposed)throw new Exception("L’application est en cours de fermeture.");Start();byte[] bytes=Encoding.UTF8.GetBytes(DataFiles.Json().Serialize(payload)+"\n");await worker.StandardInput.BaseStream.WriteAsync(bytes,0,bytes.Length);await worker.StandardInput.BaseStream.FlushAsync();var response=worker.StandardOutput.ReadLineAsync();if(await Task.WhenAny(response,Task.Delay(12000))!=response){Stop();throw new Exception("Le SDK Python n’a pas répondu dans le délai prévu. Aucun texte remplacé.");}string line=await response;if(String.IsNullOrEmpty(line)||line.Length>100000){Stop();throw new Exception("Le moteur Python s’est arrêté ou a renvoyé une réponse invalide. Aucun texte remplacé.");}Dictionary<string,object> result;try{result=DataFiles.Json().Deserialize<Dictionary<string,object>>(line);}catch{Stop();throw new Exception("Réponse du moteur Python illisible. Aucun texte remplacé.");}if(result==null||!result.ContainsKey("ok"))throw new Exception("Réponse du moteur Python incomplète.");if(!Convert.ToBoolean(result["ok"])){int code=result.ContainsKey("code")?Convert.ToInt32(result["code"]):0;string message=result.ContainsKey("message")?Convert.ToString(result["message"]):"Erreur du SDK Python.";if(code==429)throw new Exception(message+"\nAucun texte remplacé.");if(code>0&&((result.ContainsKey("providerUsed")?Convert.ToString(result["providerUsed"]):Provider)=="Groq"))throw new Exception("Erreur Groq (HTTP "+code+").\n"+message.Replace(key,"[clé masquée]")+"\nAucun texte remplacé.");if(code>0){string body=DataFiles.Json().Serialize(new{error=new{message=message}});throw new Exception(ApiErrors.Describe(code,body,key,model));}throw new Exception(String.IsNullOrEmpty(key)?message:message.Replace(key,"[clé masquée]"));}return result;}finally{gate.Release();}}
  static int Number(Dictionary<string,object> value,string key){return value.ContainsKey(key)?Convert.ToInt32(value[key]):0;}
  public async Task Warmup(string key,string model){await Request(new{op="warmup",provider=Provider,key=key,model=model},key,model);}
  public async Task Test(string key,string model){await Request(new{op="test",provider=Provider,key=key,model=model},key,model);}
  public async Task TestConnection(ApiConnection connection){await Request(new{op="test",provider=connection.Provider,key=connection.Key,model=connection.Model},connection.Key,connection.Model);}
  static string Value(Dictionary<string,object> result,string key){return result.ContainsKey(key)?Convert.ToString(result[key]):null;}
  public Task<Correction> Correct(string key,string model,Character character,string shared,string selection){return CorrectConnections(new List<ApiConnection>{new ApiConnection{Name="Connexion principale",Provider=Provider,Model=model,QuotaGroup="principal",Key=key}},character,shared,selection);}
  public async Task<Correction> CorrectConnections(List<ApiConnection> connections,Character character,string shared,string selection,bool rephrase=false,bool generation=false){
   if(character==null)throw new Exception("Choisissez un personnage.");
   if(connections==null||connections.Count==0||connections.Count>9)throw new Exception("Configurez vos connexions API.");
   if(String.IsNullOrWhiteSpace(selection)||selection.Length>3000)throw new Exception("Sélectionnez entre 1 et 3 000 caractères.");
   connections=OperationRoutes.Prepare(connections,rephrase||generation);var first=connections[0];var totalTimer=Stopwatch.StartNew();
   var result=await Request(new{op=generation?"generate":rephrase?"rephrase":"correct",provider=first.Provider,key=first.Key,model=first.Model,connections=connections,character=character,shared=shared,text=selection},first.Key,first.Model);
   string text=Value(result,"text");if(String.IsNullOrWhiteSpace(text))throw new Exception("Le SDK n’a renvoyé aucun texte. Aucun texte remplacé.");
   text=generation?text.Trim():Regex.Match(selection,@"^\s*").Value+text.Trim()+Regex.Match(selection,@"\s*$").Value;
   return new Correction{Text=text,Milliseconds=(int)totalTimer.ElapsedMilliseconds,InputTokens=Number(result,"inputTokens"),OutputTokens=Number(result,"outputTokens"),CachedTokens=Number(result,"cachedTokens"),ContextUsed=Value(result,"contextUsed"),ConnectionName=Value(result,"connectionName"),ProviderUsed=Value(result,"providerUsed"),ModelUsed=Value(result,"modelUsed"),Attempts=Number(result,"attempts"),Warning=generation?GenerationGuard.Check(text,character.GenerationFormat):RpGuard.Check(selection,text,DataFiles.Words(shared).Concat(DataFiles.Words(character.Words)),rephrase)};
  }
  public void Dispose(){disposed=true;Stop();}
 }
}
