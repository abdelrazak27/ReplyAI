using System;
using System.IO;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using ParoleRP;

class TranslationUiTests {
 static int count;
 static void Check(bool value,string name){if(!value)throw new Exception(name);count++;Console.WriteLine("PASS "+name);}
 [STAThread]static int Main(string[] args){
  try{
   var data=LocalData.Defaults();var person=new Character{LanguageMode="FR-EN",Words="Jyce\nbong"};data.Characters.Add(person);data.ActiveId=person.Id;
   var imported=DataFiles.ReadBackup(DataFiles.Export(data,null));
   Check(imported.Characters[0].LanguageMode=="FR-EN","language mode survives profile export/import");
   var old=DataFiles.Json().Deserialize<Character>("{\"Id\":\"old\",\"Name\":\"Old\",\"Gender\":\"Masculin\",\"Context\":\"\",\"Words\":\"\"}");
   DataFiles.ValidateCharacter(old);Check(old.LanguageMode=="NORMAL","older profiles default to NORMAL");
   Check(TranslationGuard.Check("j-jvais voir Jyce *baisse les yeux-..*","I-I'm going to see Jyce *looks down-..*",new[]{"Jyce"})==null,"translation accepts English stuttering and preserved RP delimiters");
   Check(TranslationGuard.Check("/do Une odeur de fumée.","/me smells smoke.",new string[0])!=null,"translation cannot change RP command");
   Check(TranslationGuard.Check("Jyce arrive.","John arrives.",new[]{"Jyce"})!=null,"translation preserves protected proper names");
   var usage=UsageTotals.From(new ApiReport{ok=true,operation="chat",usageEvents=new System.Collections.Generic.List<TokenUsage>{new TokenUsage{inputTokens=12,outputTokens=8}}});
   Check(usage.ChatTranslations==1&&usage.Requests==1&&usage.UncachedTotal==20,"chat batch measured separately in usage totals");
   var preferences=new ChatOverlaySettings{ShowOriginal=true};
   usingWindow(preferences,args[0]);
   Console.WriteLine(count+" translation/UI checks passed. No game input or provider call.");return 0;
  }catch(Exception error){Console.Error.WriteLine(error);return 1;}
 }
 static void usingWindow(ChatOverlaySettings settings,string folder){
  var window=new ChatOverlayWindow(settings,()=>{});
  try{
   new WindowInteropHelper(window).EnsureHandle();
   Check(window.CaptureExcluded,"Windows confirms WDA_EXCLUDEFROMCAPTURE on owned overlay window");
   window.SetEditing(true);Check(window.Editing,"overlay editing can be enabled");
   window.SetEditing(false);Check(!window.Editing,"overlay editing can be locked again");
   window.Add(new System.Collections.Generic.List<ChatLine>{new ChatLine{id="a",text="Marcus Hill: What's up, bro?",color="rgb(235, 231, 220)"},new ChatLine{id="b",text="*glances toward the garage*",color="rgb(190, 150, 215)"}});
   window.Translate(new System.Collections.Generic.List<ChatLine>{new ChatLine{id="a",text="Marcus Hill : Ça va, frérot ?"},new ChatLine{id="b",text="*jette un regard vers le garage*"}});
   settings.ShowOriginal=false;window.ApplyAppearance();
   var grid=(System.Windows.Controls.Grid)((System.Windows.Controls.Border)window.Content).Child;
   var list=(System.Windows.Controls.StackPanel)((System.Windows.Controls.ScrollViewer)grid.Children[1]).Content;
   var row=(System.Windows.Controls.StackPanel)list.Children[0];
   Check(row.Children.Count==1,"originals hidden until requested");
   ((System.Windows.Controls.MenuItem)row.ContextMenu.Items[0]).RaiseEvent(new RoutedEventArgs(System.Windows.Controls.MenuItem.ClickEvent));
   row=(System.Windows.Controls.StackPanel)list.Children[0];
   Check(row.Children.Count==2,"per-line original can be displayed from context menu");
   bool requested=false;window.RetranslateRequested+=line=>requested=line.id=="a";
   ((System.Windows.Controls.MenuItem)row.ContextMenu.Items[1]).RaiseEvent(new RoutedEventArgs(System.Windows.Controls.MenuItem.ClickEvent));
   Check(requested,"Groq retranslation requires explicit per-line menu action");
   settings.ShowOriginal=true;window.ApplyAppearance();
   window.SetStatus("Chat FR actif · aperçu de test");
   var visual=(FrameworkElement)window.Content;visual.Measure(new Size(430,380));visual.Arrange(new Rect(0,0,430,380));visual.UpdateLayout();
   var bitmap=new RenderTargetBitmap(430,380,96,96,PixelFormats.Pbgra32);bitmap.Render(visual);
   Directory.CreateDirectory(folder);var encoder=new PngBitmapEncoder();encoder.Frames.Add(BitmapFrame.Create(bitmap));using(var file=File.Create(Path.Combine(folder,"overlay-preview.png")))encoder.Save(file);
  }finally{window.Close();}
 }
}
