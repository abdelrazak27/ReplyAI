using System;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
namespace ParoleRP {
 public sealed partial class Desktop {
  void VerifyConnectionSettings(string folder){
   const string mainKey="FAKE_PREVIEW_MAIN_KEY_a1b2c3",firstKey="FAKE_PREVIEW_SECOND_KEY_d4e5f6",secondKey="FAKE_PREVIEW_THIRD_KEY_7a8b9c";
   SaveConnectionDraft(new ApiConnection{Name="Clé principale",Provider="Groq",Key=mainKey,Model="qwen/qwen3.8-27b",QuotaGroup="principal"},true);
   var first=new ApiConnection{Name="Compte personnel 2",Provider="Groq",Key=firstKey,Model="qwen/qwen3.8-27b",QuotaGroup="compte-2"};SaveConnectionDraft(first,false);
   var second=new ApiConnection{Name="Compte personnel 3",Provider="Groq",Key=secondKey,Model="qwen/qwen3.8-27b",QuotaGroup="compte-3"};SaveConnectionDraft(second,false);
   registered=true;page=4;Header();Render();w.UpdateLayout();
   var texts=Elements(w.Content as DependencyObject).OfType<TextBlock>().Select(t=>t.Text).ToList();
   foreach(string key in new[]{mainKey,firstKey,secondKey}){if(!texts.Contains(ConnectionDisplay.KeySuffix(key))||texts.Any(t=>t.Contains(key)))throw new Exception("Liste des clés absente ou non masquée.");}
   if(Elements(w.Content as DependencyObject).OfType<Expander>().Any(e=>e.IsExpanded))throw new Exception("Diagnostic déplié par défaut.");
   var rowToggle=Elements(w.Content as DependencyObject).OfType<CheckBox>().Single(c=>Convert.ToString(c.Tag)==first.Id);rowToggle.IsChecked=false;
   if(new ConnectionVault(store.Root).Load().Single(c=>c.Id==first.Id).Enabled)throw new Exception("Désactivation d’une clé non sauvegardée.");rowToggle.IsChecked=true;
   var fallback=Elements(w.Content as DependencyObject).OfType<CheckBox>().Single(c=>Convert.ToString(c.Content)=="Secours automatiques si quota atteint");fallback.IsChecked=true;if(!store.Load().Settings.FallbackEnabled)throw new Exception("Secours non sauvegardés.");
   MoveConnection(second.Id,-1);if(new ConnectionVault(store.Root).Load()[0].Id!=second.Id)throw new Exception("Priorité des clés perdue.");MoveConnection(second.Id,1);
   var updated=new ApiConnection{Id=first.Id,Name="Compte secondaire",Provider="Groq",Key=firstKey,Model="openai/gpt-oss-20b",QuotaGroup="compte-2",Enabled=true};SaveConnectionDraft(updated,false);
   if(new ConnectionVault(store.Root).Load().Single(c=>c.Id==first.Id).Model!="openai/gpt-oss-20b"||store.GetKey()!=mainKey)throw new Exception("Modification d’une clé non isolée.");
   var invalid=new ApiConnection{Name="Invalide",Provider="Groq",Key="bad",Model="qwen/qwen3.8-27b",QuotaGroup="compte-4"};bool rejected=false;try{SaveConnectionDraft(invalid,false);}catch(Exception){rejected=true;}if(!rejected||new ConnectionVault(store.Root).Load().Count!=2)throw new Exception("Validation a modifié les clés existantes.");
   bool saved=false;var editor=CreateConnectionEditor(updated,false,()=>saved=true);var editorContent=(FrameworkElement)editor.Content;editorContent.Measure(new Size(520,700));editorContent.Arrange(new Rect(0,0,520,editorContent.DesiredSize.Height));editorContent.UpdateLayout();
   Elements(editorContent).OfType<TextBox>().Single(t=>Convert.ToString(t.Tag)=="Nom de la clé").Text="Compte secondaire";
   if(Elements(editorContent).OfType<Expander>().Any(e=>e.IsExpanded)||Elements(editorContent).OfType<PasswordBox>().Any(k=>k.Password.Length>0))throw new Exception("Éditeur prérempli avec un secret ou trop détaillé.");
   var editorImage=new System.Windows.Media.Imaging.RenderTargetBitmap((int)editorContent.ActualWidth,(int)editorContent.ActualHeight,96,96,System.Windows.Media.PixelFormats.Pbgra32);editorImage.Render(editorContent);var editorPng=new System.Windows.Media.Imaging.PngBitmapEncoder();editorPng.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(editorImage));using(var stream=File.Create(Path.Combine(folder,"key-editor.png")))editorPng.Save(stream);
   Elements(editorContent).OfType<Button>().Single(b=>Convert.ToString(b.Content)=="Enregistrer").RaiseEvent(new RoutedEventArgs(System.Windows.Controls.Button.ClickEvent));
   if(!saved||new ConnectionVault(store.Root).Load().Single(c=>c.Id==first.Id).Key!=firstKey)throw new Exception("Modifier sans nouvelle clé ne conserve pas le secret.");
   Render();ConnectionFeedback("");ChromeScreenshot(folder,"settings-keys");
   double width=w.Width,height=w.Height;w.Width=1080;w.Height=760;w.UpdateLayout();ChromeScreenshot(folder,"settings-keys-small");var scroll=(ScrollViewer)w.FindName("PageScroll");if(scroll.ScrollableHeight>1)throw new Exception("Réglages compacts débordent avec trois clés.");w.Width=width;w.Height=height;
   File.WriteAllText(Path.Combine(folder,"connection-checks.txt"),"OK : liste principale + secours, six derniers caractères seulement, diagnostic replié, activation et priorité sauvegardées, modification isolée, validation sans perte, affichage compact avec trois clés. Clés fictives et aucun appel API.");
  }
 }
}
