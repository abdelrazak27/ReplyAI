using System;
namespace ParoleRP {
 public sealed class UndoTicket {
  public IntPtr Window;public string Original,Replacement,Focus;public DateTime Until;
  public bool Matches(IntPtr window,string focus,DateTime now){return window==Window&&now<=Until&&(Focus==null||Focus==focus);}
  public static bool ChangesInput(uint key,bool ctrlOnly,bool injected){return !injected&&key!=17&&key!=162&&key!=163&&!(key==90&&ctrlOnly);}
 }
}
