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
if(args.Contains("--supply-gap")){
    foreach(var n in w.Nodes.All.Where(n=>n.Functional && n.Owner>=0 && !Front(n))){
        int owner=n.Owner;
        var ready=battles.SelectableRobots(w,owner,n.Id,false).Where(r=>!stock.AssemblyLocked(n.Id,r) && stock.CanShip(n.Id,r)).ToArray();
        bool guard=stock.Nodes[n.Id].Robots.Any(r=>r.Owner==owner && r.DefenseOnly && r.CanFight);
        int surplus=Math.Max(0,ready.Length-(guard?0:1));if(surplus==0)continue;
        var targets=w.Nodes.All.Where(t=>t.Functional && t.Owner>=0 && w.Relations.Allied(owner,t.Owner) && Front(t)).Select(t=>{
            var path=RobotLongDelivery.Path(w,n.Id,t.Id,owner);
            return new{target=t.Id,owner=t.Owner,sameCountry=t.Owner==owner,free=stock.Nodes[t.Id].StorageLimit-stock.Nodes[t.Id].Count,path,
                fullRelays=path.Skip(1).SkipLast(1).Where(at=>!stock.Nodes[at].HasSpace).ToArray()};
        }).Where(t=>t.path.Length>1).ToArray();
        Console.WriteLine(JsonSerializer.Serialize(new{supplySource=n.Id,owner,root=w.Relations.Root(owner),surplus,
            locked=battles.LockedNode(n.Id),leave=stock.Workshops[n.Id].SupervisorLeaveRemaining,
            ownFronts=targets.Count(t=>t.sameCountry),alliedFronts=targets.Count(t=>!t.sameCountry),
            ownFreeClear=targets.Count(t=>t.sameCountry && t.free>0 && t.fullRelays.Length==0),
            alliedFreeClear=targets.Count(t=>!t.sameCountry && t.free>0 && t.fullRelays.Length==0),
            nearest=targets.OrderBy(t=>t.path.Length).ThenBy(t=>t.target).Take(6)}));
    }
}
if(args.Contains("--exchange-gate")){
        bool Busy(int node)=>battles.LockedNode(node) || w.Campaign.FunctionTransfers.Orders.Any(o=>o.Source==node || o.Target==node) ||
            stock.PartPlanning.Orders.Any(o=>o.Source==node || o.Target==node) || stock.PendingEdits.Any(o=>o.Node==node) ||
            stock.DirectDelivery.Orders.Any(o=>o.Source==node || o.Target==node) || w.Campaign.PlannedOrders.Any(o=>o.SourceNode==node || o.TargetNode==node);
        bool AnyViableAttack(int? root=null){
            var automated=Enumerable.Range(0,w.ActiveCount).Where(o=>w.Relations.CanAct(o) && (root==null || w.Relations.Root(o)==root)).ToHashSet();
            if(w.Campaign.PlannedOrders.Any(o=>automated.Contains(o.Ruler) && !o.Peaceful && (w.Nodes.All[o.TargetNode].Owner<0 || !w.Relations.Allied(o.Ruler,w.Nodes.All[o.TargetNode].Owner))))return true;
            foreach(var family in automated.GroupBy(w.Relations.Root)){
                var members=family.ToHashSet();
                var sources=w.Nodes.All.Where(n=>n.Functional && members.Contains(n.Owner) && !Busy(n.Id)).Select(n=>{
                    int count=battles.SelectableRobots(w,n.Owner,n.Id,false).Length;
                    bool guard=stock.Nodes[n.Id].Robots.Any(r=>r.Owner==n.Owner && r.DefenseOnly && r.CanFight);
                    return new{Node=n.Id,Owner=n.Owner,Count=Math.Max(0,count-(!guard && count>1?1:0))};
                }).Where(s=>s.Count>0).ToArray();
                var queens=family.Where(o=>w.Nodes.Current(o) is {} n && !battles.LockedNode(n.Id)).ToArray();
                var targets=sources.SelectMany(s=>battles.MarchTargets(w,s.Node)).Concat(queens.SelectMany(o=>battles.MarchTargets(w,w.Nodes.Current(o)!.Id))).Distinct();
                foreach(int target in targets.Where(t=>!battles.LockedNode(t) && (w.Nodes.All[t].Owner<0 || !w.Relations.Allied(family.Key,w.Nodes.All[t].Owner)))){
                    int defense=battles.DefendingRobots(w,family.Key,target);
                    if(w.Nodes.All[target].Owner<0 && defense==0 && queens.Any(o=>battles.CanMarch(w,o,target)))return true;
                    int ground=sources.Where(s=>battles.CanAttackFrom(w,s.Owner,s.Node,target)).Sum(s=>s.Count);
                    int pockets=queens.Where(o=>battles.CanSortie(w,o,target)).Sum(o=>battles.SelectableRobots(w,o,w.Nodes.Current(o)!.Id,true).Length);
                    if(ground+pockets>=defense+Math.Max(1,(defense+2)/3))return true;
                }
            }
            return false;
        }
    bool global=AnyViableAttack();
    foreach(var node in w.Nodes.All.Where(n=>n.Functional && n.Owner>=0 && Front(n) && !stock.Nodes[n.Id].HasSpace && battles.AvailableFrom(w,n.Owner,n.Id)<2)){
        int owner=node.Owner;
        var partial=stock.Nodes[node.Id].Robots.Where(r=>r.Owner==owner && r.Parts!=RobotParts.Complete && !stock.AssemblyLocked(node.Id,r)).ToArray();
        var sources=w.Nodes.All.Where(n=>n.Functional && n.Owner==owner && !Front(n) && !Busy(n.Id) && stock.Workshops[n.Id].SupervisorLeaveRemaining==0 && FunctionalDeliveryRoutes.Connected(w,n.Id,node.Id))
            .Where(n=>stock.Nodes[n.Id].Robots.Count(r=>r.Owner==owner && r.CanFight)>=2)
            .Where(n=>battles.SelectableRobots(w,owner,n.Id,false).Any(r=>!stock.AssemblyLocked(n.Id,r))).Select(n=>n.Id).ToArray();
        Console.WriteLine(JsonSerializer.Serialize(new{exchangeTarget=node.Id,owner,root=w.Relations.Root(owner),globalViableAttack=global,factionViableAttack=AnyViableAttack(w.Relations.Root(owner)),
            busy=Busy(node.Id),leave=stock.Workshops[node.Id].SupervisorLeaveRemaining,partials=partial.Select(r=>r.Id),sources}));
    }
}
if(args.Contains("--function-gates")){
    var graph=w.Nodes.All.Select(n=>w.Routes.Neighbors(n).Select(r=>w.Nodes.At(r.Target,r.Entry).Id).Distinct().ToArray()).ToArray();
    bool Busy(int id)=>w.Campaign.FunctionTransfers.Orders.Any(o=>o.Source==id || o.Target==id) ||
        stock.PartPlanning.Orders.Any(o=>o.Source==id || o.Target==id) || stock.DirectDelivery.Orders.Any(o=>o.Source==id || o.Target==id) ||
        stock.LongDelivery.Orders.Any(o=>o.Source==id || o.Target==id || o.Node==id) || stock.PendingEdits.Any(o=>o.Node==id) ||
        w.Campaign.PlannedOrders.Any(o=>o.SourceNode==id || o.TargetNode==id);
    foreach(var target in w.Nodes.All.Where(n=>n.Functional && n.Owner>=0 && w.Relations.CanAct(n.Owner) && Front(n)))
    foreach(int source in graph[target.Id].Where(id=>id!=target.Id && w.Nodes.All[id].Functional && w.Nodes.All[id].Owner==target.Owner)){
        int owner=target.Owner;
        bool strict=ComputerFunctionTransfers.EnclosedPocket(graph,source,target.Id,id=>w.Nodes.All[id].Owner==owner && !battles.LockedNode(id));
        bool allied=ComputerFunctionTransfers.EnclosedPocket(graph,source,target.Id,id=>w.Nodes.All[id].Owner>=0 && w.Relations.Allied(owner,w.Nodes.All[id].Owner) && !battles.LockedNode(id));
        Console.WriteLine(JsonSerializer.Serialize(new{functionSource=source,target=target.Id,owner,root=w.Relations.Root(owner),strictPocket=strict,alliedPocket=allied,sourceFront=Front(w.Nodes.All[source]),
            friendlyImmediateNeighbors=graph[source].All(id=>w.Nodes.All[id].Owner>=0 && w.Relations.Allied(owner,w.Nodes.All[id].Owner) && !battles.LockedNode(id)),
            busySource=Busy(source),busyTarget=Busy(target.Id),capacity=stock.Nodes[source].TotalCapacity+stock.Nodes[target.Id].TotalCapacity,
            reservedReturns=stock.Nodes[source].ReservedReturns+stock.Nodes[target.Id].ReservedReturns,
            overflow=battles.Reserves.Count(r=>r.Node==source || r.Node==target.Id),
            locked=battles.LockedNode(source) || battles.LockedNode(target.Id) || battles.LockedEdge(source,target.Id),phase=w.Campaign.Phase.ToString()}));
    }
}
foreach(var option in args.Where(a=>a.StartsWith("--target-supply="))){
 int target=int.Parse(option.Split('=')[1]),owner=w.Nodes.All[target].Owner;
 var routes=w.Nodes.All.Where(n=>n.Functional && n.Owner==owner && n.Id!=target).Select(n=>new{
 source=n.Id,toTarget=RobotLongDelivery.Path(w,n.Id,target,owner),fromTarget=RobotLongDelivery.Path(w,target,n.Id,owner),
 free=stock.Nodes[n.Id].StorageLimit-stock.Nodes[n.Id].Count,
 ready=battles.SelectableRobots(w,owner,n.Id,false).Where(r=>!stock.AssemblyLocked(n.Id,r) && stock.CanShip(n.Id,r)).Select(r=>r.Id),
 localDirect=FunctionalDeliveryRoutes.Connected(w,n.Id,target)
 }).ToArray();
 Console.WriteLine(JsonSerializer.Serialize(new{targetSupply=target,owner,root=w.Relations.Root(owner),routes}));
}