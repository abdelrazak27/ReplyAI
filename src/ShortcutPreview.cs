using System;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
namespace ParoleRP {
 public sealed partial class Desktop {
  void VerifyShortcutCapture(string folder){
   data.Settings.Hotkey="Alt + Espace";data.Settings.RephraseHotkey="Alt + Maj + Espace";page=4;Render();w.UpdateLayout();
   var correction=Elements(w.Content as DependencyObject).OfType<Button>().Single(b=>Convert.ToString(b.Tag)=="CorrectionShortcut");
   var rephrase=Elements(w.Content as DependencyObject).OfType<Button>().Single(b=>Convert.ToString(b.Tag)=="RephraseShortcut");
   if(Elements(host).OfType<ComboBox>().Any())throw new Exception("Raccourcis encore proposés en liste.");
   correction.RaiseEvent(new RoutedEventArgs(System.Windows.Controls.Button.ClickEvent));if(cancelHotkeyCapture==null||!((TextBlock)correction.Content).Text.Contains("Appuyez"))throw new Exception("Capture non armée.");ChromeScreenshot(folder,"shortcut-capture");
   cancelHotkeyCapture();if(data.Settings.Hotkey!="Alt + Espace"||cancelHotkeyCapture!=null)throw new Exception("Annulation modifie le raccourci.");
   rephrase.RaiseEvent(new RoutedEventArgs(System.Windows.Controls.Button.ClickEvent));var esc=new KeyEventArgs(Keyboard.PrimaryDevice,source,Environment.TickCount,Key.Escape){RoutedEvent=Keyboard.PreviewKeyDownEvent};rephrase.RaiseEvent(esc);
   if(cancelHotkeyCapture!=null||!esc.Handled)throw new Exception("Échap n’annule pas la capture.");
   correction.RaiseEvent(new RoutedEventArgs(System.Windows.Controls.Button.ClickEvent));
   var down=new KeyEventArgs(Keyboard.PrimaryDevice,source,Environment.TickCount,Key.F7){RoutedEvent=Keyboard.PreviewKeyDownEvent};correction.RaiseEvent(down);
   if(cancelHotkeyCapture==null||!down.Handled||!((TextBlock)correction.Content).Text.Contains("F7"))throw new Exception("Touche non capturée avant relâchement.");
   var up=new KeyEventArgs(Keyboard.PrimaryDevice,source,Environment.TickCount,Key.F7){RoutedEvent=Keyboard.PreviewKeyUpEvent};correction.RaiseEvent(up);
   if(cancelHotkeyCapture!=null||!up.Handled||data.Settings.Hotkey!="Alt + Espace")throw new Exception("Capture non terminée au relâchement ou rejet Windows non conservateur.");
   string error;int registrations=0;Func<bool> success=()=>{registrations++;registered=true;return true;};
   if(!SaveShortcut(false,"Ctrl + Win + K",false,out error,success)||store.Load().Settings.Hotkey!="Ctrl + Win + K")throw new Exception("Raccourci libre non enregistré.");
   if(SaveShortcut(true,"Win + Ctrl + K",false,out error,success)||registrations!=1)throw new Exception("Doublon accepté ou enregistré.");
   bool first=true;Func<bool> conflict=()=>{registrations++;if(first){first=false;return false;}return true;};
   if(SaveShortcut(false,"Ctrl + Maj + J",false,out error,conflict)||data.Settings.Hotkey!="Ctrl + Win + K"||store.Load().Settings.Hotkey!="Ctrl + Win + K"||registrations!=3)throw new Exception("Conflit ne restaure pas le raccourci précédent.");
   if(!SaveShortcut(true,"Désactivé",false,out error,success)||store.Load().Settings.RephraseHotkey!="Désactivé")throw new Exception("Désactivation perdue.");
   data.Settings.RephraseHotkey="Alt + Maj + Espace";Persist();Header();Render();ChromeScreenshot(folder,"shortcuts-free");
   File.WriteAllText(Path.Combine(folder,"shortcut-checks.txt"),"OK : champs sans liste prédéfinie, clic arme la capture, Échap annule, touche capturée puis soumise au relâchement, raccourci arbitraire sauvegardé, doublon refusé, conflit simulé restaure l’ancien, désactivation conservée. Pas d’appui clavier réel ni appel API.");
  }
 }
}


