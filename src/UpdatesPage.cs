using System;
using System.IO;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using ReplyAI.Updates;
namespace ParoleRP {
 public sealed partial class Desktop {
  PublishedUpdate availableUpdate;bool checkingUpdate;
  void Updates(){Title("Mises à jour","Reply AI");var state=T("",13,"#989892");var check=Button("Vérifier",()=>{},true);var install=Button("Mettre à jour",()=>{},true);install.Visibility=Visibility.Collapsed;
   var notes=T("",13,"#B0AEA8");var details=new Expander{Header="Voir les nouveautés",Content=notes,Visibility=Visibility.Collapsed,Margin=new Thickness(0,18,0,0)};
   Action show=()=>{if(availableUpdate==null)return;bool newer=UpdatePackage.ParseVersion(availableUpdate.Version)>UpdatePackage.ParseVersion(UpdateClient.CurrentVersion);state.Text=newer?"Version "+availableUpdate.Version+" disponible.":"Vous êtes à jour.";install.Visibility=newer?Visibility.Visible:Visibility.Collapsed;notes.Text=availableUpdate.Notes;details.Visibility=String.IsNullOrWhiteSpace(notes.Text)?Visibility.Collapsed:Visibility.Visible;};show();
   check.Click+=async(s,e)=>{if(checkingUpdate)return;checkingUpdate=true;check.IsEnabled=false;state.Text="Vérification…";install.Visibility=Visibility.Collapsed;try{availableUpdate=await new UpdateClient().Check();show();}catch(Exception ex){state.Text=ex is System.Threading.Tasks.TaskCanceledException?"La vérification a expiré. Réessayez.":ex.Message;}finally{checkingUpdate=false;check.IsEnabled=true;}};
   install.Click+=async(s,e)=>{if(busy||capturing){state.Text="Attendez la fin de la demande en cours.";return;}if(!Leave())return;busy=true;check.IsEnabled=false;install.IsEnabled=false;picker.IsEnabled=false;w.IsEnabled=false;try{state.Text="Téléchargement…";var update=availableUpdate;string root=await new UpdateClient().Download(update,Path.Combine(store.Root,"updates"),new Progress<int>(p=>state.Text="Téléchargement : "+p+" %"));string helper=Path.Combine(root,"ReplyAI.Updater.exe");using(var resource=typeof(Entry).Assembly.GetManifestResourceStream("ParoleRP.Updater.exe"))using(var output=File.Create(helper))resource.CopyTo(output);var process=Process.GetCurrentProcess();var job=new UpdateJob{InstallRoot=AppDomain.CurrentDomain.BaseDirectory,StageRoot=Path.Combine(root,"payload"),ProcessId=process.Id,ProcessStartTicks=process.StartTime.ToUniversalTime().Ticks,Version=update.Version};string path=Path.Combine(root,"job.json");File.WriteAllText(path,UpdatePackage.Json().Serialize(job));Process.Start(new ProcessStartInfo(helper,"\""+path+"\""){UseShellExecute=false,CreateNoWindow=true,WorkingDirectory=root});w.Close();}catch(Exception ex){state.Text="Installation impossible : "+ex.Message;}finally{busy=false;check.IsEnabled=true;install.IsEnabled=true;w.IsEnabled=true;Header();}};
   host.Children.Add(Card(S(Row(T("Version "+UpdateClient.CurrentVersion,22),Space(12)),check,Space(18),state,install,details,Space(18),Button("Ouvrir les versions sur GitHub ↗",()=>Process.Start(new ProcessStartInfo(UpdateClient.ReleasesUrl){UseShellExecute=true})))));
  }
 }
}
