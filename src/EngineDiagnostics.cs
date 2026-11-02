using System;
using System.IO;
using System.Text;
using System.Linq;
using System.Threading.Tasks;
namespace ParoleRP {
 static class EngineDiagnostics {
  static readonly object sync=new object();
  public static void Request(string root,EngineTrace entry){string operation=new[]{"correct","rephrase","generate","test","warmup"}.Contains(entry.Operation)?entry.Operation:"unknown";Write(root,"engine op="+operation+" stage="+entry.Stage+" result="+entry.Outcome+" ms="+entry.Milliseconds);}
  public static void Hotkey(string root,string status){if(new[]{"received","blocked-busy","blocked-recording","blocked-app"}.Contains(status))Write(root,"hotkey "+status);}
  static void Write(string root,string message){Task.Run(()=>{try{lock(sync){string path=Path.Combine(root,"diagnostic.log");Directory.CreateDirectory(root);if(File.Exists(path)&&new FileInfo(path).Length>262144){string previous=path+".previous";if(File.Exists(previous))File.Delete(previous);File.Move(path,previous);}File.AppendAllText(path,DateTime.UtcNow.ToString("o")+" "+message+Environment.NewLine,new UTF8Encoding(false));}}catch{}});}
 }
}
