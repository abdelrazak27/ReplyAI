using System;
namespace ParoleRP {
 public static class ConnectionDisplay {
  public static string KeySuffix(string key){return String.IsNullOrEmpty(key)?"Non configurée":key.Length<7?"••••":"•••• "+key.Substring(key.Length-6);}
  public static string ModelLabel(string model){return model=="qwen/qwen3.8-27b"?"Qwen 3.8 27B":model=="openai/gpt-oss-120b"?"GPT-OSS 120B":model=="openai/gpt-oss-20b"?"GPT-OSS 20B":model;}
 }
}
