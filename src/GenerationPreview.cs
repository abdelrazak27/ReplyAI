using System;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
namespace ParoleRP {
 public sealed partial class Desktop {
  void VerifyGenerationComponents(string folder){
   page=1;editId=data.ActiveId;Render();w.UpdateLayout();var expander=Elements(host).OfType<Expander>().Single(e=>Convert.ToString(e.Header)=="Génération RP");expander.IsExpanded=true;w.UpdateLayout();
   Elements(expander).OfType<ComboBox>().Single(c=>Convert.ToString(c.Tag)=="GenerationFormat").SelectedItem="/do";
   Elements(expander).OfType<ComboBox>().Single(c=>Convert.ToString(c.Tag)=="GenerationDetail").SelectedItem="Détaillé";
   Elements(expander).OfType<TextBox>().Single(c=>Convert.ToString(c.Tag)=="GenerationNotes").Text="Décrire les odeurs.";saveDraft();var loaded=store.Load();if(loaded.Active.GenerationFormat!="/do"||loaded.Active.GenerationDetail!="Détaillé"||loaded.Active.GenerationInstructions!="Décrire les odeurs.")throw new Exception("Réglages de génération perdus.");
   ((ScrollViewer)w.FindName("PageScroll")).ScrollToBottom();ChromeScreenshot(folder,"generation-profile");
   page=3;Render();w.UpdateLayout();var mode=Elements(host).OfType<ComboBox>().Single(c=>c.Items.Contains("Génération"));mode.SelectedItem="Génération";if(!lastGeneration||lastRephrase)throw new Exception("Mode génération mal relié.");ChromeScreenshot(folder,"generation-test");
   page=4;Render();w.UpdateLayout();var recorder=Elements(host).OfType<Button>().Single(b=>Convert.ToString(b.Tag)=="GenerationShortcut");recorder.RaiseEvent(new RoutedEventArgs(System.Windows.Controls.Button.ClickEvent));if(cancelHotkeyCapture==null)throw new Exception("Capture raccourci génération absente.");cancelHotkeyCapture();
   string error;var old=data.Settings.GenerationHotkey;if(SaveShortcut(false,data.Settings.Hotkey,false,out error,()=>true,true)||data.Settings.GenerationHotkey!=old)throw new Exception("Conflit correction-génération accepté.");
   if(!SaveShortcut(false,"Ctrl + Maj + F10",false,out error,()=>true,true)||store.Load().Settings.GenerationHotkey!="Ctrl + Maj + F10")throw new Exception("Raccourci génération non sauvegardé.");
   if(SaveShortcut(true,"Ctrl + Maj + F10",false,out error,()=>true)||SaveShortcut(false,"Ctrl + Maj + F10",false,out error,()=>true))throw new Exception("Conflit avec génération accepté.");
   if(!SaveShortcut(false,"Désactivé",false,out error,()=>true,true)||store.Load().Settings.GenerationHotkey!="Désactivé")throw new Exception("Désactivation génération perdue.");
   data.Settings.GenerationHotkey="Ctrl + Alt + G";Persist();Header();Render();ConnectionFeedback("");ChromeScreenshot(folder,"generation-shortcuts");
   File.WriteAllText(Path.Combine(folder,"generation-checks.txt"),"OK : réglages par personnage sauvegardés, mode génération, capture libre, conflits avec les deux autres fonctions, désactivation, import/export couverts par les tests de données. Données fictives, aucun appel API.");
  }
 }
}
