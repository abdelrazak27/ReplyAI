using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
namespace ParoleRP {
 public sealed class ClipboardChannel:IDisposable {
  readonly EngineWorker worker;readonly int timeout;readonly CancellationTokenSource closed=new CancellationTokenSource();volatile bool disposed;readonly SemaphoreSlim gate=new SemaphoreSlim(1,1);
  public ClipboardChannel(string executable,int timeoutMs=2000){using(var parent=System.Diagnostics.Process.GetCurrentProcess()){worker=new EngineWorker(executable,"--clipboard-worker "+parent.Id+" "+parent.StartTime.ToUniversalTime().Ticks);}timeout=timeoutMs;}
  public async Task<ClipboardReply> Send(string op,uint expected,string text,Func<bool> valid,long target=0){
   if(disposed)throw new ObjectDisposedException("ClipboardChannel");
   if(valid!=null&&!valid())throw new OperationCanceledException("La fenêtre ou le presse-papiers a changé.");
   using(var budget=CancellationTokenSource.CreateLinkedTokenSource(closed.Token)){
    budget.CancelAfter(timeout);var token=budget.Token;var json=new JavaScriptSerializer{MaxJsonLength=250000};
    bool acquired=false;try{await gate.WaitAsync(token);acquired=true;token.ThrowIfCancellationRequested();
    var command=new ClipboardCommand{Op=op,Expected=expected,Text=text,Target=target,Until=DateTime.UtcNow.AddMilliseconds(timeout).Ticks};
    var exchange=Task.Run(()=>worker.Exchange(Encoding.UTF8.GetBytes(json.Serialize(command)+"\n"),token));
    Observe(exchange);
    try{
     if(await Task.WhenAny(exchange,Task.Delay(Timeout.Infinite,token))!=exchange)token.ThrowIfCancellationRequested();
     string line=await exchange;token.ThrowIfCancellationRequested();
     if(valid!=null&&!valid())throw new OperationCanceledException("La fenêtre ou le presse-papiers a changé.");
     if(String.IsNullOrEmpty(line)||line.Length>250000)throw new InvalidOperationException("Réponse de presse-papiers absente.");
     var reply=json.Deserialize<ClipboardReply>(line);if(reply==null||!reply.Ok)throw new InvalidOperationException(reply==null?"Presse-papiers indisponible.":reply.Error);return reply;
    }catch(OperationCanceledException){Dispose();throw new InvalidOperationException("Le presse-papiers n’a pas répondu à temps ou la sélection a changé. Aucun remplacement effectué. Réessayez.");}
    catch{Dispose();throw;}
    }finally{if(acquired)gate.Release();}
   }
  }
  static void Observe(Task task){task.ContinueWith(t=>{var ignored=t.Exception;},CancellationToken.None,TaskContinuationOptions.OnlyOnFaulted,TaskScheduler.Default);}
  public void Dispose(){if(disposed)return;disposed=true;closed.Cancel();worker.Abort();}
 }
}
