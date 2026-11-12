using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;

namespace ParoleRP {
 public sealed class ChatOverlayWindow:Window {
  const int ExtendedStyle=-20;
  const int Transparent=0x20,ToolWindow=0x80,NoActivate=0x08000000;
  [DllImport("user32.dll",SetLastError=true)]static extern bool SetWindowDisplayAffinity(IntPtr window,uint affinity);
  [DllImport("user32.dll",SetLastError=true)]static extern bool GetWindowDisplayAffinity(IntPtr window,out uint affinity);
  [DllImport("user32.dll",EntryPoint="GetWindowLongPtrW")]static extern IntPtr GetLong64(IntPtr window,int index);
  [DllImport("user32.dll",EntryPoint="SetWindowLongPtrW")]static extern IntPtr SetLong64(IntPtr window,int index,IntPtr value);
  [DllImport("user32.dll",EntryPoint="GetWindowLongW")]static extern int GetLong32(IntPtr window,int index);
  [DllImport("user32.dll",EntryPoint="SetWindowLongW")]static extern int SetLong32(IntPtr window,int index,int value);
  [DllImport("user32.dll")]static extern bool SetWindowPos(IntPtr window,IntPtr after,int x,int y,int width,int height,uint flags);
  readonly ChatOverlaySettings settings;
  readonly Action save;
  readonly Border frame;
  readonly StackPanel messages;
  readonly ScrollViewer scroll;
  readonly TextBlock status;
  readonly TextBlock title;
  readonly Thumb move,resize;
  readonly DispatcherTimer saveTimer;
  readonly Queue<ChatLine> recent=new Queue<ChatLine>();
  readonly Dictionary<string,string> translations=new Dictionary<string,string>();
  readonly HashSet<string> originals=new HashSet<string>();
  public event Action<ChatLine> RetranslateRequested;
  IntPtr handle;
  bool edit,closing;
  public bool CaptureExcluded {get;private set;}
  public bool Editing {get{return edit;}}

  public ChatOverlayWindow(ChatOverlaySettings preferences,Action persist){
   settings=preferences;save=persist;settings.Normalize();
   Title="Reply AI · Chat FR";
   WindowStyle=WindowStyle.None;AllowsTransparency=true;Background=Brushes.Transparent;
   ShowInTaskbar=false;ShowActivated=false;Topmost=true;ResizeMode=ResizeMode.NoResize;
   Width=settings.Width;Height=settings.Height;MinWidth=300;MinHeight=180;
   var area=SystemParameters.WorkArea;
   Left=settings.Left<0?area.Right-Width-24:settings.Left;
   Top=settings.Top<0?area.Top+140:settings.Top;
   if(Left+Width<SystemParameters.VirtualScreenLeft+40||Left>SystemParameters.VirtualScreenLeft+SystemParameters.VirtualScreenWidth-40)Left=area.Right-Width-24;
   if(Top+Height<SystemParameters.VirtualScreenTop+40||Top>SystemParameters.VirtualScreenTop+SystemParameters.VirtualScreenHeight-40)Top=area.Top+140;

   var layout=new Grid();
   layout.RowDefinitions.Add(new RowDefinition{Height=GridLength.Auto});
   layout.RowDefinitions.Add(new RowDefinition());
   layout.RowDefinitions.Add(new RowDefinition{Height=GridLength.Auto});
   var heading=new Grid{Height=31,Margin=new Thickness(13,0,13,0)};
   title=Text("CHAT · FR",11,"#E2C47F");title.VerticalAlignment=VerticalAlignment.Center;
   heading.Children.Add(title);
   move=new Thumb{Background=Brushes.Transparent,Cursor=System.Windows.Input.Cursors.SizeAll,Template=ThumbTemplate(Brushes.Transparent)};
   move.DragDelta+=(s,e)=>{Left+=e.HorizontalChange;Top+=e.VerticalChange;};
   heading.Children.Add(move);layout.Children.Add(heading);
   messages=new StackPanel{Margin=new Thickness(13,0,13,0)};
   scroll=new ScrollViewer{Content=messages,VerticalScrollBarVisibility=ScrollBarVisibility.Hidden,HorizontalScrollBarVisibility=ScrollBarVisibility.Disabled};
   Grid.SetRow(scroll,1);layout.Children.Add(scroll);
   status=Text("En attente du chat FiveM",10,"#A5A29B");status.Margin=new Thickness(13,7,24,10);
   Grid.SetRow(status,2);layout.Children.Add(status);
   resize=new Thumb{Width=15,Height=15,HorizontalAlignment=HorizontalAlignment.Right,VerticalAlignment=VerticalAlignment.Bottom,Cursor=System.Windows.Input.Cursors.SizeNWSE,Template=ThumbTemplate(Brush("#D69A60"))};
   resize.DragDelta+=(s,e)=>{Width=Math.Max(MinWidth,Math.Min(1000,Width+e.HorizontalChange));Height=Math.Max(MinHeight,Math.Min(1000,Height+e.VerticalChange));};
   Grid.SetRowSpan(resize,3);layout.Children.Add(resize);
   frame=new Border{BorderThickness=new Thickness(1),BorderBrush=Brush("#383836"),Child=layout};Content=frame;
   saveTimer=new DispatcherTimer{Interval=TimeSpan.FromMilliseconds(400)};
   saveTimer.Tick+=(s,e)=>{saveTimer.Stop();settings.Left=Left;settings.Top=Top;settings.Width=Width;settings.Height=Height;save();};
   LocationChanged+=(s,e)=>ScheduleSave();SizeChanged+=(s,e)=>ScheduleSave();
   Closed+=(s,e)=>{closing=true;saveTimer.Stop();};
   SourceInitialized+=(s,e)=>{
    handle=new WindowInteropHelper(this).Handle;
    uint affinity;
    CaptureExcluded=SetWindowDisplayAffinity(handle,0x11)&&GetWindowDisplayAffinity(handle,out affinity)&&affinity==0x11;
    HwndSource.FromHwnd(handle).AddHook((IntPtr hwnd,int message,IntPtr wp,IntPtr lp,ref bool handled)=>{
     if(message==0x21){handled=true;return new IntPtr(3);}return IntPtr.Zero;
    });
    ApplyInteraction();
   };
   ApplyAppearance();SetEditing(false);
  }

  public void ShowProtected(){
   new WindowInteropHelper(this).EnsureHandle();
   if(!CaptureExcluded)throw new InvalidOperationException("Windows n’a pas confirmé l’exclusion de capture. L’overlay reste fermé.");
   Show();
  }
  public void SetEditing(bool value){edit=value;title.Text=value?"CHAT · FR   Déplacer / redimensionner":"CHAT · FR";move.Visibility=resize.Visibility=value?Visibility.Visible:Visibility.Collapsed;frame.BorderBrush=Brush(value?"#D69A60":"#383836");ApplyInteraction();}
  void ApplyInteraction(){
   if(handle==IntPtr.Zero)return;
   long style=IntPtr.Size==8?GetLong64(handle,ExtendedStyle).ToInt64():GetLong32(handle,ExtendedStyle);
   style|=ToolWindow|NoActivate;
   style=edit?style&~Transparent:style|Transparent;
   if(IntPtr.Size==8)SetLong64(handle,ExtendedStyle,new IntPtr(style));else SetLong32(handle,ExtendedStyle,(int)style);
   SetWindowPos(handle,IntPtr.Zero,0,0,0,0,0x37);
  }
  public void ApplyAppearance(){
   settings.Normalize();frame.Background=new SolidColorBrush(Color.FromArgb((byte)(settings.Opacity*255),20,20,20));
   Redraw();
  }
  public void SetStatus(string text){status.Text=text;}
  public void Add(List<ChatLine> lines){
   foreach(var line in lines){recent.Enqueue(line);translations[line.id]="Traduction…";}
   while(recent.Count>40){var removed=recent.Dequeue();translations.Remove(removed.id);originals.Remove(removed.id);}
   Redraw();
  }
  public void Translate(List<ChatLine> lines){foreach(var line in lines)if(translations.ContainsKey(line.id))translations[line.id]=line.text;Redraw();}
  public void Failed(IEnumerable<ChatLine> lines){foreach(var line in lines)if(translations.ContainsKey(line.id))translations[line.id]="Traduction indisponible";Redraw();}
  void Redraw(){
   if(messages==null)return;messages.Children.Clear();
   foreach(var line in recent){
    string translated;translations.TryGetValue(line.id,out translated);
    var row=new StackPanel{Margin=new Thickness(0,6,0,8)};
    if(settings.ShowOriginal||originals.Contains(line.id)||translated=="Traduction indisponible")row.Children.Add(Text(line.text,Math.Max(11,settings.FontSize-3),"#A5A29B"));
    var output=Text(translated??"Traduction…",settings.FontSize,"#E8E5DF");
    if(translated!="Traduction…"&&translated!="Traduction indisponible")output.Foreground=ChatBrush(line.color);
    row.Children.Add(output);
    var menu=new ContextMenu{Background=Brush("#202020"),Foreground=Brush("#E8E5DF"),BorderBrush=Brush("#383836")};
    var original=new MenuItem{Header="Afficher / masquer l’original"};
    var selected=line;
    original.Click+=(s,e)=>{if(!originals.Add(selected.id))originals.Remove(selected.id);Redraw();};
    var retry=new MenuItem{Header="Retraduire avec Groq · utilise le quota"};
    retry.Click+=(s,e)=>{if(RetranslateRequested!=null)RetranslateRequested(selected);};
    menu.Items.Add(original);menu.Items.Add(retry);row.ContextMenu=menu;
    messages.Children.Add(row);
   }
   scroll.ScrollToEnd();
  }
  void ScheduleSave(){if(!IsLoaded||closing)return;saveTimer.Stop();saveTimer.Start();}
  static TextBlock Text(string value,double size,string color){return new TextBlock{Text=value,FontFamily=new FontFamily("Segoe UI"),FontSize=size,Foreground=Brush(color),TextWrapping=TextWrapping.Wrap};}
  static Brush Brush(string value){return new SolidColorBrush((Color)ColorConverter.ConvertFromString(value));}
  static Brush ChatBrush(string value){
   var match=Regex.Match(value??"",@"rgba?\(\s*(\d+),\s*(\d+),\s*(\d+)");
   if(!match.Success)return Brush("#E8E5DF");
   int r=Math.Min(255,Int32.Parse(match.Groups[1].Value)),g=Math.Min(255,Int32.Parse(match.Groups[2].Value)),b=Math.Min(255,Int32.Parse(match.Groups[3].Value));
   if(r+g+b<240)return Brush("#E8E5DF");
   return new SolidColorBrush(Color.FromRgb((byte)r,(byte)g,(byte)b));
  }
  static ControlTemplate ThumbTemplate(Brush fill){var template=new ControlTemplate(typeof(Thumb));var border=new FrameworkElementFactory(typeof(Border));border.SetValue(Border.BackgroundProperty,fill);template.VisualTree=border;return template;}
 }
}
