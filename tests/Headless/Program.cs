using FolderVerse;
using System.Text.Json;

void Check(bool ok,string message){if(!ok)throw new Exception(message);Console.WriteLine("PASS "+message);}
var error=new StringWriter();
Check(HeadlessRunner.Run(new[]{"--headless","--max-turns","401"},new StringWriter(),error)==2,"Reject analysis budget above400");
Check(HeadlessRunner.Run(new[]{"--headless","--bogus","1"},new StringWriter(),error)==2,"Reject unknown flags");
var argsForRun=new[]{"--headless","--seed","42","--cast-seed","8","--players","4","--max-turns","2"};
string Run(){var output=new StringWriter();var stderr=new StringWriter();Check(HeadlessRunner.Run(argsForRun,output,stderr)==0 && stderr.ToString()=="","Headless completes without graphics or errors");return output.ToString();}
var a=Run().Split('\n',StringSplitOptions.RemoveEmptyEntries).Select(s=>JsonDocument.Parse(s)).ToArray();
var b=Run().Split('\n',StringSplitOptions.RemoveEmptyEntries).Select(s=>JsonDocument.Parse(s)).ToArray();
Check(a[0].RootElement.GetProperty("equipmentMode").GetString()=="Materials" && a[0].RootElement.GetProperty("rawEnabled").GetBoolean(),"New headless world uses nine materials and raw resources");
Check(a[0].RootElement.GetRawText()==b[0].RootElement.GetRawText(),"Same seeds reproduce initial state");
Check(a[^1].RootElement.GetProperty("turn").GetInt32()==2 && !a[^1].RootElement.GetProperty("won").GetBoolean() && a[^1].RootElement.GetProperty("stopReason").GetString()=="turn_limit_convergence_undetermined","Stop exactly at budget without declaring nonconvergence");
Check(a[^1].RootElement.GetProperty("ranking").GetRawText()==b[^1].RootElement.GetProperty("ranking").GetRawText(),"Same seeds reproduce resulting ranking");
Check(a[^1].RootElement.GetProperty("ranking").EnumerateArray().All(r=>r.TryGetProperty("Ruler",out _) && r.TryGetProperty("Cells",out _) && r.TryGetProperty("Rank",out _)),"JSON ranking includes ruler, sector count and rank");
foreach(var doc in a.Concat(b))doc.Dispose();
