using FolderVerse;
using System.Text.Json;
if(args.Length==0)throw new ArgumentException("Usage: CpuCampaignBench saved-game [turns] [output-copy]");
object saved=AutoSaveStore.Read<object>(args[0]);var w=saved is WorldSetup direct?direct:(WorldSetup)saved.GetType().GetProperty("World")!.GetValue(saved)!;int steps=args.Length>1?int.Parse(args[1]):20;
if(args.Contains("--no-display-leader")){if(!w.ComputerOnly)throw new InvalidOperationException("Capture-routing experiment requires a computer-only copy.");w.Relations.Leader=-1;}
w.Robots.ConfigureFactories(w);foreach(var battle in w.Campaign.RobotBattles.Encounters.SelectMany(e=>e.Battles))battle.Allied=w.Relations.Allied;
bool Conquered()=>w.Relations.ProvisionalRanking(w).Any(r=>r.Cells==w.Cells.Length && w.Relations.CanWinConquest(r.Ruler));
void Report(int step){var factionSizes=w.Nodes.All.Where(n=>n.Owner>=0).GroupBy(n=>w.Relations.Root(n.Owner)).Select(g=>g.Count()).ToArray();var all=w.Robots.Nodes.SelectMany(n=>n.Robots).ToArray();Console.WriteLine(JsonSerializer.Serialize(new{step,w.WorldSeed,w.CastSeed,w.PlacementSeed,w.FortuneSeed,version=typeof(WorldSetup).Assembly.GetName().Version!.ToString(),totalCells=w.Cells.Length,totalNodes=w.Nodes.All.Length,conquered=Conquered(),largestFactionNodes=factionSizes.DefaultIfEmpty(0).Max(),largestFactionCells=w.Relations.ProvisionalRanking(w).Where(r=>w.Relations.CanWinConquest(r.Ruler)).Select(r=>r.Cells).DefaultIfEmpty(0).Max(),turn=w.Population.Turn,independent=Enumerable.Range(0,w.ActiveCount).Count(o=>w.Relations.CanWinConquest(o)),powered=w.Relations.Powered.Take(w.ActiveCount).Count(p=>p),occupied=all.Length,capacity=w.Robots.Nodes.Sum(n=>n.TotalCapacity),deployable=w.Nodes.All.Where(n=>n.Owner>=0).Sum(n=>w.Campaign.RobotBattles.AvailableFrom(w,n.Owner,n.Id)),blocked=w.Robots.Workshops.Sum(s=>s.ProductionLines.Count(l=>l.Waiting)),disposal=all.Count(r=>r.Disposal),returning=all.Count(r=>r.ReturnHomeCommander.HasValue)+w.Robots.ReturningRobots.Count,longDelivery=w.Robots.LongDelivery.Orders.Count,ongoing=w.Campaign.RobotBattles.Encounters.Count(e=>!e.Finished),maxBattle=w.Campaign.RobotBattles.Encounters.Where(e=>!e.Finished).SelectMany(e=>e.Battles).Select(b=>b.Turn).DefaultIfEmpty(0).Max()}));}
var attackTargets=new Dictionary<int,int>();var encounterOutcomes=new Dictionary<string,int>();int hostileMarches=0;long committedRobots=0;
Report(0);
for(int step=1;step<=steps;step++){
    w.Robots.PrepareDomestic(w.Population.Turn,(a,b)=>FunctionalDeliveryRoutes.Connected(w,a,b),n=>w.Nodes.All[n].Owner,w.Campaign.RobotBattles.LockedNode);
    var previousRoots=Enumerable.Range(0,w.ActiveCount).Select(w.Relations.Root).ToArray();
    var previousEncounters=w.Campaign.RobotBattles.Encounters.Select(e=>e.Id).ToHashSet();
    w.Campaign.FinishDomestic(w,true);w.Campaign.BeginMilitary();w.Campaign.Advance(w,-1,0,automaticAll:true,allPlanned:true,deferResolution:orders=>{
        foreach(var order in orders.Where(o=>!o.Peaceful && w.Nodes.All[o.TargetNode].Owner>=0 && !w.Relations.Allied(o.Ruler,w.Nodes.All[o.TargetNode].Owner))){hostileMarches++;committedRobots+=order.Fighters;attackTargets[order.TargetNode]=attackTargets.GetValueOrDefault(order.TargetNode)+1;}return false;
    });
    foreach(var encounter in w.Campaign.RobotBattles.Encounters.Where(e=>!previousEncounters.Contains(e.Id))){
        int winner=encounter.Battles.FirstOrDefault()?.Winner??-1;string result=!encounter.Finished?"ongoing":winner<0?"no-winner":encounter.Defender>=0 && previousRoots[winner]==previousRoots[encounter.Defender]?"defender-held":"attacker-won";
        string key=$"{encounter.Kind}:{result}";encounterOutcomes[key]=encounterOutcomes.GetValueOrDefault(key)+1;
    }
    foreach(var captive in w.Relations.Pending.ToArray())w.Relations.Choose(w,captive,w.Relations.Accepts(w,captive.Ruler)?DefeatedChoice.Recruit:w.Relations.CanMercy(w,captive)?DefeatedChoice.Release:DefeatedChoice.RemoveBattery);
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
