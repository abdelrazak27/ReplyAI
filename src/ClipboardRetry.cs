using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
namespace ParoleRP {
 public static class ClipboardRetry {
  const int CannotOpen=unchecked((int)0x800401D0);
  // Await captures the WPF UI context, so each clipboard call stays on the STA thread.
  public static async Task<T> Run<T>(Func<T> action,Func<bool> stillValid,int maxWaitMs=900,int pauseMs=30){
   if(maxWaitMs<0||pauseMs<1)throw new ArgumentOutOfRangeException();
   var timer=Stopwatch.StartNew();
   while(true){
    if(stillValid!=null&&!stillValid())throw new OperationCanceledException("La fenêtre, la sélection ou le presse-papiers a changé. Aucun remplacement effectué.");
    int wait;
    try{return action();}
    catch(ExternalException error){
     if(error.ErrorCode!=CannotOpen)throw;
     int remaining=maxWaitMs-(int)timer.ElapsedMilliseconds;
     if(remaining<=0)throw new InvalidOperationException("Le presse-papiers Windows reste occupé par une autre application. Votre texte est conservé ; relâchez les touches puis réessayez.");
     wait=Math.Min(pauseMs,remaining);
    }
    await Task.Delay(wait);
   }
  }
 }
}
