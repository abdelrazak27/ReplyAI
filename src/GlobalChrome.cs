using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Shapes;
using Forms=System.Windows.Forms;
namespace ParoleRP {
 public sealed partial class Desktop {
  void RefreshGlobalState(){
   var state=(TextBlock)w.FindName("ApiPill");bool hasKey=store.HasKey;bool ready=data.Active!=null&&hasKey&&registered;
   state.Text=data.Active==null?"Personnage manquant":!hasKey?"Clé manquante":!registered?"Raccourci inactif":busy||capturing?"En cours…":"Prêt";
   string statusColor=data.Active==null||!hasKey?"#D4AF78":!registered?"#CD897F":busy||capturing?"#E2C47F":"#8EBA9A";state.Foreground=B(statusColor);((Rectangle)w.FindName("StateLight")).Fill=B(statusColor);
   string[] keys={"HotkeyStatus","RephraseHotkeyStatus","GenerationHotkeyStatus"};string[] buttons={"ShortcutAction","RephraseShortcutAction","GenerationShortcutAction"};string[] labels={"correction","reformulation","génération"};string[] values={data.Settings.Hotkey,data.Settings.RephraseHotkey,data.Settings.GenerationHotkey};
   for(int i=0;i<keys.Length;i++){((TextBlock)w.FindName(keys[i])).Text=values[i];var action=(Button)w.FindName(buttons[i]);action.IsEnabled=true;action.ToolTip="Modifier le raccourci de "+labels[i]+" : "+values[i];}
  }
  void OpenShortcutSettings(string tag){Go(4);if(page!=4)return;w.Dispatcher.BeginInvoke(new Action(()=>{var field=Elements(host).OfType<Button>().FirstOrDefault(b=>Convert.ToString(b.Tag)==tag);if(field!=null){field.BringIntoView();field.Focus();}}));}
  void TrayMouseClick(object sender,Forms.MouseEventArgs e){if(e.Button==Forms.MouseButtons.Left)Show();}
 }
}
