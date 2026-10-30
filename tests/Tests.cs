using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using ParoleRP;
class Tests {
 static int checks;
 static void Check(bool value,string label){if(!value)throw new Exception("FAIL: "+label);checks++;Console.WriteLine("PASS "+label);}
 static void Reject(Action a,string label){try{a();}catch{Check(true,label);return;}throw new Exception("FAIL: "+label);}
 static void Main(){try{Run();Console.WriteLine(checks+" vérifications réussies.");}catch(Exception e){Console.Error.WriteLine(e);Environment.ExitCode=1;}}
 static void Run(){var empty=LocalData.Defaults();Check(empty.Characters.Count==0&&empty.SharedWords==""&&empty.SetupCompleted,"premier lancement sans profil ni dictionnaire préparé");DataFiles.Validate(empty);var d=LocalData.Defaults();d.Characters.Add(new Character{Name="Personnage de test",Gender="Masculin",Words="bong\nlok"});d.Characters.Add(new Character{Name="Second personnage de test",Gender="Féminin"});d.ActiveId=d.Characters[0].Id;d.SharedWords="homie\nhandshake";d.Active.Name="Sophea • ខ្មែរ";var export=DataFiles.Export(d,null);var backup=DataFiles.ReadBackup(export);Check(backup.Characters[0].Name==d.Active.Name,"export unicode roundtrip");Check(!export.Contains("Key")&&!export.Contains("Active\"")&&!export.Contains("SetupCompleted"),"export explicit sans clé ni propriété calculée");var merged=DataFiles.ApplyImport(d,backup,false);Check(merged.Characters.Count==4&&d.Characters.Count==2,"fusion indépendante sans modifier la source");Check(merged.Characters.Select(c=>c.Id).Distinct().Count()==4,"doublons conservés avec nouveaux identifiants");var replaced=DataFiles.ApplyImport(merged,backup,true);Check(replaced.Characters.Count==2&&replaced.ActiveId==d.ActiveId,"remplacement complet restaure le profil actif");Reject(()=>DataFiles.ReadBackup(export.Replace("\"SchemaVersion\":1","\"SchemaVersion\":99")),"version inconnue rejetée");Reject(()=>DataFiles.ReadBackup("{}"),"format invalide rejeté");Reject(()=>DataFiles.ValidateWords(String.Join("\n",Enumerable.Range(0,251))),"dictionnaire excessif rejeté");
 string root=Path.Combine(Environment.CurrentDirectory,"work","parole-tests","fixture-"+Guid.NewGuid().ToString("N"));var store=new LocalStore(root);store.Save(d);d.Active.Context="Contexte modifié";store.Save(d);Check(File.Exists(store.DataPath+".bak")&&store.Load().Active.Context==d.Active.Context,"sauvegarde atomique avec copie précédente");var fresh=new LocalStore(root+"-fresh").Load();Check(fresh.SetupCompleted&&fresh.Active==null&&fresh.Characters.Count==0,"nouveau PC ouvre un espace vide sans tutoriel");
 store.Save(fresh);Check(store.Load().Active==null&&store.Load().SetupCompleted,"espace vide sauvegardé et relu sans tutoriel");
 var oldState=new LocalData{SetupCompleted=false,SetupVersion=1,SetupStep=2,Characters=d.Characters,ActiveId=d.ActiveId,SharedWords="à conserver"};File.WriteAllText(store.DataPath,DataFiles.Json().Serialize(oldState));var resumed=store.Load();Check(resumed.SetupCompleted&&resumed.ActiveId==d.ActiveId&&resumed.SharedWords=="à conserver","ancien tutoriel abandonné conserve profils et dictionnaire");
 var emptyRoundtrip=DataFiles.ReadBackup(DataFiles.Export(fresh,null));Check(emptyRoundtrip.Kind=="workspace"&&emptyRoundtrip.Characters.Count==0,"espace vide exportable et importable");
string fakeKey="FAKE_TEST_KEY_NEVER_REAL_123456789";store.SaveKey(fakeKey);Check(store.GetKey()==fakeKey,"clé DPAPI relue sur le même compte Windows");Check(!System.Text.Encoding.UTF8.GetString(File.ReadAllBytes(Path.Combine(root,"groq-key.protected"))).Contains(fakeKey),"clé absente du fichier en clair");store.RemoveKey();Check(!store.HasKey,"clé supprimable indépendamment des profils");store.Provider="Groq";store.SaveKey("GROQ_FAKE_KEY_123456789");Check(store.GetKey()=="GROQ_FAKE_KEY_123456789","clé principale Groq conservée");store.RemoveKey();

 var words=new[]{"homie","bong","lok","handshake","Jyce"};Check(RpGuard.Check("Frere, calme toi je vais menerver la","Frère, calme-toi, je vais m'énerver là.",words)==null,"accents, grammaire et ponctuation ordinaire autorisés");Check(RpGuard.Check("*b-bro baisse les yeux-..* jvais bien homie","*b-bro baisse les yeux-..* j'vais bien homie.",words)==null,"action, bégaiement et interruption préservés");Check(RpGuard.Check("lowkey bro jcrois quon c'est tromper de spot","lowkey bro j'crois qu'on s'est trompé de spot.",words)==null,"accord et homophone autorisés");Check(RpGuard.Check("je suis aller","je suis allée.",words)==null,"accord féminin autorisé");Check(RpGuard.Check("salut homie","salut amie",words)!=null,"substitution d'un mot protégé bloquée");Check(RpGuard.Check("*regarde lok*","regarde lok",words)!=null,"perte des astérisques bloquée");Check(RpGuard.Check("b-bro...","bro...",words)!=null,"perte du bégaiement bloquée");Check(RpGuard.Check("/me regarde bong","/do regarde bong",words)!=null,"commande modifiée bloquée");Check(RpGuard.Check("viens-..","viens.",words)!=null,"interruption modifiée bloquée");Check(RpGuard.Check("bonjour\nfrere","bonjour frère",words)!=null,"lignes fusionnées bloquées");

 Check(RpGuard.Check("j t deja dit", "Je t'ai déjà dit.",new[]{"J"})==null,"initiale SMS j ne protège pas un faux mot J");
 Check(RpGuard.Check("j-jvais bien", "j-j'vais bien",new[]{"J"})==null,"bégaiement de j corrigé sans faux mot J");
 Check(RpGuard.Check("J'vais bien", "Je vais bien",new[]{"J"})==null,"initiale élidée exclue des mots protégés");
 Check(RpGuard.Check("roule un J", "roule un joint",new[]{"J"})!=null,"mot autonome J reste protégé");
 Check(RpGuard.Check("salut Homie", "salut amie",new[]{"homie"})!=null,"vocabulaire ordinaire toujours protégé sans distinction de casse");

 var vault=new ConnectionVault(root);var routes=new System.Collections.Generic.List<ApiConnection>{new ApiConnection{Name="Groq secours",Provider="Groq",Model="qwen/qwen3.8-27b",QuotaGroup="principal",Key="FAKE_FALLBACK_KEY_123456"}};
 vault.Save(routes);Check(vault.Load()[0].Key==routes[0].Key,"connexion de secours DPAPI relue");
 Check(!System.Text.Encoding.UTF8.GetString(File.ReadAllBytes(Path.Combine(root,"connections.protected"))).Contains(routes[0].Key),"clé de secours absente du fichier en clair");
 Check(!DataFiles.Export(d,null).Contains(routes[0].Key)&&!DataFiles.Export(d,null).Contains("Groq secours"),"connexions et clés de secours exclues des exports");
 store.Provider=d.Settings.Provider;store.SaveKey("FAKE_MAIN_KEY_123456");d.Settings.FallbackEnabled=false;
 Check(vault.Routes(store,d.Settings).Count==1,"secours désactivé ne transmet que la principale");d.Settings.FallbackEnabled=true;
 Check(vault.Routes(store,d.Settings).Count==2,"secours actif ajoute la connexion après la principale");routes[0].Enabled=false;vault.Save(routes);
 Check(vault.Routes(store,d.Settings).Count==1,"connexion décochée exclue des requêtes");routes[0].Enabled=true;
 var duplicated=new System.Collections.Generic.List<ApiConnection>(routes){new ApiConnection{Name="Doublon",Provider="Groq",Model="qwen/qwen3.8-27b",QuotaGroup="autre",Key=routes[0].Key}};
 Reject(()=>vault.Save(duplicated),"clé identique refusée dans les secours");
 Check(vault.Load().Count==1,"échec de validation conserve le coffre précédent");
 var oldPreferences=DataFiles.Json().Deserialize<Preferences>("{\"Provider\":\"Groq\",\"Model\":\"qwen/qwen3.8-27b\"}");
 Check(!oldPreferences.FallbackEnabled&&oldPreferences.QuotaGroup=="principal","anciennes préférences n'activent aucun secours automatiquement");

 var migrated=new LocalData();migrated.Characters.Add(new Character{Name="Profil conservé",Context="Ma culture"});migrated.Settings.Provider="Google";migrated.Settings.Model="retired-model";DataFiles.Validate(migrated);
 Check(migrated.Settings.Provider=="Groq"&&migrated.Settings.Model=="qwen/qwen3.8-27b"&&migrated.Active.Context=="Ma culture","ancienne configuration convertie sans modifier le personnage");
 var legacyConnections=new System.Collections.Generic.List<ApiConnection>{routes[0],new ApiConnection{Name="Ancien secours",Provider="Google",Model="retired-model",QuotaGroup="old",Key="FAKE_RETIRED_KEY_123456"}};
 File.WriteAllBytes(Path.Combine(root,"connections.protected"),System.Security.Cryptography.ProtectedData.Protect(System.Text.Encoding.UTF8.GetBytes(DataFiles.Json().Serialize(legacyConnections)),null,System.Security.Cryptography.DataProtectionScope.CurrentUser));
 Check(vault.Load().Count==1&&vault.Load()[0].Key==routes[0].Key,"ancien secours retiré des routes avant tout appel");vault.RemoveRetiredConnections();
 var raw=DataFiles.Json().Deserialize<System.Collections.Generic.List<ApiConnection>>(System.Text.Encoding.UTF8.GetString(System.Security.Cryptography.ProtectedData.Unprotect(File.ReadAllBytes(Path.Combine(root,"connections.protected")),null,System.Security.Cryptography.DataProtectionScope.CurrentUser)));
 Check(raw.Count==1&&raw[0].Provider=="Groq","migration nettoie le coffre et conserve Groq");
 File.WriteAllText(Path.Combine(root,"google-key.protected"),"fake-retired-credential");store.SaveKey("GROQ_RETAINED_KEY_123456");store.RemoveRetiredCredentials();
 Check(!File.Exists(Path.Combine(root,"google-key.protected"))&&store.GetKey()=="GROQ_RETAINED_KEY_123456","ancienne clé supprimée et clé Groq intacte");

 Check(ConnectionDisplay.KeySuffix("FAKE_PRIVATE_KEY_abcdef")=="•••• abcdef","liste des clés affiche seulement six derniers caractères");
 Check(!ConnectionDisplay.KeySuffix("FAKE_PRIVATE_KEY_abcdef").Contains("FAKE_PRIVATE_KEY"),"préfixe de la clé absent de l’affichage");
 Check(ConnectionDisplay.KeySuffix("short")=="••••","clé anormalement courte jamais affichée intégralement");
 Check(ConnectionDisplay.KeySuffix(null)=="Non configurée","clé absente affichée sans erreur");

 var styleCharacter=new Character{Name="Profil styles",DialogueStyle="Street US discret",ActionStyle="Littéraire",DialogueInstructions="Anglais rare.",ActionInstructions="Deux phrases au maximum."};
 var styled=new LocalData{SetupCompleted=true,Characters=new System.Collections.Generic.List<Character>{styleCharacter},ActiveId=styleCharacter.Id};
 styled.Settings.RephraseHotkey="Ctrl + Alt + R";
 var styledRoundtrip=DataFiles.ReadBackup(DataFiles.Export(styled,null));
 Check(styledRoundtrip.Characters[0].DialogueStyle=="Street US discret"&&styledRoundtrip.Characters[0].ActionStyle=="Littéraire"&&styledRoundtrip.Characters[0].ActionInstructions==styleCharacter.ActionInstructions,"styles distincts et précisions inclus dans les exports");
 Check(styledRoundtrip.Settings.RephraseHotkey=="Ctrl + Alt + R","raccourci de reformulation exporté");
 var importedStyle=DataFiles.ApplyImport(d,DataFiles.ReadBackup(DataFiles.Export(styled,styleCharacter)),false);
 Check(importedStyle.Characters.Any(c=>c.Name==styleCharacter.Name&&c.DialogueInstructions=="Anglais rare."),"import conserve les styles du personnage");
 var oldCharacter=DataFiles.Json().Deserialize<Character>("{\"Name\":\"Ancien profil\",\"Gender\":\"Féminin\",\"Context\":\"\",\"Words\":\"\"}");DataFiles.ValidateCharacter(oldCharacter);
 Check(oldCharacter.DialogueStyle=="Naturel"&&oldCharacter.ActionStyle=="Simple","anciens profils reçoivent des styles neutres");
 styleCharacter.ActionStyle="inconnu";Reject(()=>DataFiles.ValidateCharacter(styleCharacter),"style inconnu refusé");styleCharacter.ActionStyle="Simple";
 styleCharacter.DialogueInstructions=new string('x',1001);Reject(()=>DataFiles.ValidateCharacter(styleCharacter),"consignes excessives refusées");

 uint hm,hk;Check(HotkeyChord.TryParse("Ctrl + Alt + Maj + Win + F10",out hm,out hk)&&hm==15&&hk==121,"raccourci libre avec quatre modificateurs");
 Check(HotkeyChord.TryParse("Ctrl + Maj + P",out hm,out hk)&&hm==6&&hk==80,"lettre personnalisée reconnue");
 Check(HotkeyChord.TryParse("F7",out hm,out hk)&&hm==0&&hk==118,"touche seule reconnue");
 Check(HotkeyChord.Same("Maj + Ctrl + A","Ctrl + Maj + A"),"doublons comparés indépendamment de l’ordre des modificateurs");
 Check(!HotkeyChord.TryParse("Ctrl + Ctrl + A",out hm,out hk)&&!HotkeyChord.TryParse("Ctrl",out hm,out hk),"modificateurs seuls ou répétés refusés");
 Check(!HotkeyChord.TryParse("Ctrl + n’importe quoi",out hm,out hk)&&!HotkeyChord.TryParse("Ctrl + Touche 0x10",out hm,out hk),"touche invalide refusée");
 bool allChords=true;for(uint key=8;key<=254;key++)if(HotkeyChord.IsKey(key)){uint mods,parsed;string text=HotkeyChord.Format(15,key);if(!HotkeyChord.TryParse(text,out mods,out parsed)||mods!=15||parsed!=key)allChords=false;}
 Check(allChords,"toutes les touches proposées se relisent sans perte");
 d.Settings.Hotkey="Ctrl + Win + Pavé 7";d.Settings.RephraseHotkey="Maj + F11";store.Save(d);var customKeys=store.Load();
 Check(customKeys.Settings.Hotkey=="Ctrl + Win + Pavé 7"&&customKeys.Settings.RephraseHotkey=="Maj + F11","raccourcis libres conservés après relance");
 var customBackup=DataFiles.ReadBackup(DataFiles.Export(d,null));Check(customBackup.Settings.Hotkey==d.Settings.Hotkey&&customBackup.Settings.RephraseHotkey==d.Settings.RephraseHotkey,"raccourcis libres conservés dans les exports");
 d.Settings.Hotkey="Ctrl + Maj + P";d.Settings.RephraseHotkey="Maj + Ctrl + P";DataFiles.Validate(d);Check(d.Settings.RephraseHotkey=="Désactivé","import d’un doublon désactive seulement la reformulation");

 string developed="/do Dans la pièce, l’odeur du tabac s’installe et imprègne peu à peu l’atmosphère, enveloppant l’espace d’une présence âcre et persistante. Elle occupe tout l’espace, lourde et tenace, sans laisser de répit à l’odorat.";
 Check(RpGuard.Check("/do ça sent la clope",developed,new string[0],true)==null,"reformulation longue autorisée indépendamment de la source");
 Check(RpGuard.Check("/do ça sent la clope",developed,new string[0])!=null,"correction ordinaire conserve sa limite de longueur");
 Check(RpGuard.Check("homie casse toi","Je te prie de quitter les lieux.",new[]{"homie"},true)==null,"reformulation peut remplacer le vocabulaire familier pour le registre soutenu");
 Check(RpGuard.Check("homie casse toi","Je te prie de quitter les lieux.",new[]{"homie"})!=null,"correction ordinaire conserve tous les mots protégés");
 Check(RpGuard.Check("Jyce, dégage","Quitte les lieux.",new[]{"Jyce"},true)!=null,"nom propre protégé conservé malgré la liberté de reformulation");
 Check(RpGuard.Check("/do La pièce sent le tabac.","/me sent le tabac.",new string[0],true)!=null,"commande RP reste protégée en reformulation");
 Check(RpGuard.Check("frère *baisse la voix* viens","Frère, approche.",new string[0],true)!=null,"délimitation d’action reste protégée en reformulation");
 Check(RpGuard.Check("Première phrase.\nDeuxième phrase.","Une phrase réorganisée.",new string[0],true)==null,"reformulation libre de réorganiser les phrases et paragraphes");

 var rawRoutes=new System.Collections.Generic.List<ApiConnection>{new ApiConnection{Name="Principale",Provider="Groq",Model="qwen/qwen3.8-27b",Key="FAKE_ROUTE_KEY_123456",QuotaGroup="principal"},new ApiConnection{Name="Secours",Provider="Groq",Model="openai/gpt-oss-20b",Key="FAKE_SECOND_ROUTE_KEY_123456",QuotaGroup="second"}};
 var rewriteRoutes=OperationRoutes.Prepare(rawRoutes,true);Check(rewriteRoutes.All(c=>c.Model=="openai/gpt-oss-120b"),"reformulation utilise le modèle choisi pour la qualité sur toutes les routes");
 Check(rawRoutes[0].Model=="qwen/qwen3.8-27b"&&rawRoutes[1].Model=="openai/gpt-oss-20b","modèles de correction et données enregistrées non modifiés");
 Check(rewriteRoutes[1].Key==rawRoutes[1].Key&&rewriteRoutes[1].QuotaGroup=="second"&&rewriteRoutes[1].Name=="Secours","routage conserve clés groupes de quotas et ordre de secours");
 Check(OperationRoutes.Prepare(rawRoutes,false)[0].Model=="qwen/qwen3.8-27b","correction garde le modèle configuré");

 var genCharacter=new Character{Name="Profil génération",GenerationFormat="/do",GenerationDetail="Détaillé",GenerationInstructions="Décrire seulement les odeurs."};
 var genData=new LocalData{SetupCompleted=true,Characters=new System.Collections.Generic.List<Character>{genCharacter},ActiveId=genCharacter.Id};genData.Settings.GenerationHotkey="Ctrl + Maj + G";
 var genExport=DataFiles.ReadBackup(DataFiles.Export(genData,null));Check(genExport.Characters[0].GenerationFormat=="/do"&&genExport.Characters[0].GenerationDetail=="Détaillé"&&genExport.Characters[0].GenerationInstructions==genCharacter.GenerationInstructions,"génération format détail et consignes exportés");
 Check(genExport.Settings.GenerationHotkey=="Ctrl + Maj + G","raccourci de génération exporté");
 var legacyGeneration=DataFiles.Json().Deserialize<Character>("{\"Name\":\"Ancien\",\"Context\":\"\",\"Words\":\"\"}");DataFiles.ValidateCharacter(legacyGeneration);Check(legacyGeneration.GenerationFormat=="Auto"&&legacyGeneration.GenerationDetail=="Équilibré","anciens profils reçoivent des réglages de génération neutres");
 genData.Settings.GenerationHotkey=genData.Settings.Hotkey;DataFiles.Validate(genData);Check(genData.Settings.GenerationHotkey=="Désactivé","raccourci génération identique à correction refusé");genData.Settings.GenerationHotkey=genData.Settings.RephraseHotkey;DataFiles.Validate(genData);Check(genData.Settings.GenerationHotkey=="Désactivé","raccourci génération identique à reformulation refusé");
 Check(GenerationGuard.Check("/do Une odeur de fumée flotte autour du véhicule.","Auto")==null,"génération automatique accepte une description do");
 Check(GenerationGuard.Check("/me range son téléphone.","/me")==null&&GenerationGuard.Check("/ame range son téléphone.","/ame")==null,"formats me et ame autorisés");
 Check(GenerationGuard.Check("*range son téléphone*","Entre étoiles")==null,"format étoiles autorisé");
 Check(GenerationGuard.Check("/do Une odeur persiste.","/me")!=null,"format forcé non respecté bloqué");
 Check(GenerationGuard.Check("Voici : /do Une odeur persiste.","Auto")!=null,"commentaire avant génération bloqué");
 Check(GenerationGuard.Check("/me bouge.\n/do Bruit.","Auto")!=null&&GenerationGuard.Check("/me bouge. /do Bruit.","Auto")!=null,"plusieurs commandes générées bloquées");
 Check(GenerationGuard.Check("/quit","Auto")!=null&&GenerationGuard.Check("/me /quit","Auto")!=null,"commande externe au RP bloquée");
 Check(GenerationGuard.Check("**range son téléphone**","Entre étoiles")!=null,"délimiteurs étoiles invalides bloqués");

 }
}
