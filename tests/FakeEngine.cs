using System;
using System.IO;
using System.Diagnostics;
using System.Threading;
using System.Web.Script.Serialization;
class FakeEngine {
 static string root=AppDomain.CurrentDomain.BaseDirectory;
 static bool Take(string name){string p=Path.Combine(root,name);if(!File.Exists(p))return false;File.Delete(p);return true;}
 static void Main(string[] args){Console.InputEncoding=new System.Text.UTF8Encoding(false);Console.OutputEncoding=new System.Text.UTF8Encoding(false);try{Run(args);}catch(Exception e){File.AppendAllText(Path.Combine(root,"crash.txt"),e.ToString());throw;}}
 static void Run(string[] args){File.AppendAllText(Path.Combine(root,"starts.txt"),Process.GetCurrentProcess().Id+Environment.NewLine);if(args.Length>0&&args[0]=="--child"){Thread.Sleep(30000);return;}if(Take("child.flag")){Process.Start(new ProcessStartInfo(Path.Combine(root,"FakeEngine.exe"),"--child"){UseShellExecute=false,CreateNoWindow=true});Thread.Sleep(30000);return;}if(Take("noread.flag")){Thread.Sleep(30000);return;}string line;
  while((line=Console.ReadLine())!=null){var d=new JavaScriptSerializer().DeserializeObject(line);File.AppendAllText(Path.Combine(root,"requests.txt"),"request\n");if(Take("noreply.flag")){Thread.Sleep(30000);return;}if(Take("partial.flag")){Console.Write("{\"ok\":true");Console.Out.Flush();Thread.Sleep(30000);return;}if(Take("bad.flag")){Console.WriteLine("{}");Console.Out.Flush();continue;}if(Take("quota.flag")){Console.WriteLine("{\"ok\":false,\"code\":429,\"message\":\"Fake quota\",\"operation\":\"test\"}");Console.Out.Flush();continue;}
   var map=(System.Collections.Generic.Dictionary<string,object>)d;var response=new{ok=true,operation=map["op"],text="Phrase factice.",inputTokens=4,outputTokens=3,cachedTokens=0,usageEvents=new[]{new{model="qwen/qwen3.8-27b",inputTokens=4,outputTokens=3,cachedTokens=0}},routeEvents=new object[0]};Console.WriteLine(new JavaScriptSerializer().Serialize(response));Console.Out.Flush();
  }
 }
}
