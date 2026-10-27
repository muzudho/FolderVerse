using FolderVerse;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.Json;
using Microsoft.Xna.Framework;
const BindingFlags flags=BindingFlags.Instance|BindingFlags.NonPublic;
var w=new WorldSetup();w.PrepareTutorialStepOne(1093530084,1306425989,1904727863);w.CompleteLand(601792699);w.SelectPlayer(0);w.HumanPlayers.Add(0);w.SelectTurnRuler(0);
Console.WriteLine($"SAVE: turn={w.Population.Turn} tutorial={w.TutorialStep} seed={w.WorldSeed}/{w.CastSeed}/{w.PlacementSeed} player={w.PlayerSlot}");
var g=(Game1)RuntimeHelpers.GetUninitializedObject(typeof(Game1));
FieldInfo Field(string n)=>typeof(Game1).GetField(n,flags)!;
object Call(string n,params object[] a)=>typeof(Game1).GetMethod(n,flags)!.Invoke(g,a)!;
Field("_setup").SetValue(g,w);Field("_net").SetValue(g,new CubeNet());Field("_invasionSources").SetValue(g,Activator.CreateInstance(Field("_invasionSources").FieldType));Field("_invasionDraft").SetValue(g,new HashSet<long>());
Field("_selectedMoveNode").SetValue(g,-1);Field("_withdrawalEncounter").SetValue(g,-1);Field("_warPlanning").SetValue(g,true);Field("_movementOpen").SetValue(g,true);
void Orders()=>Console.WriteLine("  ORDERS "+string.Join("; ",w.Campaign.PlannedOrders.Select(o=>$"{o.SourceNode}->{o.TargetNode}: {o.Fighters} robots [{string.Join(',',o.RobotIds??Array.Empty<long>())}], pocket={o.PocketOnly}, queen={o.Queen}")));
foreach(var e in JsonDocument.Parse(File.ReadAllText(args.Length>0?Path.GetFullPath(args[0]):Path.Combine(AppContext.BaseDirectory,"fixtures","clicks.json"))).RootElement.EnumerateArray()){
 var d=e.GetProperty("data");var p=d.GetProperty("input").GetProperty("canvas");int x=p.GetProperty("x").GetInt32(),y=p.GetProperty("y").GetInt32();int target=d.GetProperty("state").GetProperty("selectedMoveNode").GetInt32();
 if(x>=968 && target>=0){Field("_selectedMoveNode").SetValue(g,target);Call("BuildInvasionSources",target); Console.WriteLine($"TARGET {target} {w.Nodes.DisplayName(w.Nodes.All[target])}");var rows=(System.Collections.IList)Field("_invasionSources").GetValue(g)!;foreach(var row in rows){var t=row!.GetType();int source=(int)t.GetField("Node")!.GetValue(row)!;Console.WriteLine($"  SOURCE {source} pocket={t.GetField("Queen")!.GetValue(row)} complete=[{string.Join(',',((Robot[])Call("InvasionPreviewRobots",row)).Select(r=>r.Id))}]");}}
 else if(y>=914 && y<=962){Call("ReserveInvasion");Console.WriteLine($"CONFIRM #{e.GetProperty("sequence")}");Orders();}
 else if(y>=300 && y<=840){Call("InvasionClick",new Point(x,y));}
}
Console.WriteLine("FINAL");Orders();
var homeIds=w.Robots.Nodes.SelectMany(s=>s.Robots).Select(r=>r.Id).ToHashSet();
var expected=w.Campaign.PlannedOrders.SelectMany(o=>o.RobotIds??Array.Empty<long>()).ToArray();
Console.WriteLine($"Selected={expected.Length}, distinct={expected.Distinct().Count()}");
w.Campaign.Advance(w,-1,0);
Console.WriteLine($"Player marches: {string.Join(';',w.Campaign.RobotBattles.LastMarches.Where(m=>m.Owner==0))}");
Console.WriteLine($"Scenes={w.Campaign.RobotBattles.Scenes.Count}; player units={string.Join(',',w.Campaign.RobotBattles.Scenes.SelectMany(b=>b.Frames).SelectMany(f=>f.Units).Where(u=>u.Owner==0).Select(u=>u.Id).Distinct())}");

foreach(var b in w.Campaign.RobotBattles.Scenes){var f=b.Frames.FirstOrDefault();Console.WriteLine($"BATTLE {b.Id}: {b.SourceNode}->{b.TargetNode} defender={b.Defender} armies=[{string.Join(';',b.Armies.Select(a=>$"owner={a.Owner}/home={a.HomeNode}"))}] first=[{string.Join(',',f?.Units.Select(u=>$"{u.Id}:{u.Owner}")??Array.Empty<string>())}] ");}