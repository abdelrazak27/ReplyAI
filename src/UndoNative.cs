using System;
using System.Linq;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
namespace ParoleRP {
 public static partial class Native {
  public delegate IntPtr HookProc(int code,IntPtr message,IntPtr data);
  [DllImport("user32.dll",SetLastError=true)]public static extern IntPtr SetWindowsHookEx(int type,HookProc callback,IntPtr module,uint thread);
  [DllImport("user32.dll")]public static extern bool UnhookWindowsHookEx(IntPtr hook);
  [DllImport("user32.dll")]public static extern IntPtr CallNextHookEx(IntPtr hook,int code,IntPtr message,IntPtr data);
  [DllImport("kernel32.dll",CharSet=CharSet.Auto)]public static extern IntPtr GetModuleHandle(string name);
  [StructLayout(LayoutKind.Sequential)]public struct KeyboardHook {public uint Key,Scan,Flags,Time;public UIntPtr Extra;}
  [StructLayout(LayoutKind.Sequential)]public struct MouseHook {public int X,Y;public uint Data,Flags,Time;public UIntPtr Extra;}
  public static bool CtrlOnly(){return (GetAsyncKeyState(17)&0x8000)!=0&&!new[]{16,18,91,92}.Any(k=>(GetAsyncKeyState(k)&0x8000)!=0);}
  static INPUT Press(ushort key,bool up){var input=new INPUT{type=1};input.u.ki.vk=key;input.u.ki.flags=(up?2u:0)|(key==37||key==39?1u:0);return input;}
  public static async Task SelectPrevious(int length){if(length<1||length>16000)throw new InvalidOperationException("Texte trop long pour l’annulation.");var down=new[]{Press(160,false)};var up=new[]{Press(160,true)};int size=Marshal.SizeOf(typeof(INPUT));if(SendInput(1,down,size)!=1)throw new InvalidOperationException("Windows a bloqué la sélection.");try{await Task.Delay(25);var keys=new INPUT[length*2];for(int i=0;i<length;i++){keys[i*2]=Press(37,false);keys[1+i*2]=Press(37,true);}if(SendInput((uint)keys.Length,keys,size)!=keys.Length)throw new InvalidOperationException("Windows a bloqué la sélection pour annuler.");await Task.Delay(Math.Min(400,Math.Max(40,length/4)));}finally{SendInput(1,up,size);}}
  public static void Tap(ushort key){var keys=new[]{Press(key,false),Press(key,true)};if(SendInput(2,keys,Marshal.SizeOf(typeof(INPUT)))!=2)throw new InvalidOperationException("Windows a bloqué le déplacement du curseur.");}
 }
}
