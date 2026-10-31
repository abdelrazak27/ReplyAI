using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Automation;
using System.Runtime.InteropServices;
using System.Windows.Threading;
namespace ParoleRP {
 public sealed partial class Desktop {
  UndoTicket undoTicket;IntPtr keyboardHook,mouseHook;Native.HookProc keyboardCallback,mouseCallback;DispatcherTimer undoTimer;bool undoInProgress,undoOwnClipboard;int undoInputVersion;
  static string FocusStamp(){try{var f=AutomationElement.FocusedElement;return f==null?null:String.Join(".",f.GetRuntimeId());}catch{return null;}}
  async Task<string> ReadFocus(){var probe=Task.Run(()=>FocusStamp());return await Task.WhenAny(probe,Task.Delay(120))==probe?await probe:null;}
  void StopUndoHooks(){if(keyboardHook!=IntPtr.Zero)Native.UnhookWindowsHookEx(keyboardHook);if(mouseHook!=IntPtr.Zero)Native.UnhookWindowsHookEx(mouseHook);keyboardHook=mouseHook=IntPtr.Zero;if(undoTimer!=null)undoTimer.Stop();}
  void ClearUndo(){undoTicket=null;undoInputVersion++;Native.UnregisterHotKey(handle,104);if(!undoInProgress)StopUndoHooks();}
  void ArmUndo(IntPtr target,string original,string replacement,string focus){ClearUndo();if(Entry.PreviewFolder!=null||replacement.Length>16000||new[]{data.Settings.Hotkey,data.Settings.RephraseHotkey,data.Settings.GenerationHotkey}.Any(s=>HotkeyChord.Same(s,"Ctrl + Z")))return;
   keyboardCallback=(code,message,pointer)=>{if(code>=0&&(message.ToInt32()==0x100||message.ToInt32()==0x104)){var key=(Native.KeyboardHook)Marshal.PtrToStructure(pointer,typeof(Native.KeyboardHook));if(UndoTicket.ChangesInput(key.Key,Native.CtrlOnly(),(key.Flags&0x10)!=0)){ClearUndo();}}return Native.CallNextHookEx(IntPtr.Zero,code,message,pointer);};
   mouseCallback=(code,message,pointer)=>{if(code>=0&&new[]{0x201,0x204,0x207,0x20A,0x20B,0x20E}.Contains(message.ToInt32())){var mouse=(Native.MouseHook)Marshal.PtrToStructure(pointer,typeof(Native.MouseHook));if((mouse.Flags&1)==0){ClearUndo();}}return Native.CallNextHookEx(IntPtr.Zero,code,message,pointer);};
   keyboardHook=Native.SetWindowsHookEx(13,keyboardCallback,Native.GetModuleHandle(null),0);mouseHook=Native.SetWindowsHookEx(14,mouseCallback,Native.GetModuleHandle(null),0);if(keyboardHook==IntPtr.Zero||mouseHook==IntPtr.Zero||!Native.RegisterHotKey(handle,104,2|0x4000,90)){ClearUndo();return;}
   undoTicket=new UndoTicket{Window=target,Original=original,Replacement=replacement,Focus=focus,Until=DateTime.UtcNow.AddSeconds(30)};if(undoTimer==null){undoTimer=new DispatcherTimer{Interval=TimeSpan.FromMilliseconds(150)};undoTimer.Tick+=(s,e)=>{if(undoTicket!=null&&(DateTime.UtcNow>undoTicket.Until||Native.GetForegroundWindow()!=undoTicket.Window))ClearUndo();};}undoTimer.Start();
  }
  async void UndoReplacement(){try{await UndoReplacementCore();}catch(Exception ex){Balloon("Annulation interrompue : "+ex.Message);}if(undoOwnClipboard){await RestoreClipboardAsync();capturing=false;undoOwnClipboard=false;}undoInProgress=false;ClearUndo();RefreshGlobalState();}
  async Task UndoReplacementCore(){var ticket=undoTicket;IntPtr foreground=Native.GetForegroundWindow();if(ticket==null){Native.UnregisterHotKey(handle,104);return;}undoTicket=null;undoInProgress=true;Native.UnregisterHotKey(handle,104);int inputVersion=undoInputVersion;try{
    for(int i=0;i<30&&(busy||capturing);i++)await Task.Delay(20);if(busy||capturing){return;}
    if(!await Released()){return;}
    string focus=await ReadFocus();if(!ticket.Matches(Native.GetForegroundWindow(),focus,DateTime.UtcNow)){if(Native.GetForegroundWindow()==foreground&&inputVersion==undoInputVersion)Native.Key(90);return;}
    capturing=true;undoOwnClipboard=true;Func<bool> valid=()=>inputVersion==undoInputVersion&&Native.GetForegroundWindow()==ticket.Window&&DateTime.UtcNow<=ticket.Until;
    uint before=Native.GetClipboardSequenceNumber();clipBackup=await ClipboardRetry.Run(()=>Clipboard.GetDataObject(),()=>valid()&&Native.GetClipboardSequenceNumber()==before);clipOwned=before;clipCaptured=true;
    if(!valid())return;await Native.SelectPrevious(ticket.Replacement.Length);await Task.Delay(80);if(!valid())return;string selected=await CopySelection(valid);if(!valid())return;
    if(selected!=ticket.Replacement){Native.Tap(39);Native.Key(90);return;}
    if(!await Released()||!valid())return;uint copied=clipOwned;await ClipboardRetry.Run(()=>{Clipboard.SetText(ticket.Original);clipOwned=Native.GetClipboardSequenceNumber();return true;},()=>valid()&&Native.GetClipboardSequenceNumber()==copied);
    if(!valid()||Native.GetClipboardSequenceNumber()!=clipOwned)return;Native.Key(86);await Task.Delay(350);ShortcutTrace("Dernier remplacement annulé. Le message n’a pas été envoyé.");
   }catch(Exception ex){Balloon("Annulation interrompue : "+ex.Message);}}
 }
}
