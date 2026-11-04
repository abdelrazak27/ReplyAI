using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Runtime.InteropServices;
using System.Web.Script.Serialization;
using System.Windows;
namespace ParoleRP {
 public sealed class ClipboardCommand {public string Op,Text;public uint Expected;public long Target,Until;}
 public sealed class ClipboardReply {public bool Ok;public uint Sequence;public string Text,Error;}
 public interface IClipboardBackend {uint Sequence{get;}long Foreground{get;}object Backup();void Restore(object value);string Read();void Set(string text);}
 sealed class WindowsClipboardBackend:IClipboardBackend {
  [DllImport("user32.dll")]static extern uint GetClipboardSequenceNumber();
  [DllImport("user32.dll")]static extern IntPtr GetForegroundWindow();
  public uint Sequence{get{return GetClipboardSequenceNumber();}}public long Foreground{get{return GetForegroundWindow().ToInt64();}}
  public object Backup(){return Clipboard.GetDataObject();}
  public void Restore(object value){if(value==null)Clipboard.Clear();else Clipboard.SetDataObject(value,true);}
  public string Read(){return Clipboard.ContainsText()?Clipboard.GetText():null;}
  public void Set(string text){Clipboard.SetText(text);}
 }
 // Runs in a separate STA process, before app startup. A stuck OLE owner cannot freeze the UI.
 public sealed class ClipboardWorker {
  readonly IClipboardBackend backend;object backup;bool captured;
  public ClipboardWorker(IClipboardBackend value){backend=value;}
  bool Valid(ClipboardCommand command){return DateTime.UtcNow.Ticks<=command.Until&&backend.Sequence==command.Expected&&(command.Target==0||backend.Foreground==command.Target);}
  public ClipboardReply Execute(ClipboardCommand command){
   var timer=System.Diagnostics.Stopwatch.StartNew();while(true){try{
    if(command==null||!Valid(command))throw new OperationCanceledException();
    string text=null;
    switch(command.Op){
     case "backup":backup=backend.Backup();captured=true;if(!Valid(command))throw new OperationCanceledException();break;
     case "read":text=backend.Read();if(!Valid(command))throw new OperationCanceledException();break;
     case "set":if(command.Text==null||command.Text.Length>100000)throw new InvalidDataException();backend.Set(command.Text);break;
     case "restore":if(captured){backend.Restore(backup);captured=false;backup=null;}break;
     default:throw new InvalidDataException();
    }
    return new ClipboardReply{Ok=true,Sequence=backend.Sequence,Text=text};
   }catch(OperationCanceledException){return new ClipboardReply{Error="La fenêtre ou le presse-papiers a changé. Aucun remplacement effectué."};}
   catch(ExternalException error){if(error.ErrorCode==unchecked((int)0x800401D0)&&timer.ElapsedMilliseconds<900&&command!=null&&DateTime.UtcNow.Ticks<command.Until){Thread.Sleep(30);continue;}return new ClipboardReply{Error="Le presse-papiers Windows est occupé. Relâchez les touches puis réessayez."};}
   catch{return new ClipboardReply{Error="Le presse-papiers Windows n’est pas disponible. Réessayez."};}}
  }
  public static void WatchParent(int id,long ticks){var watch=new Thread(()=>{try{using(var parent=System.Diagnostics.Process.GetProcessById(id)){if(parent.StartTime.ToUniversalTime().Ticks==ticks)parent.WaitForExit();}}catch{}Environment.Exit(0);}){IsBackground=true};watch.Start();}
  public static void Run(){Run(new WindowsClipboardBackend());}
  public static void Run(IClipboardBackend backend){
   var encoding=new UTF8Encoding(false);var json=new JavaScriptSerializer{MaxJsonLength=250000};
   using(var input=new StreamReader(Console.OpenStandardInput(),encoding))using(var output=new StreamWriter(Console.OpenStandardOutput(),encoding){AutoFlush=true}){
    var worker=new ClipboardWorker(backend);var dispatcher=System.Windows.Threading.Dispatcher.CurrentDispatcher;
    // OLE clipboard ownership requires this STA to pump messages while waiting for commands.
    var reader=new Thread(()=>{try{string line;while((line=input.ReadLine())!=null){string frame=line;ClipboardReply reply=null;dispatcher.Invoke(new Action(()=>{try{reply=worker.Execute(json.Deserialize<ClipboardCommand>(frame));}catch{reply=new ClipboardReply{Error="Commande de presse-papiers invalide."};}}));output.WriteLine(json.Serialize(reply));}}catch{}finally{dispatcher.BeginInvokeShutdown(System.Windows.Threading.DispatcherPriority.Background);}}){IsBackground=true};
    reader.Start();System.Windows.Threading.Dispatcher.Run();
   }
  }
 }
}
