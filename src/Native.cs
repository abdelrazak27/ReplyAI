using System;
using System.Linq;
using System.Runtime.InteropServices;
namespace ParoleRP {
public static partial class Native {
 [DllImport("user32.dll",SetLastError=true)]public static extern bool RegisterHotKey(IntPtr hwnd,int id,uint modifiers,uint key);[DllImport("user32.dll")]public static extern bool UnregisterHotKey(IntPtr hwnd,int id);[DllImport("user32.dll")]public static extern IntPtr GetForegroundWindow();[DllImport("user32.dll")]public static extern uint GetClipboardSequenceNumber();[DllImport("user32.dll")]public static extern uint GetWindowThreadProcessId(IntPtr window,out uint process);[DllImport("user32.dll")]static extern short GetAsyncKeyState(int key);[DllImport("user32.dll",SetLastError=true)]static extern uint SendInput(uint n,INPUT[] input,int size);
 [StructLayout(LayoutKind.Sequential)]struct INPUT{public uint type;public UNION u;}[StructLayout(LayoutKind.Explicit)]struct UNION{[FieldOffset(0)]public KEYBD ki;[FieldOffset(0)]public MOUSE mi;}[StructLayout(LayoutKind.Sequential)]struct KEYBD{public ushort vk,scan;public uint flags,time;public UIntPtr extra;}[StructLayout(LayoutKind.Sequential)]struct MOUSE{public int x,y;public uint data,flags,time;public UIntPtr extra;}
 public static bool Modifiers(){return new[]{16,17,18,91,92}.Any(k=>(GetAsyncKeyState(k)&0x8000)!=0);}
 static INPUT[] BuildChord(ushort key){var a=new INPUT[4];for(int i=0;i<a.Length;i++)a[i].type=1;a[0].u.ki.vk=17;a[1].u.ki.vk=key;a[2].u.ki.vk=key;a[2].u.ki.flags=2;a[3].u.ki.vk=17;a[3].u.ki.flags=2;return a;}
 public static void Key(ushort key){var a=BuildChord(key);if(SendInput(4,a,Marshal.SizeOf(typeof(INPUT)))!=4)throw new Exception("Windows a bloqué le raccourci clavier. Vérifiez les droits du jeu et de Reply AI.");}
}
}
