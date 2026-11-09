using FolderVerse;
using System.Text.Json;
if(args.Length<1)throw new ArgumentException("Usage: CpuEconomyProbe copied-save [source-node ...]");
object saved=AutoSaveStore.Read<object>(args[0]);
var w=saved is WorldSetup direct?direct:(WorldSetup)saved.GetType().GetProperty("World")!.GetValue(saved)!;
var stock=w.Robots;var battles=w.Campaign.RobotBattles;
bool Front(Node n)=>battles.MarchTargets(w,n.Id).Any(t=>w.Nodes.All[t].Owner>=0 && !w.Relations.Allied(n.Owner,w.Nodes.All[t].Owner));
var nodes=w.Nodes.All.Where(n=>n.Functional && n.Owner>=0).Select(n=>{
    var store=stock.Nodes[n.Id];var shop=stock.Workshops[n.Id];var robots=store.Robots.ToArray();
    var mergePairs=robots.SelectMany((a,i)=>robots.Skip(i+1).Select(b=>(a,b)))
        .Where(p=>(p.a.Parts&p.b.Parts)==0 && !stock.AssemblyLocked(n.Id,p.a) && !stock.AssemblyLocked(n.Id,p.b))
        .Count(p=>p.a.Merge(p.b,n.Owner).AssemblyCredit(n.Owner)>=stock.Training[n.Owner].RequiredNative);
    return new{node=n.Id,owner=n.Owner,front=Front(n),count=store.Count,capacity=store.StorageLimit,
        reserved=shop.ReservedSlots,policy=shop.Automation.ToString(),shop.AutoAssemblyDisabled,
        disposals=shop.DisposalFacilities.Select(f=>new{f.HasDisposalFactory,f.DisposalTarget,f.DisposalAge,f.DisposalPeriod}),
        disposalReservations=robots.Count(r=>r.Disposal),
        complete=robots.Count(r=>r.Parts==RobotParts.Complete),fighters=robots.Count(r=>r.CanFight),
        foreignFighters=robots.Count(r=>r.CanFight && r.Owner!=n.Owner),
        alliedForeignFighters=robots.Count(r=>r.CanFight && r.Owner!=n.Owner && w.Relations.Allied(r.Owner,n.Owner)),
        eligible=robots.Count(stock.Training[n.Owner].Eligible),
        guard=robots.Count(r=>r.DefenseOnly && r.CanFight),mergePairs,
        damagedOnly=robots.Count(r=>(r.Parts&~r.EffectiveDamagedParts)==0),
        parts=robots.GroupBy(r=>r.Parts.ToString()).ToDictionary(g=>g.Key,g=>g.Count()),
        waiting=shop.ProductionLines.Where(l=>l.Waiting).Select(l=>{
            int target=l.IntakeTarget??n.Id;var dest=stock.Nodes[target];
            return new{product=l.Product.ToString(),target,full=dest.Count>=dest.StorageLimit,
                atReservedLimit=dest.Count>=dest.StorageLimit-stock.Workshops[target].ReservedSlots,
                l.WaitingProductionTurns,l.Paused,l.CycleFinished};
        }).ToArray()};
}).ToArray();
Console.WriteLine(JsonSerializer.Serialize(new{w.WorldSeed,w.CastSeed,w.PlacementSeed,w.FortuneSeed,turn=w.Population.Turn,
    version=typeof(WorldSetup).Assembly.GetName().Version!.ToString(),nodes=nodes.Length,
    waiting=nodes.Sum(n=>n.waiting.Length),waitingFull=nodes.Where(n=>n.count>=n.capacity).Sum(n=>n.waiting.Length),
    waitingWithReserve=nodes.Where(n=>n.count>=n.capacity-n.reserved).Sum(n=>n.waiting.Length),
    waitingTargetFull=nodes.SelectMany(n=>n.waiting).Count(l=>l.full),
    waitingTargetReservedLimit=nodes.SelectMany(n=>n.waiting).Count(l=>l.atReservedLimit),
    complete=nodes.Sum(n=>n.complete),fighters=nodes.Sum(n=>n.fighters),eligible=nodes.Sum(n=>n.eligible),
    frontFighters=nodes.Where(n=>n.front).Sum(n=>n.fighters),rearFighters=nodes.Where(n=>!n.front).Sum(n=>n.fighters),
    foreignFighters=nodes.Sum(n=>n.foreignFighters),alliedForeignFighters=nodes.Sum(n=>n.alliedForeignFighters),
    mergePairs=nodes.Sum(n=>n.mergePairs),damagedOnly=nodes.Sum(n=>n.damagedOnly),
    waitingProducts=nodes.SelectMany(n=>n.waiting).GroupBy(l=>l.product).ToDictionary(g=>g.Key,g=>g.Count()),
    delivery=stock.LongDelivery.Orders.GroupBy(o=>o.Status).ToDictionary(g=>g.Key,g=>g.Count())}));
foreach(var node in nodes.OrderByDescending(n=>n.waiting.Length).ThenBy(n=>n.node))Console.WriteLine(JsonSerializer.Serialize(node));
foreach(int source in args.Skip(1).Where(a=>!a.StartsWith("--")).Select(int.Parse)){
    int owner=w.Nodes.All[source].Owner;
    bool Busy(int node)=>battles.LockedNode(node) || w.Campaign.FunctionTransfers.Orders.Any(o=>o.Source==node || o.Target==node) ||
        stock.PartPlanning.Orders.Any(o=>o.Source==node || o.Target==node) || stock.PendingEdits.Any(o=>o.Node==node) ||
        stock.DirectDelivery.Orders.Any(o=>o.Source==node || o.Target==node) || w.Campaign.PlannedOrders.Any(o=>o.SourceNode==node || o.TargetNode==node);
    var own=w.Nodes.All.Where(n=>n.Functional && n.Owner==owner && !Busy(n.Id)).ToArray();
    var incoming=new Dictionary<int,List<int>>();
    foreach(var n in w.Nodes.All.Where(n=>n.Functional && n.Owner>=0 && w.Relations.Allied(owner,n.Owner)))
        foreach(int next in RobotLongDelivery.Neighbors(w,n.Id,owner)){
            if(!incoming.TryGetValue(next,out var list))incoming[next]=list=new();list.Add(n.Id);
        }
    var distance=own.Where(Front).ToDictionary(n=>n.Id,n=>0);var queue=new Queue<int>(distance.Keys);
    while(queue.TryDequeue(out int at))if(incoming.TryGetValue(at,out var preceding))
        foreach(int next in preceding)if(distance.TryAdd(next,distance[at]+1))queue.Enqueue(next);
    Console.WriteLine(JsonSerializer.Serialize(new{routeSource=source,owner,canAct=w.Relations.CanAct(owner),busy=Busy(source),
        phase=w.Campaign.Phase.ToString(),leave=stock.Workshops[source].SupervisorLeaveRemaining,
        frontDistance=distance.GetValueOrDefault(source,-1),
        robots=stock.Nodes[source].Robots.Select(r=>new{r.Id,r.Owner,parts=r.Parts.ToString(),native=r.NativeParts(owner),r.Disposal,r.CanFight,r.DefenseOnly,locked=stock.AssemblyLocked(source,r),canShip=stock.CanShip(source,r),r.LongDeliveryNumber}),
        candidates=own.Where(n=>n.Id!=source && distance.GetValueOrDefault(n.Id,int.MaxValue)<distance.GetValueOrDefault(source,int.MaxValue)).Select(n=>{
            var path=RobotLongDelivery.Path(w,source,n.Id,owner);
            return new{target=n.Id,frontDistance=distance[n.Id],path,free=stock.Nodes[n.Id].StorageLimit-stock.Nodes[n.Id].Count,
                reservedSlots=stock.LongDelivery.Orders.Where(o=>o.Target==n.Id && !o.Receiver.HasValue).Select(o=>o.TargetSlot),
                fullRelays=path.Skip(1).SkipLast(1).Where(p=>!stock.Nodes[p].HasSpace).ToArray()};
        })}));
}
if(args.Contains("--plan")){
    var before=stock.LongDelivery.Orders.Select(o=>o.Number).ToHashSet();
    var disposed=stock.Nodes.SelectMany(s=>s.Robots).Where(r=>r.Disposal).Select(r=>r.Id).ToHashSet();
    ComputerLogistics.Plan(w,true);
    Console.WriteLine(JsonSerializer.Serialize(new{newLongOrders=stock.LongDelivery.Orders.Where(o=>!before.Contains(o.Number)).Select(o=>new{o.Number,o.Commander,o.Source,o.Target,o.RobotId,o.Parts}),
        newDisposals=stock.Nodes.SelectMany((s,n)=>s.Robots.Where(r=>r.Disposal && !disposed.Contains(r.Id)).Select(r=>new{node=n,r.Id,r.Owner,parts=r.Parts.ToString()}))}));
}
if(args.Contains("--battle-history")){
    Console.WriteLine(JsonSerializer.Serialize(new{leader=w.Relations.Leader,w.ComputerOnly,pending=w.Relations.Pending,
        foreignRulers=Enumerable.Range(0,w.ActiveCount).Where(o=>w.Relations.Powered[o] && w.Nodes.Current(o) is {} n && n.Owner>=0 && !w.Relations.Allied(o,n.Owner))
            .Select(o=>new{owner=o,root=w.Relations.Root(o),node=w.Nodes.Current(o)!.Id,host=w.Nodes.Current(o)!.Owner})}));
    foreach(var group in battles.Encounters.Where(e=>e.Finished).GroupBy(e=>e.Target).OrderByDescending(g=>g.Count()).Take(10))
        Console.WriteLine(JsonSerializer.Serialize(new{historyTarget=group.Key,count=group.Count(),recent=group.OrderByDescending(e=>e.Id).Take(4).Select(e=>new{
            e.Id,e.Defender,kind=e.Kind.ToString(),battles=e.Battles.Select(b=>new{b.Winner,b.Turn,
                armies=b.Armies.Select(a=>new{a.Owner,a.HomeNode,a.Withdrawing,a.NoWillCount,limit=b.Rules.NoWillTurns,
                    robots=b.Units.Count(u=>u.Owner==a.Owner && u.Robot!=null)+a.Waiting.Count(u=>u.Robot!=null)})})})}));
}
if(args.Contains("--returns")){
    foreach(var group in stock.ReturningRobots.GroupBy(j=>j.Country).OrderByDescending(g=>g.Count())){
        int country=group.Key;var homes=w.Nodes.All.Where(n=>n.Functional && n.Owner==country).ToArray();
        var queen=w.Nodes.Current(country);
        var paths=queen==null?Array.Empty<int[]>():homes.Select(n=>RobotLongDelivery.Path(w,queen.Id,n.Id,country)).Where(p=>p.Length>0).ToArray();
        Console.WriteLine(JsonSerializer.Serialize(new{returnCountry=country,powered=w.Relations.Powered[country],root=w.Relations.Root(country),count=group.Count(),
            queenNode=queen?.Id,reachableHomes=paths.Length,nearestHomeDistance=paths.Select(p=>p.Length-1).DefaultIfEmpty(-1).Min(),
            statuses=group.GroupBy(j=>j.Status).ToDictionary(g=>g.Key,g=>g.Count()),nodes=homes.Length,
            capacity=homes.Sum(n=>stock.Nodes[n.Id].StorageLimit),occupied=homes.Sum(n=>stock.Nodes[n.Id].Count),
            projectedEligible=group.Count(j=>stock.Training[country].Eligible(j.Robot with {Owner=country,ReturnHomeCommander=null,ReturnHomeStatus=null})),
            arrivalNodes=group.GroupBy(j=>j.Node).Select(g=>new{node=g.Key,count=g.Count(),owner=w.Nodes.All[g.Key].Owner,
                free=stock.Nodes[g.Key].StorageLimit-stock.Nodes[g.Key].Count}),
            currentHomes=homes.Select(n=>new{node=n.Id,front=Front(n),free=stock.Nodes[n.Id].StorageLimit-stock.Nodes[n.Id].Count,
                fighters=stock.Nodes[n.Id].Robots.Count(r=>r.CanFight),available=battles.AvailableFrom(w,country,n.Id),
                guard=stock.Nodes[n.Id].Robots.Count(r=>r.DefenseOnly),queenHere=w.Nodes.Current(country)?.Id==n.Id})}));
    }
}

if(args.Contains("--return-space")){
    foreach(var group in stock.ReturningRobots.Where(j=>w.Nodes.All[j.Node].Owner==j.Country && w.Relations.Powered[j.Country]).GroupBy(j=>j.Node)){
        int source=group.Key,owner=w.Nodes.All[source].Owner;var store=stock.Nodes[source];
        if(store.HasSpace)continue;
        var cargo=store.Robots.Where(r=>r.Owner==owner && r.Parts!=RobotParts.Complete && !r.Disposal && !stock.AssemblyLocked(source,r) && stock.CanShip(source,r)).ToArray();
        var routes=w.Nodes.All.Where(n=>n.Functional && n.Owner==owner && n.Id!=source && stock.Nodes[n.Id].HasSpace).Select(n=>{
            var path=RobotLongDelivery.Path(w,source,n.Id,owner);
            return new{target=n.Id,free=stock.Nodes[n.Id].StorageLimit-stock.Nodes[n.Id].Count,path,
                fullRelays=path.Skip(1).SkipLast(1).Where(at=>!stock.Nodes[at].HasSpace).ToArray()};
        }).Where(r=>r.path.Length>1).ToArray();
        Console.WriteLine(JsonSerializer.Serialize(new{returnSpaceSource=source,owner,waiting=group.Count(),cargo=cargo.Select(r=>new{r.Id,parts=r.Parts.ToString()}),
            locked=battles.LockedNode(source),leave=stock.Workshops[source].SupervisorLeaveRemaining,routes}));
    }
}