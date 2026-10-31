using System;
using System.IO;
using System.Linq;
using System.Windows.Controls;
using System.Collections.Generic;
namespace ParoleRP {
 public sealed partial class Desktop {
  void VerifyUsage(string folder){usage=new UsageTracker(Path.Combine(folder,"usage-preview"));var report=new ApiReport{ok=true,operation="correct",usageEvents=new List<TokenUsage>{new TokenUsage{inputTokens=400,outputTokens=30,cachedTokens=100}},routeEvents=new List<RouteEvent>{new RouteEvent{connectionId="primary",quotaGroup="principal",model=data.Settings.Model,state="quota",code=429,retrySeconds=120}}};ReceiveReport(report);ReceiveReport(new ApiReport{ok=true,operation="rephrase",usageEvents=new List<TokenUsage>{new TokenUsage{inputTokens=900,outputTokens=100}},routeEvents=new List<RouteEvent>{new RouteEvent{connectionId="primary",quotaGroup="principal",model=OperationRoutes.RephraseModel,state="ready"}}});page=7;Render();ChromeScreenshot(folder,"reply-usage");if(!Elements(host).OfType<TextBlock>().Any(t=>t.Text=="1"))throw new Exception("Compteurs de session absents.");var selector=Elements(host).OfType<ComboBox>().Single();selector.SelectedIndex=1;if(usagePeriod!=1)throw new Exception("Période non sélectionnable.");selector.SelectedIndex=2;if(usagePeriod!=2)throw new Exception("Mois non sélectionnable.");page=4;Render();ChromeScreenshot(folder,"reply-key-states");if(!Elements(host).OfType<TextBlock>().Any(t=>t.Text.Contains("Quota")))throw new Exception("État de quota absent.");}
 }
}
