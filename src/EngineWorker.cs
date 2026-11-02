using System;
using System.IO;
using System.Text;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
namespace ParoleRP {
 public enum EngineStage {Gate,Serialize,Startup,Send,Flush,Read,Decode,Complete}
 public enum EngineOutcome {Success,ProviderError,Timeout,Cancelled,TransportError,ProtocolError}
 public sealed class EngineTrace {public string Operation;public EngineStage Stage;public EngineOutcome Outcome;public long Milliseconds;}
 // Each exchange retains its own channel reference. Retired I/O can never use a replacement worker.
 sealed class EngineWorker {
  readonly string executable;readonly object sync=new object();Process process;int retired;public volatile EngineStage Stage=EngineStage.Startup;
  public EngineWorker(string path){executable=path;}
  Process Open(){
   if(Volatile.Read(ref retired)!=0)throw new OperationCanceledException();
   lock(sync){if(process!=null&&!process.HasExited)return process;}
   if(!File.Exists(executable))throw new FileNotFoundException("Le moteur Python est absent. Extrayez le dossier complet de Reply AI.");
   var info=new ProcessStartInfo(executable){UseShellExecute=false,CreateNoWindow=true,RedirectStandardInput=true,RedirectStandardOutput=true,RedirectStandardError=true,StandardOutputEncoding=new UTF8Encoding(false),StandardErrorEncoding=new UTF8Encoding(false),WorkingDirectory=Path.GetDirectoryName(executable)};
   var started=Process.Start(info);lock(sync){process=started;}
   if(Volatile.Read(ref retired)!=0){EngineProcesses.StopOwned(started,executable);throw new OperationCanceledException();}
   started.ErrorDataReceived+=(s,e)=>{};started.BeginErrorReadLine();return started;
  }
  public async Task<string> Exchange(byte[] bytes,CancellationToken cancellation){
   cancellation.ThrowIfCancellationRequested();Stage=EngineStage.Startup;var owner=Open();
   cancellation.ThrowIfCancellationRequested();Stage=EngineStage.Send;await owner.StandardInput.BaseStream.WriteAsync(bytes,0,bytes.Length,cancellation);
   cancellation.ThrowIfCancellationRequested();Stage=EngineStage.Flush;await owner.StandardInput.BaseStream.FlushAsync(cancellation);
   cancellation.ThrowIfCancellationRequested();Stage=EngineStage.Read;return await owner.StandardOutput.ReadLineAsync();
  }
  public void Abort(){if(Interlocked.Exchange(ref retired,1)!=0)return;Process owner;lock(sync){owner=process;}if(owner!=null)Task.Run(()=>EngineProcesses.StopOwned(owner,executable));}
 }
}
