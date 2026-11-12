using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ParoleRP;

class ChatReaderTests {
 static int Main(string[] args){
  try{Run(args[0]).GetAwaiter().GetResult();return 0;}
  catch(Exception error){Console.Error.WriteLine(error);return 1;}
 }
 static async Task Run(string endpoint){
  using(var stop=new CancellationTokenSource()){
   var captured=new List<ChatLine>();
   var reader=new FiveMChatReader(new Uri(endpoint));
   reader.Lines+=lines=>{captured.AddRange(lines);if(captured.Count>=2)stop.Cancel();};
   var task=reader.Run(stop.Token);
   if(await Task.WhenAny(task,Task.Delay(10000))!=task){stop.Cancel();throw new Exception("Chat reader did not receive the synthetic chat.");}
   await task;
   if(captured.Count!=2||captured[0].id!="new-1"||captured[1].id!="new-2"||captured.Select(line=>line.text).Distinct().Count()!=1)throw new Exception("History baseline or repeated-message deduplication failed.");
   Console.WriteLine("PASS synthetic HTTP/CDP chat: contexts, fragmented WebSocket responses, history skipped, identical new messages retained, cancellation.");
  }
 }
}
