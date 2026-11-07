using FolderVerse;
using System.Reflection;
using Microsoft.Xna.Framework;
static void Check(bool value,string message){if(!value)throw new Exception(message);}
if(args.Length>0){
    object saved=AutoSaveStore.Read<object>(args[^1]);var w=(WorldSetup)saved.GetType().GetProperty("World")!.GetValue(saved)!;
    Console.WriteLine($"SAVE globalTurn={w.Population.Turn}");
    if(args[0]=="--plan-save"){
        w.Robots.ConfigureFactories(w);var timer=System.Diagnostics.Stopwatch.StartNew();ComputerLogistics.Plan(w,true);
        var orders=w.Campaign.PlannedOrders.ToList();ComputerJointAttacks.Plan(w,orders,true);timer.Stop();
        Console.WriteLine($"PLAN {timer.Elapsed.TotalMilliseconds:F0}ms: disposal={w.Robots.Nodes.Sum(n=>n.Robots.Count(r=>r.Disposal))}, return={w.Robots.Nodes.Sum(n=>n.Robots.Count(r=>r.ReturnHomeCommander.HasValue))}, long={w.Robots.LongDelivery.Orders.Count}, marches={orders.Count}, jointTargets={orders.Where(o=>!o.Queen).GroupBy(o=>o.TargetNode).Count(g=>g.Count()>1)}");
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
static WorldSetup Fixture(){var w=new WorldSetup();w.SetParticipantCounts(0,3);w.SetWorld(1);w.SetCast(2);w.SetPlacement(3);w.SelectPlayer(0);w.Robots.Initialize(w.Nodes.All.Length,w.ActiveCount);w.Robots.ConfigureFactories(w);foreach(var node in w.Nodes.All)node.Owner=0;return w;}
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
    w.Robots.Nodes[source].Add(w.Robots.Create(0,RobotParts.Head));w.Robots.Nodes[target].Add(w.Robots.Create(0,RobotParts.Body|RobotParts.Legs));
    ComputerLogistics.Plan(w,true);Check(w.Robots.LongDelivery.Orders.Count==1,"CPU reserves a relay that fills missing parts at another location");
    for(int i=0;i<w.Nodes.All.Length && w.Robots.LongDelivery.Orders.Count>0;i++)ResolveTransfers(w);
    Check(w.Robots.Nodes[target].Robots.Any(r=>r.Parts==RobotParts.Complete),"CPU parcel assembles on arrival");
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
    Check(BattleAi.Choose(b,a).Step==Point.Zero,"One soldier breaks diagonal pursuit by holding for an approach");
    rifle.Position=new(7,5);var step=BattleAi.Choose(b,rifle).Step;Check(BattleAi.Distance(rifle.Position+step,a.Position)>=2,"A rifle-only soldier creates a firing interval from an adjacent enemy");
}
Console.WriteLine("PASS: CPU disposal, repatriation, remote assembly, coordinated subordinate attacks, unique selections and manual control protection.");
