using System;using System.Collections.Generic;using ParoleRP;
class LocalTranslatorTests {
 static int Main(string[] args){try{using(var local=new LocalChatTranslator(args[0])){
  for(int i=0;i<2;i++){
   var result=local.Translate(new List<ChatLine>{new ChatLine{id="a",text="Marcus Hill says: We need to leave."},new ChatLine{id="b",text="/do Smoke fills the room."}}).GetAwaiter().GetResult();
   if(result.Count!=2||result[0].id!="a"||!result[0].text.StartsWith("Marcus Hill dit : ")||!result[1].text.StartsWith("/do "))throw new Exception("Offline protocol mismatch");
  }
 }Console.WriteLine("PASS C# to real offline worker, warm reuse, ordered IDs and RP prefixes. No API keys.");return 0;}catch(Exception error){Console.Error.WriteLine(error);return 1;}}
}
