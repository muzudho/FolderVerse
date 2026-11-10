using FolderVerse;
using System.Text.Json;
if(args.Length==0)throw new ArgumentException("Usage: CpuCampaignBench saved-game [turns] [output-copy]");
object saved=AutoSaveStore.Read<object>(args[0]);var w=saved is WorldSetup direct?direct:(WorldSetup)saved.GetType().GetProperty("World")!.GetValue(saved)!;int steps=args.Length>1?int.Parse(args[1]):20;
if(args.Contains("--no-display-leader")){if(!w.ComputerOnly)throw new InvalidOperationException("Capture-routing experiment requires a computer-only copy.");w.Relations.Leader=-1;}
var tracedTargets=args.Where(a=>a.StartsWith("--trace-target=")).Select(a=>int.Parse(a.Split('=')[1])).ToHashSet();
bool traceAll=args.Contains("--trace-all");
var clearNodes=args.Where(a=>a.StartsWith("--clear-head-node=")).Select(a=>int.Parse(a.Split('=')[1])).ToHashSet();
var rally=args.Where(a=>a.StartsWith("--rally-ruler=")).Select(a=>a.Split('=')[1].Split(':').Select(int.Parse).ToArray()).ToArray();
var swaps=args.Where(a=>a.StartsWith("--swap-head=") || a.StartsWith("--swap-head-once="))
    .Select(a=>(Path:a.Split('=')[1].Split(':').Select(int.Parse).ToArray(),Once:a.StartsWith("--swap-head-once="))).ToArray();
var swapped=new HashSet<int>();
w.Robots.ConfigureFactories(w);foreach(var battle in w.Campaign.RobotBattles.Encounters.SelectMany(e=>e.Battles))battle.Allied=w.Relations.Allied;
bool Conquered()=>w.Relations.ProvisionalRanking(w).Any(r=>r.Cells==w.Cells.Length && w.Relations.CanWinConquest(r.Ruler));
void Report(int step){var factionSizes=w.Nodes.All.Where(n=>n.Owner>=0).GroupBy(n=>w.Relations.Root(n.Owner)).Select(g=>g.Count()).ToArray();var all=w.Robots.Nodes.SelectMany(n=>n.Robots).ToArray();Console.WriteLine(JsonSerializer.Serialize(new{step,w.WorldSeed,w.CastSeed,w.PlacementSeed,w.FortuneSeed,version=typeof(WorldSetup).Assembly.GetName().Version!.ToString(),totalCells=w.Cells.Length,totalNodes=w.Nodes.All.Length,conquered=Conquered(),largestFactionNodes=factionSizes.DefaultIfEmpty(0).Max(),largestFactionCells=w.Relations.ProvisionalRanking(w).Where(r=>w.Relations.CanWinConquest(r.Ruler)).Select(r=>r.Cells).DefaultIfEmpty(0).Max(),turn=w.Population.Turn,independent=Enumerable.Range(0,w.ActiveCount).Count(o=>w.Relations.CanWinConquest(o)),powered=w.Relations.Powered.Take(w.ActiveCount).Count(p=>p),occupied=all.Length,capacity=w.Robots.Nodes.Sum(n=>n.TotalCapacity),deployable=w.Nodes.All.Where(n=>n.Owner>=0).Sum(n=>w.Campaign.RobotBattles.AvailableFrom(w,n.Owner,n.Id)),blocked=w.Robots.Workshops.Sum(s=>s.ProductionLines.Count(l=>l.Waiting)),disposal=all.Count(r=>r.Disposal),returning=all.Count(r=>r.ReturnHomeCommander.HasValue)+w.Robots.ReturningRobots.Count,longDelivery=w.Robots.LongDelivery.Orders.Count,ongoing=w.Campaign.RobotBattles.Encounters.Count(e=>!e.Finished),maxBattle=w.Campaign.RobotBattles.Encounters.Where(e=>!e.Finished).SelectMany(e=>e.Battles).Select(b=>b.Turn).DefaultIfEmpty(0).Max()}));}
var attackTargets=new Dictionary<int,int>();var encounterOutcomes=new Dictionary<string,int>();int hostileMarches=0;long committedRobots=0;
Report(0);
for(int step=1;step<=steps;step++){
    w.Robots.PrepareDomestic(w.Population.Turn,(a,b)=>FunctionalDeliveryRoutes.Connected(w,a,b),n=>w.Nodes.All[n].Owner,w.Campaign.RobotBattles.LockedNode);
    if(step==1)foreach(var option in args.Where(a=>a.StartsWith("--relieve-return-once="))){
        var fields=option.Split('=')[1].Split(':');int source=int.Parse(fields[0]),target=int.Parse(fields[1]);long id=long.Parse(fields[2]);
        int owner=w.Nodes.All[source].Owner,leader=w.Relations.Leader;
        if(owner<0 || w.Nodes.All[target].Owner!=owner || !w.Robots.ReturningRobots.Any(j=>j.Node==source && j.Country==owner) || w.Robots.Nodes[source].HasSpace)
            throw new InvalidOperationException("Relief experiment needs a full same-country return destination.");
        var robot=w.Robots.Nodes[source].Robots.Single(r=>r.Id==id);
        if(robot.Parts==RobotParts.Complete || !w.Robots.CanShip(source,robot) || !w.Robots.Nodes[target].HasSpace)
            throw new InvalidOperationException("Relief experiment needs partial cargo and a free destination slot.");
        var path=RobotLongDelivery.Path(w,source,target,owner);
        if(path.Length!=2)throw new InvalidOperationException("Relief experiment is restricted to a direct route.");
        int slot=w.Robots.Nodes[target].Count;
        if(w.Robots.LongDelivery.Orders.Any(o=>o.Target==target && !o.Receiver.HasValue && o.TargetSlot==slot))
            throw new InvalidOperationException("Relief destination is already reserved.");
        try{w.Relations.Leader=owner;w.Robots.LongDelivery.Reserve(w,source,target,id,robot.Parts,null,owner,slot);}
        finally{w.Relations.Leader=leader;}
        w.Robots.Check();
        Console.WriteLine(JsonSerializer.Serialize(new{trace="relieve-return",step,source,target,id,slot,waiting=w.Robots.ReturningRobots.Count(j=>j.Node==source)}));
    }
    for(int index=0;index<swaps.Length;index++){
        var swap=swaps[index];if(swap.Once && swapped.Contains(index))continue;
        int source=swap.Path[0],target=swap.Path[1],owner=w.Nodes.All[source].Owner;
        if(owner<0 || w.Nodes.All[target].Owner!=owner || !FunctionalDeliveryRoutes.Connected(w,source,target) || w.Campaign.RobotBattles.LockedNode(source) || w.Campaign.RobotBattles.LockedNode(target))continue;
        var ready=w.Robots.Nodes[source].Robots.FirstOrDefault(r=>r.Owner==owner && r.CanFight && !w.Robots.AssemblyLocked(source,r));
        var head=w.Robots.Nodes[target].Robots.FirstOrDefault(r=>r.Owner==owner && r.Parts==RobotParts.Head && !w.Robots.AssemblyLocked(target,r));
        if(ready==null || head==null)continue;
        int sourceSlot=Array.FindIndex(w.Robots.Nodes[source].Robots.ToArray(),r=>r.Id==ready.Id),targetSlot=Array.FindIndex(w.Robots.Nodes[target].Robots.ToArray(),r=>r.Id==head.Id);
        int previousLeader=w.Relations.Leader;
        try{
            // Exercise the ordinary permission-checked player API on the copied world.
            w.Relations.Leader=owner;
            w.Robots.DirectDelivery.ReserveParts(w,source,target,ready.Id,RobotParts.Complete,null,targetSlot);
            w.Robots.DirectDelivery.ReserveParts(w,target,source,head.Id,RobotParts.Head,null,sourceSlot);
        }finally{w.Relations.Leader=previousLeader;}
        var preview=w.Robots.DirectDelivery.Preview(w.Robots,target);
        if(preview.Length!=w.Robots.Nodes[target].Count || w.Robots.DirectDelivery.Preview(w.Robots,source).Length!=w.Robots.Nodes[source].Count)
            throw new InvalidOperationException("Swap must preserve both occupied slot counts");
        swapped.Add(index);Console.WriteLine(JsonSerializer.Serialize(new{trace="swap-head",step,source,target,soldier=ready.Id,head=head.Id,previewCount=preview.Length,previewFighters=preview.Count(r=>r.CanFight)}));
    }
    foreach(int node in clearNodes){
        var store=w.Robots.Nodes[node];int owner=w.Nodes.All[node].Owner;
        if(owner<0 || w.Campaign.RobotBattles.LockedNode(node) || store.Robots.Any(r=>r.Disposal) || !w.Robots.Workshops[node].DisposalFacilities.Any(f=>f.HasDisposalFactory))continue;
        var heads=store.Robots.Where(r=>r.Parts==RobotParts.Head && r.NativeParts(owner)>0 && !w.Robots.AssemblyLocked(node,r)).OrderBy(r=>r.Id).ToArray();
        if(heads.Length<=2)continue;
        w.Robots.ToggleReservation(node,heads[0].Id,HangarReservation.Disposal,owner);
        Console.WriteLine(JsonSerializer.Serialize(new{trace="clear-head",step,node,robot=heads[0].Id,remaining=heads.Length-1}));
    }
    var previousRoots=Enumerable.Range(0,w.ActiveCount).Select(w.Relations.Root).ToArray();
    var previousPowered=w.Relations.Powered.Take(w.ActiveCount).ToArray();
    var previousEncounters=w.Campaign.RobotBattles.Encounters.Select(e=>e.Id).ToHashSet();
    var transferSafety=new List<(int Source,int Target,int Owner,int[] Outgoing,int[] Incoming)>();
    w.Campaign.FinishDomestic(w,true,deferDelivery:()=>{
        if(args.Contains("--check-transfer-safety"))foreach(var transfer in w.Campaign.FunctionTransfers.Orders)
            transferSafety.Add((transfer.Source,transfer.Target,transfer.Owner,
                w.Campaign.RobotBattles.MarchTargets(w,transfer.Target).Where(t=>w.Nodes.All[t].Owner>=0 && !w.Relations.Allied(transfer.Owner,w.Nodes.All[t].Owner)).ToArray(),
                w.Nodes.All.Where(n=>n.Functional && n.Owner>=0 && !w.Relations.Allied(transfer.Owner,n.Owner) && w.Campaign.RobotBattles.CanAttackFrom(w,n.Owner,n.Id,transfer.Target)).Select(n=>n.Id).ToArray()));
        if(args.Contains("--trace-logistics"))Console.WriteLine(JsonSerializer.Serialize(new{trace="logistics",step,
            direct=w.Robots.DirectDelivery.Orders.Select(o=>new{o.Source,o.Target,o.RobotId,o.Part,o.Owner,
                sourceAvailable=w.Campaign.RobotBattles.AvailableFrom(w,o.Owner,o.Source),targetAvailable=w.Campaign.RobotBattles.AvailableFrom(w,o.Owner,o.Target),
                sourceCount=w.Robots.Nodes[o.Source].Count,targetCount=w.Robots.Nodes[o.Target].Count,
                targetDefense=w.Campaign.RobotBattles.MarchTargets(w,o.Target).Where(t=>w.Nodes.All[t].Owner>=0 && !w.Relations.Allied(o.Owner,w.Nodes.All[t].Owner)).Select(t=>new{target=t,defense=w.Campaign.RobotBattles.DefendingRobots(w,o.Owner,t)})}),
            longDistance=w.Robots.LongDelivery.Orders.Select(o=>new{o.Source,o.Target,o.RobotId,o.Parts})}));
        return false;
    });
    foreach(var transfer in transferSafety){
        var outgoing=w.Campaign.RobotBattles.MarchTargets(w,transfer.Target).Where(t=>w.Nodes.All[t].Owner>=0 && !w.Relations.Allied(transfer.Owner,w.Nodes.All[t].Owner)).ToArray();
        var incoming=w.Nodes.All.Where(n=>n.Functional && n.Owner>=0 && !w.Relations.Allied(transfer.Owner,n.Owner) && w.Campaign.RobotBattles.CanAttackFrom(w,n.Owner,n.Id,transfer.Target)).Select(n=>n.Id).ToArray();
        if(outgoing.Except(transfer.Outgoing).Any() || incoming.Except(transfer.Incoming).Any())throw new Exception("Function transfer opened a new hostile approach.");
        Console.WriteLine(JsonSerializer.Serialize(new{trace="transfer-safety",step,transfer.Source,transfer.Target,transfer.Owner,addedHostileApproaches=0}));
    }
    w.Campaign.BeginMilitary();w.Campaign.Advance(w,-1,0,automaticAll:true,allPlanned:true,deferResolution:orders=>{
        foreach(var instruction in rally){
            int ruler=instruction[0],target=instruction[1];var home=w.Nodes.Current(ruler);
            if(home==null || !w.Relations.CanAct(ruler) || w.Campaign.RobotBattles.LockedNode(home.Id))continue;
            var list=(List<March>)orders;list.RemoveAll(o=>o.Ruler==ruler && o.Queen);
            var path=RobotLongDelivery.Path(w,home.Id,target,ruler);
            if(path.Length>1 && w.Campaign.RobotBattles.CanMarch(w,ruler,path[1])){
                list.Add(new(ruler,path[1],0,home.Id,Queen:true,PocketOnly:true,Peaceful:true));
                Console.WriteLine(JsonSerializer.Serialize(new{trace="rally",step,ruler,source=home.Id,target=path[1],goal=target}));
            }
        }
        foreach(var order in orders.Where(o=>!o.Peaceful && w.Nodes.All[o.TargetNode].Owner>=0 && !w.Relations.Allied(o.Ruler,w.Nodes.All[o.TargetNode].Owner))){hostileMarches++;committedRobots+=order.Fighters;attackTargets[order.TargetNode]=attackTargets.GetValueOrDefault(order.TargetNode)+1;}
        foreach(var order in orders.Where(o=>traceAll || tracedTargets.Contains(o.TargetNode)))Console.WriteLine(JsonSerializer.Serialize(new{trace="order",step,order,defense=w.Campaign.RobotBattles.DefendingRobots(w,order.Ruler,order.TargetNode),
            reserveDefenders=w.Campaign.RobotBattles.Reserves.Count(r=>r.Node==order.TargetNode && r.Robot.CanFight && !w.Relations.Allied(order.Ruler,r.Robot.Owner)),
            equipment=order.RobotIds==null?null:w.Robots.Nodes.Concat(w.Robots.Retinues).SelectMany(s=>s.Robots).Where(r=>order.RobotIds.Contains(r.Id))
                .Select(r=>new{r.Id,native=r.NativeParts(order.Ruler),uses=r.CombatParts(order.Ruler),rifles=r.CombatRifles(order.Ruler)}).ToArray()}));
        return false;
    });
    foreach(var encounter in w.Campaign.RobotBattles.Encounters.Where(e=>!previousEncounters.Contains(e.Id))){
        int winner=encounter.Battles.FirstOrDefault()?.Winner??-1;string result=!encounter.Finished?"ongoing":winner<0?"no-winner":encounter.Defender>=0 && previousRoots[winner]==previousRoots[encounter.Defender]?"defender-held":"attacker-won";
        string key=$"{encounter.Kind}:{result}";encounterOutcomes[key]=encounterOutcomes.GetValueOrDefault(key)+1;
        if(traceAll || tracedTargets.Contains(encounter.Target))Console.WriteLine(JsonSerializer.Serialize(new{trace="battle",step,encounter.Id,encounter.Source,encounter.Target,kind=encounter.Kind.ToString(),result,
            battles=encounter.Battles.Select(b=>new{b.Id,b.Winner,b.Turn,seed=w.PlacementSeed^encounter.Id^b.Id,terrain=b.Terrain.ToString(),b.BoardSize,walls=b.Walls.OrderBy(p=>p.X).ThenBy(p=>p.Y).Select(p=>new{p.X,p.Y}),initial=b.Frames.FirstOrDefault()?.Units.Select(u=>new{u.Id,u.Owner,u.Role,x=u.Position.X,y=u.Position.Y,u.Daggers,u.Rifles,u.Shields}),conversions=b.Persuasions.Where(p=>p.Success).Select(p=>new{p.Turn,p.Target,p.PreviousOwner,p.Owner}),armies=b.Armies.Select(a=>new{a.Owner,a.HomeNode,a.Withdrawing,a.NoWillCount,limit=b.Rules.NoWillTurns,
                robots=b.Units.Count(u=>u.Owner==a.Owner && u.Robot!=null)+a.Waiting.Count(u=>u.Robot!=null),ids=b.Units.Where(u=>u.Owner==a.Owner && u.Robot!=null).Select(u=>u.Id).Concat(a.Waiting.Where(u=>u.Robot!=null).Select(u=>u.Id)),fallen=b.Units.Count(u=>u.Owner==a.Owner && u.Fallen),retreated=b.Units.Count(u=>u.Owner==a.Owner && u.Retreated)})})}));
    }
    if(args.Contains("--trace-relations") && w.Relations.Pending.Count>0)Console.WriteLine(JsonSerializer.Serialize(new{trace="pending",step,captives=w.Relations.Pending}));
    foreach(var captive in w.Relations.Pending.ToArray())w.Relations.Choose(w,captive,w.Relations.Accepts(w,captive.Ruler)?DefeatedChoice.Recruit:w.Relations.CanMercy(w,captive)?DefeatedChoice.Release:DefeatedChoice.RemoveBattery);
    if(args.Contains("--trace-relations")){
        var changed=Enumerable.Range(0,w.ActiveCount).Where(o=>previousRoots[o]!=w.Relations.Root(o) || previousPowered[o]!=w.Relations.Powered[o]).Select(o=>new{
            ruler=o,previousRoot=previousRoots[o],root=w.Relations.Root(o),previousPowered=previousPowered[o],powered=w.Relations.Powered[o],node=w.Nodes.Current(o)?.Id}).ToArray();
        if(changed.Length>0 || step%5==0)Console.WriteLine(JsonSerializer.Serialize(new{trace="relations",step,changed,
            pending=w.Relations.Pending.Count,rulers=Enumerable.Range(0,w.ActiveCount).Where(w.Relations.CanAct).Select(o=>new{ruler=o,root=w.Relations.Root(o),node=w.Nodes.Current(o)?.Id})}));
    }
    w.Lessons.Research();
    w.Industry.Roll(w.PlacementSeed^w.FortuneSeed);w.Industry.Apply(w);
    foreach(var battle in w.Campaign.RobotBattles.Encounters.SelectMany(e=>e.Battles)){battle.Frames.Clear();battle.Events.Clear();battle.Persuasions.Clear();battle.Attacks.Clear();}
    w.Robots.Check();if(step%5==0 || step==steps)Report(step);
    if(Conquered()){if(step%5!=0 && step!=steps)Report(step);break;}
}

var functional=w.Nodes.All.Where(n=>n.Functional && n.Owner>=0).ToArray();
bool Front(Node n)=>w.Campaign.RobotBattles.MarchTargets(w,n.Id).Any(t=>w.Nodes.All[t].Owner>=0 && !w.Relations.Allied(n.Owner,w.Nodes.All[t].Owner));
var fronts=functional.Where(Front).Select(n=>n.Id).ToHashSet();
var diagnostics=new{
    turn=w.Population.Turn,fronts=fronts.Count,
    rearDefenders=functional.Where(n=>!fronts.Contains(n.Id)).Sum(n=>w.Robots.Nodes[n.Id].Robots.Count(r=>r.DefenseOnly && r.CanFight)),
    frontDefenders=functional.Where(n=>fronts.Contains(n.Id)).Sum(n=>w.Robots.Nodes[n.Id].Robots.Count(r=>r.DefenseOnly && r.CanFight)),
    physicalFree=functional.Sum(n=>w.Robots.Nodes[n.Id].StorageLimit-w.Robots.Nodes[n.Id].Count),productionBuffers=functional.Sum(n=>w.Robots.Workshops[n.Id].ReservedSlots),
    waitingReturns=w.Robots.ReturningRobots.GroupBy(j=>j.Status).ToDictionary(g=>g.Key,g=>g.Count()),
    waitingDelivery=w.Robots.LongDelivery.Orders.GroupBy(o=>o.Status).ToDictionary(g=>g.Key,g=>g.Count()),
    longResults=w.Robots.LongDelivery.Results.GroupBy(o=>o.Status).ToDictionary(g=>g.Key,g=>g.Count()),
    sources=functional.Select(n=>new{node=n.Id,owner=n.Owner,front=fronts.Contains(n.Id),count=w.Robots.Nodes[n.Id].Count,capacity=w.Robots.Nodes[n.Id].StorageLimit,buffer=w.Robots.Workshops[n.Id].ReservedSlots,blocked=w.Robots.Workshops[n.Id].ProductionLines.Count(l=>l.Waiting),available=w.Campaign.RobotBattles.AvailableFrom(w,n.Owner,n.Id)}).Where(n=>n.blocked>0).OrderByDescending(n=>n.blocked).Take(8).ToArray()
};
Console.WriteLine(JsonSerializer.Serialize(new{diagnostics}));
Console.WriteLine(JsonSerializer.Serialize(new{hostileMarches,committedRobots,encounterOutcomes,repeatedTargets=attackTargets.OrderByDescending(p=>p.Value).Take(10).Select(p=>new{target=p.Key,marches=p.Value})}));
var factions=Enumerable.Range(0,w.ActiveCount).Where(w.Relations.CanWinConquest).Select(root=>{
    var nodes=functional.Where(n=>w.Relations.Root(n.Owner)==root).ToArray();
    var borders=nodes.Where(Front).ToArray();
    return new{root,nodes=nodes.Length,fronts=borders.Length,available=nodes.Sum(n=>w.Campaign.RobotBattles.AvailableFrom(w,n.Owner,n.Id)),frontAvailable=borders.Sum(n=>w.Campaign.RobotBattles.AvailableFrom(w,n.Owner,n.Id)),defenders=nodes.Sum(n=>w.Robots.Nodes[n.Id].Robots.Count(r=>r.DefenseOnly && r.CanFight)),bestRatio=borders.SelectMany(n=>w.Campaign.RobotBattles.MarchTargets(w,n.Id).Where(t=>w.Nodes.All[t].Owner>=0 && !w.Relations.Allied(n.Owner,w.Nodes.All[t].Owner)).Select(t=>(double)w.Campaign.RobotBattles.AvailableFrom(w,n.Owner,n.Id)/Math.Max(1,w.Campaign.RobotBattles.DefendingRobots(w,n.Owner,t)))).DefaultIfEmpty(0).Max()};
}).ToArray();
Console.WriteLine(JsonSerializer.Serialize(new{leader=w.Relations.Leader,w.ComputerOnly,factions}));
Console.WriteLine(JsonSerializer.Serialize(new{rulers=Enumerable.Range(0,w.ActiveCount).Where(w.Relations.CanAct).Select(ruler=>{
    var home=w.Nodes.Current(ruler);int node=home?.Id??-1;
    return new{ruler,root=w.Relations.Root(ruler),node,locationOwner=home?.Owner??-1,pocket=w.Robots.Retinues[ruler].Robots.Count(w.Robots.Training[ruler].Eligible),local=node<0?0:w.Campaign.RobotBattles.AvailableFrom(w,ruler,node),neutral=node<0?Array.Empty<int>():w.Campaign.RobotBattles.MarchTargets(w,node).Where(t=>w.Nodes.All[t].Owner<0).ToArray()};
})}));
if(args.Length>2)AutoSaveStore.Write(args[2],saved);
