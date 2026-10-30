using System;
using System.Runtime.InteropServices;
using System.Diagnostics;
using ParoleRP;
class ClipboardRetryTests {
 static int checks;
 static void Check(bool value,string label){if(!value)throw new Exception("FAIL: "+label);checks++;Console.WriteLine("PASS "+label);}
 static ExternalException Busy(){return new COMException("clipboard locked",unchecked((int)0x800401D0));}
 static void Main(){try{Run();Console.WriteLine(checks+" vérifications presse-papiers réussies.");}catch(Exception e){Console.Error.WriteLine(e);Environment.ExitCode=1;}}
 static void Run(){
  int calls=0;var timer=Stopwatch.StartNew();
  string result=ClipboardRetry.Run(()=>{calls++;return "ready";},()=>true).GetAwaiter().GetResult();
  Check(result=="ready"&&calls==1,"aucune attente imposée lorsque le presse-papiers est disponible");
  calls=0;result=ClipboardRetry.Run(()=>{calls++;if(calls<4)throw Busy();return "recovered";},()=>true,200,2).GetAwaiter().GetResult();
  Check(calls==4&&result=="recovered","verrou temporaire récupéré après plusieurs refus COM");
  calls=0;try{ClipboardRetry.Run<string>(()=>{calls++;throw new COMException("other failure",unchecked((int)0x80004005));},()=>true,50,2).GetAwaiter().GetResult();throw new Exception("expected COM error");}catch(COMException){Check(calls==1,"autre erreur COM propagée sans nouvelle tentative");}
  calls=0;try{ClipboardRetry.Run<string>(()=>{calls++;return "must not write";},()=>false).GetAwaiter().GetResult();throw new Exception("expected cancellation");}catch(OperationCanceledException){Check(calls==0,"aucune écriture si la fenêtre ou le presse-papiers a changé");}
  calls=0;bool valid=true;try{ClipboardRetry.Run<string>(()=>{calls++;valid=false;throw Busy();},()=>valid,50,2).GetAwaiter().GetResult();throw new Exception("expected cancellation");}catch(OperationCanceledException){Check(calls==1,"changement de contexte pendant une attente interrompt les tentatives");}
  calls=0;try{ClipboardRetry.Run<string>(()=>{calls++;throw Busy();},()=>true,0,2).GetAwaiter().GetResult();throw new Exception("expected occupied message");}catch(InvalidOperationException e){Check(calls==1&&e.Message.Contains("presse-papiers")&&!e.Message.Contains("0x800401D0"),"verrou persistant expliqué sans erreur COM brute");}
  calls=0;try{ClipboardRetry.Run<string>(()=>{calls++;throw Busy();},()=>true,20,2).GetAwaiter().GetResult();throw new Exception("expected bounded retries");}catch(InvalidOperationException){Check(calls>1,"attente bornée puis abandon lorsque le verrou persiste");}
  try{ClipboardRetry.Run(()=>true,()=>true,-1,2).GetAwaiter().GetResult();throw new Exception("expected invalid budget");}catch(ArgumentOutOfRangeException){Check(true,"délai négatif rejeté");}
 }
}
