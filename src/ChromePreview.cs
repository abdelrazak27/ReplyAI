using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
namespace ParoleRP {
 public sealed partial class Desktop {
  void ChromeScreenshot(string folder,string name){w.UpdateLayout();var visual=(FrameworkElement)w.Content;var bitmap=new RenderTargetBitmap((int)visual.ActualWidth,(int)visual.ActualHeight,96,96,PixelFormats.Pbgra32);bitmap.Render(visual);var png=new PngBitmapEncoder();png.Frames.Add(BitmapFrame.Create(bitmap));using(var stream=File.Create(Path.Combine(folder,name+".png")))png.Save(stream);}
  void PressChrome(string name){((Button)w.FindName(name)).RaiseEvent(new RoutedEventArgs(System.Windows.Controls.Button.ClickEvent));w.UpdateLayout();}
  string GlobalStatus(){return ((TextBlock)w.FindName("ApiPill")).Text;}
  void VerifyGlobalChrome(string folder){
   if(w.FindName("NavHome")!=null||page<1)throw new Exception("Ancien accueil encore présent.");
   if(w.FindName("StatisticsAction")!=null)throw new Exception("Compteurs encore présents.");
   store.SaveKey("FAKE_PREVIEW_MAIN_KEY_123456");registered=true;page=1;Header();Render();
   if(GlobalStatus()!="Prêt")throw new Exception("État prêt absent du header.");
   ChromeScreenshot(folder,"characters-ready");
   var second=new Character{Name="Second personnage de test",Context="Contexte à conserver."};data.Characters.Add(second);Header();picker.SelectedItem=second;
   if(data.ActiveId!=second.Id||store.Load().ActiveId!=second.Id||data.Active.Context!="Contexte à conserver.")throw new Exception("Sélecteur global non fonctionnel.");
   string[] names={"NavProfiles","NavDictionary","NavTest","NavSettings","NavBackups","NavUpdates","NavUsage"};
   for(int i=0;i<names.Length;i++){PressChrome(names[i]);if(page!=i+1)throw new Exception("Navigation décalée : "+names[i]);}
   if(w.FindName("PauseToggle")!=null||w.FindName("HeaderStatusAction")!=null)throw new Exception("Anciennes actions du header présentes.");
   var indicator=(Border)w.FindName("HeaderStatusIndicator");if(indicator.IsHitTestVisible||indicator.Focusable)throw new Exception("État encore interactif.");
   foreach(System.Windows.Forms.ToolStripItem item in tray.ContextMenuStrip.Items)if(item.Text.IndexOf("pause",StringComparison.OrdinalIgnoreCase)>=0)throw new Exception("Pause encore proposée dans le menu de notification.");
   busy=true;RefreshGlobalState();if(GlobalStatus()!="En cours…")throw new Exception("Correction en cours non indiquée.");busy=false;RefreshGlobalState();
   store.RemoveKey();Header();if(GlobalStatus()!="Clé manquante")throw new Exception("État sans clé incorrect.");
   store.SaveKey("FAKE_PREVIEW_MAIN_KEY_123456");registered=false;Header();if(GlobalStatus()!="Raccourci inactif")throw new Exception("Raccourci inactif non indiqué.");
   data.Settings.Hotkey="Ctrl + Alt + C";Header();if(((TextBlock)w.FindName("HotkeyStatus")).Text!=data.Settings.Hotkey)throw new Exception("Raccourci du footer incorrect.");
   PressChrome("NavProfiles");PressChrome("ShortcutAction");if(page!=4)throw new Exception("Lien du raccourci perdu.");
   registered=true;data.Settings.Hotkey="Alt + Espace";Header();PressChrome("NavProfiles");
   w.Width=1080;w.Height=760;second.Name=new string('M',80);Header();Note(new string('M',300));ChromeScreenshot(folder,"chrome-small-long-text");
   var visual=(Visual)w.Content;var p=picker.TransformToAncestor(visual).TransformBounds(new Rect(picker.RenderSize));
   var status=(Border)w.FindName("HeaderStatusIndicator");var r=status.TransformToAncestor(visual).TransformBounds(new Rect(status.RenderSize));
   if(p.IntersectsWith(r)||r.Right>((FrameworkElement)visual).ActualWidth)throw new Exception("Contrôles du header superposés.");
   File.WriteAllText(Path.Combine(folder,"chrome-checks.txt"),"OK : accueil absent, cinq onglets bien reliés, sélecteur de personnage sauvegardé, état passif et pause retirée du header et du menu, correction en cours, clé manquante, raccourci inactif, raccourci cliquable, petite fenêtre sans superposition du header. Aucun appel API.");
  }
 }
}
