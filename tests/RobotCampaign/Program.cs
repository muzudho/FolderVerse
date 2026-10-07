using FolderVerse;
using Microsoft.Xna.Framework;
try
{
static void Check(bool ok,string message){if(!ok)throw new Exception(message);}
WorldSetup World(int seed=0)
{
    var w=new WorldSetup();w.SetWorld(seed);w.SetCast(123);w.SetPlacement(456);w.SelectPlayer(0);
    w.Robots.Initialize(w.Nodes.All.Length,w.ActiveCount);return w;
}
void Place(WorldSetup w,int owner,Node node){node.Owner=owner;w.ConquerorLocations[owner]=node.Cell;w.ConquerorPoints[owner]=node.Center;}
void Add(WorldSetup w,Node node,int owner,int count){for(int i=0;i<count;i++)w.Robots.Nodes[node.Id].Add(w.Robots.Create(owner,RobotParts.Complete));}
var w=World();var a=w.Nodes.All.First(n=>w.Routes.Neighbors(n).Length>0);var route=w.Routes.Neighbors(a)[0];var b=w.Nodes.At(route.Target,route.Entry);Place(w,0,a);b.Owner=-1;Add(w,a,0,3);
var people=a.Population.People[0];w.Campaign.Advance(w,b.Id,2,false);Check(w.Population.Turn==1 && b.Owner==0 && w.Nodes.Current(0).Id==b.Id,"Neutral flag raising / turn");Check(a.Population.People[0]==people,"Robots do not consume population");Check(w.Robots.Nodes[a.Id].Count==1 && w.Robots.Nodes[b.Id].Count==2,"Robot march count");
w=World();a=w.Nodes.All.First(n=>w.Routes.Neighbors(n).Length>0);route=w.Routes.Neighbors(a)[0];b=w.Nodes.At(route.Target,route.Entry);Place(w,0,a);Place(w,1,b);Add(w,a,0,3);Add(w,b,1,3);w.Campaign.RobotBattles.Rules=new(){SegmentTurns=1,NoWillTurns=1000,PersuasionPercent=0};
w.Campaign.Resolve(w,new[]{new March(0,b.Id,3)});Check(w.Population.Turn==1 && w.Campaign.RobotBattles.LockedNode(b.Id),"Battle persisted");var battle=w.Campaign.RobotBattles.Encounters.Single().Battles.Single();int turn=battle.Turn;var ammunition=battle.Units.Select(u=>(u.Id,u.Daggers,u.Rifles,u.Shields)).ToArray();
b.Population.Conversion[0]=50;b.Population.BirthPercent=100;var before=(long[])b.Population.People.Clone();int age=w.Robots.Workshops[b.Id].ProductionAge;
w.Campaign.Resolve(w,Array.Empty<March>());Check(battle.Turn==turn+1 && w.Population.Turn==2,"Continued same battle / one global turn");Check(b.Population.People.SequenceEqual(before) && w.Robots.Workshops[b.Id].ProductionAge==age,"Locked domestic phase");Check(battle.Frames.Count>0 && battle.Units.All(u=>ammunition.Any(v=>v.Id==u.Id) || u.Role==BattleRole.Queen),"Continuation preserves identities / frames");
int willBefore=battle.NoWillCount;bool noTroopsRejected=false;try{w.Campaign.RobotBattles.SendReinforcement(w,0,a.Id,w.Campaign.RobotBattles.Encounters[0].Id);}catch(InvalidOperationException){noTroopsRejected=true;}Check(noTroopsRejected && battle.NoWillCount==willBefore,"Empty reinforcements cannot reset will");
Add(w,a,0,1);w.Campaign.RobotBattles.SendReinforcement(w,0,a.Id,w.Campaign.RobotBattles.Encounters[0].Id);Check(w.Robots.Nodes[a.Id].Count==0 && battle.NoWillCount==willBefore && battle.Army(0).Waiting.Any(u=>u.Robot!=null),"World reinforcement queue does not reset will before entry");w.Campaign.RobotBattles.Check(w);
w=World();a=w.Nodes.All.First(n=>w.Routes.Neighbors(n).Length>0);route=w.Routes.Neighbors(a)[0];b=w.Nodes.At(route.Target,route.Entry);Place(w,0,a);Place(w,1,b);Add(w,a,0,2);Add(w,b,1,2);w.Campaign.RobotBattles.Rules=new(){SegmentTurns=1,NoWillTurns=1000,PersuasionPercent=0};w.Campaign.Resolve(w,new[]{new March(0,b.Id,2),new March(1,a.Id,2)});
Check(w.Campaign.RobotBattles.LockedEdge(a.Id,b.Id) && !w.Campaign.RobotBattles.CanMarch(w,0,b.Id),"Continued edge cannot be crossed");a.Owner=2;b.Owner=3;w.Campaign.Resolve(w,Array.Empty<March>());Check(w.Campaign.RobotBattles.Encounters.All(e=>e.Finished),"Both edge armies lose their homes");
// Real seeded worlds exercise many simultaneous marches and continued battles.
for(int seed=0;seed<4;seed++)
{
    w=new WorldSetup();w.SetWorld(seed);w.SetCast(123);w.SetPlacement(456);w.SelectPlayer(0);w.Campaign.RobotBattles.Rules=new(){SegmentTurns=10,NoWillTurns=10,PersuasionPercent=5};
    for(int t=0;t<12;t++){w.Campaign.Advance(w,-1,0);w.Campaign.RobotBattles.Check(w);Check(w.Population.Turn==t+1,"Global turn increments once");}
}
// Five invaders plus one defender must use multiple boards, preserving all individual IDs.
Node target=null;
for(int seed=0;seed<64 && target==null;seed++){w=World(seed);target=w.Nodes.All.FirstOrDefault(n=>w.Routes.Neighbors(n).Length>=5);}
Check(target!=null,"Five-route fixture");Place(w,5,target);Add(w,target,5,2);var neighbors=w.Routes.Neighbors(target).Take(5).Select(r=>w.Nodes.At(r.Target,r.Entry)).ToArray();
var marches=new List<March>();for(int owner=0;owner<5;owner++){Place(w,owner,neighbors[owner]);Add(w,neighbors[owner],owner,2);marches.Add(new(owner,target.Id,2));}
w.Campaign.RobotBattles.Rules=new(){SegmentTurns=1,NoWillTurns=100,PersuasionPercent=0};w.Campaign.Resolve(w,marches);
Check(w.Campaign.RobotBattles.Encounters.Single().Battles.Count>=2 && w.Campaign.RobotBattles.Encounters.Single().Battles.All(state=>state.Armies.Count<=4),"Six countries split into boards");w.Campaign.RobotBattles.Check(w);
var split=w.Campaign.RobotBattles.Encounters.Single();
foreach(var state in split.Battles)
{
    var survivor=state.Units.First(u=>u.Robot!=null);survivor.Daggers=1;survivor.Rifles=2;survivor.Shields=3;
    foreach(var unit in state.Units.Where(u=>u!=survivor))unit.Fallen=true;
    foreach(var army in state.Armies)while(army.Waiting.TryDequeue(out var unit)){unit.Fallen=true;state.Units.Add(unit);}
    state.NoWillCount=3;state.Finished=true;state.Winner=survivor.Owner;
}
w.Campaign.Resolve(w,Array.Empty<March>());Check(split.Battles.Count==1 && split.Battles[0].Armies.Count==2 && split.Battles[0].NoWillCount==3 && split.Battles[0].Turn==1 && split.Battles[0].Units.All(u=>u.Daggers==1 && u.Rifles==2 && u.Shields==3),"Merge preserves survivors, equipment and battle history");
for(int t=0;t<120;t++){w.Campaign.Resolve(w,Array.Empty<March>());w.Campaign.RobotBattles.Check(w);}
Console.WriteLine("PASS: independent population, neutral acquisition, robot marches, continuity, domestic locks, reinforcement, blocked edges, lost homes, four seeded campaigns and six-country split/merge.");
}
catch(Exception error){Console.Error.WriteLine(error);Environment.ExitCode=1;}
