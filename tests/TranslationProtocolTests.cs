using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using ParoleRP;

class TranslationProtocolTests {
 static int Main(string[] args){
  if(args.Length==0){Serve();return 0;}
  try{Run().GetAwaiter().GetResult();return 0;}
  catch(Exception error){Console.Error.WriteLine(error);return 1;}
 }
 static async Task Run(){
  using(var api=new GroqApi(typeof(TranslationProtocolTests).Assembly.Location,2500)){
   var person=new Character{LanguageMode="FR-EN"};
   var result=await api.Correct("FAKE_TEST_KEY_123456","qwen/qwen3.8-27b",person,"","j'arrive");
   if(result.Text!="I'm on my way."||result.Warning!=null)throw new Exception("Character language mode was not sent through Correct().");
   person.LanguageMode="NORMAL";
   result=await api.Correct("FAKE_TEST_KEY_123456","qwen/qwen3.8-27b",person,"","j'arrive");
   if(result.Text!="J'arrive.")throw new Exception("NORMAL mode remained stuck in English.");
   var lines=await api.TranslateChat(new List<ApiConnection>{new ApiConnection{Provider="Groq",Model="qwen/qwen3.8-27b",Key="FAKE_TEST_KEY_123456"}},new List<ChatLine>{new ChatLine{id="a",text="Hello"}});
   if(lines.Count!=1||lines[0].id!="a"||lines[0].text!="Bonjour")throw new Exception("Chat request/response mapping failed.");
   Console.WriteLine("PASS public correction entry point, mode switch on existing channel, and chat batch JSON protocol. Fake worker only.");
  }
 }
 static void Serve(){
  using(var input=new StreamReader(Console.OpenStandardInput(),new UTF8Encoding(false)))
  using(var output=new StreamWriter(Console.OpenStandardOutput(),new UTF8Encoding(false)){AutoFlush=true}){
   string line;
   while((line=input.ReadLine())!=null){
    var request=DataFiles.Json().Deserialize<Dictionary<string,object>>(line);
    string operation=Convert.ToString(request["op"]);
    string text;
    if(operation=="chat"){
     var rows=DataFiles.Json().Deserialize<List<ChatLine>>(Convert.ToString(request["text"]));
     rows.ForEach(row=>row.text="Bonjour");text=DataFiles.Json().Serialize(rows);
    }else text=Convert.ToString(request["targetLanguage"])=="en"?"I'm on my way.":"J'arrive.";
    output.WriteLine(DataFiles.Json().Serialize(new{ok=true,operation=operation,text=text,inputTokens=4,outputTokens=4}));
   }
  }
 }
}
