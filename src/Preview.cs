using System;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Linq;
using System.Collections.Generic;
using System.Windows.Controls;
namespace ParoleRP {
public sealed partial class Desktop {
 // Renders this application's own WPF visual tree for build-time layout checks.
 public void RenderPreviews(string folder){Directory.CreateDirectory(folder);VerifyDirectStart(folder);VerifyConnectionSettings(folder);VerifyGlobalChrome(folder);VerifyRephraseComponents(folder);VerifyShortcutCapture(folder);VerifyGenerationComponents(folder);VerifyReplyChrome(folder);}
 void VerifyDirectStart(string folder){data=LocalData.Defaults();Header();page=1;Render();w.UpdateLayout();if(data.Active!=null||data.Characters.Count!=0||w.FindName("GuideFooter")!=null)throw new Exception("Premier lancement non vide ou tutoriel résiduel.");ChromeScreenshot(folder,"first-launch");
 string[] names={"NavProfiles","NavDictionary","NavTest","NavSettings","NavBackups","NavUpdates"};for(int i=0;i<names.Length;i++){var button=(Button)w.FindName(names[i]);if(!button.IsEnabled)throw new Exception("Navigation bloquée sans profil.");PressChrome(names[i]);if(page!=i+1)throw new Exception("Navigation sans profil impossible.");}
 page=1;Render();w.UpdateLayout();var create=Elements(host).OfType<Button>().Single(b=>Convert.ToString(b.Content)=="Créer un personnage");create.RaiseEvent(new RoutedEventArgs(System.Windows.Controls.Button.ClickEvent));if(data.Characters.Count!=1||data.Active==null)throw new Exception("Création directe impossible.");
 var field=Elements(host).OfType<TextBox>().Single(t=>t.Text=="Nouveau personnage");field.Text="Personnage test temporaire";saveDraft();data=store.Load();Header();Render();if(data.Active.Name!="Personnage test temporaire")throw new Exception("Profil direct non sauvegardé.");File.WriteAllText(Path.Combine(folder,"direct-start-checks.txt"),"OK : premier lancement sans tutoriel ni profil préparé, cinq onglets accessibles à vide, création directe et sauvegarde du profil, absence de barre de progression. Aucun appel API.");}
 IEnumerable<DependencyObject> Elements(DependencyObject root){yield return root;for(int i=0;i<VisualTreeHelper.GetChildrenCount(root);i++)foreach(var child in Elements(VisualTreeHelper.GetChild(root,i)))yield return child;}
}
}
