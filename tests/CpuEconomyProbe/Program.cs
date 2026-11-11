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
}foreach(var option in args.Where(a=>a.StartsWith("--allied-exits="))){
 int source=int.Parse(option.Split('=')[1]),owner=w.Nodes.All[source].Owner;
 var exits=w.Nodes.All.Where(n=>n.Functional && n.Owner>=0 && n.Id!=source && w.Relations.Allied(owner,n.Owner)).Select(n=>new{
 target=n.Id,owner=n.Owner,path=RobotLongDelivery.Path(w,source,n.Id,owner),free=stock.Nodes[n.Id].StorageLimit-stock.Nodes[n.Id].Count,
 disposal=stock.Workshops[n.Id].DisposalFacilities.Any(f=>f.HasDisposalFactory),computerEndpointAuthority=n.Owner==owner
 }).Where(n=>n.path.Length>1).ToArray();
 Console.WriteLine(JsonSerializer.Serialize(new{alliedExits=source,owner,exits}));
}if(args.Contains("--relay-head-test")){
 // A real reservation/advance on the deserialized copy, never the original save.
 var flags=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;
 var delivery=stock.LongDelivery;
 var reserve=typeof(RobotLongDelivery).GetMethod("ReserveComputer",flags)!;
 var advance=typeof(RobotLongDelivery).GetMethod("Advance",flags)!;
 int before=stock.Nodes[156].Count;
 var cargo=stock.Nodes[34].Robots.Single(r=>r.Id==495);
 var physical=cargo.ConnectedParts(RobotParts.Head);
 try{
  var order=(LongPartOrder)reserve.Invoke(delivery,new object[]{w,34,156,495L,RobotParts.Head,92L,3,0})!;
  advance.Invoke(delivery,new object[]{w});
  if(order.Node!=34 || order.Started || !order.Status.Contains("満杯") || stock.Nodes[156].Count!=before || !stock.Nodes[34].Robots.Any(r=>r.Id==495))
   throw new Exception("Head shipment must wait at the full relay without losing cargo.");
  Console.WriteLine(JsonSerializer.Serialize(new{relayHeadTest="PASS_WAIT",order.Status,order.Node,order.Started,physical=physical.ToString()}));
 }catch(System.Reflection.TargetInvocationException e) when(e.InnerException is InvalidOperationException){
  if((physical&stock.Nodes[156].Robots.Single(r=>r.Id==92).Parts)==0)throw;
  Console.WriteLine(JsonSerializer.Serialize(new{relayHeadTest="PASS_REJECT_CONNECTED",physical=physical.ToString(),reason=e.InnerException.Message}));
 }
 var reverse=(LongPartOrder)reserve.Invoke(delivery,new object[]{w,156,34,404L,RobotParts.Legs,495L,3,0})!;
 advance.Invoke(delivery,new object[]{w});
 if(reverse.Node!=156 || reverse.Started || !reverse.Status.Contains("満杯") || !stock.Nodes[156].Robots.Any(r=>r.Id==404))throw new Exception("Reverse assembly delivery must wait at relay157.");
 Console.WriteLine(JsonSerializer.Serialize(new{reverseAssembly="PASS_WAIT",reverse.Status,reverse.Node,reverse.Started,path=RobotLongDelivery.Path(w,156,34,3)}));}if(args.Contains("--relay-transfer-test")){
 var flags=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;
 var transfers=w.Campaign.FunctionTransfers;
 var graph=w.Nodes.All.Select(n=>w.Routes.Neighbors(n).Select(r=>w.Nodes.At(r.Target,r.Entry).Id).Distinct().ToArray()).ToArray();
 bool enclosed=ComputerFunctionTransfers.EnclosedPocket(graph,157,34,id=>w.Nodes.All[id].Owner==3 && !battles.LockedNode(id));
 int queen=w.Nodes.Current(3)?.Id??-1;
 var foreign=stock.Nodes[157].Robots.Single(r=>r.Id==393);
 bool reserve=(bool)typeof(NodeFunctionTransfers).GetMethod("ReserveComputer",flags)!.Invoke(transfers,new object[]{w,157,34,3})!;
 Console.WriteLine(JsonSerializer.Serialize(new{relayTransfer=reserve,enclosed,queen,foreignNative=foreign.NativeParts(3),persuadable=foreign.PersuadableParts(3).ToString(),neighbors=graph[157]}));
 if(reserve){
  typeof(NodeFunctionTransfers).GetMethod("Resolve",flags)!.Invoke(transfers,new object[]{w});
  var path=RobotLongDelivery.Path(w,156,34,3);
  var order=(LongPartOrder)typeof(RobotLongDelivery).GetMethod("ReserveComputer",flags)!.Invoke(stock.LongDelivery,new object[]{w,156,34,404L,RobotParts.Legs,495L,3,0})!;
  typeof(RobotLongDelivery).GetMethod("Advance",flags)!.Invoke(stock.LongDelivery,new object[]{w});
  var merged=stock.Nodes[34].Robots.Single(r=>r.Id==495);
  if(path.Length!=2 || merged.Parts!=RobotParts.Complete || stock.Nodes[156].Robots.Any(r=>r.Id==404) || stock.LongDelivery.Orders.Contains(order))throw new Exception("Transfer should bypass relay and complete shipment.");
  stock.Check();
  Console.WriteLine(JsonSerializer.Serialize(new{relayTransferResult="PASS",path,merged.Id,parts=merged.Parts.ToString(),merged.CanFight,eligible=stock.Training[3].Eligible(merged),frontFree=stock.Nodes[156].StorageLimit-stock.Nodes[156].Count}));
 }
}foreach(var option in args.Where(a=>a.StartsWith("--node-state="))){
 int id=int.Parse(option.Split('=')[1]);var node=w.Nodes.All[id];var store=stock.Nodes[id];
 Console.WriteLine(JsonSerializer.Serialize(new{nodeState=id,node.Owner,node.Functional,node.FunctionHost,node.FunctionUnits,store.Count,store.TotalCapacity,store.ReservedReturns,reserved=stock.Workshops[id].ReservedSlots,
 robots=store.Robots.Select(r=>new{r.Id,r.Owner,r.CanFight,r.DefenseOnly}),incomingReturns=stock.ReturningRobots.Where(r=>r.Node==id)}));
}foreach(var option in args.Where(a=>a.StartsWith("--target-force="))){
 int target=int.Parse(option.Split('=')[1]);
 var orders=new List<March>();ComputerJointAttacks.Plan(w,orders,true);
 var sources=w.Nodes.All.Where(n=>n.Functional && n.Owner>=0 && battles.CanAttackFrom(w,n.Owner,n.Id,target)).Select(n=>new{
 node=n.Id,owner=n.Owner,root=w.Relations.Root(n.Owner),ready=battles.SelectableRobots(w,n.Owner,n.Id,false).Select(r=>r.Id),
 guard=stock.Nodes[n.Id].Robots.Any(r=>r.Owner==n.Owner && r.DefenseOnly && r.CanFight),
 defense=battles.DefendingRobots(w,n.Owner,target),failed=battles.Encounters.Where(e=>e.Finished && e.Target==target && e.Battles.FirstOrDefault()?.Winner is not >=0).SelectMany(e=>e.Battles).Select(b=>b.Units.Count(u=>w.Relations.Allied(u.Owner,n.Owner))).DefaultIfEmpty(0).Max()
 }).ToArray();
 Console.WriteLine(JsonSerializer.Serialize(new{targetForce=target,owner=w.Nodes.All[target].Owner,sources,orders=orders.Where(o=>o.TargetNode==target)}));
}foreach(var option in args.Where(a=>a.StartsWith("--locate-robots="))){
 var ids=option.Split('=')[1].Split(',').Select(long.Parse).ToHashSet();
 Console.WriteLine(JsonSerializer.Serialize(new{locateRobots=ids,nodes=stock.Nodes.SelectMany((store,n)=>store.Robots.Where(r=>ids.Contains(r.Id)).Select(r=>new{node=n,nodeOwner=w.Nodes.All[n].Owner,r.Id,r.Owner,r.CanFight,r.DefenseOnly,r.LongDeliveryNumber,r.ReturnHomeCommander})),
 deliveries=stock.LongDelivery.Orders.Where(o=>ids.Contains(o.CargoId) || ids.Contains(o.RobotId))}));
}
if(args.Contains("--supply225-test")){
 var flags=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;
 var delivery=stock.LongDelivery;
 var reserve=typeof(RobotLongDelivery).GetMethod("ReserveComputer",flags)!;
 var advance=typeof(RobotLongDelivery).GetMethod("Advance",flags)!;
 var reinforcement=stock.Nodes[216].Robots.Single(r=>r.Id==349);
 var outgoing=stock.Nodes[225].Robots.Single(r=>r.Id==1330);
 var guards=stock.Nodes[216].Robots.Where(r=>r.Id!=349 && r.Owner==15 && r.CanFight).Select(r=>r.Id).ToArray();
 reserve.Invoke(delivery,new object[]{w,225,106,outgoing.Id,outgoing.Parts,null,15,4});
 reserve.Invoke(delivery,new object[]{w,216,225,reinforcement.Id,reinforcement.Parts,null,15,7});
 advance.Invoke(delivery,new object[]{w});
 if(!stock.Nodes[106].Robots.Any(r=>r.Id==1330) || !stock.Nodes[225].Robots.Any(r=>r.Id==349))throw new Exception("Supply chain did not deliver both robots.");
 stock.Check();
 Console.WriteLine(JsonSerializer.Serialize(new{supply225="PASS",remaining216=guards,reinforcement=reinforcement.Id,native=reinforcement.NativeParts(15),uses=reinforcement.CombatParts(15),rifles=reinforcement.CombatRifles(15)}));
}
if(args.Contains("--relay3-release-test")){
 const int relay=3,owner=1,destination=5;
 var flags=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;
 var delivery=stock.LongDelivery;
 var advance=typeof(RobotLongDelivery).GetMethod("Advance",flags)!;
 var reserve=typeof(RobotLongDelivery).GetMethod("ReserveComputer",flags)!;
 var first=delivery.Orders.Single(o=>o.Number==1);
 var assembly=delivery.Orders.Single(o=>o.Number==5);
 var store=stock.Nodes[relay];
 int initial=store.Count;
 if(store.HasSpace || first.Node!=7 || assembly.Node!=13 || assembly.Receiver!=26)
  throw new InvalidOperationException("Expected the turn-5 relay fixture.");
 advance.Invoke(delivery,new object[]{w});
 var completed=store.Robots.Single(r=>r.Id==26);
 if(store.Count!=initial || delivery.Orders.Contains(assembly) || !completed.CanFight ||
    !stock.CanShip(relay,completed) || stock.AssemblyLocked(relay,completed) || first.Started)
  throw new InvalidOperationException("Expected assembly in place while the first order remains blocked.");
 var target=stock.Nodes[destination];
 var outgoing=(LongPartOrder)reserve.Invoke(delivery,new object[]{w,relay,destination,completed.Id,completed.Parts,null,owner,target.Count})!;
 advance.Invoke(delivery,new object[]{w});
 if(delivery.Orders.Contains(outgoing) || !target.Robots.Any(r=>r.Id==26) || !store.HasSpace || first.Started)
  throw new InvalidOperationException("Expected the later departure to free one slot after the first order had been checked.");
 advance.Invoke(delivery,new object[]{w});
 if(first.Node!=relay || !first.Started || store.HasSpace)
  throw new InvalidOperationException("Expected the first order to enter the relay on the next pass.");
 advance.Invoke(delivery,new object[]{w});
 if(delivery.Orders.Contains(first) || !stock.Nodes[13].Robots.Single(r=>r.Id==64).CanFight)
  throw new InvalidOperationException("Expected the first order to complete the receiver at node 13.");
 stock.Check();
 Console.WriteLine(JsonSerializer.Serialize(new{relay3Release="PASS",assembled=completed.Id,outgoing=outgoing.Number,
  firstOrder=first.Number,first.Node,first.Status,relayCount=store.Count,turn=w.Population.Turn}));
}
if(args.Contains("--relay3-normal-trace")){
 void Trace(string phase){
  var relay=stock.Nodes[3];
  Console.WriteLine(JsonSerializer.Serialize(new{relayTrace=phase,turn=w.Population.Turn,
   relay.Count,relay.StorageLimit,ids=relay.Robots.Select(r=>r.Id),
   location26=Array.FindIndex(stock.Nodes,n=>n.Robots.Any(r=>r.Id==26)),
   orders=stock.LongDelivery.Orders.Select(o=>new{o.Number,o.Source,o.Target,o.Node,o.CargoId,o.Receiver,o.Status}),
   delivered=stock.LongDelivery.Results.Where(o=>o.Number is 1 or 5 or 6 || o.RobotId==26)
    .Select(o=>new{o.Number,o.Status})}));
 }
 Trace("initial");
 for(int step=0;step<4;step++){
  if(w.Campaign.Phase!=CampaignPhase.Domestic)
   throw new InvalidOperationException("Expected a domestic phase.");
  w.Campaign.FinishDomestic(w,true,deferDelivery:()=>{Trace("planned");return false;});
  Trace("delivered");
  w.Campaign.BeginMilitary();
  w.Campaign.Advance(w,-1,0,automaticAll:true,allPlanned:true);
  stock.Check();
  Trace("after-turn");
 }
}
