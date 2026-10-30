using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
namespace ParoleRP {
 public sealed partial class Desktop {
  void EditConnection(ApiConnection original,bool primary){bool saved=false;var dialog=CreateConnectionEditor(original,primary,()=>saved=true);dialog.ShowDialog();if(saved){Render();ConnectionFeedback(primary?"Clé principale enregistrée.":"Clé enregistrée.");}}
  Window CreateConnectionEditor(ApiConnection original,bool primary,Action saved){
   string title=original==null?"Ajouter une clé Groq":"Modifier la clé";
   var dialog=new Window{Title=title,Owner=w,Width=520,MinWidth=480,MaxHeight=Math.Max(500,SystemParameters.WorkArea.Height-60),SizeToContent=SizeToContent.Height,ResizeMode=ResizeMode.NoResize,WindowStartupLocation=WindowStartupLocation.CenterOwner,Background=B("#181818"),Foreground=B("#E8E5DF"),FontFamily=w.FontFamily,FontSize=14};
   dialog.Resources.MergedDictionaries.Add(w.Resources);
   var name=Input(primary?"Clé principale":original==null?"Secours "+(new ConnectionVault(store.Root).Load().Count+1):original.Name);name.Tag="Nom de la clé";
   var key=new PasswordBox{Tag="Clé API"};var group=Input(original==null?"principal":original.QuotaGroup);group.Tag="Compte Groq";
   var model=new ComboBox{SelectedValuePath="Tag"};foreach(string id in new[]{"qwen/qwen3.8-27b","openai/gpt-oss-120b","openai/gpt-oss-20b"})model.Items.Add(new ComboBoxItem{Content=ConnectionDisplay.ModelLabel(id),Tag=id});model.SelectedValue=original==null?data.Settings.Model:original.Model;
   var feedback=T("",12,"#D0AE78");var fields=S(T(title,24),Space(18));
   if(!primary)fields.Children.Add(Field("Nom",name));
   fields.Children.Add(Field(original==null?"Clé API":"Nouvelle clé (facultatif)",key));
   fields.Children.Add(Field("Compte Groq",group,"Même nom pour les clés du même compte."));
   fields.Children.Add(new Expander{Header="Modèle de correction",Foreground=B("#989892"),Margin=new Thickness(0,0,0,14),Content=new Border{Padding=new Thickness(0,10,0,0),Child=S(model,Space(10),T("Reformulation & génération : GPT-OSS 120B",11,"#989892"))}});
   var save=Button("Enregistrer",()=>{try{
    string value=key.Password.Trim();if(value.Length==0&&original!=null)value=original.Key;
    var draft=new ApiConnection{Id=original==null?Guid.NewGuid().ToString("N"):original.Id,Name=primary?"Clé principale":name.Text.Trim(),Provider="Groq",Key=value,Model=Convert.ToString(model.SelectedValue),QuotaGroup=group.Text.Trim(),Enabled=original==null||original.Enabled};
    SaveConnectionDraft(draft,primary);key.Clear();saved();dialog.Close();
   }catch(Exception ex){feedback.Text=ex.Message;}},true);save.IsDefault=true;
   var cancel=Button("Annuler",()=>dialog.Close());cancel.IsCancel=true;
   fields.Children.Add(feedback);fields.Children.Add(Row(save,cancel));
   dialog.Content=new ScrollViewer{Background=B("#181818"),VerticalScrollBarVisibility=ScrollBarVisibility.Auto,Content=new Border{Padding=new Thickness(26),Child=fields}};
   return dialog;
  }
  void SaveConnectionDraft(ApiConnection draft,bool primary){
   ConnectionVault.Validate(new List<ApiConnection>{draft});var vault=new ConnectionVault(store.Root);var list=vault.Load();
   if(primary){
    if(list.Any(c=>c.Key==draft.Key))throw new Exception("Cette clé figure déjà dans les secours.");
    store.SaveKey(draft.Key);data.Settings.Provider="Groq";data.Settings.Model=draft.Model;data.Settings.QuotaGroup=draft.QuotaGroup;Persist();
   }else{
    if(store.HasKey&&store.GetKey()==draft.Key)throw new Exception("Cette clé est déjà la clé principale.");
    int index=list.FindIndex(c=>c.Id==draft.Id);if(index<0)list.Add(draft);else list[index]=draft;vault.Save(list);
   }
   revision++;Header();
  }
 }
}
