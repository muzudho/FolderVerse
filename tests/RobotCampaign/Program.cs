using FolderVerse;
using Microsoft.Xna.Framework;
try
{
static void Check(bool ok,string message){if(!ok)throw new Exception(message);}
WorldSetup World(int seed=0)
{
    var w=new WorldSetup();w.SetWorld(seed);w.SetCast(123);w.SetPlacement(456);w.SelectPlayer(0);
    w.Salvage.Reset();w.Robots.Initialize(w.Nodes.All.Length,w.ActiveCount);return w;
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
{
var controlled=new WorldSetup();controlled.SetWorld(1);controlled.SetCast(123);controlled.SetPlacement(456);controlled.SelectPlayer(0);controlled.Relations.SetSuperior(1,0);controlled.Relations.SetSuperior(2,1);
int before=controlled.Population.Turn;controlled.Campaign.Advance(controlled,-1,0);if(controlled.PlayerSlot==0 || controlled.Population.Turn!=before)throw new Exception("Subordinate turn skipped");
controlled.Campaign.Advance(controlled,-1,0);if(controlled.Population.Turn!=before)throw new Exception("Party committed too early");
controlled.Campaign.Advance(controlled,-1,0);if(controlled.PlayerSlot!=0 || controlled.Population.Turn!=before+1)throw new Exception("Party turn commit failed");
Console.WriteLine("PASS: leader and nested subordinate actions commit one shared turn");
}
{
    var world=new WorldSetup();world.SetWorld(2);world.SetCast(123);world.SetPlacement(456);world.SelectPlayer(0);
    world.Robots.Initialize(world.Nodes.All.Length,world.ActiveCount);
    var target=world.Nodes.All.First(n=>world.Routes.Neighbors(n).Select(r=>world.Nodes.At(r.Target,r.Entry).Id).Distinct().Count()>=3);
    var homes=world.Routes.Neighbors(target).Select(r=>world.Nodes.At(r.Target,r.Entry)).DistinctBy(n=>n.Id).Take(3).ToArray();
    target.Owner=1;foreach(var home in homes){home.Owner=0;for(int i=0;i<3;i++)world.Robots.Nodes[home.Id].Add(world.Robots.Create(0,RobotParts.Complete));}
    world.ConquerorLocations[0]=homes[0].Cell;world.ConquerorPoints[0]=homes[0].Center;
    for(int i=0;i<3;i++)world.Robots.Nodes[target.Id].Add(world.Robots.Create(1,RobotParts.Complete));
    world.Campaign.RobotBattles.Rules=new(){SegmentTurns=1,NoWillTurns=1000,PersuasionPercent=0};
    int turn=world.Population.Turn;
    foreach(var home in homes)world.Campaign.Plan(new(0,target.Id,2,home.Id,false));
    if(world.Population.Turn!=turn || homes.Any(h=>world.Robots.Nodes[h.Id].Count!=3))throw new Exception("Planning changed world");
    world.Campaign.Advance(world,-1,0,false);
    var encounter=world.Campaign.RobotBattles.Encounters.Single();
    var soldiers=encounter.Battles.SelectMany(b=>b.Units.Concat(b.Armies.SelectMany(a=>a.Waiting))).Where(u=>u.Owner==0).ToArray();
    if(soldiers.Length!=6 || soldiers.Any(u=>u.Role==BattleRole.Queen))throw new Exception("Multi-source node invasion lost robots or sent queen");
    if(world.Nodes.Current(0).Id!=homes[0].Id || world.Population.Turn!=turn+1)throw new Exception("Node attack moved queen or advanced multiple turns");
    world.Campaign.RobotBattles.Check(world);
    Console.WriteLine("PASS: planning is inert; three sources merge six soldiers; node invasion leaves conqueror at home; one end-turn commit");
}
{
    void Assert(bool ok,string message){if(!ok)throw new Exception(message);}
    WorldSetup Setup(){var x=new WorldSetup();x.SetWorld(2);x.SetCast(123);x.SetPlacement(456);x.SelectPlayer(0);x.Salvage.Reset();x.Robots.Initialize(x.Nodes.All.Length,x.ActiveCount);return x;}
    var x=Setup();var home=x.Nodes.All.First(n=>x.Routes.Neighbors(n).Length>0);var route=x.Routes.Neighbors(home)[0];var target=x.Nodes.At(route.Target,route.Entry);
    home.Owner=0;target.Owner=1;x.ConquerorLocations[0]=home.Cell;x.ConquerorPoints[0]=home.Center;
    for(int i=0;i<12;i++)x.Robots.Nodes[home.Id].Add(x.Robots.Create(0,RobotParts.Complete));
    for(int i=0;i<11;i++)x.Robots.Nodes[target.Id].Add(x.Robots.Create(0,RobotParts.Body));
    var defender=x.Robots.Create(1,RobotParts.Complete);x.Robots.Nodes[target.Id].Add(defender);
    var campaign=x.Campaign.RobotBattles;campaign.Rules=new(){SegmentTurns=1,NoWillTurns=1000,PersuasionPercent=0};
    var original=x.Robots.Nodes.SelectMany(s=>s.Robots).Select(r=>r.Id).Order().ToArray();
    campaign.Advance(x,new[]{new March(0,target.Id,2,home.Id,false)});
    Assert(x.Robots.Nodes[home.Id].ReservedReturns==2 && !x.Robots.Nodes[home.Id].HasSpace,"Departed robots reserve their exact home slots");
    bool blocked=false;try{x.Robots.Nodes[home.Id].Add(x.Robots.Create(0,RobotParts.Head));}catch(InvalidOperationException){blocked=true;}Assert(blocked,"Return slots cannot be filled");
    x.Robots.Workshops[home.Id].Configure(RobotParts.Head,1);
    x.Robots.Advance((a,b)=>false,n=>x.Nodes.All[n].Owner,1,campaign.LockedNode);
    Assert(x.Robots.Nodes[home.Id].Count==10 && x.Robots.Workshops[home.Id].Waiting,"Production waits for reserved return slots");
    var cargo=x.Robots.Nodes[target.Id].Robots.First();
    x.Robots.Transport.Resolve(x.Robots,new[]{new RobotShipment(target.Id,home.Id,cargo.Id,cargo.Parts)},(a,b)=>true);
    Assert(x.Robots.Nodes[home.Id].Count==10 && x.Robots.Nodes[target.Id].Robots.Any(r=>r.Id==cargo.Id),"Transport cannot consume return slots");
    x.Robots.Workshops[home.Id].Paused=true;
    var encounter=campaign.Encounters.Single();var board=encounter.Battles.Single();
    foreach(var army in board.Armies)while(army.Waiting.TryDequeue(out var u))board.Units.Add(u);
    foreach(var u in board.Units)if(u.Owner==1)u.Fallen=true;
    board.Winner=0;board.Finished=true;
    campaign.Advance(x,Array.Empty<March>());
    var stored=x.Robots.Nodes.Concat(x.Robots.Retinues).SelectMany(s=>s.Robots).ToArray();
    Assert(stored.Select(r=>r.Id).Order().SequenceEqual(original),"Battle preserves all robot IDs including defeated robots");
    var damaged=stored.Single(r=>r.Id==defender.Id);
    Assert(new[]{RobotParts.Head,RobotParts.Body,RobotParts.Legs}.Count(damaged.IsDamaged)==1 && damaged.Origin(RobotParts.Head)==1,"Fallen robot damages one part without rewriting country");
    Assert(x.Robots.Nodes[home.Id].Count==12 && x.Robots.Nodes[target.Id].Count==12,"Full destination swaps without overflow");
    Assert(x.Robots.Nodes[home.Id].Robots.Count(r=>r.Parts==RobotParts.Body)==2,"Swap brings native-part residents home first");
    Assert(x.Robots.Nodes.All(s=>s.ReservedReturns==0),"Completed battle releases all return reservations");campaign.Check(x);
    // Multiple sources of the same country must not collapse into one army home.
    x=Setup();target=x.Nodes.All.First(n=>x.Routes.Neighbors(n).Select(r=>x.Nodes.At(r.Target,r.Entry).Id).Distinct().Count()>=2);
    var sources=x.Routes.Neighbors(target).Select(r=>x.Nodes.At(r.Target,r.Entry)).DistinctBy(n=>n.Id).Take(2).ToArray();target.Owner=1;
    foreach(var n in sources){n.Owner=0;x.Robots.Nodes[n.Id].Add(x.Robots.Create(0,RobotParts.Complete));}
    x.Robots.Nodes[target.Id].Add(x.Robots.Create(1,RobotParts.Complete));campaign=x.Campaign.RobotBattles;campaign.Rules=new(){SegmentTurns=1,NoWillTurns=1000,PersuasionPercent=0};
    var homes=sources.ToDictionary(n=>x.Robots.Nodes[n.Id].Robots.Single().Id,n=>n.Id);
    campaign.Advance(x,sources.Select(n=>new March(0,target.Id,1,n.Id,false)).ToArray());board=campaign.Encounters.Single().Battles.Single();
    foreach(var army in board.Armies)while(army.Waiting.TryDequeue(out var u))board.Units.Add(u);
    foreach(var u in board.Units.Where(u=>u.Owner==0))u.Retreated=true;
    board.Winner=1;board.Finished=true;campaign.Advance(x,Array.Empty<March>());
    foreach(var h in homes)Assert(x.Robots.Nodes[h.Value].Robots.Any(r=>r.Id==h.Key && r.DamagedParts==0),"Retreat returns intact to individual source");campaign.Check(x);
    Console.WriteLine("PASS: return-slot reservations, no battle disposal, one-part damage, full-hangar native-priority swaps, individual multi-source retreats.");
}

{
    void Assert(bool ok,string message){if(!ok)throw new Exception(message);}
    var x=new WorldSetup();x.SetWorld(2);x.SetCast(123);x.SetPlacement(456);x.SelectPlayer(0);x.Salvage.Reset();x.Robots.Initialize(x.Nodes.All.Length,x.ActiveCount);
    var home=x.Nodes.All.First(n=>x.Routes.Neighbors(n).Length>0);var route=x.Routes.Neighbors(home)[0];var target=x.Nodes.At(route.Target,route.Entry);
    home.Owner=0;target.Owner=1;x.ConquerorLocations[0]=home.Cell;x.ConquerorPoints[0]=home.Center;
    var pocket=x.Robots.Create(0,RobotParts.Complete).Materialize() with {BodyOrigin=1,LegsOrigin=1,DamagedParts=RobotParts.Body};
    x.Robots.Retinues[0].Add(pocket);x.Robots.Nodes[target.Id].Add(x.Robots.Create(1,RobotParts.Complete));
    x.Robots.Training[0].DeploymentPolicy=2;
    var campaign=x.Campaign.RobotBattles;campaign.Rules=new(){SegmentTurns=1,NoWillTurns=1000,PersuasionPercent=0};
    campaign.Advance(x,new[]{new March(0,target.Id,0)});
    var board=campaign.Encounters.Single().Battles.Single();
    foreach(var army in board.Armies)while(army.Waiting.TryDequeue(out var u))board.Units.Add(u);
    foreach(var u in board.Units.Where(u=>u.Owner==0)){if(u.Robot!=null)u.Fallen=true;else u.Retreated=true;}
    // Force the next random sample to hit the already damaged body (head/body/legs).
    while(board.Random.Clone().Next(3)!=1)board.Random.Next(3);
    board.Winner=1;board.Finished=true;campaign.Advance(x,Array.Empty<March>());
    var returned=x.Robots.Retinues[0].Robots.Single();
    Assert(returned.Id==pocket.Id && returned.DamagedParts==RobotParts.Body && returned.NativeParts(0)==1,"Repeated damaged-part hit adds no damage and pocket robot returns to pocket");
    Assert(x.Robots.Retinues[0].ReservedReturns==0,"Pocket return reservation released");campaign.Check(x);
    campaign.Advance(x,new[]{new March(0,target.Id,0)});board=campaign.Encounters.Single(e=>!e.Finished).Battles.Single();
    foreach(var army in board.Armies)while(army.Waiting.TryDequeue(out var u))board.Units.Add(u);
    foreach(var u in board.Units.Where(u=>u.Owner==0)){if(u.Robot!=null)u.Fallen=true;else u.Retreated=true;}
    while(board.Random.Clone().Next(3)!=0)board.Random.Next(3);
    board.Winner=1;board.Finished=true;campaign.Advance(x,Array.Empty<March>());
    returned=x.Robots.Retinues[0].Robots.Single();
    Assert(returned.Id==pocket.Id && !returned.CanFight && returned.NativeParts(0)==0 && returned.Origin(RobotParts.Head)==0,"Last native part damage disables combat without disposing robot");campaign.Check(x);
    Console.WriteLine("PASS: already-damaged hit, exact pocket return, last-native-part combat disable without disposal.");
}

// Battery proxies must not consume a real node's hangar or overwrite its order.
{
    var world=new WorldSetup();world.SetWorld(2);world.SetCast(123);world.SetPlacement(456);world.SelectPlayer(0);
    world.Robots.Initialize(world.Nodes.All.Length,world.ActiveCount);
    var home=world.Nodes.All.First(n=>world.Routes.Neighbors(n).Length>0);
    var route=world.Routes.Neighbors(home)[0];var target=world.Nodes.At(route.Target,route.Entry);
    home.Owner=0;target.Owner=1;
    world.ConquerorLocations[0]=home.Cell;world.ConquerorPoints[0]=home.Center;
    world.ConquerorLocations[1]=target.Cell;world.ConquerorPoints[1]=target.Center;
    for(int i=0;i<3;i++)world.Robots.Nodes[home.Id].Add(world.Robots.Create(0,RobotParts.Complete));
    var pocket=world.Robots.Create(0,RobotParts.Complete);world.Robots.Retinues[0].Add(pocket);
    world.Robots.Nodes[target.Id].Add(world.Robots.Create(1,RobotParts.Complete));
    int nodes=world.Nodes.All.Length,cell=world.ConquerorLocations[0];
    world.Campaign.Plan(new(0,target.Id,2,home.Id,false));
    world.Campaign.Plan(new(0,target.Id,0,home.Id,true,true));
    if(world.Campaign.PlannedOrders.Count!=2)throw new Exception("Battery overwrote hangar reservation");
    world.Campaign.RobotBattles.Rules=new(){SegmentTurns=1,NoWillTurns=1000,PersuasionPercent=0};
    world.Campaign.Resolve(world,world.Campaign.PlannedOrders.Append(new March(1,home.Id,1,target.Id,false)).ToArray());
    var edge=world.Campaign.RobotBattles.Encounters.First(e=>e.Kind==BattleKind.Edge);
    var units=edge.Battles.SelectMany(b=>b.Units.Concat(b.Armies.SelectMany(a=>a.Waiting)));
    if(units.Count(u=>u.Owner==0 && u.Robot!=null)!=3 || units.Count(u=>u.Owner==0 && u.Role==BattleRole.Queen)!=1)
        throw new Exception("Battery and hangar did not share edge encounter");
    if(world.Robots.Nodes[home.Id].Count!=1 || world.Robots.Retinues[0].Count!=0 || world.Nodes.All.Length!=nodes || world.ConquerorLocations[0]!=cell)
        throw new Exception("Proxy changed physical location or used wrong troop store");
    world.Campaign.RobotBattles.Check(world);
    world.Campaign.CancelReservation(0,home.Id,true,target.Id);
    if(world.Campaign.PlannedOrders.Count!=1 || world.Campaign.PlannedOrders[0].Queen)throw new Exception("Battery cancellation removed hangar order");
    Console.WriteLine("PASS: independent battery/hangar reservations, shared edge encounter, pocket-only troops and physical node invariants");
}

{
 void Check(bool ok,string message){if(!ok)throw new Exception(message);}
 var world=new WorldSetup();world.SetWorld(0);world.SetCast(123);world.SetPlacement(456);world.SelectPlayer(0);
 world.Salvage.Reset();world.Robots.Initialize(world.Nodes.All.Length,world.ActiveCount);var node=world.Nodes.All[0];
 Check(!world.Campaign.OwnershipChanged(node),"New world has no ownership history");
 world.Campaign.Resolve(world,Array.Empty<March>());int before=node.Owner;node.Owner=before==0?1:0;
 Check(world.Campaign.OwnershipChanged(node) && world.Campaign.PreviousNodeOwners[node.Id]==before,"History retains owner at start of global turn");
 var restored=AutoSaveStore.Restore<WorldSetup>(AutoSaveStore.Capture(world));
 Check(restored.Campaign.OwnershipChanged(restored.Nodes.All[node.Id]),"Ownership transition survives save/resume");
 world.Campaign.Resolve(world,Array.Empty<March>());
 Check(!world.Campaign.OwnershipChanged(node),"Unchanged next turn clears previous transition");
 world.Campaign.Reset();Check(!world.Campaign.OwnershipChanged(node),"Reset clears history");
 Console.WriteLine("PASS: ownership transition snapshot, save/resume, next-turn expiry and reset.");
}
