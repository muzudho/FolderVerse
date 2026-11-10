using FolderVerse;
using System.Reflection;
static void Check(bool ok,string text){if(!ok)throw new Exception(text);}
var w=new WorldSetup();w.SetParticipantCounts(0,3);w.SetWorld(1);w.SetCast(2);w.SetPlacement(3);w.SelectPlayer(0);
w.Robots.Initialize(w.Nodes.All.Length,w.ActiveCount);w.Robots.ConfigureFactories(w);
var c=w.Campaign.RobotBattles;
var b=new BattleState(1,new()){Kind=BattleKind.Node,Finished=true,Winner=-1};
b.AddArmy(0,0,new[]{new Robot(10001,0,RobotParts.Complete),new Robot(10002,0,RobotParts.Complete)});
b.AddArmy(2,1,new[]{new Robot(10003,2,RobotParts.Complete)});
b.Army(0).Withdrawing=true;b.Army(0).NoWillCount=b.Rules.NoWillTurns;
var e=new RobotEncounter{Id=10,Target=1,Defender=2,Finished=true};e.Battles.Add(b);c.Encounters.Add(e);
Check(c.CpuFailedForce(w,0,1)==2,"Legacy fallback counts original force");
foreach(var army in b.Armies)while(army.Waiting.TryDequeue(out var soldier))b.Units.Add(soldier);
var homes=(Dictionary<long,int>)typeof(RobotCampaign).GetField("_homeOwners",BindingFlags.NonPublic|BindingFlags.Instance)!.GetValue(c)!;
foreach(var u in b.Units.Concat(b.Armies.SelectMany(a=>a.Waiting)).Where(u=>u.Owner==0)){homes[u.Id]=0;u.Owner=2;}
Check(c.CpuFailedForce(w,0,1)==0,"Fixture reproduces converted force loss");
typeof(RobotCampaign).GetMethod("CaptureCpuAssaultForces",BindingFlags.NonPublic|BindingFlags.Instance)!.Invoke(c,new object[]{e});
Check(c.CpuFailedForce(w,0,1)==2,"Original force survives conversion");
Check(c.CpuFailedForce(w,1,1)==0 && c.CpuFailedForce(w,0,2)==0,"Faction and target isolation");
homes.Clear();AutoSaveStore.Write(args[0],w);var restored=AutoSaveStore.Read<WorldSetup>(args[0]);
Check(restored.Campaign.RobotBattles.CpuFailedForce(restored,0,1)==2,"Full save roundtrip");
b.Army(0).NoWillCount=0;Check(c.CpuFailedForce(w,0,1)==0,"Non-NoWill defeat excluded");
b.Army(0).NoWillCount=b.Rules.NoWillTurns;c.Encounters.Clear();Check(c.CpuFailedForce(w,0,1)==0,"No extended memory");
var raw=AutoSaveStore.Read<object>(args[1]);var legacy=raw is WorldSetup direct?direct:(WorldSetup)raw.GetType().GetProperty("World")!.GetValue(raw)!;Check(legacy.Campaign.RobotBattles.Encounters.All(x=>x.CpuAssaultForces!=null),"Legacy save migration");
Console.WriteLine("PASS conversion, legacy fallback/migration, save roundtrip, target/faction, withdrawal and encounter lifetime");