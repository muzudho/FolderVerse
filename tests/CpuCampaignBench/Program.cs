using FolderVerse;
using System.Text.Json;
using System.Security.Cryptography;
if (args.Contains("--withdrawal-origin-fixture"))
{
    var battle = new BattleState(1);
    battle.Units.Add(new BattleUnit { Id = 101, Owner = 15, Robot = new Robot(101, 15, RobotParts.Complete), Retreated = true });
    battle.Units.Add(new BattleUnit { Id = 202, Owner = 15, Robot = new Robot(202, 15, RobotParts.Complete), Retreated = true });
    battle.Units.Add(new BattleUnit { Id = 303, Owner = 15, Robot = new Robot(303, 15, RobotParts.Complete), Retreated = true });
    battle.Units.Add(new BattleUnit { Id = 404, Owner = 15, Robot = new Robot(404, 15, RobotParts.Complete), Retreated = true });
    var origins = new Dictionary<long, int> { [101] = 225, [202] = 216, [303] = 7 };
    var groups = WithdrawalGroups(battle, 7, origins).ToDictionary(group => group.Key.Source);
    if (groups.Count != 2 || groups[225].Single().Id != 101 || groups[216].Single().Id != 202)
        throw new Exception("Withdrawal attribution included residents, pocket units or a different source.");
    Console.WriteLine("PASS joint withdrawal attribution excludes residents and units without a node origin.");
    return;
}
if(args.Length==0)throw new ArgumentException("Usage: CpuCampaignBench saved-game [turns] [output-copy]");
object saved=AutoSaveStore.Read<object>(args[0]);var w=saved is WorldSetup direct?direct:(WorldSetup)saved.GetType().GetProperty("World")!.GetValue(saved)!;int steps=args.Length>1?int.Parse(args[1]):20;
if (args.Contains("--short-segment-fixture"))
{
    // Force continuation in an isolated fixture while retaining the other battle parameters.
    var rules = w.Campaign.RobotBattles.Rules;
    w.Campaign.RobotBattles.Rules = new BattleRules
    {
        SegmentTurns = 10,
        NoWillTurns = rules.NoWillTurns,
        FlagCaptureTurns = rules.FlagCaptureTurns,
        FlagDefenseTurns = rules.FlagDefenseTurns,
        PersuasionRadius = rules.PersuasionRadius,
        PersuasionPercent = rules.PersuasionPercent
    };
}
if(args.Contains("--no-display-leader")){if(!w.ComputerOnly)throw new InvalidOperationException("Capture-routing experiment requires a computer-only copy.");w.Relations.Leader=-1;}
var tracedTargets=args.Where(a=>a.StartsWith("--trace-target=")).Select(a=>int.Parse(a.Split('=')[1])).ToHashSet();
var tracedStockNodes=args.Where(a=>a.StartsWith("--trace-stock-node=",StringComparison.Ordinal))
    .Select(a=>int.Parse(a.Split('=')[1])).Distinct().Order().ToArray();
var tracedQueenRoutes=args.Where(a=>a.StartsWith("--trace-queen-route=",StringComparison.Ordinal))
    .Select(a=>a.Split('=')[1].Split(':').Select(int.Parse).ToArray()).ToArray();
if(tracedQueenRoutes.Any(route=>route.Length!=2 || route[0]<0 || route[0]>=w.ActiveCount ||
    route[1]<0 || route[1]>=w.Nodes.All.Length))
    throw new ArgumentException("--trace-queen-route=RULER:TARGET uses zero-based IDs.");
if(tracedStockNodes.Any(node=>node<0 || node>=w.Nodes.All.Length))
    throw new ArgumentOutOfRangeException("--trace-stock-node");
void TraceStock(int step,string phase)
{
    foreach(int node in tracedStockNodes)
        Console.WriteLine(JsonSerializer.Serialize(new {trace="stock",step,phase,node,owner=w.Nodes.All[node].Owner,
            robots=w.Robots.Nodes[node].Robots.OrderBy(r=>r.Id)
                .Select(r=>new{r.Id,r.Owner,r.Parts,r.Role,r.DamagedParts,r.CanFight}),
            intakes=w.Robots.ProductionIntakes.Where(i=>i.Source==node || (i.Target??i.Source)==node)
                .Select(i=>new{i.Source,i.Target,i.Product.Id,i.Product.Parts}),
            edits=w.Robots.PendingEdits.Where(e=>e.Node==node).Select(e=>new{e.Ids,e.SplitPart}),
            direct=w.Robots.DirectDelivery.Orders.Where(o=>o.Target==node || o.Source==node)
                .Select(o=>new{o.Number,o.Source,o.Target,o.RobotId,o.Part,o.TargetSlot}),
            completed=w.Robots.DirectDelivery.CompletedArrivals.Where(a=>w.Robots.Nodes[node].Robots.Any(r=>r.Id==a.Value)),
            transfers=w.Robots.Transport.LastTransfers.Where(t=>t.Source==node || t.Target==node)}));
}
var holdAgainst=args.Where(a=>a.StartsWith("--hold-against=")).Select(a=>a.Split('=')[1].Split(':').Select(int.Parse).ToArray()).ToArray();
if(holdAgainst.Any(h=>h.Length!=3 || h[0]<1 || h[0]>steps || h[1]<0 || h[2]<0 ||
    h[1]>=w.ActiveCount || h[2]>=w.ActiveCount || h[1]==h[2]))
    throw new ArgumentException("--hold-against=STEP:RULER:OPPONENT uses a simulation step and zero-based ruler slots.");
const string beginnerOption = "--beginner-military=";
bool beginnerPocket = args.Contains("--beginner-pocket");
const string guardNeutralOption="--guard-neutral-queen=";
var guardNeutralRulers=args.Where(a=>a.StartsWith(guardNeutralOption,StringComparison.Ordinal))
    .Select(a=>int.Parse(a[guardNeutralOption.Length..])).ToHashSet();
if(guardNeutralRulers.Any(r=>r<0 || r>=w.ActiveCount))throw new ArgumentOutOfRangeException(guardNeutralOption);
var beginnerActors = args.Where(a => a.StartsWith(beginnerOption, StringComparison.Ordinal))
    .Select(a => int.Parse(a[beginnerOption.Length..])).Distinct().ToArray();
if (beginnerActors.Any(actor => actor < 0 || actor >= w.ActiveCount))
    throw new ArgumentOutOfRangeException(beginnerOption, "Use a zero-based ruler slot.");
const string skipQueenOption="--skip-queen-order=";
var skipQueenOrders=args.Where(a=>a.StartsWith(skipQueenOption,StringComparison.Ordinal))
    .Select(a=>a[skipQueenOption.Length..].Split(':').Select(int.Parse).ToArray()).ToArray();
if(skipQueenOrders.Any(o=>o.Length!=3 || o[0]<1 || o[0]>steps || o[1]<0 || o[1]>=w.ActiveCount ||
    o[2]<0 || o[2]>=w.Nodes.All.Length))
    throw new ArgumentException("--skip-queen-order=STEP:RULER:TARGET uses zero-based ruler and node IDs.");
bool strategy=args.Contains("--strategy-report") || holdAgainst.Length>0 || beginnerActors.Length>0 || skipQueenOrders.Length>0;
bool traceAll=args.Contains("--trace-all") || strategy;
var clearNodes=args.Where(a=>a.StartsWith("--clear-head-node=")).Select(a=>int.Parse(a.Split('=')[1])).ToHashSet();
var rally=args.Where(a=>a.StartsWith("--rally-ruler=")).Select(a=>a.Split('=')[1].Split(':').Select(int.Parse).ToArray()).ToArray();
var swaps=args.Where(a=>a.StartsWith("--swap-head=") || a.StartsWith("--swap-head-once="))
    .Select(a=>(Path:a.Split('=')[1].Split(':').Select(int.Parse).ToArray(),Once:a.StartsWith("--swap-head-once="))).ToArray();
var swapped=new HashSet<int>();
bool requireRepeat = args.Contains("--repeat-return-relief");
bool tryReturnRelief = requireRepeat || args.Contains("--auto-return-relief");
var repeatedWithdrawals = new Dictionary<(int Source, int Target, int Owner), (long[] Ids, int Step, int Count)>();
int startingTurn = w.Population.Turn;
string SaveHash(string path)
{
    using var stream = File.OpenRead(path);
    return Convert.ToHexString(SHA256.HashData(stream));
}
string historyInput = args[0] + ".withdrawals.json";
if (requireRepeat && File.Exists(historyInput))
{
    var history = JsonSerializer.Deserialize<WithdrawalHistory>(File.ReadAllText(historyInput))!;
    if (history.SaveHash != SaveHash(args[0]))
        throw new InvalidDataException("Withdrawal history belongs to a different save.");
    foreach (var entry in history.Entries)
        repeatedWithdrawals.Add((entry.Source, entry.Target, entry.Owner), (entry.Ids, entry.Turn, entry.Count));
}
static IEnumerable<IGrouping<(int Source, int Owner), BattleUnit>> WithdrawalGroups(
    BattleState battle, int target, IReadOnlyDictionary<long, int> origins)
    => battle.Units.Where(unit => unit.Robot != null && origins.TryGetValue(unit.Id, out int source) && source != target)
        .GroupBy(unit => (Source: origins[unit.Id], unit.Owner));

void RecordWithdrawal(RobotEncounter encounter, int turn, string result, bool continued, IReadOnlyDictionary<long, int> origins)
{
    if (encounter.Kind != BattleKind.Node || result == "ongoing")
        return;
    foreach (var battle in encounter.Battles)
    foreach (var group in WithdrawalGroups(battle, encounter.Target, origins))
    {
        var key = (group.Key.Source, encounter.Target, group.Key.Owner);
        var units = group.ToArray();
        if (result != "defender-held" || units.Length is < 1 or > 2 || !units.All(unit => unit.Retreated))
        {
            repeatedWithdrawals.Remove(key);
            continue;
        }
        var ids = units.Select(unit => unit.Id).Order().ToArray();
        int count = repeatedWithdrawals.TryGetValue(key, out var last) && turn - last.Step <= 6 &&
            ids.SequenceEqual(last.Ids) ? last.Count + 1 : 1;
        repeatedWithdrawals[key] = (ids, turn, count);
        if (continued)
            Console.WriteLine(JsonSerializer.Serialize(new { trace = "continued-withdrawal", turn, encounter.Id,
                source = group.Key.Source, target = encounter.Target, owner = group.Key.Owner, ids, count }));
    }
}
void PlanReturnRelief(int step)
{
    var stock = w.Robots;
    var manual = w.ManualRulers().ToHashSet();
    var busy = w.Campaign.PlannedOrders.SelectMany(order => new[] { order.SourceNode, order.TargetNode })
        .Concat(stock.LongDelivery.Orders.SelectMany(order => new[] { order.Source, order.Target }))
        .Concat(stock.DirectDelivery.Orders.SelectMany(order => new[] { order.Source, order.Target }))
        .Concat(stock.PartPlanning.Orders.SelectMany(order => new[] { order.Source, order.Target }))
        .Concat(w.Campaign.FunctionTransfers.Orders.SelectMany(order => new[] { order.Source, order.Target }))
        .Concat(stock.PendingEdits.Select(edit => edit.Node)).ToHashSet();
    bool Available(int node) => !busy.Contains(node) && !w.Campaign.RobotBattles.LockedNode(node)
        && stock.Workshops[node].SupervisorLeaveRemaining == 0;
    var reserve = typeof(RobotLongDelivery).GetMethod("ReserveComputer",
        System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!;
    var destinations = stock.ReturningRobots
        .Where(wait => (wait.Robot with { ReturnHomeCommander = null }).CanFight)
        .Select(wait => (Node: wait.Node, Owner: wait.Country)).Distinct().OrderBy(wait => wait.Node);
    foreach (var (source, owner) in destinations)
    {
        if (manual.Contains(owner) || !w.Relations.CanAct(owner) || !w.Nodes.All[source].Functional ||
            w.Nodes.All[source].Owner != owner || stock.Nodes[source].HasSpace || !Available(source))
            continue;
        if (requireRepeat && !repeatedWithdrawals.Any(pair => pair.Key.Source == source && pair.Key.Owner == owner &&
            pair.Value.Count >= 2 && startingTurn + step - pair.Value.Step <= 6 &&
            w.Nodes.All[pair.Key.Target].Owner >= 0 && !w.Relations.Allied(owner, w.Nodes.All[pair.Key.Target].Owner)))
            continue;
        var cargo = stock.Nodes[source].Robots
            .Where(robot => robot.Owner == owner && robot.Parts != RobotParts.Complete && !robot.Disposal &&
                !stock.AssemblyLocked(source, robot) && stock.CanShip(source, robot))
            .OrderBy(robot => robot.NativeParts(owner)).ThenBy(robot => robot.Id).FirstOrDefault();
        if (cargo == null)
            continue;
        foreach (int target in RobotLongDelivery.Neighbors(w, source, owner).Order())
        {
            var receiver = stock.Nodes[target];
            if (w.Nodes.All[target].Owner != owner || !Available(target) || !receiver.HasSpace ||
                stock.ReturningRobots.Any(wait => wait.Node == target))
                continue;
            try
            {
                reserve.Invoke(stock.LongDelivery, new object[] { w, source, target, cargo.Id,
                    cargo.Parts, null, owner, receiver.Count });
            }
            catch (System.Reflection.TargetInvocationException error) when (error.InnerException is InvalidOperationException)
            {
                continue;
            }
            busy.Add(source);
            busy.Add(target);
            Console.WriteLine(JsonSerializer.Serialize(new { trace = "auto-return-relief", step, source, target, owner, cargo.Id }));
            break;
        }
    }
}
w.Robots.ConfigureFactories(w);foreach(var battle in w.Campaign.RobotBattles.Encounters.SelectMany(e=>e.Battles))battle.Allied=w.Relations.Allied;
bool Conquered()=>w.Relations.ProvisionalRanking(w).Any(r=>r.Cells==w.Cells.Length && w.Relations.CanWinConquest(r.Ruler));
var initialRanks=strategy?StrategyRanks():Array.Empty<StrategyRank>();
var previousRanks=initialRanks;
var previousLeaders=TerritoryLeaders(initialRanks);
int leaderChanges=0, overtakes=0, removedOrders=0;
StrategyRank[] StrategyRanks()=>w.Relations.ProvisionalRanking(w).Where(r=>w.Relations.CanWinConquest(r.Ruler))
    .Select(r=>new StrategyRank(r.Ruler,r.Rank,r.Cells,r.Nodes)).ToArray();
static int[] TerritoryLeaders(StrategyRank[] ranks)
{
    if(ranks.Length==0)return Array.Empty<int>();
    int maximum=ranks.Max(r=>r.Cells);
    return ranks.Where(r=>r.Cells==maximum).Select(r=>r.Ruler).Order().ToArray();
}
if(strategy)
{
    var models=initialRanks.Select(r=>{
        var nodes=w.Nodes.All.Where(n=>n.Owner>=0 && w.Relations.Root(n.Owner)==r.Ruler).ToArray();
        var units=nodes.SelectMany(n=>w.Robots.Nodes[n.Id].Robots)
            .Concat(Enumerable.Range(0,w.ActiveCount).Where(i=>w.Relations.Root(i)==r.Ruler).SelectMany(i=>w.Robots.Retinues[i].Robots))
            .Where(u=>u.CanFight).ToArray();
        var equipment=units.Select(u=>u.Equipment(u.Owner,w.EquipmentMode)).ToArray();
        return new {r.Ruler,r.Rank,r.Cells,r.Nodes,completeRobots=units.Length,
            shields=equipment.Sum(e=>e.Shields),daggers=equipment.Sum(e=>e.Daggers),rifles=equipment.Sum(e=>e.Rifles),
            productionLanes=nodes.Sum(n=>w.Robots.Workshops[n.Id].ProductionLines.Length)};
    }).ToArray();
    Console.WriteLine(JsonSerializer.Serialize(new {trace="strategy-initial",startingTurn,w.WorldSeed,w.CastSeed,w.PlacementSeed,w.FortuneSeed,
        version=typeof(WorldSetup).Assembly.GetName().Version!.ToString(),equipmentMode=w.EquipmentMode.ToString(),
        displayLeader=w.Relations.Leader,inputHash=SaveHash(args[0]),modelHash=SaveHash(typeof(WorldSetup).Assembly.Location),
        probeHash=SaveHash(typeof(StrategyRank).Assembly.Location),holds=holdAgainst,leaders=previousLeaders,factions=models}));
}
void Report(int step){var factionSizes=w.Nodes.All.Where(n=>n.Owner>=0).GroupBy(n=>w.Relations.Root(n.Owner)).Select(g=>g.Count()).ToArray();var all=w.Robots.Nodes.SelectMany(n=>n.Robots).ToArray();Console.WriteLine(JsonSerializer.Serialize(new{step,w.WorldSeed,w.CastSeed,w.PlacementSeed,w.FortuneSeed,version=typeof(WorldSetup).Assembly.GetName().Version!.ToString(),totalCells=w.Cells.Length,totalNodes=w.Nodes.All.Length,conquered=Conquered(),largestFactionNodes=factionSizes.DefaultIfEmpty(0).Max(),largestFactionCells=w.Relations.ProvisionalRanking(w).Where(r=>w.Relations.CanWinConquest(r.Ruler)).Select(r=>r.Cells).DefaultIfEmpty(0).Max(),turn=w.Population.Turn,independent=Enumerable.Range(0,w.ActiveCount).Count(o=>w.Relations.CanWinConquest(o)),powered=w.Relations.Powered.Take(w.ActiveCount).Count(p=>p),occupied=all.Length,capacity=w.Robots.Nodes.Sum(n=>n.TotalCapacity),deployable=w.Nodes.All.Where(n=>n.Owner>=0).Sum(n=>w.Campaign.RobotBattles.AvailableFrom(w,n.Owner,n.Id)),blocked=w.Robots.Workshops.Sum(s=>s.ProductionLines.Count(l=>l.Waiting)),disposal=all.Count(r=>r.Disposal),returning=all.Count(r=>r.ReturnHomeCommander.HasValue)+w.Robots.ReturningRobots.Count,longDelivery=w.Robots.LongDelivery.Orders.Count,ongoing=w.Campaign.RobotBattles.Encounters.Count(e=>!e.Finished),maxBattle=w.Campaign.RobotBattles.Encounters.Where(e=>!e.Finished).SelectMany(e=>e.Battles).Select(b=>b.Turn).DefaultIfEmpty(0).Max()}));}
var attackTargets=new Dictionary<int,int>();var encounterOutcomes=new Dictionary<string,int>();int hostileMarches=0;long committedRobots=0;
Report(0);
for(int step=1;step<=steps;step++){
    w.Robots.PrepareDomestic(w.Population.Turn,(a,b)=>FunctionalDeliveryRoutes.Connected(w,a,b),n=>w.Nodes.All[n].Owner,w.Campaign.RobotBattles.LockedNode);
    if (tryReturnRelief)
        PlanReturnRelief(step);
    if (step == 1)
    {
        foreach (var option in args.Where(a => a.StartsWith("--supply-once=")))
        {
            var fields = option.Split('=')[1].Split(':');
            int source = int.Parse(fields[0]), target = int.Parse(fields[1]), slot = int.Parse(fields[3]);
            long id = long.Parse(fields[2]);
            int owner = w.Nodes.All[source].Owner;
            var robot = w.Robots.Nodes[source].Robots.Single(r => r.Id == id);
            var reserve = typeof(RobotLongDelivery).GetMethod("ReserveComputer",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!;
            reserve.Invoke(w.Robots.LongDelivery, new object[] { w, source, target, id, robot.Parts, null, owner, slot });
            if (!w.Robots.LongDelivery.Orders.Any(o => o.Source == source && o.Target == target && o.RobotId == id))
                throw new InvalidOperationException("Requested supply order was not recorded.");
            Console.WriteLine(JsonSerializer.Serialize(new { trace = "supply-once", step, source, target, id, slot }));
        }
    }
    if(step==1)foreach(var option in args.Where(a=>a.StartsWith("--function-transfer-once="))){
        var pair=option.Split('=')[1].Split(':').Select(int.Parse).ToArray();
        int source=pair[0],target=pair[1],owner=w.Nodes.All[source].Owner;
        var method=typeof(NodeFunctionTransfers).GetMethod("ReserveComputer",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic)!;
        bool reserved=(bool)method.Invoke(w.Campaign.FunctionTransfers,new object[]{w,source,target,owner})!;
        if(!reserved)throw new InvalidOperationException("Requested transfer experiment is unavailable.");
        Console.WriteLine(JsonSerializer.Serialize(new{trace="function-transfer-once",step,source,target,owner}));
    }
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
    var superiors = strategy ? w.Relations.Superiors.Take(w.ActiveCount).ToArray() : null;
    void TraceSuperiorChanges(string phase)
    {
        if (superiors == null) return;
        var changed = Enumerable.Range(0, w.ActiveCount)
            .Where(ruler => superiors[ruler] != w.Relations.Superiors[ruler])
            .Select(ruler => new { ruler, previousSuperior = superiors[ruler],
                superior = w.Relations.Superiors[ruler], root = w.Relations.Root(ruler) }).ToArray();
        if (changed.Length > 0)
            Console.WriteLine(JsonSerializer.Serialize(new { trace = "superiors", step, phase, changed }));
        Array.Copy(w.Relations.Superiors, superiors, superiors.Length);
    }
    var previousEncounters=w.Campaign.RobotBattles.Encounters.Select(e=>e.Id).ToHashSet();
    var ongoingEncounters = requireRepeat
        ? w.Campaign.RobotBattles.Encounters.Where(encounter => !encounter.Finished).Select(encounter => encounter.Id).ToHashSet()
        : new HashSet<int>();
    var transferSafety=new List<(int Source,int Target,int Owner,int[] Outgoing,int[] Incoming)>();
    TraceStock(step,"before-domestic");
    w.Campaign.FinishDomestic(w,true,deferDelivery:()=>{
        TraceStock(step,"before-delivery");
        if(args.Contains("--check-transfer-safety"))foreach(var transfer in w.Campaign.FunctionTransfers.Orders)
            transferSafety.Add((transfer.Source,transfer.Target,transfer.Owner,
                w.Campaign.RobotBattles.MarchTargets(w,transfer.Target).Where(t=>w.Nodes.All[t].Owner>=0 && !w.Relations.Allied(transfer.Owner,w.Nodes.All[t].Owner)).ToArray(),
                w.Nodes.All.Where(n=>n.Functional && n.Owner>=0 && !w.Relations.Allied(transfer.Owner,n.Owner) && w.Campaign.RobotBattles.CanAttackFrom(w,n.Owner,n.Id,transfer.Target)).Select(n=>n.Id).ToArray()));
        if(strategy || args.Contains("--trace-logistics"))Console.WriteLine(JsonSerializer.Serialize(new{trace="logistics",step,
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
    TraceSuperiorChanges("domestic");
    TraceStock(step,"after-domestic");
    foreach(var route in tracedQueenRoutes)
    {
        int ruler=route[0], target=route[1];
        int source=w.Nodes.Current(ruler)?.Id??-1;
        var battles=w.Campaign.RobotBattles;
        var escort=w.Robots.Retinues[ruler].Robots.Where(w.Robots.Training[ruler].Eligible).Select(r=>r.Id);
        Console.WriteLine(JsonSerializer.Serialize(new{trace="queen-route",step,ruler,source,target,
            owner=w.Nodes.All[target].Owner,sourceLocked=source>=0 && battles.LockedNode(source),
            targetLocked=battles.LockedNode(target),edgeLocked=source>=0 && battles.LockedEdge(source,target),
            canSortie=battles.CanSortie(w,ruler,target),defense=battles.DefendingRobots(w,ruler,target),escort}));
    }
    var origins = new Dictionary<long, int>();
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
        foreach (int actor in beginnerActors)
        {
            var list = (List<March>)orders;
            var replaced = list.Where(order => order.Ruler == actor && !order.Peaceful).ToArray();
            list.RemoveAll(order => order.Ruler == actor && !order.Peaceful);
            var beginnerOrders = new List<March>();
            int sources=0, withoutTroops=0, withoutTargets=0, outnumbered=0;
            var battles = w.Campaign.RobotBattles;
            if (w.Relations.CanAct(actor))
            {
                var reserved = list.SelectMany(order => order.RobotIds ?? Array.Empty<long>()).ToHashSet();
                var targeted = new HashSet<int>();
                foreach (var source in w.Nodes.All.Where(node => node.Owner == actor && !battles.LockedNode(node.Id)))
                {
                    if(!source.Functional)continue;
                    sources++;
                    var troops = battles.SelectableRobots(w, actor, source.Id, false)
                        .Where(robot => !reserved.Contains(robot.Id)).OrderBy(robot => robot.Id).Take(3).ToArray();
                    if (troops.Length == 0)
                    {
                        withoutTroops++;
                        continue;
                    }
                    var candidates = battles.MarchTargets(w, source.Id)
                        .Where(node => w.Nodes.All[node].Owner < 0 || !w.Relations.Allied(actor, w.Nodes.All[node].Owner))
                        .Where(node => !targeted.Contains(node) && !battles.LockedNode(node) &&
                            !battles.LockedEdge(source.Id, node) && battles.CanAttackFrom(w, actor, source.Id, node))
                        .Select(node => (Node: node, Defense: battles.DefendingRobots(w, actor, node))).ToArray();
                    var target = candidates
                        .Where(choice => choice.Defense < troops.Length)
                        .OrderBy(choice => choice.Defense).ThenBy(choice => choice.Node).Select(choice => choice.Node)
                        .FirstOrDefault(-1);
                    if (target < 0)
                    {
                        if(candidates.Length==0)withoutTargets++;else outnumbered++;
                        continue;
                    }
                    targeted.Add(target);
                    beginnerOrders.Add(new(actor, target, troops.Length, source.Id, Queen: false,
                        RobotIds: troops.Select(robot => robot.Id).ToArray()));
                }
                var home = w.Nodes.Current(actor);
                if (beginnerPocket && home != null && !list.Any(order => order.Ruler == actor && order.Queen))
                {
                    var escort = battles.SelectableRobots(w, actor, home.Id, true)
                        .Where(robot => !reserved.Contains(robot.Id)).OrderBy(robot => robot.Id).Take(3).ToArray();
                    var target = battles.MarchTargets(w, home.Id)
                        .Where(node => !targeted.Contains(node) && !battles.LockedNode(node) &&
                            !battles.LockedEdge(home.Id,node) && battles.CanSortie(w,actor,node))
                        .Where(node => w.Nodes.All[node].Owner < 0 || !w.Relations.Allied(actor,w.Nodes.All[node].Owner))
                        .Select(node => (Node: node, Defense: battles.DefendingRobots(w,actor,node)))
                        .Where(choice => choice.Defense < escort.Length)
                        .OrderBy(choice => choice.Defense).ThenBy(choice => choice.Node).FirstOrDefault((-1,0)).Item1;
                    if(target>=0)
                        beginnerOrders.Add(new(actor,target,escort.Length,home.Id,Queen:true,PocketOnly:true,
                            RobotIds:escort.Select(robot=>robot.Id).ToArray()));
                }
            }
            list.AddRange(beginnerOrders);
            Console.WriteLine(JsonSerializer.Serialize(new { trace = "beginner-military", step, actor,
                replaced, orders = beginnerOrders, sources, withoutTroops, withoutTargets, outnumbered }));
        }
        foreach(var skip in skipQueenOrders.Where(o=>o[0]==step)){
            var list=(List<March>)orders;
            var skipped=list.Where(o=>o.Ruler==skip[1] && o.TargetNode==skip[2] && o.Queen && !o.Peaceful).ToArray();
            foreach(var order in skipped)list.Remove(order);
            removedOrders+=skipped.Length;
            Console.WriteLine(JsonSerializer.Serialize(new{trace="skip-queen-order",step,ruler=skip[1],target=skip[2],skipped}));
        }
        if(guardNeutralRulers.Count>0 || args.Contains("--guard-neutral-queen")){
            var list=(List<March>)orders;
            var battles=w.Campaign.RobotBattles;
            var skipped=list.Where(o=>(guardNeutralRulers.Count==0 || guardNeutralRulers.Contains(o.Ruler)) &&
                o.Queen && !o.Peaceful && o.Fighters==0 && o.RobotIds==null &&
                w.Nodes.All[o.TargetNode].Owner<0 && w.Nodes.Current(o.Ruler) is {} home &&
                battles.DefendingRobots(w,o.Ruler,o.TargetNode) is int defense && defense>0 &&
                defense>=battles.SelectableRobots(w,o.Ruler,home.Id,true).Length).ToArray();
            foreach(var order in skipped)list.Remove(order);
            removedOrders+=skipped.Length;
            if(skipped.Length>0)Console.WriteLine(JsonSerializer.Serialize(new{trace="guard-neutral-queen",step,skipped}));
        }
        foreach(var hold in holdAgainst.Where(h=>h[0]==step)){
            int actor=hold[1],opponent=hold[2];
            var list=(List<March>)orders;
            var withheld=list.Where(o=>o.Ruler==actor && !o.Peaceful && w.Nodes.All[o.TargetNode].Owner is int owner &&
                owner>=0 && !w.Relations.Allied(actor,owner) && w.Relations.Allied(opponent,owner)).ToArray();
            foreach(var order in withheld)list.Remove(order);
            removedOrders+=withheld.Length;
            Console.WriteLine(JsonSerializer.Serialize(new{trace="strategy-hold",step,actor,opponent,withheld}));
        }
        foreach(var order in orders.Where(o=>!o.Peaceful && w.Nodes.All[o.TargetNode].Owner>=0 && !w.Relations.Allied(o.Ruler,w.Nodes.All[o.TargetNode].Owner))){hostileMarches++;committedRobots+=order.Fighters;attackTargets[order.TargetNode]=attackTargets.GetValueOrDefault(order.TargetNode)+1;}
        foreach(var order in orders.Where(o=>traceAll || tracedTargets.Contains(o.TargetNode)))Console.WriteLine(JsonSerializer.Serialize(new{trace="order",step,order,
            targetOwner=w.Nodes.All[order.TargetNode].Owner,defense=w.Campaign.RobotBattles.DefendingRobots(w,order.Ruler,order.TargetNode),
            reserveDefenders=w.Campaign.RobotBattles.Reserves.Count(r=>r.Node==order.TargetNode && r.Robot.CanFight && !w.Relations.Allied(order.Ruler,r.Robot.Owner)),
            equipment=order.RobotIds==null?null:w.Robots.Nodes.Concat(w.Robots.Retinues).SelectMany(s=>s.Robots).Where(r=>order.RobotIds.Contains(r.Id))
                .Select(r=>new{r.Id,native=r.NativeParts(order.Ruler),uses=r.CombatParts(order.Ruler),rifles=r.CombatRifles(order.Ruler)}).ToArray()}));
        if (requireRepeat)
        {
            foreach (var node in w.Nodes.All)
            foreach (var robot in w.Robots.Nodes[node.Id].Robots)
                origins[robot.Id] = node.Id;
            // In-flight robots are absent from their stores, including when resuming a saved encounter.
            var campaign = w.Campaign.RobotBattles;
            var flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
            var homes = (Dictionary<long, (RobotStore Store, int Node)>)typeof(RobotCampaign).GetField("_homes", flags)!.GetValue(campaign)!;
            foreach (var (id, home) in homes)
                if (ReferenceEquals(home.Store, w.Robots.Nodes[home.Node]))
                    origins[id] = home.Node;
        }
        return false;
    });
    TraceSuperiorChanges("military");
    TraceStock(step,"after-military");
    foreach(var encounter in w.Campaign.RobotBattles.Encounters.Where(e=>!previousEncounters.Contains(e.Id) ||
        e.Finished && ongoingEncounters.Contains(e.Id))){
        int winner=encounter.Battles.FirstOrDefault()?.Winner??-1;string result=!encounter.Finished?"ongoing":winner<0?"no-winner":encounter.Defender>=0 && previousRoots[winner]==previousRoots[encounter.Defender]?"defender-held":"attacker-won";
        if (requireRepeat) RecordWithdrawal(encounter, startingTurn + step, result, ongoingEncounters.Contains(encounter.Id), origins);
        if (previousEncounters.Contains(encounter.Id)) continue;
        string key=$"{encounter.Kind}:{result}";encounterOutcomes[key]=encounterOutcomes.GetValueOrDefault(key)+1;
        if(traceAll || tracedTargets.Contains(encounter.Target))Console.WriteLine(JsonSerializer.Serialize(new{trace="battle",step,encounter.Id,encounter.Source,encounter.Target,kind=encounter.Kind.ToString(),result,
            battles=encounter.Battles.Select(b=>new{b.Id,b.Winner,b.Turn,seed=w.PlacementSeed^encounter.Id^b.Id,terrain=b.Terrain.ToString(),b.BoardSize,walls=b.Walls.OrderBy(p=>p.X).ThenBy(p=>p.Y).Select(p=>new{p.X,p.Y}),initial=b.Frames.FirstOrDefault()?.Units.Select(u=>new{u.Id,u.Owner,u.Role,x=u.Position.X,y=u.Position.Y,u.Daggers,u.Rifles,u.Shields}),conversions=b.Persuasions.Where(p=>p.Success).Select(p=>new{p.Turn,p.Target,p.PreviousOwner,p.Owner}),armies=b.Armies.Select(a=>new{a.Owner,a.HomeNode,a.Withdrawing,a.NoWillCount,limit=b.Rules.NoWillTurns,
                robots=b.Units.Count(u=>u.Owner==a.Owner && u.Robot!=null)+a.Waiting.Count(u=>u.Robot!=null),ids=b.Units.Where(u=>u.Owner==a.Owner && u.Robot!=null).Select(u=>u.Id).Concat(a.Waiting.Where(u=>u.Robot!=null).Select(u=>u.Id)),fallen=b.Units.Count(u=>u.Owner==a.Owner && u.Fallen),retreated=b.Units.Count(u=>u.Owner==a.Owner && u.Retreated)})})}));
    }
    if(args.Contains("--trace-relations") && w.Relations.Pending.Count>0)Console.WriteLine(JsonSerializer.Serialize(new{trace="pending",step,captives=w.Relations.Pending}));
    foreach(var captive in w.Relations.Pending.ToArray())w.Relations.Choose(w,captive,w.Relations.Accepts(w,captive.Ruler)?DefeatedChoice.Recruit:w.Relations.CanMercy(w,captive)?DefeatedChoice.Release:DefeatedChoice.RemoveBattery);
    if(strategy || args.Contains("--trace-relations")){
        var changed=Enumerable.Range(0,w.ActiveCount).Where(o=>previousRoots[o]!=w.Relations.Root(o) || previousPowered[o]!=w.Relations.Powered[o]).Select(o=>new{
            ruler=o,previousRoot=previousRoots[o],root=w.Relations.Root(o),previousPowered=previousPowered[o],powered=w.Relations.Powered[o],node=w.Nodes.Current(o)?.Id}).ToArray();
        if(changed.Length>0 || step%5==0)Console.WriteLine(JsonSerializer.Serialize(new{trace="relations",step,changed,
            pending=w.Relations.Pending.Count,rulers=Enumerable.Range(0,w.ActiveCount).Where(w.Relations.CanAct).Select(o=>new{ruler=o,root=w.Relations.Root(o),node=w.Nodes.Current(o)?.Id})}));
    }
    w.Lessons.Research();
    w.Industry.Roll(w.PlacementSeed^w.FortuneSeed);w.Industry.Apply(w);
    if(strategy)
    {
        var current=StrategyRanks();
        var leaders=TerritoryLeaders(current);
        var before=previousRanks.ToDictionary(r=>r.Ruler);
        var rankLosses=current.Where(r=>before.TryGetValue(r.Ruler,out var prior) && r.Rank>prior.Rank)
            .Select(r=>new{r.Ruler,fromRank=before[r.Ruler].Rank,toRank=r.Rank,fromCells=before[r.Ruler].Cells,toCells=r.Cells}).ToArray();
        var passed=(from ahead in current from behind in current
                    where ahead.Cells>behind.Cells && before.TryGetValue(ahead.Ruler,out var oldAhead) &&
                        before.TryGetValue(behind.Ruler,out var oldBehind) && oldAhead.Cells<oldBehind.Cells
                    select new{ahead=ahead.Ruler,behind=behind.Ruler}).ToArray();
        overtakes+=passed.Length;
        if(!leaders.SequenceEqual(previousLeaders))leaderChanges++;
        Console.WriteLine(JsonSerializer.Serialize(new{trace="strategy-turn",step,turn=w.Population.Turn,leaders,
            factions=current,rankLosses,passed,departed=previousRanks.Select(r=>r.Ruler).Except(current.Select(r=>r.Ruler))}));
        previousRanks=current;previousLeaders=leaders;
    }
    foreach(var battle in w.Campaign.RobotBattles.Encounters.SelectMany(e=>e.Battles)){battle.Frames.Clear();battle.Events.Clear();battle.Persuasions.Clear();battle.Attacks.Clear();}
    w.Robots.Check();if(step%5==0 || step==steps)Report(step);
    if(Conquered()){if(step%5!=0 && step!=steps)Report(step);break;}
}

if(strategy)
{
    var winner=previousRanks.FirstOrDefault(r=>r.Cells==w.Cells.Length);
    var start=initialRanks.FirstOrDefault(r=>r.Ruler==winner?.Ruler);
    int initialMax=initialRanks.Select(r=>r.Cells).DefaultIfEmpty(0).Max();
    Console.WriteLine(JsonSerializer.Serialize(new{trace="strategy-summary",turn=w.Population.Turn,winner=winner?.Ruler,
        winnerCharacter=winner==null?(int?)null:w.Looks[winner.Ruler].BaseId+1,
        characterAssignments=Enumerable.Range(0,w.ActiveCount).Select(ruler=>new{ruler,
            character=w.Looks[ruler].BaseId+1,variant=w.Looks[ruler].VariantId+1}),
        winnerInitialRank=start?.Rank,winnerInitialCells=start?.Cells,
        winnerWasInitialTerritoryLeader=start==null?(bool?)null:start.Cells==initialMax,
        leaderChanges,strictTurnOvertakes=overtakes,removedOrders}));
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
if (args.Length > 2)
{
    AutoSaveStore.Write(args[2], saved);
    if (requireRepeat)
    {
        var entries = repeatedWithdrawals.Where(pair => w.Population.Turn - pair.Value.Step <= 6)
            .Select(pair => new WithdrawalEntry(pair.Key.Source, pair.Key.Target, pair.Key.Owner,
                pair.Value.Ids, pair.Value.Step, pair.Value.Count)).ToArray();
        File.WriteAllText(args[2] + ".withdrawals.json",
            JsonSerializer.Serialize(new WithdrawalHistory(SaveHash(args[2]), entries)));
    }
}

sealed record WithdrawalEntry(int Source, int Target, int Owner, long[] Ids, int Turn, int Count);
sealed record WithdrawalHistory(string SaveHash, WithdrawalEntry[] Entries);
sealed record StrategyRank(int Ruler,int Rank,int Cells,int Nodes);
