using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
namespace ParoleRP {
 public sealed partial class Desktop {
  Action cancelHotkeyCapture;
  bool SaveShortcut(bool rephrase,string chosen,bool duringSetup,out string error,Func<bool> registration=null,bool generation=false){
   error=null;uint modifiers,key;
   if(!((rephrase||generation)&&chosen=="Désactivé")){
    if(!HotkeyChord.TryParse(chosen,out modifiers,out key)){error="Combinaison de touches invalide.";return false;}
    chosen=HotkeyChord.Format(modifiers,key);
    if((generation||rephrase)&&HotkeyChord.Same(chosen,data.Settings.Hotkey)||!generation&&!rephrase&&HotkeyChord.Same(chosen,data.Settings.RephraseHotkey)||!generation&&HotkeyChord.Same(chosen,data.Settings.GenerationHotkey)||generation&&HotkeyChord.Same(chosen,data.Settings.RephraseHotkey)){error="Ce raccourci est déjà utilisé par l’autre fonction.";return false;}
   }
   string previous=ShortcutValue(rephrase,generation);
   Func<bool> apply=registration??(()=>{bool primary=Register(duringSetup);return Entry.PreviewFolder==null&&(generation?(chosen=="Désactivé"||generationRegistered):rephrase?(chosen=="Désactivé"||rephraseRegistered):primary);});
   SetShortcutValue(rephrase,generation,chosen);
   try{
    if(!apply()){error="Raccourci occupé ou réservé par Windows. L’ancien est conservé.";SetShortcutValue(rephrase,generation,previous);apply();return false;}
    Persist();revision++;Header();return true;
   }catch(Exception){SetShortcutValue(rephrase,generation,previous);try{apply();}catch{}error="Impossible d’enregistrer le raccourci. L’ancien est conservé.";return false;}
  }
  Button ShortcutRecorder(bool rephrase,bool duringSetup,Action<string,bool> feedback,bool generation=false){
   Func<string> value=()=>ShortcutValue(rephrase,generation);
   var caption=T(value(),13);caption.TextWrapping=TextWrapping.NoWrap;caption.TextTrimming=TextTrimming.CharacterEllipsis;
   var button=new Button{Content=caption,Style=(Style)w.FindResource("BaseButton"),HorizontalContentAlignment=HorizontalAlignment.Left,Margin=new Thickness(0),Padding=new Thickness(12,10,12,10),Tag=generation?"GenerationShortcut":rephrase?"RephraseShortcut":"CorrectionShortcut",ToolTip="Cliquez puis appuyez sur votre raccourci. Échap pour annuler."};
   bool armed=false;uint pendingKey=0;string pending=null;EventHandler deactivated=null;
   Action<bool> end=null;Action cancel=null;
   end=save=>{if(!armed)return;armed=false;cancelHotkeyCapture=null;w.Deactivated-=deactivated;button.BorderBrush=B("#444441");
    if(save&&pending!=null){string error;if(SaveShortcut(rephrase,pending,duringSetup,out error,null,generation))feedback("Raccourci enregistré : "+value(),false);else{Register(duringSetup);feedback(error,true);}}
    else{Register(duringSetup);feedback("Modification annulée.",false);}
    caption.Text=value();button.ToolTip="Cliquez puis appuyez sur votre raccourci. Échap pour annuler.";
   };
   cancel=()=>end(false);deactivated=(s,e)=>cancel();
   button.Click+=(s,e)=>{if(cancelHotkeyCapture!=null)cancelHotkeyCapture();if(busy||capturing){feedback("Attendez la fin de la demande en cours.",true);return;}armed=true;pendingKey=0;pending=null;cancelHotkeyCapture=cancel;Native.UnregisterHotKey(handle,101);Native.UnregisterHotKey(handle,102);Native.UnregisterHotKey(handle,103);w.Deactivated+=deactivated;caption.Text="Appuyez sur les touches…";button.BorderBrush=B("#E8E5DF");button.Focus();};
   button.PreviewKeyDown+=(s,e)=>{if(!armed)return;e.Handled=true;var pressed=e.Key==Key.System?e.SystemKey:e.Key;
    if(pressed==Key.Escape&&Keyboard.Modifiers==ModifierKeys.None){cancel();return;}if(e.IsRepeat)return;
    uint vk=(uint)KeyInterop.VirtualKeyFromKey(pressed);uint modifiers=0;var held=Keyboard.Modifiers;if((held&ModifierKeys.Control)!=0)modifiers|=2;if((held&ModifierKeys.Alt)!=0)modifiers|=1;if((held&ModifierKeys.Shift)!=0)modifiers|=4;if((held&ModifierKeys.Windows)!=0)modifiers|=8;
    if(!HotkeyChord.IsKey(vk)){caption.Text="Maintenez vos touches…";return;}
    pendingKey=vk;pending=HotkeyChord.Format(modifiers,vk);caption.Text=pending;
   };
   button.PreviewKeyUp+=(s,e)=>{if(!armed)return;e.Handled=true;var key=e.Key==Key.System?e.SystemKey:e.Key;if(pending!=null&&(uint)KeyInterop.VirtualKeyFromKey(key)==pendingKey)end(true);};
   button.LostKeyboardFocus+=(s,e)=>{if(armed)cancel();};
   if(rephrase||generation){var menu=new ContextMenu();var clear=new MenuItem{Header="Désactiver ce raccourci"};clear.Click+=(s,e)=>{string error;if(SaveShortcut(rephrase,"Désactivé",duringSetup,out error,null,generation)){caption.Text=value();feedback("Raccourci désactivé.",false);}else feedback(error,true);};menu.Items.Add(clear);button.ContextMenu=menu;}
   return button;
  }
 }
}
