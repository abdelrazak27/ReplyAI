using System;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Forms=System.Windows.Forms;
namespace ParoleRP {
 public sealed partial class Desktop {
  void VerifyReplyChrome(string folder){
   if(w.Title!="Reply AI"||w.FindName("PageCrumb")!=null||w.FindName("BrandMark")==null)throw new Exception("Identité ou header obsolète.");
   string[] actions={"ShortcutAction","RephraseShortcutAction","GenerationShortcutAction"};string[] labels={"HotkeyStatus","RephraseHotkeyStatus","GenerationHotkeyStatus"};
   data.Settings.Hotkey="Ctrl + Alt + Maj + Win + Retour arrière";data.Settings.RephraseHotkey="Ctrl + Alt + Maj + Win + Page suivante";data.Settings.GenerationHotkey="Ctrl + Alt + Maj + Win + Pavé Multiplier";
   Header();page=4;Render();w.Width=1080;w.Height=760;Note(new string('M',300));w.UpdateLayout();
   var rectangles=new System.Collections.Generic.List<Rect>();var visual=(Visual)w.Content;
   foreach(string name in actions){var button=(Button)w.FindName(name);var rect=button.TransformToAncestor(visual).TransformBounds(new Rect(button.RenderSize));if(rect.Right>((FrameworkElement)visual).ActualWidth||rectangles.Any(r=>r.Left<rect.Right-0.5&&r.Right>rect.Left+0.5&&r.Top<rect.Bottom-0.5&&r.Bottom>rect.Top+0.5))throw new Exception("Raccourcis du footer superposés.");rectangles.Add(rect);button.RaiseEvent(new RoutedEventArgs(System.Windows.Controls.Button.ClickEvent));if(page!=4)throw new Exception("Lien de raccourci perdu.");}
   if(((TextBlock)w.FindName(labels[0])).Text!=data.Settings.Hotkey||((TextBlock)w.FindName(labels[1])).Text!=data.Settings.RephraseHotkey||((TextBlock)w.FindName(labels[2])).Text!=data.Settings.GenerationHotkey)throw new Exception("Raccourcis du footer incomplets.");
   foreach(var border in Elements(w.Content as DependencyObject).OfType<Border>()){var c=border.CornerRadius;if(c.TopLeft!=0||c.TopRight!=0||c.BottomLeft!=0||c.BottomRight!=0)throw new Exception("Coins arrondis résiduels.");}
   ChromeScreenshot(folder,"reply-small-long-shortcuts");
   if(tray.Text!="Reply AI"||!tray.ContextMenuStrip.Items.Cast<Forms.ToolStripItem>().Any(i=>i.Text=="Ouvrir Reply AI")||!tray.ContextMenuStrip.Items.Cast<Forms.ToolStripItem>().Any(i=>i.Text=="Quitter"))throw new Exception("Menu de notification incorrect.");
   data.Settings.MinimizeToTray=true;w.WindowState=WindowState.Minimized;TrayMouseClick(tray,new Forms.MouseEventArgs(Forms.MouseButtons.Right,1,0,0,0));if(w.WindowState!=WindowState.Minimized)throw new Exception("Clic droit rouvre la fenêtre.");TrayMouseClick(tray,new Forms.MouseEventArgs(Forms.MouseButtons.Left,1,0,0,0));if(w.WindowState!=WindowState.Normal||!w.IsVisible)throw new Exception("Clic gauche ne restaure pas la fenêtre.");
   var menu=tray.ContextMenuStrip.Items.OfType<Forms.ToolStripMenuItem>().Single(i=>i.Text=="Personnage actif");var profile=data.Characters.First();menu.DropDownItems.OfType<Forms.ToolStripMenuItem>().Single(i=>i.Text==profile.Name).PerformClick();if(data.ActiveId!=profile.Id)throw new Exception("Changement de personnage depuis la notification perdu.");
   data.Settings.Hotkey="Alt + Espace";data.Settings.RephraseHotkey="Alt + Maj + Espace";data.Settings.GenerationHotkey="Ctrl + Alt + G";data.Active.Name="Personnage exemple";Header();Persist();page=4;Render();ConnectionFeedback("");Note("");w.Width=1280;w.Height=860;ChromeScreenshot(folder,"reply-ai-overview");
   page=3;lastInput="";lastOutput="";lastWarning=null;Render();ChromeScreenshot(folder,"reply-ai-test");
   page=6;Render();ChromeScreenshot(folder,"reply-ai-updates");
   File.WriteAllText(Path.Combine(folder,"reply-chrome-checks.txt"),"OK : Reply AI, header sans titre redondant, logo vectoriel, trois raccourcis nommés et liens actifs, petits écrans et combinaisons longues sans chevauchement, coins droits, clic gauche restaure directement, clic droit ne restaure pas, menu ouvrir/quitter et personnages conservé. Données fictives, aucun appel API ni interaction avec une application externe.");
  }
 }
}

