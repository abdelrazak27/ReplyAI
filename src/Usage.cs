using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Web.Script.Serialization;
namespace ParoleRP {
 public sealed class TokenUsage {public string model {get;set;} public long inputTokens {get;set;} public long outputTokens {get;set;} public long cachedTokens {get;set;}}
 public sealed class RouteEvent {public string connectionId {get;set;} public string quotaGroup {get;set;} public string model {get;set;} public string state {get;set;} public int code {get;set;} public double retrySeconds {get;set;}}
 public sealed class ApiReport {public bool ok {get;set;} public string operation {get;set;} public List<TokenUsage> usageEvents {get;set;} public List<RouteEvent> routeEvents {get;set;}}
 public sealed class UsageTotals {
  public long ChatTranslations {get;set;} public long ChatTokens {get;set;} public long MeasuredChat {get;set;} public long Corrections {get;set;} public long Rephrases {get;set;} public long Generations {get;set;} public long Tests {get;set;} public long Failures {get;set;} public long Input {get;set;} public long Output {get;set;} public long Cached {get;set;} public long Unreported {get;set;}
  public long CorrectionTokens {get;set;} public long RephraseTokens {get;set;} public long GenerationTokens {get;set;} public long MeasuredCorrections {get;set;} public long MeasuredRephrases {get;set;} public long MeasuredGenerations {get;set;}
  public long Requests {get{return Corrections+Rephrases+Generations+Tests+ChatTranslations;}}
  public long UncachedTotal {get{return Math.Max(0,Input-Cached)+Output;}}
  public void Add(UsageTotals n){ChatTranslations+=n.ChatTranslations;ChatTokens+=n.ChatTokens;MeasuredChat+=n.MeasuredChat;CorrectionTokens+=n.CorrectionTokens;RephraseTokens+=n.RephraseTokens;GenerationTokens+=n.GenerationTokens;MeasuredCorrections+=n.MeasuredCorrections;MeasuredRephrases+=n.MeasuredRephrases;MeasuredGenerations+=n.MeasuredGenerations;Corrections+=n.Corrections;Rephrases+=n.Rephrases;Generations+=n.Generations;Tests+=n.Tests;Failures+=n.Failures;Input+=n.Input;Output+=n.Output;Cached+=n.Cached;Unreported+=n.Unreported;}
  public static UsageTotals From(ApiReport r){var n=new UsageTotals();if(r.operation=="correct")n.Corrections=1;else if(r.operation=="rephrase")n.Rephrases=1;else if(r.operation=="generate")n.Generations=1;else if(r.operation=="test")n.Tests=1;else if(r.operation=="chat")n.ChatTranslations=1;else return n;if(!r.ok)n.Failures=1;var usage=r.usageEvents??new List<TokenUsage>();if(usage.Count==0)n.Unreported=1;foreach(var u in usage){long input=Math.Max(0,u.inputTokens);n.Input+=input;n.Output+=Math.Max(0,u.outputTokens);n.Cached+=Math.Min(input,Math.Max(0,u.cachedTokens));}if(usage.Count>0){if(r.operation=="chat"){n.ChatTokens=n.UncachedTotal;n.MeasuredChat=1;}if(r.operation=="correct"){n.CorrectionTokens=n.UncachedTotal;n.MeasuredCorrections=1;}else if(r.operation=="rephrase"){n.RephraseTokens=n.UncachedTotal;n.MeasuredRephrases=1;}else if(r.operation=="generate"){n.GenerationTokens=n.UncachedTotal;n.MeasuredGenerations=1;}}return n;}
 }
 public sealed class DailyUsage {public string Day {get;set;} public UsageTotals Totals {get;set;}}
 public sealed class UsageHistory {public int Version {get;set;} public List<DailyUsage> Days {get;set;}}
 public sealed class UsageTracker {
  readonly string path;readonly List<DailyUsage> days=new List<DailyUsage>();
  public UsageTotals Session {get;private set;} public DateTime Started {get;private set;} public string Warning {get;private set;}
  public UsageTracker(string root){path=Path.Combine(root,"usage.json");Reset();try{if(File.Exists(path)){if(new FileInfo(path).Length>1048576)throw new InvalidDataException();var h=DataFiles.Json().Deserialize<UsageHistory>(File.ReadAllText(path));if(h==null||h.Version!=1||h.Days==null||h.Days.Count>400||h.Days.Any(d=>d==null||d.Totals==null||!ValidDay(d.Day))||h.Days.Select(d=>d.Day).Distinct().Count()!=h.Days.Count)throw new InvalidDataException();days.AddRange(h.Days);}}catch{Warning="Historique local illisible : les nouveaux chiffres restent disponibles pour cette session.";}}
  static bool ValidDay(string day){DateTime d;return DateTime.TryParseExact(day,"yyyy-MM-dd",System.Globalization.CultureInfo.InvariantCulture,System.Globalization.DateTimeStyles.None,out d);}
  public void Reset(){Session=new UsageTotals();Started=DateTime.Now;}
  public UsageTotals Period(int period,DateTime now){if(period==0)return Session;string key=now.ToString(period==1?"yyyy-MM-dd":"yyyy-MM");var total=new UsageTotals();foreach(var d in days.Where(d=>period==1?d.Day==key:d.Day.StartsWith(key+"-",StringComparison.Ordinal)))total.Add(d.Totals);return total;}
  public void Record(ApiReport report,DateTime now){if(report==null||!new[]{"correct","rephrase","generate","test","chat"}.Contains(report.operation))return;var n=UsageTotals.From(report);Session.Add(n);string day=now.ToString("yyyy-MM-dd");var entry=days.FirstOrDefault(d=>d.Day==day);if(entry==null){entry=new DailyUsage{Day=day,Totals=new UsageTotals()};days.Add(entry);}entry.Totals.Add(n);days.Sort((a,b)=>String.CompareOrdinal(a.Day,b.Day));if(days.Count>400)days.RemoveRange(0,days.Count-400);if(Warning!=null)return;try{var h=new UsageHistory{Version=1,Days=days};DataFiles.AtomicWrite(path,DataFiles.Json().Serialize(h));}catch{Warning="L’historique n’a pas pu être enregistré. Les chiffres de session restent disponibles.";}}
 }
 public sealed class ConnectionHealth {
  sealed class Item {public string State;public int Code;public DateTime Until;}
  readonly Dictionary<string,Item> states=new Dictionary<string,Item>();readonly Dictionary<string,Item> quotas=new Dictionary<string,Item>();
  static string Scope(string group,string model){return (group??"principal").Trim().ToLowerInvariant()+"\n"+model;}
  public void Record(ApiReport report,DateTime now){foreach(var e in report.routeEvents??new List<RouteEvent>()){if(e==null||!new[]{"ready","quota","error"}.Contains(e.state))continue;double wait=Double.IsNaN(e.retrySeconds)||Double.IsInfinity(e.retrySeconds)?60:Math.Max(1,Math.Min(86400*30,e.retrySeconds));var item=new Item{State=e.state,Code=e.code,Until=now.AddSeconds(wait)};if(e.state=="quota")quotas[Scope(e.quotaGroup,e.model)]=item;else{states[(e.connectionId??"primary")+"\n"+e.model]=item;if(e.state=="ready")quotas.Remove(Scope(e.quotaGroup,e.model));}}}
  public void Clear(){states.Clear();quotas.Clear();}
  public string Label(string id,string group,string model,DateTime now){Item item;if(!quotas.TryGetValue(Scope(group,model),out item)&&!states.TryGetValue((id??"primary")+"\n"+model,out item))return "À vérifier";if(item.State=="ready")return "Disponible";if(item.State=="error")return item.Code>0?"Erreur "+item.Code:"Erreur / délai";if(now>=item.Until)return "À retester";var wait=item.Until-now;return "Quota · ~"+(wait.TotalHours>=1?Math.Ceiling(wait.TotalHours)+" h":wait.TotalMinutes>=1?Math.Ceiling(wait.TotalMinutes)+" min":Math.Ceiling(wait.TotalSeconds)+" s");}
 }
}
