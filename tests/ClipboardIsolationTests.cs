using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ParoleRP;
class MemoryClipboard:IClipboardBackend {
 public uint Sequence{get;set;}public long Foreground{get;set;}public string Value="original";public int Writes;public string Hang;public int Busy;
 public object Backup(){if(Hang=="backup")Thread.Sleep(30000);return Value;}
 public void Restore(object value){if(Hang=="restore")Thread.Sleep(30000);Value=(string)value;Sequence++;Writes++;}
 public string Read(){if(Hang=="read")Thread.Sleep(30000);return Value;}
 public void Set(string value){if(Busy-->0)throw new System.Runtime.InteropServices.COMException("Fake busy",unchecked((int)0x800401D0));if(Hang=="set")Thread.Sleep(30000);Value=value;Sequence++;Writes++;}
}
class ClipboardIsolationTests {
 static int count;static string executable;
 static void Check(bool value,string name){if(!value)throw new Exception(name);count++;Console.WriteLine("PASS "+name);}
 static ClipboardCommand Command(string op,uint expected=1,string text=null){return new ClipboardCommand{Op=op,Expected=expected,Text=text,Target=42,Until=DateTime.UtcNow.AddSeconds(5).Ticks};}
 static async Task<Exception> Error(Task value){try{await value;return null;}catch(Exception error){return error;}}
 static async Task Run(){
  var memory=new MemoryClipboard{Sequence=1,Foreground=42};var worker=new ClipboardWorker(memory);
  Check(worker.Execute(Command("backup")).Ok,"clipboard original snapshot retained in worker");
  Check(worker.Execute(Command("set",1,"replacement")).Sequence==2&&memory.Value=="replacement","worker reports sequence after write");
  Check(!worker.Execute(Command("restore",1)).Ok&&memory.Value=="replacement","stale restore never overwrites newer clipboard");
  Check(worker.Execute(Command("restore",2)).Ok&&memory.Value=="original","original clipboard restored with matching ownership");
  memory.Foreground=99;int writes=memory.Writes;Check(!worker.Execute(Command("set",3,"wrong-window")).Ok&&writes==memory.Writes,"worker rejects changed foreground before writing");
  memory.Foreground=42;var expired=Command("set",3,"expired");expired.Until=DateTime.UtcNow.AddSeconds(-1).Ticks;Check(!worker.Execute(expired).Ok&&writes==memory.Writes,"expired command cannot write later");
  memory.Busy=2;Check(worker.Execute(Command("set",3,"after-busy")).Ok&&memory.Value=="after-busy","temporary clipboard contention is retried within budget");
  foreach(string op in new[]{"backup","read","set","restore"}){
   using(var channel=new ClipboardChannel(executable)){
    // Fake backend is selected in this test executable only; Windows clipboard is never accessed.
    await channel.Send("backup",1,null,()=>true,42);
    await channel.Send("set",1,"hang:"+op,()=>true,42);
    var timer=System.Diagnostics.Stopwatch.StartNew();var pending=Error(channel.Send(op,2,"unused",()=>true,42));
    Check(await Task.WhenAny(pending,Task.Delay(3500))==pending,op+" blocking clipboard call is bounded");
    Check(await pending!=null&&timer.ElapsedMilliseconds<3000,op+" timeout releases caller");
   }
   using(var fresh=new ClipboardChannel(executable,2000)){var next=await fresh.Send("read",1,null,()=>true,42);Check(next.Text=="original","fresh clipboard session works after "+op+" timeout");}
  }
  using(var channel=new ClipboardChannel(executable,2000)){await channel.Send("set",1,"hang:read",()=>true,42);var active=Error(channel.Send("read",2,null,()=>true,42));var queued=Error(channel.Send("read",2,null,()=>true,42));await Task.Delay(100);channel.Dispose();var done=Task.WhenAll(active,queued);Check(await Task.WhenAny(done,Task.Delay(2500))==done&&await active!=null&&await queued!=null,"disposal releases active and queued clipboard operations");}
  using(var channel=new ClipboardChannel(executable)){var rejected=await Error(channel.Send("set",1,"no-write",()=>false,42));Check(rejected is OperationCanceledException,"changed selection rejected before worker command");}
  Console.WriteLine(count+" clipboard isolation checks passed. No desktop clipboard or keyboard access.");
 }
 [STAThread]static int Main(string[] args){if(args.Length==1&&args[0]=="--clipboard-worker"){ClipboardWorker.Run(new TestBackend());return 0;}executable=typeof(ClipboardIsolationTests).Assembly.Location;try{Run().GetAwaiter().GetResult();return 0;}catch(Exception error){Console.Error.WriteLine(error);return 1;}}
 class TestBackend:MemoryClipboard,IClipboardBackend {
  public TestBackend(){Sequence=1;Foreground=42;}
  void IClipboardBackend.Set(string text){if(text!=null&&text.StartsWith("hang:")){Hang=text.Substring(5);Sequence++;return;}base.Set(text);}
 }
}
