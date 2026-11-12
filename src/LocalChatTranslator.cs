using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
namespace ParoleRP {
 public sealed class LocalChatTranslator:IDisposable {
  EngineWorker worker;
  readonly CancellationTokenSource closing=new CancellationTokenSource();
  bool disposed;
  public static bool Installed {get{return File.Exists(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"engine-local","reply-translate.exe"))&&File.Exists(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"engine-local","model","model.bin"));}}
  public LocalChatTranslator():this(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"engine-local","reply-translate.exe")){}
  public LocalChatTranslator(string path){worker=new EngineWorker(path);executable=path;}
  readonly string executable;
  public async Task<List<ChatLine>> Translate(List<ChatLine> lines){
   if(disposed)throw new OperationCanceledException();
   if(worker==null)worker=new EngineWorker(executable);
   var owner=worker;
   using(var budget=CancellationTokenSource.CreateLinkedTokenSource(closing.Token)){
    budget.CancelAfter(10000);
    try{
     var exchange=Task.Run(()=>owner.Exchange(Encoding.UTF8.GetBytes(DataFiles.Json().Serialize(new{lines=lines})+"\n"),budget.Token));
     if(await Task.WhenAny(exchange,Task.Delay(Timeout.Infinite,budget.Token))!=exchange){Observe(exchange);throw new TimeoutException("Traduction locale trop lente.");}
     var raw=await exchange;
     if(String.IsNullOrEmpty(raw)||raw.Length>100000)throw new Exception("Réponse locale invalide.");
     var result=DataFiles.Json().Deserialize<Dictionary<string,object>>(raw);
     if(!result.ContainsKey("ok")||!Convert.ToBoolean(result["ok"]))throw new Exception("Traduction locale indisponible.");
     var translated=DataFiles.Json().Deserialize<List<ChatLine>>(DataFiles.Json().Serialize(result["lines"]));
     if(translated==null||translated.Count!=lines.Count||translated.Where((x,i)=>x==null||x.id!=lines[i].id||String.IsNullOrWhiteSpace(x.text)).Any())throw new Exception("Traduction locale incomplète.");
     return translated;
    }catch{owner.Abort();if(Object.ReferenceEquals(owner,worker))worker=null;throw;}
   }
  }
  static void Observe(Task task){task.ContinueWith(t=>{var ignored=t.Exception;},TaskContinuationOptions.OnlyOnFaulted);}
  public void Dispose(){disposed=true;closing.Cancel();if(worker!=null)worker.Abort();}
 }
}
