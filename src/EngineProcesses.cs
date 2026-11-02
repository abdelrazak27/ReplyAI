using System;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
namespace ParoleRP {
 static class EngineProcesses {
  [StructLayout(LayoutKind.Sequential,CharSet=CharSet.Unicode)]struct ProcessEntry {public uint Size,Usage,Id;public UIntPtr Heap;public uint Module,Threads,Parent;public int Priority;public uint Flags;[MarshalAs(UnmanagedType.ByValTStr,SizeConst=260)]public string Name;}
  [DllImport("kernel32.dll",SetLastError=true)]static extern IntPtr CreateToolhelp32Snapshot(uint flags,uint id);
  [DllImport("kernel32.dll",CharSet=CharSet.Unicode,EntryPoint="Process32FirstW")]static extern bool First(IntPtr snapshot,ref ProcessEntry entry);
  [DllImport("kernel32.dll",CharSet=CharSet.Unicode,EntryPoint="Process32NextW")]static extern bool Next(IntPtr snapshot,ref ProcessEntry entry);
  [DllImport("kernel32.dll")]static extern bool CloseHandle(IntPtr handle);
  // PyInstaller may have a bootloader child. Only descendants with the exact engine path are stopped.
  public static void StopOwned(Process owner,string executable){
   try{if(owner.HasExited){owner.Dispose();return;}int root=owner.Id;long started=owner.StartTime.ToUniversalTime().Ticks;
    var entries=new List<ProcessEntry>();IntPtr snapshot=CreateToolhelp32Snapshot(2,0);
    if(snapshot!=new IntPtr(-1)){try{var entry=new ProcessEntry{Size=(uint)Marshal.SizeOf(typeof(ProcessEntry))};if(First(snapshot,ref entry))do{entries.Add(entry);}while(Next(snapshot,ref entry));}finally{CloseHandle(snapshot);}}
    var family=new HashSet<uint>{(uint)root};bool changed;do{changed=false;foreach(var entry in entries)if(family.Contains(entry.Parent)&&family.Add(entry.Id))changed=true;}while(changed);
    foreach(uint id in family.Where(id=>id!=root).Reverse()){try{using(var child=Process.GetProcessById((int)id)){if(child.StartTime.ToUniversalTime().Ticks>=started&&String.Equals(child.MainModule.FileName,executable,StringComparison.OrdinalIgnoreCase))child.Kill();}}catch{}}
   }catch{}finally{try{if(!owner.HasExited)owner.Kill();}catch{}try{owner.Dispose();}catch{}}
  }
 }
}
