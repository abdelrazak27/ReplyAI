using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace ParoleRP {
 public sealed class ChatTranslationSession:IDisposable {
  readonly CancellationTokenSource cancellation=new CancellationTokenSource();
  readonly Queue<ChatLine> pending=new Queue<ChatLine>();
  readonly GroqApi api=new GroqApi();
  readonly LocalChatTranslator local=new LocalChatTranslator();
  bool groqBusy;
  readonly HashSet<string> upgraded=new HashSet<string>();
  readonly ChatOverlayWindow overlay;
  readonly Dispatcher dispatcher;
  readonly Func<List<ApiConnection>> routes;
  readonly Func<bool> foregroundBusy;
  readonly Action<string> status;
  bool closed;
  public ChatTranslationSession(ChatOverlayWindow window,Func<List<ApiConnection>> connections,Func<bool> busy,Action<ApiReport> report,Action<string> update){
   overlay=window;dispatcher=window.Dispatcher;routes=connections;foregroundBusy=busy;status=update;
   api.Reported+=report;
   overlay.RetranslateRequested+=Retranslate;
  }
  public void Start(){
   var reader=new FiveMChatReader();
   reader.Status+=text=>Post(()=>SetStatus(text));
   reader.Lines+=lines=>Post(()=>{
    overlay.Add(lines);
    foreach(var line in lines){
     pending.Enqueue(line);
     if(pending.Count>60){var skipped=pending.Dequeue();overlay.Failed(new[]{skipped});}
    }
   });
   Observe(Task.Run(()=>reader.Run(cancellation.Token)));
   Observe(TranslateLoop());
  }
  async Task TranslateLoop(){
   while(!closed){
    try{await Task.Delay(250,cancellation.Token);}catch(OperationCanceledException){return;}
    if(closed||foregroundBusy()||pending.Count==0)continue;
    var batch=new List<ChatLine>();int length=2;
    while(pending.Count>0&&batch.Count<6){
     var next=pending.Peek();int cost=DataFiles.Json().Serialize(next).Length+1;
     if(length+cost>2800){
      if(batch.Count>0)break;
      pending.Dequeue();overlay.Failed(new[]{next});continue;
     }
     pending.Dequeue();batch.Add(next);length+=cost;
    }
    if(batch.Count==0)continue;
    bool failed=false;
    try{
     SetStatus("Traduction locale…");
     var translated=await local.Translate(batch);
     if(closed)return;
     overlay.Translate(translated.FindAll(line=>!upgraded.Contains(line.id)));
     SetStatus("Chat FR · local"+(pending.Count>0?" · "+pending.Count+" ligne(s) en attente":""));
    }catch(Exception){
     if(closed)return;
     overlay.Failed(batch);SetStatus("Traduction locale indisponible · original conservé");
     failed=true;
    }
    if(failed){try{await Task.Delay(10000,cancellation.Token);}catch(OperationCanceledException){return;}}
   }
  }
  async void Retranslate(ChatLine line){
   if(closed||groqBusy)return;
   groqBusy=true;
   try{
    SetStatus("Retraduction Groq…");
    var result=await api.TranslateChat(routes(),new List<ChatLine>{line});
    if(!closed){if(upgraded.Count>2000)upgraded.Clear();upgraded.Add(line.id);overlay.Translate(result);SetStatus("Ligne retraduite avec Groq");}
   }catch(Exception){if(!closed)SetStatus("Groq indisponible · traduction locale conservée");}
   finally{groqBusy=false;}
  }
  void Post(Action action){if(closed||dispatcher.HasShutdownStarted)return;dispatcher.BeginInvoke(new Action(()=>{if(!closed)action();}));}
  void SetStatus(string text){overlay.SetStatus(text);status(text);}
  static void Observe(Task task){task.ContinueWith(t=>{var ignored=t.Exception;},CancellationToken.None,TaskContinuationOptions.OnlyOnFaulted,TaskScheduler.Default);}
  public void Dispose(){if(closed)return;closed=true;cancellation.Cancel();pending.Clear();overlay.RetranslateRequested-=Retranslate;local.Dispose();api.Dispose();}
 }
}
