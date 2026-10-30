using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
namespace ParoleRP {
 public sealed partial class Desktop {
  string ShortcutValue(bool rephrase,bool generation){return generation?data.Settings.GenerationHotkey:rephrase?data.Settings.RephraseHotkey:data.Settings.Hotkey;}
  void SetShortcutValue(bool rephrase,bool generation,string value){if(generation)data.Settings.GenerationHotkey=value;else if(rephrase)data.Settings.RephraseHotkey=value;else data.Settings.Hotkey=value;}
  void RegisterGeneration(){Native.UnregisterHotKey(handle,103);generationRegistered=false;generationHotkeyError=0;if(data.Settings.GenerationHotkey=="Désactivé")return;uint mod,key;generationRegistered=HotkeyChord.TryParse(data.Settings.GenerationHotkey,out mod,out key)&&Native.RegisterHotKey(handle,103,mod|0x4000,key);generationHotkeyError=generationRegistered?0:Marshal.GetLastWin32Error();if(!generationRegistered)Note("Raccourci de génération indisponible : modifiez-le dans Connexion & API.");}
  UIElement GenerationEditor(ComboBox format,ComboBox detail,TextBox notes){format.Tag="GenerationFormat";detail.Tag="GenerationDetail";notes.Tag="GenerationNotes";return new Expander{Header="Génération RP",Foreground=B("#E8E5DF"),Margin=new Thickness(0,0,0,18),Content=S(Space(14),Two(Field("Format",format),Field("Détail",detail)),Field("Précisions (facultatif)",notes),T("Utilise le style Actions du personnage. Auto : /me pour une action, /do pour un fait ou l’environnement.",11,"#989892"))};}
  UIElement GenerationShortcutSettings(){var chooser=ShortcutRecorder(false,false,ConnectionFeedback,true);chooser.Width=245;chooser.HorizontalAlignment=HorizontalAlignment.Left;var row=new Grid();row.ColumnDefinitions.Add(new ColumnDefinition{Width=new GridLength(94)});row.ColumnDefinitions.Add(new ColumnDefinition());var label=T("Génération",12);label.VerticalAlignment=VerticalAlignment.Center;label.Cursor=System.Windows.Input.Cursors.Hand;label.MouseLeftButtonDown+=(s,e)=>chooser.RaiseEvent(new RoutedEventArgs(System.Windows.Controls.Button.ClickEvent));row.Children.Add(label);Grid.SetColumn(chooser,1);row.Children.Add(chooser);return row;}
 }
}
