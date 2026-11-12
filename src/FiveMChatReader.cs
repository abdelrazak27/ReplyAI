using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;

namespace ParoleRP {
 public sealed class ChatSnapshot {
  public bool found {get;set;}
  public List<ChatLine> lines {get;set;}
 }

 public sealed class FiveMChatReader {
  public event Action<List<ChatLine>> Lines;
  public event Action<string> Status;
  readonly Uri discovery;
  public FiveMChatReader():this(new Uri("http://127.0.0.1:13172/json")){}
  public FiveMChatReader(Uri endpoint){if(endpoint==null||endpoint.Scheme!="http"||!endpoint.IsLoopback)throw new ArgumentException("Le chat doit être un service local.");discovery=endpoint;}
  const string Script=@"(() => {
    if (typeof document === 'undefined') return {found:false,lines:[]};
    const selector='.chat__messages > li,.chat-messages > li,.chat-messages > div,#chat-messages > li,#chat-messages > div,.chat__message,.chat-message,#messages > div,#messages > li';
    const nodes=Array.from(new Set(document.querySelectorAll(selector)));
    const found=nodes.length>0 || !!document.querySelector('.chat__messages,.chat-messages,#chat-messages,.chat-window');
    if (!found) return {found:false,lines:[]};
    const state=window.__replyAiChatReader || (window.__replyAiChatReader={nodes:new WeakMap(),next:1,epoch:Math.random().toString(36).slice(2)});
    const lines=nodes.slice(-100).map(node => {
      const text=(node.innerText || node.textContent || '').trim().slice(0,2000);
      let record=state.nodes.get(node);
      if (!record || record.text!==text) {record={text:text,id:state.epoch+':'+state.next++};state.nodes.set(node,record);}
      return {id:record.id,text:text,color:getComputedStyle(node).color};
    }).filter(line=>line.text.length>0);
    return {found:true,lines:lines};
  })()";

  public async Task Run(CancellationToken cancellation){
   using(var handler=new HttpClientHandler{UseProxy=false})
   using(var http=new HttpClient(handler){Timeout=TimeSpan.FromSeconds(3)}){
    while(!cancellation.IsCancellationRequested){
     try{
      Notify("Recherche du chat FiveM…");
      using(var response=await http.GetAsync(discovery,cancellation)){
       response.EnsureSuccessStatusCode();
       if(response.Content.Headers.ContentLength>1000000)throw new InvalidDataException();
       string json=await response.Content.ReadAsStringAsync();
       if(json.Length>1000000)throw new InvalidDataException();
       var targets=DataFiles.Json().Deserialize<List<Dictionary<string,object>>>(json);
       foreach(var target in targets.OrderByDescending(t=>Value(t,"url").IndexOf("chat",StringComparison.OrdinalIgnoreCase)>=0).Take(20)){
        cancellation.ThrowIfCancellationRequested();
        Uri address;
        if(!Uri.TryCreate(Value(target,"webSocketDebuggerUrl"),UriKind.Absolute,out address)||address.Scheme!="ws"||!address.IsLoopback||address.Port!=discovery.Port)continue;
        try{if(await Follow(address,cancellation))break;}
        catch(OperationCanceledException){cancellation.ThrowIfCancellationRequested();}
        catch{ }
       }
      }
      Notify("En attente du chat FiveM");
     }catch(OperationCanceledException){if(cancellation.IsCancellationRequested)return;Notify("En attente du chat FiveM");}
     catch{Notify("En attente de FiveM · accès local au chat indisponible");}
     try{await Task.Delay(2500,cancellation);}catch(OperationCanceledException){return;}
    }
   }
  }

  async Task<bool> Follow(Uri address,CancellationToken cancellation){
   using(var socket=new ClientWebSocket()){
    socket.Options.Proxy=null;
    using(var connect=CancellationTokenSource.CreateLinkedTokenSource(cancellation)){
     connect.CancelAfter(2500);
     await socket.ConnectAsync(address,connect.Token);
    }
    var contexts=new List<int>();int requestId=0;
    await Rpc(socket,++requestId,"Runtime.enable",new{},contexts,cancellation);
    int? selected=null;ChatSnapshot snapshot=await Snapshot(socket,++requestId,null,contexts,cancellation);
    if(!snapshot.found){
     foreach(int context in contexts.ToArray()){
      snapshot=await Snapshot(socket,++requestId,context,contexts,cancellation);
      if(snapshot.found){selected=context;break;}
     }
    }
    if(!snapshot.found)return false;
    var seen=new HashSet<string>(snapshot.lines.Select(line=>line.id));
    var history=new Queue<string>(seen);
    Notify("Chat FiveM connecté · nouvelles lignes uniquement");
    while(!cancellation.IsCancellationRequested){
     await Task.Delay(700,cancellation);
     snapshot=await Snapshot(socket,++requestId,selected,contexts,cancellation);
     if(!snapshot.found)return true;
     var fresh=new List<ChatLine>();
     foreach(var line in snapshot.lines){
      if(String.IsNullOrWhiteSpace(line.id)||String.IsNullOrWhiteSpace(line.text)||!seen.Add(line.id))continue;
      history.Enqueue(line.id);fresh.Add(line);
     }
     while(history.Count>2000)seen.Remove(history.Dequeue());
     if(fresh.Count>0&&Lines!=null)Lines(fresh);
    }
    return true;
   }
  }

  static string Value(Dictionary<string,object> map,string name){object value;return map.TryGetValue(name,out value)?Convert.ToString(value):"";}
  async Task<ChatSnapshot> Snapshot(ClientWebSocket socket,int id,int? context,List<int> contexts,CancellationToken cancellation){
   var arguments=new Dictionary<string,object>{{"expression",Script},{"returnByValue",true},{"silent",true}};
   if(context.HasValue)arguments["contextId"]=context.Value;
   var reply=await Rpc(socket,id,"Runtime.evaluate",arguments,contexts,cancellation);
   object outer,inner,value;
   if(!reply.TryGetValue("result",out outer)||!(outer is Dictionary<string,object>))return Empty();
   var result=(Dictionary<string,object>)outer;
   if(!result.TryGetValue("result",out inner)||!(inner is Dictionary<string,object>))return Empty();
   if(!((Dictionary<string,object>)inner).TryGetValue("value",out value))return Empty();
   var snapshot=DataFiles.Json().Deserialize<ChatSnapshot>(DataFiles.Json().Serialize(value));
   return snapshot==null||snapshot.lines==null?Empty():snapshot;
  }
  static ChatSnapshot Empty(){return new ChatSnapshot{lines=new List<ChatLine>()};}
  async Task<Dictionary<string,object>> Rpc(ClientWebSocket socket,int id,string method,object arguments,List<int> contexts,CancellationToken cancellation){
   using(var deadline=CancellationTokenSource.CreateLinkedTokenSource(cancellation)){
    deadline.CancelAfter(3000);
    using(deadline.Token.Register(()=>socket.Abort())){
     byte[] request=Encoding.UTF8.GetBytes(DataFiles.Json().Serialize(new{id=id,method=method,@params=arguments}));
     await socket.SendAsync(new ArraySegment<byte>(request),WebSocketMessageType.Text,true,deadline.Token);
     while(true){
      using(var stream=new MemoryStream()){
       var buffer=new byte[8192];WebSocketReceiveResult received;
       do{
        received=await socket.ReceiveAsync(new ArraySegment<byte>(buffer),deadline.Token);
        if(received.MessageType!=WebSocketMessageType.Text)throw new IOException("Chat déconnecté.");
        stream.Write(buffer,0,received.Count);
        if(stream.Length>1000000)throw new InvalidDataException();
       }while(!received.EndOfMessage);
       var message=DataFiles.Json().Deserialize<Dictionary<string,object>>(Encoding.UTF8.GetString(stream.ToArray()));
       object eventName;
       if(message.TryGetValue("method",out eventName)&&Convert.ToString(eventName)=="Runtime.executionContextCreated"){
        var details=(Dictionary<string,object>)message["params"];
        var context=(Dictionary<string,object>)details["context"];
        int contextId=Convert.ToInt32(context["id"]);
        if(contexts.Count<100&&!contexts.Contains(contextId))contexts.Add(contextId);
       }
       object responseId;
       if(message.TryGetValue("id",out responseId)&&Convert.ToInt32(responseId)==id)return message;
      }
     }
    }
   }
  }
  void Notify(string text){if(Status!=null)Status(text);}
 }
}
