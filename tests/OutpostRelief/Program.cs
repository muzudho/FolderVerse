using FolderVerse;
using Microsoft.Xna.Framework;
using System.Reflection;
AppContext.SetData("APP_CONTEXT_BASE_DIRECTORY",Environment.GetEnvironmentVariable("FOLDERVERSE_PROBE_CONTENT_BASE"));
const BindingFlags F=BindingFlags.Instance|BindingFlags.NonPublic;
void Check(bool ok,string message){if(!ok)throw new Exception(message);Console.WriteLine("PASS: "+message);}
WorldSetup World(){var w=new WorldSetup{EquipmentMode=RobotEquipmentMode.Materials};w.SetWorld(0);w.SetCast(123);w.SetPlacement(456);w.Relations.Reset();foreach(var n in w.Nodes.All)n.Owner=0;w.Robots.Initialize(w.Nodes.All.Length,w.ActiveCount);w.Robots.ConfigureFactories(w);return w;}
int[] Path(WorldSetup w){var campaign=w.Campaign.RobotBattles;foreach(var a in w.Nodes.All)foreach(int b in campaign.MarchTargets(w,a.Id))foreach(int c in campaign.MarchTargets(w,b).Where(c=>c!=a.Id))foreach(int d in campaign.MarchTargets(w,c).Where(d=>d!=a.Id && d!=b))if(!campaign.MarchTargets(w,a.Id).Contains(c) && !campaign.MarchTargets(w,a.Id).Contains(d) && !campaign.MarchTargets(w,b).Contains(d))return new[]{a.Id,b,c,d};throw new Exception("No four-node path");}
Robot Add(WorldSetup w,int home){var r=w.Robots.Create(0,RobotParts.Complete) with {InterceptOnly=true};var store=w.Robots.Nodes[home];typeof(RobotStore).GetMethod("SetCapacity",F)!.Invoke(store,new object[]{60});store.Add(r);return r;}
RobotEncounter Encounter(WorldSetup w,BattleKind kind,int from,int to){var e=new RobotEncounter{Id=w.Campaign.RobotBattles.Encounters.Count+1,Kind=kind,Source=from,Target=to,Defender=kind==BattleKind.Node?0:-1};var b=new BattleState(e.Id,new(){NoWillTurns=1000,PersuasionPercent=0}){Kind=kind,Defender=e.Defender,EquipmentMode=RobotEquipmentMode.Materials};b.AddArmy(1,from,new[]{w.Robots.Create(1,RobotParts.Complete)});b.BuildTerrain(BattleTerrain.Field);b.Deploy();e.Battles.Add(b);w.Campaign.RobotBattles.Encounters.Add(e);return e;}
void Dispatch(WorldSetup w)=>typeof(RobotCampaign).GetMethod("DispatchInterceptRelief",F)!.Invoke(w.Campaign.RobotBattles,new object[]{w});
foreach(var scenario in new[]{(BattleKind.Edge,0,0),(BattleKind.Node,1,4),(BattleKind.Edge,1,8),(BattleKind.Node,2,12),(BattleKind.Edge,2,16)}){
 var w=World();var path=Path(w);w.Nodes.All[path[0]].Purpose=NodePurpose.Frontline;var robot=Add(w,path[0]);var e=Encounter(w,scenario.Item1,scenario.Item1==BattleKind.Node?path[scenario.Item2]:path[scenario.Item2+1],path[scenario.Item2]);Dispatch(w);
 var unit=e.Battles.SelectMany(b=>b.Units.Concat(b.Armies.SelectMany(a=>a.Waiting))).Single(u=>u.Id==robot.Id);
 Check(unit.ArrivalTurn==scenario.Item3,$"{scenario.Item1} distance {scenario.Item2}: delay {scenario.Item3}");
 if(scenario.Item3>0){var board=e.Battles.Single();board.Capture(BattlePhase.Place);Check(board.Frames.Last().Relief!.Single().Remaining==scenario.Item3,"Replay records pending arrival");var restored=AutoSaveStore.Restore<WorldSetup>(AutoSaveStore.Capture(w));Check(restored.Campaign.RobotBattles.Encounters.Single().Battles.Single().Armies.SelectMany(a=>a.Waiting).Single(u=>u.Id==robot.Id).ArrivalTurn==scenario.Item3,"Pending relief survives save");board.Turn=scenario.Item3-1;Check(!board.ReadyWaiting(board.Army(0)).Any(),"Not ready before delay");board.Turn++;Check(board.ReadyWaiting(board.Army(0)).Single().Id==robot.Id,"Ready exactly at arrival");}
}
{
 var w=World();var path=Path(w);var robot=Add(w,path[0]);var e=Encounter(w,BattleKind.Node,path[1],path[1]);Dispatch(w);Check(w.Robots.Nodes[path[0]].Robots.Any(r=>r.Id==robot.Id),"Ordinary location cannot send distant relief");
 w.Nodes.All[path[0]].Purpose=NodePurpose.Frontline;w.Nodes.All[path[1]].Owner=1;Dispatch(w);Check(w.Robots.Nodes[path[0]].Robots.Any(r=>r.Id==robot.Id),"Cannot relieve hostile location");
}
{
 var w=World();var path=Path(w);w.Nodes.All[path[0]].Purpose=NodePurpose.Frontline;for(int i=0;i<7;i++)Add(w,path[0]);var first=Encounter(w,BattleKind.Edge,path[1],path[0]);var second=Encounter(w,BattleKind.Edge,path[2],path[1]);Dispatch(w);int Count(RobotEncounter e)=>e.Battles.SelectMany(b=>b.Units.Concat(b.Armies.SelectMany(a=>a.Waiting))).Count(u=>u.Owner==0);Check(new[]{Count(first),Count(second)}.Order().SequenceEqual(new[]{3,4}),"Seven interceptors divide 3/4 between two battles");Check(first.Battles.Concat(second.Battles).SelectMany(b=>b.Units.Concat(b.Armies.SelectMany(a=>a.Waiting))).Where(u=>u.Owner==0).Select(u=>u.Id).Distinct().Count()==7,"No duplicate dispatch");
}
{
 var board=new BattleState(3){Kind=BattleKind.Edge,EquipmentMode=RobotEquipmentMode.Materials};board.AddArmy(0,0,Array.Empty<Robot>());board.AddArmy(1,1,Array.Empty<Robot>());board.Units.Add(new(){Id=1,Owner=1,Position=new Point(6,6)});var pending=new BattleUnit{Id=2,Owner=0,ArrivalTurn=16};board.Army(0).Waiting.Enqueue(pending);BattleTurnResolver.Finish(board);Check(board.Finished && board.Winner==1 && pending.Retreated,"Unarrived relief cannot prolong decided battle");var saved=AutoSaveStore.Capture(board);foreach(var entry in saved.Objects){entry.Fields?.Remove("ArrivalTurn");entry.Fields?.Remove("<Relief>k__BackingField");}var old=AutoSaveStore.Restore<BattleState>(saved);Check(old.Armies.SelectMany(a=>a.Waiting).Single().ArrivalTurn==0,"Old battle saves default to immediate arrival");
}
{
 var board=new BattleState(7,new(){NoWillTurns=1000,PersuasionPercent=0}){Kind=BattleKind.Edge};
 board.AddArmy(0,0,Array.Empty<Robot>());board.AddArmy(1,1,Array.Empty<Robot>());
 board.BuildTerrain(BattleTerrain.Field);var unit=new BattleUnit{Id=20,Owner=0,ArrivalTurn=4};board.Army(0).Waiting.Enqueue(unit);
 board.Turn=3;board.Reinforce();Check(!board.Units.Contains(unit),"Reinforce does not deploy early");board.Turn=4;board.Reinforce();Check(board.Units.Contains(unit) && !board.Army(0).Waiting.Any(),"Reinforce deploys at due turn");board.Check();
}
{
 var w=World();var path=Path(w);w.Nodes.All[path[0]].Purpose=NodePurpose.Frontline;var robot=Add(w,path[0]);var e=Encounter(w,BattleKind.Node,path[2],path[2]);Dispatch(w);var board=e.Battles.Single();BattleTurnResolver.Finish(board);
 typeof(RobotCampaign).GetMethod("Complete",F)!.Invoke(w.Campaign.RobotBattles,new object[]{w,e});
 Check(w.Robots.Nodes[path[0]].Robots.Any(r=>r.Id==robot.Id),"Unarrived relief returns to its actual origin");
}
{
 var w=World();var path=Path(w);foreach(var n in w.Nodes.All)n.Owner=2;w.Nodes.All[path[0]].Owner=0;w.Nodes.All[path[2]].Owner=0;w.Nodes.All[path[0]].Purpose=NodePurpose.Frontline;

 var robot=Add(w,path[0]);Encounter(w,BattleKind.Node,path[2],path[2]);Dispatch(w);Check(w.Robots.Nodes[path[0]].Robots.Any(r=>r.Id==robot.Id),"Enemy intermediate location blocks relief");
}
{
 var w=World();var path=Path(w);w.Nodes.All[path[0]].Purpose=NodePurpose.Frontline;w.Nodes.All[path[1]].Owner=2;w.Relations.Superiors[2]=0;var robot=Add(w,path[0]);var e=Encounter(w,BattleKind.Node,path[2],path[2]);Dispatch(w);Check(e.Battles.SelectMany(b=>b.Armies.SelectMany(a=>a.Waiting)).Any(u=>u.Id==robot.Id && u.ArrivalTurn==12),"Allied intermediate location permits relief");
}