using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ReplyAI.Updates;
namespace ParoleRP {
 public sealed class PublishedUpdate {public string Version,Tag,Notes,DownloadUrl,Sha256;public long Size;}
 public sealed class UpdateClient {
  public static string CurrentVersion {get{return typeof(Entry).Assembly.GetName().Version.ToString(3);}}
  public const string ReleasesUrl="https://github.com/abdelrazak27/ReplyAI/releases";
  static HttpClient Client(){var c=new HttpClient{Timeout=TimeSpan.FromMinutes(5)};c.DefaultRequestHeaders.UserAgent.ParseAdd("ReplyAI/"+CurrentVersion);return c;}
  public static PublishedUpdate ParseRelease(string json){var d=UpdatePackage.Json().Deserialize<Dictionary<string,object>>(json);if(d==null||Convert.ToBoolean(d["draft"])||Convert.ToBoolean(d["prerelease"]))throw new InvalidDataException("Publication invalide.");string tag=Convert.ToString(d["tag_name"]);UpdatePackage.ParseVersion(tag);var assets=((IEnumerable)d["assets"]).Cast<Dictionary<string,object>>().Where(a=>Convert.ToString(a["name"])==UpdatePackage.AssetName).ToList();if(assets.Count!=1)throw new InvalidDataException("Le paquet Windows n’est pas disponible.");var asset=assets[0];string url=Convert.ToString(asset["browser_download_url"]),digest=Convert.ToString(asset["digest"]);long size=Convert.ToInt64(asset["size"]);if(url!="https://github.com/"+UpdatePackage.Repository+"/releases/download/"+tag+"/"+UpdatePackage.AssetName||!digest.StartsWith("sha256:")||!UpdatePackage.ValidHash(digest.Substring(7))||size<1||size>UpdatePackage.MaxArchiveBytes)throw new InvalidDataException("Paquet ou somme de contrôle invalide.");return new PublishedUpdate{Version=UpdatePackage.ParseVersion(tag).ToString(3),Tag=tag,Notes=d.ContainsKey("body")?Convert.ToString(d["body"]):"",DownloadUrl=url,Sha256=digest.Substring(7),Size=size};}
  public async Task<PublishedUpdate> Check(){using(var client=Client())using(var cancel=new CancellationTokenSource(15000))using(var response=await client.GetAsync("https://api.github.com/repos/"+UpdatePackage.Repository+"/releases/latest",cancel.Token)){if((int)response.StatusCode==404)throw new IOException("Aucune version publiée pour le moment.");if((int)response.StatusCode==403||(int)response.StatusCode==429)throw new IOException("GitHub limite les vérifications. Réessayez plus tard.");if(!response.IsSuccessStatusCode)throw new IOException("GitHub est indisponible ("+(int)response.StatusCode+").");string json=await response.Content.ReadAsStringAsync();if(json.Length>2097152)throw new InvalidDataException("Réponse GitHub trop volumineuse.");return ParseRelease(json);}}
  public async Task<string> Download(PublishedUpdate update,string cache,IProgress<int> progress){string root=Path.Combine(cache,Guid.NewGuid().ToString("N"));Directory.CreateDirectory(root);string zip=Path.Combine(root,"package.zip");using(var client=Client())using(var cancel=new CancellationTokenSource(TimeSpan.FromMinutes(5)))using(var response=await client.GetAsync(update.DownloadUrl,HttpCompletionOption.ResponseHeadersRead,cancel.Token)){response.EnsureSuccessStatusCode();using(var input=await response.Content.ReadAsStreamAsync())using(var output=new FileStream(zip,FileMode.CreateNew,FileAccess.Write,FileShare.None,81920,true)){byte[] buffer=new byte[81920];long total=0;int n;while((n=await input.ReadAsync(buffer,0,buffer.Length,cancel.Token))>0){total+=n;if(total>update.Size)throw new InvalidDataException("Téléchargement trop volumineux.");await output.WriteAsync(buffer,0,n,cancel.Token);progress.Report((int)(total*99/update.Size));}if(total!=update.Size)throw new IOException("Téléchargement incomplet.");}}await Task.Run(()=>UpdatePackage.Extract(zip,Path.Combine(root,"payload"),update.Version,update.Sha256));progress.Report(100);return root;}
 }
}
