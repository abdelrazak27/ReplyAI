using System;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
namespace ParoleRP {
 public sealed partial class Desktop {
  void VerifyRephraseComponents(string folder){
   data.Active.Name="Personnage de test";editId=data.ActiveId;page=1;Header();Render();w.UpdateLayout();
   var expander=Elements(w.Content as DependencyObject).OfType<Expander>().Single(e=>Convert.ToString(e.Header)=="Styles d’écriture");expander.IsExpanded=true;w.UpdateLayout();
   var dialogue=Elements(expander).OfType<ComboBox>().Single(c=>c.Items.Contains("Street US discret"));var actions=Elements(expander).OfType<ComboBox>().Single(c=>c.Items.Contains("Cinématographique"));
   dialogue.SelectedItem="Street US discret";actions.SelectedItem="Descriptif";
   var notes=Elements(expander).OfType<TextBox>().ToList();notes[0].Text="Français surtout, anglais rare.";notes[1].Text="Précis, sans ajout.";
   saveDraft();var loaded=store.Load();if(loaded.Active.DialogueStyle!="Street US discret"||loaded.Active.ActionStyle!="Descriptif"||loaded.Active.ActionInstructions!="Précis, sans ajout.")throw new Exception("Styles non sauvegardés ou inversés.");
   ((ScrollViewer)w.FindName("PageScroll")).ScrollToBottom();ChromeScreenshot(folder,"rephrase-styles");
   page=3;Render();w.UpdateLayout();var mode=Elements(w.Content as DependencyObject).OfType<ComboBox>().Single(c=>c.Items.Contains("Reformulation"));mode.SelectedIndex=1;ChromeScreenshot(folder,"rephrase-test");
   if(!lastRephrase||lastOutput!="")throw new Exception("Ancien résultat conservé après changement d’opération.");
   File.WriteAllText(Path.Combine(folder,"rephrase-checks.txt"),"OK : dialogue et actions indépendants, réglages sauvegardés par personnage, précisions conservées, mode reformulation disponible dans l’essai. Aucun appel API ; données fictives.");
  }
 }
}
