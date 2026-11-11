using FolderVerse;
using System.Reflection;
using Microsoft.Xna.Framework;
static void Check(bool value,string message){if(!value)throw new Exception(message);}
if(args.Length>0 && args[0]=="--create-world"){
    int seed=int.Parse(args[1]),players=int.Parse(args[2]);
    var fresh=new WorldSetup{EquipmentMode=args.Contains("--materials")?RobotEquipmentMode.Materials:RobotEquipmentMode.Legacy};
    fresh.SetParticipantCounts(0,players);fresh.SetWorld(seed);fresh.SetCast(seed+1000);fresh.SetTerritoryRights(seed+2000);
    foreach(int ruler in fresh.LandOrder()){
        var random=new SeedRandom(fresh.PlacementSeed^ruler*7919);var available=fresh.Cells.Where(c=>fresh.Owners[c.Id]<0).Select(c=>c.Id).ToList();var chosen=new List<int>();
        int first=available[random.Next(available.Count)];chosen.Add(first);available.Remove(first);
        while(chosen.Count<fresh.TerritoryRights[ruler]){var frontier=chosen.SelectMany(id=>fresh.Cells[id].Neighbors).Where(available.Contains).Distinct().ToArray();int next=frontier.Length>0?frontier[random.Next(frontier.Length)]:available[random.Next(available.Count)];chosen.Add(next);available.Remove(next);}
        fresh.ClaimLand(ruler,chosen.ToArray());
    }
    fresh.CompleteLand(seed+3000,25);fresh.SelectPlayer(0);AutoSaveStore.Write(args[3],fresh);
    Console.WriteLine(System.Text.Json.JsonSerializer.Serialize(new{fresh.WorldSeed,fresh.CastSeed,fresh.PlacementSeed,fresh.FortuneSeed,fresh.ActiveCount,cells=fresh.Cells.Length,nodes=fresh.Nodes.All.Length}));return;
}
if(args.Length>0 && args[0]=="--capture-routing"){
    foreach(bool displayed in new[]{true,false}){
        var probe=Fixture(10);int ruler=Enumerable.Range(0,10).First(r=>probe.Relations.Accepts(probe,r));int victor=(ruler+1)%10;
        probe.Relations.Leader=displayed?ruler:-1;
        probe.Relations.Capture(probe,ruler,victor,probe.Nodes.Current(ruler)!.Id);
        Console.WriteLine(System.Text.Json.JsonSerializer.Serialize(new{probe.ComputerOnly,displayed,ruler,victor,pending=probe.Relations.Pending.Count,superior=probe.Relations.Superiors[ruler],powered=probe.Relations.Powered[ruler]}));
    }
    return;
}
if(args.Length>0){
    object saved=AutoSaveStore.Read<object>(args[^1]);var w=saved is WorldSetup direct?direct:(WorldSetup)saved.GetType().GetProperty("World")!.GetValue(saved)!;
    Console.WriteLine($"SAVE globalTurn={w.Population.Turn}");
    if(args[0]=="--mixed-opportunities-save"){
        w.Robots.ConfigureFactories(w);var battle=w.Campaign.RobotBattles;
        foreach(var family in Enumerable.Range(0,w.ActiveCount).Where(w.Relations.CanAct).GroupBy(w.Relations.Root)){
            var owners=family.ToHashSet();
            foreach(var target in w.Nodes.All.Where(n=>n.Functional && n.Owner>=0 && !w.Relations.Allied(n.Owner,family.Key) && !battle.LockedNode(n.Id))){
                int defense=battle.DefendingRobots(w,family.Key,target.Id),required=defense+Math.Max(1,(defense+2)/3);
                var ground=w.Nodes.All.Where(n=>n.Functional && owners.Contains(n.Owner) && battle.CanAttackFrom(w,n.Owner,n.Id,target.Id)).Select(n=>{
                    int count=battle.SelectableRobots(w,n.Owner,n.Id,false).Length;bool guard=w.Robots.Nodes[n.Id].Robots.Any(r=>r.Owner==n.Owner && r.DefenseOnly && r.CanFight);
                    return new{node=n.Id,owner=n.Owner,count=Math.Max(0,count-(!guard && count>1?1:0))};
                }).Where(n=>n.count>0).ToArray();
                var escorts=family.Where(o=>battle.CanSortie(w,o,target.Id)).Select(o=>new{owner=o,count=battle.SelectableRobots(w,o,w.Nodes.Current(o)!.Id,true).Length}).Where(p=>p.count>0).ToArray();
                int g=ground.Sum(n=>n.count),p=escorts.Sum(e=>e.count);
                if(g>0 && p>0 && g<required && p<required && g+p>=required)Console.WriteLine(System.Text.Json.JsonSerializer.Serialize(new{root=family.Key,target=target.Id,defense,required,ground,escorts}));
            }
        }
        return;
    }
    if(args[0]=="--trace-next-save"){
        int target=int.Parse(args[1]);w.Robots.ConfigureFactories(w);
        var rules=w.Campaign.RobotBattles.Rules;int segment=rules.SegmentTurns;
        typeof(BattleRules).GetProperty(nameof(BattleRules.SegmentTurns))!.SetValue(rules,1);
        w.Robots.PrepareDomestic(w.Population.Turn,(a,b)=>FunctionalDeliveryRoutes.Connected(w,a,b),n=>w.Nodes.All[n].Owner,w.Campaign.RobotBattles.LockedNode);
        w.Campaign.FinishDomestic(w,true);w.Campaign.BeginMilitary();w.Campaign.Advance(w,-1,0,automaticAll:true,allPlanned:true);
        typeof(BattleRules).GetProperty(nameof(BattleRules.SegmentTurns))!.SetValue(rules,segment);
        foreach(var battle in w.Campaign.RobotBattles.Encounters.Where(e=>e.Target==target && !e.Finished).SelectMany(e=>e.Battles)){
            battle.Allied=w.Relations.Allied;
            for(int i=0;i<40 && !battle.Finished;i++){
                var units=battle.Units.Where(u=>u.Active).OrderBy(u=>u.Id).ToArray();var actions=units.ToDictionary(u=>u.Id,u=>BattleAi.Choose(battle,u));
                Console.WriteLine(System.Text.Json.JsonSerializer.Serialize(new{battle.Turn,units=units.Select(u=>new{u.Id,u.Owner,role=u.Role.ToString(),x=u.Position.X,y=u.Position.Y,u.Daggers,u.Rifles,u.Shields,u.Retreating,step=actions[u.Id].Step.ToString(),weapon=actions[u.Id].Weapon.ToString()}),armies=battle.Armies.Select(a=>new{a.Owner,a.NoWillCount,a.Withdrawing})}));
                BattleTurnResolver.Advance(battle,actions);battle.Frames.Clear();
            }
            Console.WriteLine($"TRACE finished={battle.Finished} winner={battle.Winner} turn={battle.Turn}");
        }
        return;
    }
    if(args[0]=="--plan-save"){
        w.Robots.ConfigureFactories(w);var timer=System.Diagnostics.Stopwatch.StartNew();ComputerLogistics.Plan(w,true);
        var orders=w.Campaign.PlannedOrders.ToList();ComputerJointAttacks.Plan(w,orders,true);timer.Stop();
        Console.WriteLine($"PLAN {timer.Elapsed.TotalMilliseconds:F0}ms: disposal={w.Robots.Nodes.Sum(n=>n.Robots.Count(r=>r.Disposal))}, return={w.Robots.Nodes.Sum(n=>n.Robots.Count(r=>r.ReturnHomeCommander.HasValue))}, long={w.Robots.LongDelivery.Orders.Count}, marches={orders.Count}, jointTargets={orders.Where(o=>!o.Queen).GroupBy(o=>o.TargetNode).Count(g=>g.Count()>1)}");
        Console.WriteLine(System.Text.Json.JsonSerializer.Serialize(new{marches=orders.Select(o=>new{o.Ruler,o.SourceNode,o.TargetNode,o.Fighters,o.Queen,o.Peaceful,defense=w.Campaign.RobotBattles.DefendingRobots(w,o.Ruler,o.TargetNode)})}));
        Console.WriteLine(System.Text.Json.JsonSerializer.Serialize(new{lastBattles=w.Campaign.RobotBattles.Encounters.OrderByDescending(e=>e.Id).Take(6).Select(e=>new{e.Id,e.Target,e.Defender,e.Finished,battles=e.Battles.Select(b=>new{b.Turn,b.Winner,noWillLimit=b.Rules.NoWillTurns,armies=b.Armies.Select(a=>new{a.Owner,a.Withdrawing,a.NoWillCount}),units=b.Units.GroupBy(u=>u.Owner).Select(g=>new{owner=g.Key,total=g.Count(),fallen=g.Count(u=>u.Fallen),retreated=g.Count(u=>u.Retreated),active=g.Count(u=>u.Active),shields=g.Sum(u=>u.Shields)})})})}));
        Console.WriteLine(string.Join("\n",w.Robots.LongDelivery.Orders.Take(8).Select(o=>$"DELIVERY {o.Source}->{o.Target} parts={o.Parts}, receiver={o.Receiver}")));return;
    }
    foreach(var encounter in w.Campaign.RobotBattles.Encounters.Where(e=>!e.Finished))foreach(var battle in encounter.Battles.Where(b=>!b.Finished)){
        battle.Allied=w.Relations.Allied;int turn=battle.Turn;var signatures=new Dictionary<string,int>();var counts=new Dictionary<string,int>();
        for(int i=0;i<150 && !battle.Finished;i++){
            var active=battle.Units.Where(u=>u.Active).OrderBy(u=>u.Id).ToArray();
            var decisions=active.ToDictionary(u=>u.Id,u=>BattleAi.Choose(battle,u));
            string key=string.Join(";",active.Select(u=>$"{u.Id}:{u.Position}:{u.Facing}:{u.Daggers},{u.Rifles},{u.Shields}:{u.Retreating}:{decisions[u.Id]}"));
            if(signatures.TryGetValue(key,out int first) && !counts.ContainsKey("period")){counts["period"]=i-first;Console.WriteLine($"REPEAT encounter={encounter.Id} start={turn} at={i} period={i-first}: {key}");}else signatures.TryAdd(key,i);
            BattleTurnResolver.Advance(battle,decisions);battle.Frames.Clear();
        }
        Console.WriteLine($"BATTLE {encounter.Id}: {turn}->{battle.Turn}, finished={battle.Finished}, flag={battle.FlagStanding}, active={battle.Units.Count(u=>u.Active)}");
    }
    return;
}
static WorldSetup Fixture(int computers=3){var w=new WorldSetup();w.SetParticipantCounts(0,computers);w.SetWorld(1);w.SetCast(2);w.SetPlacement(3);w.SelectPlayer(0);w.Robots.Initialize(w.Nodes.All.Length,w.ActiveCount);w.Robots.ConfigureFactories(w);foreach(var node in w.Nodes.All)node.Owner=0;return w;}
static void ResolveTransfers(WorldSetup w)=>typeof(RobotLongDelivery).GetMethod("Advance",BindingFlags.Instance|BindingFlags.NonPublic)!.Invoke(w.Robots.LongDelivery,new object[]{w});
{
    var w=Fixture();var s=w.Nodes.All.First();var store=w.Robots.Nodes[s.Id];w.Robots.Workshops[s.Id].ConfigureDisposal();
    var broken=w.Robots.Create(0,RobotParts.Head) with {DamagedParts=RobotParts.Head};store.Add(broken);while(store.Count<store.TotalCapacity)store.Add(w.Robots.Create(0,RobotParts.Legs));
    ComputerLogistics.Plan(w,true);Check(store.Robots.Single(r=>r.Id==broken.Id).Disposal,"CPU reserves local disposal for wholly damaged cargo under pressure");
}
{
    var w=Fixture();int source=0;int target=w.Nodes.All.OrderByDescending(n=>RobotLongDelivery.Path(w,source,n.Id,0).Length).First().Id;w.Robots.Workshops[target].ConfigureDisposal();
    var robot=w.Robots.Create(0,RobotParts.Complete) with {DamagedParts=RobotParts.Head};w.Robots.Nodes[source].Add(robot);w.Robots.Nodes[source].Add(w.Robots.Create(0,RobotParts.Complete));while(w.Robots.Nodes[source].HasSpace)w.Robots.Nodes[source].Add(w.Robots.Create(0,RobotParts.Legs));
    ComputerLogistics.Plan(w,true);Check(w.Robots.LongDelivery.Orders.Any(o=>o.RobotId==robot.Id && o.Parts==RobotParts.Head && o.Target==target),"CPU sends damaged parts to a rear disposal facility while retaining healthy parts");
}

{
    var w=Fixture();int source=0,target=w.Nodes.All.Last().Id;w.Nodes.All[target].Owner=1;var store=w.Robots.Nodes[source];var foreign=w.Robots.Create(1,RobotParts.Complete);store.Add(foreign);while(store.Count<store.TotalCapacity)store.Add(w.Robots.Create(0,RobotParts.Legs));
    ComputerLogistics.Plan(w,true);Check(store.Robots.Single(r=>r.Id==foreign.Id).ReturnHomeCommander==0,"CPU uses the normal return-home reservation");
}
{
    var w=Fixture();int source=0;int target=w.Nodes.All.OrderByDescending(n=>RobotLongDelivery.Path(w,source,n.Id,0).Length).First().Id;
    w.Robots.Nodes[source].Add(w.Robots.Create(0,RobotParts.Head));w.Robots.Nodes[target].Add(w.Robots.Create(0,RobotParts.Body|RobotParts.Legs));while(w.Robots.Nodes[source].HasSpace)w.Robots.Nodes[source].Add(w.Robots.Create(0,RobotParts.Head));while(w.Robots.Nodes[target].HasSpace)w.Robots.Nodes[target].Add(w.Robots.Create(0,RobotParts.Head));
    ComputerLogistics.Plan(w,true);Check(w.Robots.LongDelivery.Orders.Count==1,"CPU reserves a relay that fills missing parts at another location");
    for(int i=0;i<w.Nodes.All.Length && w.Robots.LongDelivery.Orders.Count>0;i++)ResolveTransfers(w);
    Check(w.Robots.Nodes[target].Robots.Any(r=>r.Parts==RobotParts.Complete),"CPU parcel assembles on arrival");
}
{
    var w=Fixture();int source=0;int target=w.Nodes.All.OrderByDescending(n=>RobotLongDelivery.Path(w,source,n.Id,0).Length).First().Id;
    var path=RobotLongDelivery.Path(w,source,target,0);Check(path.Length>2,"Fixture has a transit hangar");int middle=path[1];
    var head=w.Robots.Create(0,RobotParts.Head);w.Robots.Nodes[source].Add(head);w.Robots.Nodes[target].Add(w.Robots.Create(0,RobotParts.Body|RobotParts.Legs));
    while(w.Robots.Nodes[middle].HasSpace)w.Robots.Nodes[middle].Add(w.Robots.Create(0,RobotParts.Legs));ComputerLogistics.Plan(w,true);
    Check(!w.Robots.LongDelivery.Orders.Any(o=>o.RobotId==head.Id),"CPU does not lock the missing part into a relay with a full transit hangar");
}

{
    var w=Fixture();var enemy=w.Nodes.All.First(n=>w.Campaign.RobotBattles.MarchTargets(w,n.Id).Count()>=2);int target=enemy.Id;enemy.Owner=2;
    var sources=w.Campaign.RobotBattles.MarchTargets(w,target).Where(id=>w.Campaign.RobotBattles.MarchTargets(w,id).Contains(target)).Take(2).ToArray();Check(sources.Length==2,"Fixture supports a shared target");
    w.Nodes.All[sources[1]].Owner=1;w.Relations.SetSuperior(1,0);
    foreach(int source in sources)for(int i=0;i<2;i++)w.Robots.Nodes[source].Add(w.Robots.Create(w.Nodes.All[source].Owner,RobotParts.Complete));
    w.Robots.Nodes[target].Add(w.Robots.Create(2,RobotParts.Complete));var orders=new List<March>();ComputerJointAttacks.Plan(w,orders,true);
    var joint=orders.Where(o=>o.TargetNode==target && !o.Queen).ToArray();Check(joint.Length==2 && joint.Select(o=>o.Ruler).Distinct().Count()==2,"Own and subordinate forces combine against a target neither attacks alone");
    Check(joint.SelectMany(o=>o.RobotIds!).Distinct().Count()==joint.Sum(o=>o.RobotIds!.Length),"Each robot has exactly one destination");
    w.Campaign.Resolve(w,orders);w.Robots.Check();
}
{
    var w=Fixture();w.SetParticipantCounts(1,2);w.SelectPlayer(0);var node=w.Nodes.All.First();var broken=w.Robots.Create(0,RobotParts.Head) with {DamagedParts=RobotParts.Head};w.Robots.Nodes[node.Id].Add(broken);w.Robots.Workshops[node.Id].ConfigureDisposal();
    ComputerLogistics.Plan(w);var orders=new List<March>();ComputerJointAttacks.Plan(w,orders);Check(!w.Robots.Nodes[node.Id].Robots.Single().Disposal && orders.All(o=>o.Ruler!=0),"Manual human faction is protected");
}
{
    var b=new BattleState(1,new()){Kind=BattleKind.Edge};b.AddArmy(0,0,new[]{new Robot(1,0,RobotParts.Complete)});b.AddArmy(1,1,new[]{new Robot(2,1,RobotParts.Complete)});b.Reinforce();
    var a=b.Units.Single(u=>u.Owner==0);var rifle=b.Units.Single(u=>u.Owner==1);a.Position=new(6,5);rifle.Position=new(7,6);a.Role=rifle.Role=BattleRole.Soldier;a.Daggers=1;rifle.Daggers=0;a.Rifles=1;rifle.Rifles=2;
    b.Lessons[0]=b.Lessons[1]=BattleLesson.PursuitLoop|BattleLesson.RifleSpacing;
    Check(BattleAi.Choose(b,a).Step==Point.Zero,"One soldier breaks diagonal pursuit by holding for an approach");
    rifle.Position=new(7,5);var step=BattleAi.Choose(b,rifle).Step;Check(BattleAi.Distance(rifle.Position+step,a.Position)>=2,"A rifle-only soldier creates a firing interval from an adjacent enemy");
}
{
    var w=Fixture(10);int checkedChoices=0;
    // Controller tests need both personalities regardless of the cast lottery algorithm.
    w.Looks[1]=Enumerable.Range(0,60).Select(i=>new ConquerorLook(i,0)).First(l=>l.Keywords.Personality is not (3 or 9 or 11));
    w.Looks[9]=Enumerable.Range(0,60).Select(i=>new ConquerorLook(i,0)).First(l=>l.Keywords.Personality is 3 or 9 or 11);
    foreach(int victor in Enumerable.Range(1,9))foreach(int ruler in Enumerable.Range(1,9).Where(r=>r!=victor && w.Relations.Accepts(w,r))){
        w.Relations.Reset();w.Relations.Leader=0;w.Relations.Capture(w,ruler,victor,w.Nodes.Current(ruler)!.Id);
        Check(w.Relations.Superiors[ruler]==victor && w.Relations.Pending.Count==0,"CPU recruits an accepting captive rather than releasing a new rival");checkedChoices++;
    }
    Check(checkedChoices>10,"Capture fixture covers multiple CPU countries");
    int rejecting=Enumerable.Range(1,9).First(r=>!w.Relations.Accepts(w,r));w.Relations.Reset();w.Relations.Leader=0;
    int captor=Enumerable.Range(1,9).First(r=>r!=rejecting);w.Relations.Capture(w,rejecting,captor,w.Nodes.Current(rejecting)!.Id);
    Check(w.Relations.Superiors[rejecting]<0 && !w.Relations.Powered[rejecting],"A rejecting captive without enough territory still follows the existing removal rule");
    int accepting=Enumerable.Range(1,9).First(r=>w.Relations.Accepts(w,r));w.Relations.Reset();w.Relations.Leader=0;
    w.Relations.Capture(w,accepting,0,w.Nodes.Current(accepting)!.Id);
    Check(w.Relations.Pending.Count==1 && w.Relations.Superiors[accepting]<0,"The player's captive disposition stays pending for their choice");
}
{
    var w=Fixture();int enemy=w.Nodes.All.Last().Id;w.Nodes.All[enemy].Owner=1;
    var front=w.Nodes.All.Where(n=>n.Owner==0 && w.Campaign.RobotBattles.MarchTargets(w,n.Id).Contains(enemy)).Select(n=>n.Id).ToHashSet();
    var distance=front.ToDictionary(n=>n,n=>0);var queue=new Queue<int>(front);
    while(queue.TryDequeue(out int at))foreach(int next in RobotLongDelivery.Neighbors(w,at,0))if(w.Nodes.All[next].Owner==0 && distance.TryAdd(next,distance[at]+1))queue.Enqueue(next);
    int source=distance.OrderByDescending(p=>p.Value).First().Key;Check(distance[source]>1,"Staging fixture has a rear behind the front");
    foreach(int n in front)while(w.Robots.Nodes[n].HasSpace)w.Robots.Nodes[n].Add(w.Robots.Create(0,RobotParts.Legs));
    for(int i=0;i<2;i++)w.Robots.Nodes[source].Add(w.Robots.Create(0,RobotParts.Complete));
    ComputerLogistics.Plan(w,true);var parcels=w.Robots.LongDelivery.Orders.Where(o=>o.Source==source).ToArray();
    Check(parcels.Length>0 && parcels.All(o=>!front.Contains(o.Target) && distance[o.Target]<distance[source]),"CPU stages surplus troops toward a full front instead of leaving them far behind");
    Check(w.Robots.Nodes[source].Robots.Count(r=>!r.LongDeliveryNumber.HasValue)>=1,"Staging leaves a robot at home");
}
{
    var w=Fixture();int target=w.Nodes.All.First(n=>w.Campaign.RobotBattles.MarchTargets(w,n.Id).Any()).Id;
    int source=w.Campaign.RobotBattles.MarchTargets(w,target).First(n=>w.Campaign.RobotBattles.MarchTargets(w,n).Contains(target));w.Nodes.All[target].Owner=1;
    for(int i=0;i<5;i++)w.Robots.Nodes[source].Add(w.Robots.Create(0,RobotParts.Complete));
    w.Robots.Nodes[target].Add(w.Robots.Create(1,RobotParts.Complete));var orders=new List<March>();ComputerJointAttacks.Plan(w,orders,true);
    var attack=orders.Where(o=>!o.Queen && o.TargetNode==target).ToArray();
    Check(attack.Sum(o=>o.Fighters)==2,"Fresh attacks do not consume surplus troops without a failed precedent");
        var failed=new BattleState(1,new()){Kind=BattleKind.Node,Defender=1};
    failed.AddArmy(1,target,new[]{new Robot(100001,1,RobotParts.Complete)});failed.AddArmy(0,source,w.Robots.Nodes[source].Robots.Take(2));
    failed.Winner=1;failed.Finished=true;failed.Army(0).Withdrawing=true;failed.Army(0).NoWillCount=failed.Rules.NoWillTurns;
    var encounter=new RobotEncounter{Target=target,Defender=1,Finished=true,Kind=BattleKind.Node};encounter.Battles.Add(failed);w.Campaign.RobotBattles.Encounters.Add(encounter);
    orders.Clear();ComputerJointAttacks.Plan(w,orders,true);attack=orders.Where(o=>!o.Queen && o.TargetNode==target).ToArray();
    Check(attack.Sum(o=>o.Fighters)==3 && attack.SelectMany(o=>o.RobotIds!).Distinct().Count()==3,"A failed two-unit assault is retried with three distinct robots");
    foreach(var robot in w.Robots.Nodes[source].Robots.Skip(3).ToArray())w.Robots.Nodes[source].Remove(robot.Id);
    orders.Clear();ComputerJointAttacks.Plan(w,orders,true);
    Check(!orders.Any(o=>!o.Queen && o.TargetNode==target),"CPU waits rather than repeating the same failed force when reinforcements are unavailable");
}
{
    var w=Fixture();var home=w.Nodes.All.First(n=>w.Campaign.RobotBattles.MarchTargets(w,n.Id).Any());int target=w.Campaign.RobotBattles.MarchTargets(w,home.Id).First();w.Nodes.All[target].Owner=2;
    w.Relations.SetSuperior(1,0);
    foreach(int owner in new[]{0,1}){w.ConquerorLocations[owner]=home.Cell;w.ConquerorPoints[owner]=home.Center;for(int i=0;i<2;i++)w.Robots.Retinues[owner].Add(w.Robots.Create(owner,RobotParts.Complete));}
    for(int i=0;i<3;i++)w.Robots.Nodes[target].Add(w.Robots.Create(2,RobotParts.Complete));
    var orders=new List<March>();ComputerJointAttacks.Plan(w,orders,true);
    var joint=orders.Where(o=>o.Queen && o.PocketOnly && o.TargetNode==target && o.RobotIds?.Length>0).ToArray();
    Check(joint.Length==2 && joint.Sum(o=>o.Fighters)==4,"Two allied rulers combine pocket escorts against a defense neither can beat alone");
    Check(joint.SelectMany(o=>o.RobotIds!).Distinct().Count()==4,"Joint escorts reserve each pocket robot once");
    Check(w.Robots.Nodes[home.Id].Count==0,"Pocket escorts do not consume local hangar robots");
    w.Relations.SetSuperior(1,-1);var separate=new List<March>();ComputerJointAttacks.Plan(w,separate,true);
    Check(!separate.Any(o=>o.Queen && o.TargetNode==target && (o.Ruler==0 || o.Ruler==1) && o.Fighters>0),"Independent countries never combine their pocket escorts");w.Relations.SetSuperior(1,0);
    w.Campaign.Resolve(w,orders);w.Robots.Check();
}
{
    var w=Fixture();var home=w.Nodes.All.First(n=>w.Campaign.RobotBattles.MarchTargets(w,n.Id).Any());int target=w.Campaign.RobotBattles.MarchTargets(w,home.Id).First();w.Nodes.All[target].Owner=2;
    w.ConquerorLocations[0]=home.Cell;w.ConquerorPoints[0]=home.Center;
    for(int i=0;i<2;i++){w.Robots.Nodes[home.Id].Add(w.Robots.Create(0,RobotParts.Complete));w.Robots.Retinues[0].Add(w.Robots.Create(0,RobotParts.Complete));w.Robots.Nodes[target].Add(w.Robots.Create(2,RobotParts.Complete));}
    var orders=new List<March>();ComputerJointAttacks.Plan(w,orders,true);var mixed=orders.Where(o=>o.TargetNode==target && o.Ruler==0).ToArray();
    Check(mixed.Any(o=>!o.Queen && o.Fighters==1) && mixed.Any(o=>o.Queen && o.PocketOnly && o.Fighters==2),"CPU combines one hangar soldier and two pocket escorts against two defenders");
    Check(mixed.SelectMany(o=>o.RobotIds!).Distinct().Count()==3,"Mixed assault reserves three distinct robots");
    Check(mixed.Count(o=>o.Queen)==1 && orders.SelectMany(o=>o.RobotIds??Array.Empty<long>()).GroupBy(id=>id).All(g=>g.Count()==1),"Mixed assault has one queen and no duplicate robot selections across targets");
    w.SetParticipantCounts(1,2);w.SelectPlayer(0);var manual=new List<March>();ComputerJointAttacks.Plan(w,manual);
    Check(!manual.Any(o=>o.Ruler==0),"Mixed assault respects manual human control");
    w.SetParticipantCounts(0,3);w.Campaign.Resolve(w,orders);w.Robots.Check();
}
{
    var w=Fixture();var home=w.Nodes.All.First(n=>w.Campaign.RobotBattles.MarchTargets(w,n.Id).Any());int target=w.Campaign.RobotBattles.MarchTargets(w,home.Id).First();w.Nodes.All[target].Owner=2;
    w.ConquerorLocations[0]=home.Cell;w.ConquerorPoints[0]=home.Center;
    for(int i=0;i<2;i++)w.Robots.Retinues[0].Add(w.Robots.Create(0,RobotParts.Complete));w.Robots.Nodes[target].Add(w.Robots.Create(2,RobotParts.Complete));
    var failed=new BattleState(1,new()){Kind=BattleKind.Edge};failed.AddArmy(0,home.Id,w.Robots.Retinues[0].Robots);failed.AddArmy(2,target,new[]{new Robot(100002,2,RobotParts.Complete)});
    failed.Finished=true;failed.Winner=-1;failed.Army(0).Withdrawing=true;failed.Army(0).NoWillCount=failed.Rules.NoWillTurns;
    var encounter=new RobotEncounter{Target=target,Kind=BattleKind.Edge,Finished=true};encounter.Battles.Add(failed);w.Campaign.RobotBattles.Encounters.Add(encounter);
    var orders=new List<March>();ComputerJointAttacks.Plan(w,orders,true);
    Check(!orders.Any(o=>o.Ruler==0 && o.Queen && o.TargetNode==target && !o.Peaceful),"An individual queen does not immediately repeat a failed escort assault with the same strength");
}
{
    var w=Fixture();int source=w.Nodes.All.First(n=>w.Campaign.RobotBattles.MarchTargets(w,n.Id).Any()).Id;
    int target=w.Campaign.RobotBattles.MarchTargets(w,source).First();w.Nodes.All[target].Owner=1;
    var guard=w.Robots.Create(0,RobotParts.Complete) with {DefenseOnly=true};
    var weak=w.Robots.Create(0,RobotParts.Complete) with {BodyOrigin=1,LegsOrigin=1};
    var regular=w.Robots.Create(0,RobotParts.Complete);
    var premium=w.Robots.Create(0,RobotParts.Complete) with {PremiumParts=RobotParts.Head};
    foreach(var r in new[]{guard,weak,regular,premium})w.Robots.Nodes[source].Add(r);
    var orders=new List<March>();ComputerJointAttacks.Plan(w,orders,true);
    Check(orders.Single(o=>!o.Queen && o.TargetNode==target).RobotIds!.SequenceEqual(new[]{premium.Id}),"Automatic selection sends the best equipped soldier while keeping the guard");
    Check(w.Robots.Nodes[source].Robots.Single(r=>r.Id==guard.Id).DefenseOnly,"Quality selection preserves defense reservation");
    w.Robots.Training[0].DeploymentPolicy=2;orders.Clear();ComputerJointAttacks.Plan(w,orders,true);
    Check(orders.Single(o=>!o.Queen && o.TargetNode==target).RobotIds!.SequenceEqual(new[]{weak.Id}),"Explicit low-native deployment preference is respected");
}
Console.WriteLine("PASS: CPU disposal, repatriation, remote assembly, coordinated subordinate attacks, unique selections, equipment priority and manual control protection.");
