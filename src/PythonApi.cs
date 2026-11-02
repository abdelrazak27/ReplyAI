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
  public event Action<ApiReport> Reported;
  public string Provider {get;set;}
  readonly string executable;readonly int timeoutMilliseconds;
  readonly SemaphoreSlim gate=new SemaphoreSlim(1,1);readonly object workerSync=new object();readonly CancellationTokenSource closing=new CancellationTokenSource();EngineWorker worker;volatile bool disposed;
  public event Action<EngineTrace> Diagnostic;
  public GroqApi():this(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"engine-v1.9","parole-engine.exe")){}
  public GroqApi(string enginePath):this(enginePath,10000){}
  public GroqApi(string enginePath,int timeoutMs){if(timeoutMs<100||timeoutMs>60000)throw new ArgumentOutOfRangeException("timeoutMs");Provider="Groq";executable=Path.GetFullPath(enginePath);timeoutMilliseconds=timeoutMs;}
  EngineWorker GetWorker(){lock(workerSync){if(disposed)throw new OperationCanceledException();if(worker==null)worker=new EngineWorker(executable);return worker;}}
  void Retire(EngineWorker owner){if(owner==null)return;lock(workerSync){if(Object.ReferenceEquals(worker,owner))worker=null;}owner.Abort();}
  void Trace(string operation,EngineStage stage,EngineOutcome outcome,long elapsed){try{if(Diagnostic!=null)Diagnostic(new EngineTrace{Operation=operation,Stage=stage,Outcome=outcome,Milliseconds=elapsed});}catch{}}
  void Report(ApiReport report){try{if(Reported!=null)Reported(report);}catch{}}
  static string Operation(object payload){var property=payload.GetType().GetProperty("op");string op=property==null?"":Convert.ToString(property.GetValue(payload,null));return new[]{"correct","rephrase","generate","test","warmup"}.Contains(op)?op:"unknown";}
  static void Observe(Task task){task.ContinueWith(t=>{var ignored=t.Exception;},CancellationToken.None,TaskContinuationOptions.OnlyOnFaulted,TaskScheduler.Default);}
  async Task<Dictionary<string,object>> Request(object payload,string key,string model){
   string operation=Operation(payload);var timer=Stopwatch.StartNew();bool acquired=false,reported=false;EngineWorker owner=null;EngineStage stage=EngineStage.Gate;EngineOutcome outcome=EngineOutcome.TransportError;
   using(var budget=CancellationTokenSource.CreateLinkedTokenSource(closing.Token)){
    budget.CancelAfter(timeoutMilliseconds);var token=budget.Token;
    try{
     await gate.WaitAsync(token);acquired=true;token.ThrowIfCancellationRequested();
     stage=EngineStage.Serialize;byte[] bytes=Encoding.UTF8.GetBytes(DataFiles.Json().Serialize(payload)+"\n");token.ThrowIfCancellationRequested();owner=GetWorker();
     var exchange=Task.Run(()=>owner.Exchange(bytes,token));Observe(exchange);
     if(await Task.WhenAny(exchange,Task.Delay(Timeout.Infinite,token))!=exchange){token.ThrowIfCancellationRequested();throw new OperationCanceledException();}
     string line=await exchange;token.ThrowIfCancellationRequested();stage=EngineStage.Decode;
     if(String.IsNullOrEmpty(line)||line.Length>100000)throw new InvalidDataException("Réponse du moteur absente ou trop volumineuse.");
     Dictionary<string,object> result;try{result=DataFiles.Json().Deserialize<Dictionary<string,object>>(line);}catch{throw new InvalidDataException("Réponse du moteur illisible.");}
     if(result==null||!result.ContainsKey("ok")||!(result["ok"] is bool))throw new InvalidDataException("Réponse du moteur incomplète.");
     if(result.ContainsKey("operation")){ApiReport report;try{report=DataFiles.Json().Deserialize<ApiReport>(DataFiles.Json().Serialize(result));}catch{throw new InvalidDataException("Mesures du moteur illisibles.");}reported=true;Report(report);}
     if(!Convert.ToBoolean(result["ok"])){
      outcome=EngineOutcome.ProviderError;int code=result.ContainsKey("code")?Convert.ToInt32(result["code"]):0;string message=result.ContainsKey("message")?Convert.ToString(result["message"]):"Erreur du SDK Python.";
      if(!String.IsNullOrEmpty(key))message=message.Replace(key,"[clé masquée]");
      if(code==429)throw new EngineProviderError(message+"\nAucun texte remplacé.");
      if(code>0&&((result.ContainsKey("providerUsed")?Convert.ToString(result["providerUsed"]):Provider)=="Groq"))throw new EngineProviderError("Erreur Groq (HTTP "+code+").\n"+message+"\nAucun texte remplacé.");
      if(code>0)message=ApiErrors.Describe(code,DataFiles.Json().Serialize(new{error=new{message=message}}),key,model);
      throw new EngineProviderError(message);
     }
     outcome=EngineOutcome.Success;stage=EngineStage.Complete;return result;
    }catch(EngineProviderError){throw;}
    catch(OperationCanceledException){stage=owner==null?stage:owner.Stage;outcome=disposed?EngineOutcome.Cancelled:EngineOutcome.Timeout;Retire(owner);throw new Exception(disposed?"L’application est en cours de fermeture.":"Délai de "+(timeoutMilliseconds/1000.0).ToString("0.#")+" secondes atteint. Aucun texte remplacé. Vous pouvez réessayer ; le moteur sera recréé.");}
    catch(Exception error){stage=owner==null||stage==EngineStage.Decode?stage:owner.Stage;outcome=error is InvalidDataException?EngineOutcome.ProtocolError:EngineOutcome.TransportError;Retire(owner);if(error is FileNotFoundException)throw;throw new Exception("La liaison avec le moteur a été interrompue. Aucun texte remplacé. Réessayez pour recréer le moteur.",error);}
    finally{if(acquired)gate.Release();if(!reported&&outcome!=EngineOutcome.Success)Report(new ApiReport{ok=false,operation=operation});Trace(operation,stage,outcome,timer.ElapsedMilliseconds);}
   }
  }
  sealed class EngineProviderError:Exception{public EngineProviderError(string message):base(message){}}
  static int Number(Dictionary<string,object> value,string key){return value.ContainsKey(key)?Convert.ToInt32(value[key]):0;}
  public async Task Warmup(string key,string model){await Request(new{op="warmup",provider=Provider,key=key,model=model},key,model);}
  public async Task Test(string key,string model){await Request(new{op="test",provider=Provider,key=key,model=model},key,model);}
  public async Task TestConnection(ApiConnection connection){await Request(new{op="test",provider=connection.Provider,key=connection.Key,model=connection.Model,connectionId=connection.Id,quotaGroup=connection.QuotaGroup},connection.Key,connection.Model);}
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
  public void Dispose(){if(disposed)return;disposed=true;closing.Cancel();EngineWorker owner;lock(workerSync){owner=worker;worker=null;}if(owner!=null)owner.Abort();}
 }
}
