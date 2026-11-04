using System;
using System.IO;
using System.Diagnostics;
using System.Threading;
using System.Web.Script.Serialization;
class FakeEngine {
 static string root=AppDomain.CurrentDomain.BaseDirectory;static System.IO.StreamReader input;static System.IO.StreamWriter output;
 static bool Take(string name){string p=Path.Combine(root,name);if(!File.Exists(p))return false;File.Delete(p);return true;}
 static void Main(string[] args){input=new StreamReader(Console.OpenStandardInput(),new System.Text.UTF8Encoding(false));output=new StreamWriter(Console.OpenStandardOutput(),new System.Text.UTF8Encoding(false)){AutoFlush=true};try{Run(args);}catch(Exception e){File.AppendAllText(Path.Combine(root,"crash.txt"),e.ToString());throw;}}
 static void Run(string[] args){File.AppendAllText(Path.Combine(root,"starts.txt"),Process.GetCurrentProcess().Id+Environment.NewLine);if(args.Length>0&&args[0]=="--child"){Thread.Sleep(30000);return;}if(Take("child.flag")){Process.Start(new ProcessStartInfo(Path.Combine(root,"FakeEngine.exe"),"--child"){UseShellExecute=false,CreateNoWindow=true});Thread.Sleep(30000);return;}if(Take("noread.flag")){Thread.Sleep(30000);return;}string line;
  while((line=input.ReadLine())!=null){object d;try{d=new JavaScriptSerializer().DeserializeObject(line);}catch(Exception error){throw new Exception("Fake frame length="+line.Length+" prefix UTF8="+BitConverter.ToString(System.Text.Encoding.UTF8.GetBytes(line.Substring(0,Math.Min(24,line.Length)))),error);}File.AppendAllText(Path.Combine(root,"requests.txt"),"request\n");if(Take("noreply.flag")){Thread.Sleep(30000);return;}if(Take("partial.flag")){output.Write("{\"ok\":true");output.Flush();Thread.Sleep(30000);return;}if(Take("bad.flag")){output.WriteLine("{}");output.Flush();continue;}if(Take("quota.flag")){output.WriteLine("{\"ok\":false,\"code\":429,\"message\":\"Fake quota\",\"operation\":\"test\"}");output.Flush();continue;}
   var map=(System.Collections.Generic.Dictionary<string,object>)d;var response=new{ok=true,operation=map["op"],text="Phrase factice.",inputTokens=4,outputTokens=3,cachedTokens=0,usageEvents=new[]{new{model="qwen/qwen3.8-27b",inputTokens=4,outputTokens=3,cachedTokens=0}},routeEvents=new object[0]};output.WriteLine(new JavaScriptSerializer().Serialize(response));output.Flush();
  }
 }
}
