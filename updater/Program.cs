using System;
using System.IO;
using System.Diagnostics;
using System.Threading;
using System.Windows.Forms;
namespace ReplyAI.Updates {
 static class UpdaterProgram {
  [STAThread] static int Main(string[] args){UpdateJob job=null;try{
   if(args.Length!=1||new FileInfo(args[0]).Length>65536)throw new InvalidDataException("Instructions de mise à jour invalides.");
   string transaction=Path.GetDirectoryName(Path.GetFullPath(args[0]));job=UpdatePackage.Json().Deserialize<UpdateJob>(File.ReadAllText(args[0]));
   if(job==null||Path.GetFullPath(job.StageRoot)!=Path.Combine(transaction,"payload"))throw new InvalidDataException("Dossier temporaire invalide.");
   UpdatePackage.Verify(job.StageRoot,job.Version);
   try{using(var process=Process.GetProcessById(job.ProcessId)){if(process.StartTime.ToUniversalTime().Ticks!=job.ProcessStartTicks)throw new IOException("Le processus a changé.");if(!process.WaitForExit(120000))throw new IOException("Reply AI ne s’est pas fermé.");}}catch(ArgumentException){}
   using(var mutex=new Mutex(false,"Local\\ParoleRP-Desktop-v1")){bool owned=false;try{try{owned=mutex.WaitOne(15000);}catch(AbandonedMutexException){owned=true;}if(!owned)throw new IOException("Une autre instance de Reply AI est ouverte.");UpdatePackage.Install(job.StageRoot,job.InstallRoot,Path.Combine(transaction,"backup"),job.Version);File.WriteAllText(Path.Combine(transaction,"result.txt"),"OK");}finally{if(owned)mutex.ReleaseMutex();}}
   Start(job);return 0;
  }catch(Exception ex){MessageBox.Show(ex.Message,"Mise à jour Reply AI",MessageBoxButtons.OK,MessageBoxIcon.Error);if(job!=null)try{Start(job);}catch{}return 1;}}
  static void Start(UpdateJob job){Process.Start(new ProcessStartInfo(Path.Combine(job.InstallRoot,"ReplyAI.exe")){UseShellExecute=true,WorkingDirectory=job.InstallRoot});}
 }
}
