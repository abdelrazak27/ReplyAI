using System;
using System.Windows;
using System.Windows.Controls;

namespace ParoleRP {
 public sealed partial class Desktop {
  ChatOverlayWindow chatOverlay;
  ChatTranslationSession chatSession;
  TextBlock chatState;
  string lastChatState="Overlay désactivé";

  void InitializeLanguageControls(){
   var mode=(ComboBox)w.FindName("LanguageModePicker");
   mode.ItemsSource=new[]{"NORMAL","FR-EN"};
   mode.SelectionChanged+=(s,e)=>{
    if(refreshing||data.Active==null)return;
    string selected=Convert.ToString(mode.SelectedItem);
    if(selected==data.Active.LanguageMode)return;
    if(!Leave()){RefreshLanguageControls();return;}
    data.Active.LanguageMode=selected;revision++;Persist();RefreshLanguageControls();
    lastOutput="";lastWarning=null;
    Note(selected=="FR-EN"?"FR‑EN actif · sorties en anglais":"Mode NORMAL actif");
    if(page==3)Render();
   };
   ((Button)w.FindName("ChatOverlayAction")).Click+=(s,e)=>Go(8);
  }
  void RefreshLanguageControls(){
   var mode=(ComboBox)w.FindName("LanguageModePicker");
   bool previous=refreshing;refreshing=true;
   mode.IsEnabled=data.Active!=null;
   mode.SelectedItem=data.Active==null?"NORMAL":data.Active.LanguageMode;
   refreshing=previous;
   ((Button)w.FindName("ChatOverlayAction")).Content=chatSession==null?"Chat FR":"Chat FR · actif";
  }
  void ChatOverlayPage(){
   Title("Chat FR","Les nouvelles lignes du chat FiveM sont traduites en français.");
   var enabled=new CheckBox{Content="Activer l’overlay et la traduction du chat",IsChecked=chatSession!=null};
   var edit=new CheckBox{Content="Déplacer et redimensionner l’overlay",IsChecked=chatOverlay!=null&&chatOverlay.Editing};
   var original=new CheckBox{Content="Afficher aussi les messages originaux",IsChecked=data.Settings.ChatOverlay.ShowOriginal};
   var font=new Slider{Minimum=12,Maximum=28,Value=data.Settings.ChatOverlay.FontSize,Width=240,HorizontalAlignment=HorizontalAlignment.Left,TickFrequency=1,IsSnapToTickEnabled=true};
   var opacity=new Slider{Minimum=0.3,Maximum=1,Value=data.Settings.ChatOverlay.Opacity,Width=240,HorizontalAlignment=HorizontalAlignment.Left};
   chatState=T(lastChatState,13,"#D4AF78");
   enabled.Checked+=(s,e)=>{
    try{StartChatOverlay();}
    catch(Exception error){StopChatOverlay();enabled.IsChecked=false;ChatStatus(error.Message);}
   };
   enabled.Unchecked+=(s,e)=>{StopChatOverlay();edit.IsChecked=false;};
   edit.Checked+=(s,e)=>{if(chatOverlay!=null)chatOverlay.SetEditing(true);};
   edit.Unchecked+=(s,e)=>{if(chatOverlay!=null)chatOverlay.SetEditing(false);};
   Action appearance=()=>{if(chatOverlay!=null)chatOverlay.ApplyAppearance();store.Save(data);};
   original.Checked+=(s,e)=>{data.Settings.ChatOverlay.ShowOriginal=true;appearance();};
   original.Unchecked+=(s,e)=>{data.Settings.ChatOverlay.ShowOriginal=false;appearance();};
   font.ValueChanged+=(s,e)=>{data.Settings.ChatOverlay.FontSize=font.Value;if(chatOverlay!=null)chatOverlay.ApplyAppearance();};
   opacity.ValueChanged+=(s,e)=>{data.Settings.ChatOverlay.Opacity=opacity.Value;if(chatOverlay!=null)chatOverlay.ApplyAppearance();};
   font.LostMouseCapture+=(s,e)=>store.Save(data);opacity.LostMouseCapture+=(s,e)=>store.Save(data);font.LostKeyboardFocus+=(s,e)=>store.Save(data);opacity.LostKeyboardFocus+=(s,e)=>store.Save(data);
   var reset=Button("Replacer à droite",()=>{
    data.Settings.ChatOverlay.Left=-1;data.Settings.ChatOverlay.Top=-1;
    if(chatOverlay!=null){chatOverlay.Left=SystemParameters.WorkArea.Right-chatOverlay.Width-24;chatOverlay.Top=SystemParameters.WorkArea.Top+140;}
    store.Save(data);
   });
   host.Children.Add(Card(S(enabled,Space(7),T("Traduction locale sur CPU · aucun quota API. En mode déplacement : clic droit sur une ligne pour l’original ou une retraduction Groq.",12,"#989892"),Space(17),chatState,Space(12),edit,original,Space(14),Field("Taille du texte",font),Field("Opacité du fond",opacity),reset,Space(12),T("Exclusion des captures Windows activée lors de l’ouverture. Vérifiez le résultat avec votre logiciel de capture.",12,"#989892"))));
  }
  void ChatStatus(string text){lastChatState=text;if(chatState!=null)chatState.Text=text;}
  void StartChatOverlay(){
   if(chatSession!=null)return;
   if(!LocalChatTranslator.Installed)throw new Exception("Le pack de traduction locale est absent. Réinstallez le dossier complet de Reply AI.");
   chatOverlay=new ChatOverlayWindow(data.Settings.ChatOverlay,()=>store.Save(data));
   try{chatOverlay.ShowProtected();}
   catch{chatOverlay.Close();chatOverlay=null;throw;}
   chatSession=new ChatTranslationSession(chatOverlay,()=>new ConnectionVault(store.Root).Routes(store,data.Settings),()=>busy||capturing,ReceiveReport,ChatStatus);
   chatSession.Start();RefreshLanguageControls();
  }
  void StopChatOverlay(){
   if(chatSession!=null)chatSession.Dispose();chatSession=null;
   if(chatOverlay!=null)chatOverlay.Close();chatOverlay=null;
   ChatStatus("Overlay désactivé");RefreshLanguageControls();
  }
 }
}
