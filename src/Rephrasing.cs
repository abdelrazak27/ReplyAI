using System;
using System.Windows;
using System.Windows.Controls;
namespace ParoleRP {
 public sealed partial class Desktop {
  ComboBox StylePicker(string[] values,string value){return new ComboBox{ItemsSource=values,SelectedItem=value??values[0]};}
  UIElement RephraseStyleEditor(ComboBox dialogue,ComboBox actions,TextBox dialogueNotes,TextBox actionNotes){
   return new Expander{Header="Styles d’écriture",IsExpanded=false,Foreground=B("#E8E5DF"),Margin=new Thickness(0,0,0,18),Content=S(Space(14),Two(S(Field("Dialogue",dialogue),Field("Précisions (facultatif)",dialogueNotes)),S(Field("Actions",actions),Field("Précisions (facultatif)",actionNotes))),T("Dialogue pour reformuler ; Actions pour reformuler et générer.",11,"#989892"))};
  }
  UIElement RephraseShortcutSettings(){
   var chooser=ShortcutRecorder(true,false,ConnectionFeedback);chooser.Width=245;chooser.HorizontalAlignment=HorizontalAlignment.Left;
   var state=T("",12,"#989892");state.VerticalAlignment=VerticalAlignment.Center;
   var row=new Grid();row.ColumnDefinitions.Add(new ColumnDefinition{Width=new GridLength(94)});row.ColumnDefinitions.Add(new ColumnDefinition{Width=new GridLength(263)});row.ColumnDefinitions.Add(new ColumnDefinition());var label=T("Reformulation",12);label.VerticalAlignment=VerticalAlignment.Center;label.Cursor=System.Windows.Input.Cursors.Hand;label.MouseLeftButtonDown+=(s,e)=>chooser.RaiseEvent(new RoutedEventArgs(System.Windows.Controls.Button.ClickEvent));row.Children.Add(label);Grid.SetColumn(chooser,1);row.Children.Add(chooser);Grid.SetColumn(state,2);row.Children.Add(state);return row;
  }
 }
}
