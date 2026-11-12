using System;
using System.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace ParoleRP {
 public sealed class ChatLine {
  public string id {get;set;}
  public string text {get;set;}
  public string color {get;set;}
 }

 public sealed class ChatOverlaySettings {
  public double Left {get;set;}
  public double Top {get;set;}
  public double Width {get;set;}
  public double Height {get;set;}
  public double FontSize {get;set;}
  public double Opacity {get;set;}
  public bool ShowOriginal {get;set;}
  public ChatOverlaySettings(){Left=-1;Top=-1;Width=430;Height=380;FontSize=16;Opacity=0.8;}
  public void Normalize(){
   Left=Finite(Left,-1);Top=Finite(Top,-1);
   Width=Limit(Width,300,1000,430);Height=Limit(Height,180,1000,380);
   FontSize=Limit(FontSize,12,28,16);Opacity=Limit(Opacity,0.3,1,0.8);
  }
  static double Finite(double value,double fallback){return Double.IsNaN(value)||Double.IsInfinity(value)?fallback:value;}
  static double Limit(double value,double min,double max,double fallback){return Math.Max(min,Math.Min(max,Finite(value,fallback)));}
 }

 public static class TranslationGuard {
  static string[] Parts(string text,string pattern){return Regex.Matches(text,pattern,RegexOptions.Multiline|RegexOptions.IgnoreCase).Cast<Match>().Select(m=>m.Value).ToArray();}
  public static string Check(string source,string result,IEnumerable<string> words){
   if(String.IsNullOrWhiteSpace(result)||result.Length>16000)return "Traduction vide ou trop longue.";
   if(!Parts(source,@"\*+").SequenceEqual(Parts(result,@"\*+")))return "Les astérisques RP ont été modifiés.";
   if(!Parts(source,@"^\s*/[\p{L}\d_]+\b").SequenceEqual(Parts(result,@"^\s*/[\p{L}\d_]+\b")))return "Une commande RP a été modifiée.";
   if(Parts(source,@"[-–—][.…]+|\.{2,}|…+").Length!=Parts(result,@"[-–—][.…]+|\.{2,}|…+").Length)return "Une interruption RP a été modifiée.";
   if(Parts(source,@"\b([\p{L}]{1,3})(?:-\1)*-(?=\1)").Length!=Parts(result,@"\b([\p{L}]{1,3})(?:-\1)*-(?=\1)").Length)return "Un bégaiement volontaire a été modifié.";
   foreach(string word in words.Where(w=>w.Any(Char.IsUpper))){
    string pattern=@"(?<![\p{L}\p{N}_])"+Regex.Escape(word)+@"(?![\p{L}\p{N}_])";
    if(Regex.Matches(source,pattern,RegexOptions.IgnoreCase).Count!=Regex.Matches(result,pattern,RegexOptions.IgnoreCase).Count)return "Un nom protégé a été modifié : "+word+".";
   }
   return null;
  }
 }
}
