using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using System.Diagnostics;

namespace ParoleRP {

 public static class HotkeyChord {
  static readonly Dictionary<uint,string> names=new Dictionary<uint,string>{{8,"Retour arrière"},{9,"Tab"},{13,"Entrée"},{19,"Pause"},{20,"Verr. Maj"},{27,"Échap"},{32,"Espace"},{33,"Page précédente"},{34,"Page suivante"},{35,"Fin"},{36,"Début"},{37,"Gauche"},{38,"Haut"},{39,"Droite"},{40,"Bas"},{44,"Impr. écran"},{45,"Inser"},{46,"Suppr"},{106,"Pavé Multiplier"},{107,"Pavé Plus"},{109,"Pavé Moins"},{110,"Pavé Décimal"},{111,"Pavé Diviser"},{144,"Verr. Num"},{145,"Arrêt défil."},{186,"OEM1"},{187,"Plus"},{188,"Virgule"},{189,"Moins"},{190,"Point"},{191,"OEM2"},{192,"OEM3"},{219,"OEM4"},{220,"OEM5"},{221,"OEM6"},{222,"OEM7"},{226,"OEM102"}};
  public static bool IsKey(uint key){return key>=8&&key<=254&&!new uint[]{16,17,18,91,92,160,161,162,163,164,165,231}.Contains(key);}
  public static string KeyName(uint key){if(key>=65&&key<=90||key>=48&&key<=57)return ((char)key).ToString();if(key>=112&&key<=135)return "F"+(key-111);if(key>=96&&key<=105)return "Pavé "+(key-96);string name;return names.TryGetValue(key,out name)?name:"Touche 0x"+key.ToString("X2");}
  public static string Format(uint modifiers,uint key){if((modifiers&~15u)!=0||!IsKey(key))return null;return ((modifiers&2)!=0?"Ctrl + ":"")+((modifiers&1)!=0?"Alt + ":"")+((modifiers&4)!=0?"Maj + ":"")+((modifiers&8)!=0?"Win + ":"")+KeyName(key);}
  public static bool TryParse(string text,out uint modifiers,out uint key){modifiers=0;key=0;if(String.IsNullOrWhiteSpace(text)||text.Length>100)return false;var parts=text.Split('+').Select(p=>p.Trim()).ToArray();if(parts.Any(p=>p.Length==0))return false;for(int i=0;i<parts.Length-1;i++){uint bit=parts[i].Equals("Ctrl",StringComparison.OrdinalIgnoreCase)?2u:parts[i].Equals("Alt",StringComparison.OrdinalIgnoreCase)?1u:parts[i].Equals("Maj",StringComparison.OrdinalIgnoreCase)?4u:parts[i].Equals("Win",StringComparison.OrdinalIgnoreCase)?8u:0u;if(bit==0||(modifiers&bit)!=0)return false;modifiers|=bit;}string name=parts[parts.Length-1];for(uint k=8;k<=254;k++)if(IsKey(k)&&KeyName(k).Equals(name,StringComparison.OrdinalIgnoreCase)){key=k;return true;}return false;}
  public static bool Same(string a,string b){uint am,ak,bm,bk;return TryParse(a,out am,out ak)&&TryParse(b,out bm,out bk)&&am==bm&&ak==bk;}
 }
 public static class WritingStyles {
  public static readonly string[] GenerationFormats={"Auto","/me","/do","/ame","Entre étoiles"};
  public static readonly string[] GenerationDetails={"Bref","Équilibré","Détaillé"};
  public static readonly string[] Dialogue={"Naturel","Soutenu","Élégant","Familier","Street discret","Street US discret","Populaire / beauf","Ancienne génération","Chaleureux","Froid / direct","Timide / hésitant","Théâtral","Personnalisé"};
  public static readonly string[] Actions={"Simple","Concis","Descriptif","Détaillé","Littéraire","Cinématographique","Personnalisé"};
 }
 public sealed class Character {
  public string Id {get;set;} public string Name {get;set;} public string Gender {get;set;} public string Context {get;set;} public string Words {get;set;} public string Accent {get;set;} public string DialogueStyle {get;set;} public string ActionStyle {get;set;} public string DialogueInstructions {get;set;} public string ActionInstructions {get;set;} public string GenerationFormat {get;set;} public string GenerationDetail {get;set;} public string GenerationInstructions {get;set;}
  public override string ToString(){return Name;}
  public Character(){Id=Guid.NewGuid().ToString("N");Name="Nouveau personnage";Gender="À préciser";Context="";Words="";Accent="#E8E5DF";DialogueStyle="Naturel";ActionStyle="Simple";DialogueInstructions="";ActionInstructions="";GenerationFormat="Auto";GenerationDetail="Équilibré";GenerationInstructions="";}
 }
 public sealed class Preferences {
  public bool FallbackEnabled {get;set;} public string QuotaGroup {get;set;} public int PerformanceVersion {get;set;} public string Provider {get;set;} public string Model {get;set;} public string Hotkey {get;set;} public string RephraseHotkey {get;set;} public string GenerationHotkey {get;set;} public bool MinimizeToTray {get;set;}
  public Preferences(){QuotaGroup="principal";Provider="Groq";Model="qwen/qwen3.8-27b";Hotkey="Alt + Espace";RephraseHotkey="Alt + Maj + Espace";GenerationHotkey="Ctrl + Alt + G";MinimizeToTray=true;}
 }
 public sealed class LocalData {
  public bool SetupCompleted {get;set;} public int SetupVersion {get;set;} public int SetupStep {get;set;}
  public int SchemaVersion {get;set;} public string ActiveId {get;set;} public string SharedWords {get;set;} public Preferences Settings {get;set;} public List<Character> Characters {get;set;}
  public LocalData(){SchemaVersion=1;SharedWords="";Settings=new Preferences();Characters=new List<Character>();}
  public Character Active {get {return Characters.FirstOrDefault(c=>c.Id==ActiveId)??Characters.FirstOrDefault();}}
  public static LocalData Defaults(){return new LocalData{SetupCompleted=true,SetupVersion=2,Settings=new Preferences{Provider="Groq",Model="qwen/qwen3.8-27b",PerformanceVersion=2}};}
 }
 public sealed class Backup {
  public string Application {get;set;} public int SchemaVersion {get;set;} public string ExportedAt {get;set;} public string Kind {get;set;} public string ActiveId {get;set;} public string SharedWords {get;set;} public Preferences Settings {get;set;} public List<Character> Characters {get;set;}
 }
 public static class DataFiles {
  public static JavaScriptSerializer Json(){return new JavaScriptSerializer{MaxJsonLength=2000000,RecursionLimit=64};}
  public static void ValidateCharacter(Character c){if(c==null||String.IsNullOrWhiteSpace(c.Id)||c.Id.Length>80)throw new Exception("Identifiant de personnage invalide.");if(String.IsNullOrWhiteSpace(c.Name)||c.Name.Length>80)throw new Exception("Le nom doit contenir de 1 à 80 caractères.");if(c.Context==null||c.Context.Length>5000)throw new Exception("Le contexte est limité à 5 000 caractères.");if(c.Words==null||c.Words.Length>18000)throw new Exception("Le dictionnaire du personnage est trop long.");if(c.Gender!="Masculin"&&c.Gender!="Féminin"&&c.Gender!="Neutre"&&c.Gender!="À préciser")throw new Exception("Genre grammatical inconnu.");if(!Regex.IsMatch(c.Accent??"",@"^#[0-9a-fA-F]{6}$"))c.Accent="#E8E5DF";if(String.IsNullOrEmpty(c.DialogueStyle))c.DialogueStyle="Naturel";if(String.IsNullOrEmpty(c.ActionStyle))c.ActionStyle="Simple";if(!WritingStyles.Dialogue.Contains(c.DialogueStyle)||!WritingStyles.Actions.Contains(c.ActionStyle))throw new Exception("Style de reformulation inconnu.");c.DialogueInstructions=c.DialogueInstructions??"";c.ActionInstructions=c.ActionInstructions??"";if(c.DialogueInstructions.Length>1000||c.ActionInstructions.Length>1000)throw new Exception("Chaque consigne de style est limitée à 1 000 caractères.");if(String.IsNullOrEmpty(c.GenerationFormat))c.GenerationFormat="Auto";if(String.IsNullOrEmpty(c.GenerationDetail))c.GenerationDetail="Équilibré";c.GenerationInstructions=c.GenerationInstructions??"";if(!WritingStyles.GenerationFormats.Contains(c.GenerationFormat)||!WritingStyles.GenerationDetails.Contains(c.GenerationDetail)||c.GenerationInstructions.Length>1000)throw new Exception("Réglages de génération invalides.");ValidateWords(c.Words);}
  public static void ValidateWords(string text){var words=Words(text);if(words.Count>250||words.Any(w=>w.Length>100))throw new Exception("Un dictionnaire est limité à 250 entrées de 100 caractères maximum.");}
  public static List<string> Words(string text){return (text??"").Split(new[]{'\r','\n'},StringSplitOptions.RemoveEmptyEntries).Select(x=>x.Trim()).Where(x=>x.Length>0&&!x.StartsWith("#")).Distinct(StringComparer.OrdinalIgnoreCase).ToList();}
  public static void Validate(LocalData d){if(d==null||d.SchemaVersion!=1)throw new Exception("Cette version de sauvegarde n'est pas prise en charge.");if(d.Characters==null||d.Characters.Count>50)throw new Exception("Les données doivent contenir au maximum 50 personnages.");foreach(var c in d.Characters)ValidateCharacter(c);if(d.Characters.Select(x=>x.Id).Distinct().Count()!=d.Characters.Count)throw new Exception("Identifiants de personnages en double.");if(d.SharedWords==null||d.SharedWords.Length>18000)throw new Exception("Dictionnaire commun invalide.");ValidateWords(d.SharedWords);if(d.Settings==null)d.Settings=new Preferences();if(d.Settings.Provider=="Google"){d.Settings.Provider="Groq";d.Settings.Model="qwen/qwen3.8-27b";d.Settings.QuotaGroup="principal";}if(d.Settings.Provider!="Groq")throw new Exception("Fournisseur API inconnu.");if(String.IsNullOrWhiteSpace(d.Settings.QuotaGroup))d.Settings.QuotaGroup="principal";if(d.Settings.QuotaGroup.Length>80||d.Settings.QuotaGroup.Any(Char.IsControl))throw new Exception("Nom de groupe de quota invalide.");if(!Regex.IsMatch(d.Settings.Model??"",@"^[a-zA-Z0-9._/-]{1,100}$"))throw new Exception("Nom de modèle invalide.");uint hotkeyModifiers,hotkeyKey;if(!HotkeyChord.TryParse(d.Settings.Hotkey,out hotkeyModifiers,out hotkeyKey))d.Settings.Hotkey="Alt + Espace";if(d.Settings.RephraseHotkey!="Désactivé"&&!HotkeyChord.TryParse(d.Settings.RephraseHotkey,out hotkeyModifiers,out hotkeyKey))d.Settings.RephraseHotkey="Alt + Maj + Espace";if(HotkeyChord.Same(d.Settings.RephraseHotkey,d.Settings.Hotkey))d.Settings.RephraseHotkey="Désactivé";if(d.Settings.GenerationHotkey!="Désactivé"&&!HotkeyChord.TryParse(d.Settings.GenerationHotkey,out hotkeyModifiers,out hotkeyKey))d.Settings.GenerationHotkey="Ctrl + Alt + G";if(HotkeyChord.Same(d.Settings.GenerationHotkey,d.Settings.Hotkey)||HotkeyChord.Same(d.Settings.GenerationHotkey,d.Settings.RephraseHotkey))d.Settings.GenerationHotkey="Désactivé";if(!d.Characters.Any(x=>x.Id==d.ActiveId))d.ActiveId=d.Characters.Count==0?null:d.Characters[0].Id;d.SetupCompleted=true;d.SetupVersion=2;d.SetupStep=0;}
  public static string Export(LocalData d,Character one){Validate(d);var b=new Backup{Application="Parole RP",SchemaVersion=1,ExportedAt=DateTime.UtcNow.ToString("o"),Kind=one==null?"workspace":"character",ActiveId=one==null?d.ActiveId:one.Id,SharedWords=d.SharedWords,Settings=one==null?d.Settings:null,Characters=one==null?d.Characters:new List<Character>{one}};return Json().Serialize(b);}
  public static Backup ReadBackup(string content){if(content==null||content.Length>2000000)throw new Exception("Le fichier dépasse la taille autorisée (2 Mo de texte).");var b=Json().Deserialize<Backup>(content);if(b==null||b.Application!="Parole RP"||b.SchemaVersion!=1||(b.Kind!="workspace"&&b.Kind!="character"))throw new Exception("Ce fichier n'est pas une sauvegarde Reply AI compatible.");if(b.Characters==null||(b.Kind=="character"&&b.Characters.Count==0))throw new Exception("La sauvegarde ne contient aucun personnage.");var temp=new LocalData{SchemaVersion=b.SchemaVersion,Characters=b.Characters,SharedWords=b.SharedWords??"",Settings=b.Settings??new Preferences(),ActiveId=b.ActiveId};Validate(temp);return b;}
  public static LocalData ApplyImport(LocalData current,Backup b,bool replace){var next=Json().Deserialize<LocalData>(Json().Serialize(current));if(replace&&b.Kind=="workspace"){next.Characters=b.Characters;next.Settings=b.Settings??new Preferences();next.SharedWords=b.SharedWords??"";next.ActiveId=b.ActiveId;}else {foreach(var incoming in b.Characters){var copy=Json().Deserialize<Character>(Json().Serialize(incoming));if(replace){next.Characters.RemoveAll(c=>c.Id==copy.Id);next.Characters.Add(copy);}else{if(next.Characters.Any(c=>c.Id==copy.Id||c.Name.Equals(copy.Name,StringComparison.OrdinalIgnoreCase))){copy.Id=Guid.NewGuid().ToString("N");copy.Name=copy.Name.Substring(0,Math.Min(copy.Name.Length,68))+" (importé)";}next.Characters.Add(copy);}}next.SharedWords=String.Join("\n",Words(next.SharedWords).Concat(Words(b.SharedWords)).Distinct(StringComparer.OrdinalIgnoreCase));}Validate(next);return next;}
  public static void AtomicWrite(string path,string content){Directory.CreateDirectory(Path.GetDirectoryName(path));string temp=path+".tmp-"+Guid.NewGuid().ToString("N");try{File.WriteAllText(temp,content,new UTF8Encoding(false));if(File.Exists(path))File.Replace(temp,path,path+".bak");else File.Move(temp,path);}finally{if(File.Exists(temp))File.Delete(temp);}}
 }
 public sealed class LocalStore {
  public readonly string Root;public string DataPath {get{return Path.Combine(Root,"workspace.json");}}public string Provider {get;set;}
  private string KeyPath{get{return Path.Combine(Root,"groq-key.protected");}}
  public LocalStore(string root){Root=root;Provider="Groq";}
  public LocalData Load(){if(!File.Exists(DataPath))return LocalData.Defaults();LocalData d;try{d=DataFiles.Json().Deserialize<LocalData>(File.ReadAllText(DataPath,Encoding.UTF8));DataFiles.Validate(d);}catch{throw new Exception("Les données locales sont illisibles. Elles n'ont pas été écrasées. Une copie précédente peut être disponible dans workspace.json.bak.");}return d;}
  // Explicit persistence DTO: computed properties and credentials are never serialized.
  public void Save(LocalData d){DataFiles.Validate(d);var dto=new {SchemaVersion=d.SchemaVersion,ActiveId=d.ActiveId,SharedWords=d.SharedWords,Settings=d.Settings,Characters=d.Characters};DataFiles.AtomicWrite(DataPath,DataFiles.Json().Serialize(dto));}
  public bool HasKey {get{return File.Exists(KeyPath);}}
  public void SaveKey(string key){key=(key??"").Trim();if(key.Length<15||key.Length>300||key.Any(Char.IsWhiteSpace))throw new Exception("La clé API semble incomplète. Copiez uniquement la clé.");Directory.CreateDirectory(Root);var protectedBytes=ProtectedData.Protect(Encoding.UTF8.GetBytes(key),null,DataProtectionScope.CurrentUser);string tmp=KeyPath+".tmp";File.WriteAllBytes(tmp,protectedBytes);if(File.Exists(KeyPath))File.Replace(tmp,KeyPath,null);else File.Move(tmp,KeyPath);}
  public string GetKey(){if(!HasKey)throw new Exception("Ajoutez la clé du fournisseur choisi dans Connexion & API.");try{return Encoding.UTF8.GetString(ProtectedData.Unprotect(File.ReadAllBytes(KeyPath),null,DataProtectionScope.CurrentUser));}catch{throw new Exception("Cette clé ne peut pas être déchiffrée sur ce compte Windows. Enregistrez-la à nouveau.");}}
  public void RemoveRetiredCredentials(){foreach(string name in new[]{"google-key.protected","google-key.protected.tmp"}){string legacy=Path.Combine(Root,name);if(File.Exists(legacy))File.Delete(legacy);}}
  public void RemoveKey(){if(File.Exists(KeyPath))File.Delete(KeyPath);}
 }
 public static class RpGuard {
  static List<string> Extract(string s,string pattern){return Regex.Matches(s,pattern,RegexOptions.Multiline|RegexOptions.IgnoreCase).Cast<Match>().Select(m=>m.Value).ToList();}
  public static string Check(string source,string result,IEnumerable<string> words,bool rephrase=false){if(String.IsNullOrWhiteSpace(result))return "La réponse est vide.";if(result.Length>(rephrase?16000:Math.Max(source.Length*2+80,160)))return "La réponse développe trop le message.";if(!Extract(source,@"\*+").SequenceEqual(Extract(result,@"\*+")))return "Les astérisques RP ont été modifiés.";if(!Extract(source,@"^\s*/[\p{L}\d_]+\b").SequenceEqual(Extract(result,@"^\s*/[\p{L}\d_]+\b")))return "Une commande RP a été modifiée.";string stutter=@"\b([\p{L}]{1,3})(?:-\1)*-(?=\1)";if(!Extract(source,stutter).SequenceEqual(Extract(result,stutter)))return "Un bégaiement volontaire a été modifié.";string punctuation=@"[-–—][.…]+|\.{2,}|…+|-(?=$|[\s*])";if(!Extract(source,punctuation).SequenceEqual(Extract(result,punctuation)))return "Une interruption ou une ponctuation RP a été modifiée.";if(!rephrase&&source.Count(c=>c=='\n')!=result.Count(c=>c=='\n'))return "Les sauts de ligne ont été modifiés.";foreach(string w in words){if(String.IsNullOrWhiteSpace(w)||(rephrase&&!w.Any(Char.IsUpper)))continue;string pattern=@"(?<![\p{L}\p{N}_])"+Regex.Escape(w)+@"(?![\p{L}\p{N}_])";RegexOptions wordOptions=RegexOptions.IgnoreCase;if(w.Length==1){wordOptions=RegexOptions.None;pattern+=@"(?!['’\-][\p{L}])";}int before=Regex.Matches(source,pattern,wordOptions).Count;if(before>0&&Regex.Matches(result,pattern,wordOptions).Count!=before)return "Un mot protégé a été modifié : "+w+".";}return null;}
 }
 public static class GenerationGuard {
  public static string Check(string text,string format){
   if(String.IsNullOrWhiteSpace(text))return "La génération est vide.";
   if(text.Length>3000||text.Any(c=>Char.IsControl(c)||c=='\u2028'||c=='\u2029')||text.Contains("```"))return "La génération doit rester un seul message RP de 3 000 caractères maximum.";
   bool stars=format=="Entre étoiles";string body;
   if(stars){if(!text.StartsWith("*")||!text.EndsWith("*")||text.Count(c=>c=='*')!=2)return "La génération doit être délimitée par deux étoiles.";body=text.Substring(1,text.Length-2).Trim();}
   else{var match=Regex.Match(text,@"^/(me|do|ame) +(.+)$");if(!match.Success)return "La génération doit commencer par /me, /do ou /ame.";if(format!="Auto"&&format!="/"+match.Groups[1].Value)return "Le format de génération demandé n’a pas été respecté.";body=match.Groups[2].Value.Trim();}
   if(body.Length==0||Regex.IsMatch(body,@"(?:^|\s)/[\p{L}]+\b"))return "La génération doit contenir une seule action ou description RP.";
   return null;
  }
 }
 public sealed class Correction {public string Text;public string Warning;public int Milliseconds;public int InputTokens;public int OutputTokens;public int CachedTokens;public string ContextUsed;public string ConnectionName;public string ProviderUsed;public string ModelUsed;public int Attempts;}
}
