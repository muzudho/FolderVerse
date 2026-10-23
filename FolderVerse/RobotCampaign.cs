namespace FolderVerse;
using System;
using System.Collections.Generic;
using System.Linq;

public sealed class RobotEncounter
{
    public int Id,Source,Target,Defender=-1;
    public BattleKind Kind;
    public List<BattleState> Battles {get;}=new();
    public Dictionary<int,int> LostHomes {get;}=new();
    public bool Finished;
    public HashSet<int> WithdrawalOwners {get;}=new();
    public HashSet<long> IncomingWinners {get;}=new();
    public HashSet<long> ReturnedWanderers {get;}=new();
}
public sealed class RobotCampaign
{
    private int _nextBattle=1;
    private readonly HashSet<long> _retinueIds=new();
    private readonly Dictionary<long,(RobotStore Store,int Node)> _homes=new();
    private readonly Dictionary<long,int> _homeOwners=new();
    private void Depart(WorldSetup world,int node,Robot robot,bool pocket=false)
    {
        var store=pocket?world.Robots.Retinues[robot.Owner]:world.Robots.Nodes[node];
        store.Depart(robot.Id);_homes.Add(robot.Id,(store,node));_homeOwners[robot.Id]=robot.Owner;
        if(pocket)_retinueIds.Add(robot.Id);
    }
    private (RobotStore Store,int Node) ReleaseHome(WorldSetup world,Robot robot,int fallback)
    {
        if(!_homes.Remove(robot.Id,out var home))return (world.Robots.Nodes[fallback],fallback);
        home.Store.ReleaseReturn(robot.Id);
        if(_homeOwners.Remove(robot.Id,out int owner) && owner!=robot.Owner)return (world.Robots.Nodes[fallback],fallback);
        return home;
    }
    public BattleRules Rules {get;set;}=new();
    public List<RobotEncounter> Encounters {get;}=new();
    public List<BattleState> Scenes {get;}=new();
    public List<(int Owner,int Source,int Target)> LastMarches {get;}=new();
    public List<(int Owner,int Source,int Target)> LastQueenMarches {get;}=new();
    public List<(int Node,Robot Robot)> Reserves {get;}=new();
    public bool LockedNode(int node)=>Encounters.Any(e=>!e.Finished && e.Kind==BattleKind.Node && e.Target==node);
    public bool LockedEdge(int a,int b)=>Encounters.Any(e=>!e.Finished && e.Kind==BattleKind.Edge && (e.Source==a && e.Target==b || e.Source==b && e.Target==a));
    public void Reset(){_nextBattle=1;Encounters.Clear();Scenes.Clear();LastMarches.Clear();LastQueenMarches.Clear();Reserves.Clear();foreach(var h in _homes)h.Value.Store.ReleaseReturn(h.Key);_homes.Clear();_homeOwners.Clear();_retinueIds.Clear();}
    public int Available(WorldSetup world,int ruler)
    {
        var node=world.Nodes.Current(ruler);return node==null || LockedNode(node.Id)?0:world.Robots.Nodes[node.Id].Robots.Count(r=>r.Owner==ruler && world.Robots.Training[ruler].Eligible(r) && !r.Stationed && world.Robots.Workshops[node.Id].DisposalTarget!=r.Id && !world.Robots.PendingEdits.Any(e=>e.Ids.Contains(r.Id)));
    }
    public bool CanMarch(WorldSetup world,int ruler,int target)
    {
        var node=world.Nodes.Current(ruler);
        bool deployed=Encounters.Where(e=>!e.Finished).SelectMany(e=>e.Battles).SelectMany(b=>b.Units.Concat(b.Armies.SelectMany(a=>a.Waiting))).Any(u=>u.Owner==ruler && u.Role==BattleRole.Queen && !u.Fallen && !u.Retreated);
        return node!=null && !deployed && !LockedNode(node.Id) && !LockedEdge(node.Id,target) && world.Routes.ToNode(ruler,target)!=null;
    }
    private static int Source(WorldSetup world,March order)=>order.SourceNode>=0?order.SourceNode:world.Nodes.Current(order.Ruler).Id;
    public int AvailableFrom(WorldSetup world,int owner,int source)
        =>source<0 || source>=world.Nodes.All.Length || LockedNode(source)?0:world.Robots.Nodes[source].Robots.Count(r=>r.Owner==owner && world.Robots.Training[owner].Eligible(r) && !r.Stationed && world.Robots.Workshops[source].DisposalTarget!=r.Id && !world.Robots.PendingEdits.Any(e=>e.Ids.Contains(r.Id)));
    public Robot[] SelectableRobots(WorldSetup world,int owner,int source,bool pocket)
    {
        if(owner<0 || owner>=world.ActiveCount || source<0 || source>=world.Nodes.All.Length || LockedNode(source))return Array.Empty<Robot>();
        var store=pocket?world.Robots.Retinues[owner]:world.Robots.Nodes[source];
        return store.Robots.Where(r=>r.Owner==owner && world.Robots.Training[owner].Eligible(r) && !r.Stationed && (pocket || world.Robots.Workshops[source].DisposalTarget!=r.Id && !world.Robots.PendingEdits.Any(e=>e.Ids.Contains(r.Id)))).ToArray();
    }
    public bool CanAttackFrom(WorldSetup world,int owner,int source,int target)
        =>source>=0 && source<world.Nodes.All.Length && target>=0 && target<world.Nodes.All.Length && world.Nodes.All[source].Owner==owner && !LockedNode(source) && !LockedEdge(source,target) && world.Routes.Neighbors(world.Nodes.All[source]).Any(r=>world.Nodes.At(r.Target,r.Entry).Id==target);
    public void RequestWithdrawal(WorldSetup world,int encounterId,int owner,bool requested=true)
    {
        var encounter=Encounters.Single(e=>e.Id==encounterId && !e.Finished);
        if(owner<0 || owner>=world.ActiveCount || !world.Relations.Controls(owner) || !encounter.Battles.Any(b=>b.Armies.Any(a=>a.Owner==owner)))throw new InvalidOperationException("Cannot withdraw this army.");
        if(requested)encounter.WithdrawalOwners.Add(owner);else encounter.WithdrawalOwners.Remove(owner);
    }
    public void SendReinforcement(WorldSetup world,int owner,int source,int encounterId)
    {
        var encounter=Encounters.Single(e=>e.Id==encounterId && !e.Finished);
        if(source<0 || source>=world.Nodes.All.Length || world.Nodes.All[source].Owner!=owner || LockedNode(source))throw new InvalidOperationException("Reinforcement source is unavailable.");
        bool reachable=encounter.Kind==BattleKind.Edge?source==encounter.Source || source==encounter.Target:
            world.Routes.Neighbors(world.Nodes.All[source]).Any(r=>world.Nodes.At(r.Target,r.Entry).Id==encounter.Target) && !LockedEdge(source,encounter.Target);
        if(!reachable)throw new InvalidOperationException("Reinforcement cannot reach battlefield.");
        var robots=world.Robots.Training[owner].Order(world.Robots.Nodes[source].Robots.Where(r=>r.Owner==owner && world.Robots.Training[owner].Eligible(r) && !r.Stationed && world.Robots.Workshops[source].DisposalTarget!=r.Id && !world.Robots.PendingEdits.Any(e=>e.Ids.Contains(r.Id)))).ToList();
        if(robots.Count==0)throw new InvalidOperationException("No complete robots are available for reinforcement.");
        foreach(var robot in robots)Depart(world,source,robot);
        AddReinforcement(world,encounter,owner,source,robots,false);Check(world);
    }
    public void Advance(WorldSetup world,IReadOnlyList<March> orders)
    {
        if(orders.Any(o=>o.Peaceful && (!o.Queen || !o.PocketOnly || o.Fighters!=0 || o.RobotIds?.Length>0)))throw new ArgumentException("A peaceful visit carries only the queen and her pocket.");
        if(orders.Where(o=>o.Queen).GroupBy(o=>o.Ruler).Any(g=>g.Count()>1) || orders.GroupBy(o=>(o.Ruler,Source(world,o),Pocket:o.Queen || o.PocketOnly)).Any(g=>g.Count()>1 && (g.Any(o=>o.RobotIds==null) || g.GroupBy(o=>o.TargetNode).Any(t=>t.Count()>1))))throw new ArgumentException("Duplicate target, ambiguous legacy selection or conqueror order.");
        var reservedIds=orders.Where(o=>o.RobotIds!=null).SelectMany(o=>o.RobotIds).ToArray();
        if(reservedIds.Distinct().Count()!=reservedIds.Length)throw new ArgumentException("A robot can have only one destination.");
        foreach(var o in orders)if(o.Ruler<0 || o.Ruler>=world.ActiveCount || !world.Relations.Powered[o.Ruler] || world.Relations.Pending.Any(p=>p.Ruler==o.Ruler) || o.Fighters<0 || (o.Queen || o.PocketOnly) && o.SourceNode>=0 && o.SourceNode!=world.Nodes.Current(o.Ruler)?.Id || !((o.Queen || o.PocketOnly)?CanMarch(world,o.Ruler,o.TargetNode):CanAttackFrom(world,o.Ruler,o.SourceNode,o.TargetNode)))throw new ArgumentException("Invalid robot march.");
        // Validate every explicit selection before any stores or encounters are mutated.
        foreach(var o in orders.Where(o=>o.RobotIds!=null))
        {
            var available=SelectableRobots(world,o.Ruler,Source(world,o),o.PocketOnly);
            if(o.RobotIds.Length>12 || o.RobotIds.Distinct().Count()!=o.RobotIds.Length || o.Fighters!=o.RobotIds.Length || o.RobotIds.Any(id=>!available.Any(r=>r.Id==id)))throw new ArgumentException("Reserved robot selection is no longer available.");
        }
        Scenes.Clear();Encounters.RemoveAll(e=>e.Finished);
        foreach(var reserve in Reserves.ToArray())if(!LockedNode(reserve.Node) && world.Robots.Nodes[reserve.Node].HasSpace)
        {world.Robots.Nodes[reserve.Node].Add(reserve.Robot);Reserves.Remove(reserve);}
        var source=orders.ToDictionary(o=>o,o=>Source(world,o));
        LastMarches.Clear();LastMarches.AddRange(orders.Select(o=>(o.Ruler,source[o],o.TargetNode)));
        LastQueenMarches.Clear();LastQueenMarches.AddRange(orders.Where(o=>o.Queen).Select(o=>(o.Ruler,source[o],o.TargetNode)));
        var troops=new Dictionary<March,List<Robot>>();
        foreach(var o in orders)
        {
            var store=world.Robots.Nodes[source[o]];
            var selected=o.RobotIds!=null?SelectableRobots(world,o.Ruler,source[o],o.PocketOnly).Where(r=>o.RobotIds.Contains(r.Id)).ToArray():world.Robots.Training[o.Ruler].Order(store.Robots.Where(r=>!o.PocketOnly && r.Owner==o.Ruler && world.Robots.Training[o.Ruler].Eligible(r) && !r.Stationed && world.Robots.Workshops[source[o]].DisposalTarget!=r.Id && !world.Robots.PendingEdits.Any(e=>e.Ids.Contains(r.Id)))).Take((int)Math.Min(12,o.Fighters)).ToArray();
            foreach(var r in selected)Depart(world,source[o],r,o.PocketOnly);
            troops[o]=selected.ToList();
            // Direct retinue travels with its conqueror and is excluded from transport plans.
            foreach(var r in world.Robots.Retinues[o.Ruler].Robots.Where(r=>o.RobotIds==null && o.Queen && world.Robots.Training[o.Ruler].Eligible(r)).ToArray()){Depart(world,source[o],r,true);troops[o].Add(r);}
        }
        // One global update: disposal, queued assembly/splits, simultaneous transport, shipment.
        world.Robots.Advance((a,b)=>!LockedEdge(a,b) && world.Routes.Neighbors(world.Nodes.All[a]).Any(r=>world.Nodes.At(r.Target,r.Entry).Id==b),
            n=>world.Nodes.All[n].Owner,world.PlacementSeed^world.Population.Turn^0x61238,LockedNode,(owner,target)=>world.Nodes.All[target].Owner<0 || world.Relations.Allied(owner,world.Nodes.All[target].Owner),industry:false,directLocked:n=>LockedNode(n) || orders.Any(o=>!o.Peaceful && o.TargetNode==n && !world.Relations.Allied(o.Ruler,world.Nodes.All[n].Owner)));
        world.Population.Advance(world);
        world.Industry.Queue(world.Population.Turn);
        var stopped=new HashSet<March>();
        // Visits carry the queen and her pocket without claiming land or starting a war.
        foreach(var visit in orders.Where(o=>o.Peaceful))
        {
            foreach(var robot in troops[visit])ReturnHome(world,robot,source[visit]);
            Move(world,visit.Ruler,visit.TargetNode);stopped.Add(visit);
            if(world.TutorialStep==2 && visit.Ruler==0 && source[visit]!=visit.TargetNode)world.TutorialMoved=true;
        }
        foreach(var first in orders.OrderBy(o=>o.Ruler))
        {
            if(stopped.Contains(first))continue;
            var second=orders.Where(o=>o.Ruler>first.Ruler && !stopped.Contains(o) && source[o]==first.TargetNode && o.TargetNode==source[first] && !world.Relations.Allied(first.Ruler,o.Ruler)).OrderBy(o=>o.Ruler).FirstOrDefault();
            if(second==null)continue;
            // Both source kinds travelling the same edge share the encounter.
            var crossing=orders.Where(o=>!stopped.Contains(o) &&
                (source[o]==source[first] && o.TargetNode==first.TargetNode || source[o]==first.TargetNode && o.TargetNode==source[first])).ToArray();
            var armies=crossing.GroupBy(o=>(o.Ruler,Home:source[o])).Select(g=>(g.Key.Ruler,g.Key.Home,g.SelectMany(o=>troops[o]).ToList(),g.Any(o=>o.Queen))).ToArray();
            var e=CreateEncounter(world,BattleKind.Edge,source[first],first.TargetNode,-1,armies);
            Encounters.Add(e);foreach(var o in crossing)stopped.Add(o);
        }
        foreach(var group in orders.Where(o=>!stopped.Contains(o)).GroupBy(o=>o.TargetNode).OrderBy(g=>g.Key))
        {
            int target=group.Key;var node=world.Nodes.All[target];var existing=Encounters.FirstOrDefault(e=>!e.Finished && e.Kind==BattleKind.Node && e.Target==target);
            if(existing!=null)
            {
                foreach(var o in group)AddReinforcement(world,existing,o.Ruler,source[o],troops[o],o.Queen);
                continue;
            }
            var armies=group.GroupBy(o=>o.Ruler).Select(g=>(Owner:g.Key,Home:source[g.FirstOrDefault(o=>o.Queen)??g.First()],Robots:g.SelectMany(o=>troops[o]).ToList(),Queen:g.Any(o=>o.Queen))).ToList();
            foreach(var ownership in world.Robots.Nodes[target].Robots.Where(r=>r.CanFight && !r.FactoryWorker && (!r.DefenseOnly || world.Robots.Training[r.Owner].Eligible(r))).GroupBy(r=>r.Owner).ToArray())
            {
                var robots=ownership.ToList();foreach(var r in robots)Depart(world,target,r);
                int index=armies.FindIndex(a=>a.Owner==ownership.Key);
                if(index>=0)armies[index].Robots.AddRange(robots);else armies.Add((ownership.Key,target,robots,false));
            }
            if(armies.All(a=>world.Relations.Allied(a.Owner,armies[0].Owner)) && (node.Owner<0 || world.Relations.Allied(node.Owner,armies[0].Owner)))
            {
                if(node.Owner<0)SetOwner(world,node,armies[0].Owner);
                var arrivals=armies.SelectMany(a=>a.Robots).ToArray();
                var staying=arrivals.Where(r=>_homes.TryGetValue(r.Id,out var h) && !ReferenceEquals(h.Store,world.Robots.Nodes[target])).Select(r=>r.Id).ToHashSet();
                foreach(var r in arrivals.OrderBy(r=>staying.Contains(r.Id)?1:0).ThenBy(r=>r.Id))PlaceWinner(world,target,r,target,staying);
                foreach(var army in armies)if(army.Queen)Move(world,army.Owner,target);
                continue;
            }
            if(node.Owner>=0 && !armies.Any(a=>a.Owner==node.Owner))armies.Add((node.Owner,target,new List<Robot>(),false));
            var encounter=CreateEncounter(world,BattleKind.Node,target,target,node.Owner,armies.ToArray());Encounters.Add(encounter);
        }
        // Residents waiting outside full stores can be used as reinforcements at their own node.
        foreach(var reserve in Reserves.ToArray())
        {
            var encounter=Encounters.FirstOrDefault(e=>!e.Finished && e.Kind==BattleKind.Node && e.Target==reserve.Node);
            if(encounter!=null){AddReinforcement(world,encounter,reserve.Robot.Owner,reserve.Node,new(){reserve.Robot},false);Reserves.Remove(reserve);}
        }
        foreach(var encounter in Encounters.Where(e=>!e.Finished).ToArray())
        {JoinWanderers(world,encounter);RunSegment(world,encounter);ReturnRetreatedWanderers(world,encounter);}
        // A node battle later in the same global turn may remove an earlier edge battle's home.
        bool changed;
        do
        {
            changed=false;
            foreach(var encounter in Encounters.Where(e=>!e.Finished && e.Kind==BattleKind.Edge).ToArray())
            {
                foreach(var state in encounter.Battles)
                {
                    if(!LoseHomes(world,encounter,state))continue;changed=true;
                    state.Capture(BattlePhase.Fall);BattleTurnResolver.Finish(state);state.Capture(BattlePhase.Result);
                }
                if(encounter.Battles.All(b=>b.Finished))Complete(world,encounter);
            }
        }while(changed);
        world.Nodes.Recount();Check(world);
    }
    private RobotEncounter CreateEncounter(WorldSetup world,BattleKind kind,int source,int target,int defender,
        (int Owner,int Home,List<Robot> Robots,bool Queen)[] armies)
    {
        var e=new RobotEncounter{Id=_nextBattle++,Kind=kind,Source=source,Target=target,Defender=defender};
        // Stable route/source ordering puts nearby invasion groups in the same board.
        int Bearing(int home)
        {
            var node=world.Nodes.All[target];var route=world.Routes.Neighbors(node).FirstOrDefault(r=>world.Nodes.At(r.Target,r.Entry).Id==home);
            return route==null?-1:world.Routes.Bearing(node,route);
        }
        var sorted=armies.OrderBy(a=>a.Owner==defender?0:1).ThenBy(a=>Bearing(a.Home)).ThenBy(a=>a.Home).ThenBy(a=>a.Owner).ToArray();
        var sizes=new List<int>();int left=sorted.Length;
        while(left>4){int size=left==5?2:4;sizes.Add(size);left-=size;}sizes.Add(left);
        int offset=0;foreach(int size in sizes)
        {
            var state=NewState(world,e);
            if(sizes.Count>1){state.SplitBoard=true;state.Kind=BattleKind.Edge;state.Defender=-1;state.BuildTerrain(world.Nodes.All[target].Terrain==TravelTerrain.Sea?BattleTerrain.Sea:BattleTerrain.Field);}
            foreach(var a in sorted.Skip(offset).Take(size))state.AddArmy(a.Owner,a.Home,a.Robots,a.Queen);
            state.ChooseLayout();state.Deploy();e.Battles.Add(state);offset+=size;
        }
        return e;
    }
    private void JoinWanderers(WorldSetup world,RobotEncounter encounter)
    {
        if(encounter.Kind!=BattleKind.Node)return;
        foreach(int owner in Enumerable.Range(0,world.ActiveCount))
        {
            if(!world.Relations.CanAct(owner) || world.TerritoryCounts[owner]!=0 || world.Nodes.Current(owner)?.Id!=encounter.Target ||
                Encounters.Where(e=>!e.Finished).Any(e=>e.Battles.SelectMany(b=>b.Units.Concat(b.Armies.SelectMany(a=>a.Waiting))).Any(u=>u.Owner==owner && u.Role==BattleRole.Queen && (e==encounter || u.Active))))continue;
            var robots=world.Robots.Retinues[owner].Robots.Where(world.Robots.Training[owner].Eligible).ToList();
            foreach(var robot in robots)Depart(world,encounter.Target,robot,true);
            AddReinforcement(world,encounter,owner,encounter.Target,robots,true);
            var state=encounter.Battles.First(b=>b.Armies.Any(a=>a.Owner==owner));var army=state.Army(owner);
            army.Wanderer=true;var policy=world.ManualRulers().Contains(owner)?world.WandererPolicyFor(owner):WandererPolicy.Random;
            army.WandererPolicy=policy==WandererPolicy.Random?(WandererPolicy)state.Random.Next(3):policy;
            if(army.WandererPolicy==WandererPolicy.Retreat)encounter.WithdrawalOwners.Add(owner);
        }
        PrepareSplitBoards(encounter);
    }
    private static void PrepareSplitBoards(RobotEncounter encounter)
    {
        if(encounter.Battles.Count<=1)return;
        // A partial board cannot decide ownership of a node shared by other boards.
        foreach(var state in encounter.Battles){state.SplitBoard=true;state.Kind=BattleKind.Edge;state.Defender=-1;}
    }
    private BattleState NewState(WorldSetup world,RobotEncounter e)
    {
        var state=new BattleState(world.PlacementSeed^e.Id^_nextBattle,Rules){Id=_nextBattle++,GlobalTurn=world.Population.Turn,Kind=e.Kind,SourceNode=e.Source,TargetNode=e.Target,Defender=e.Defender,Allied=world.Relations.Allied};
        state.Lessons=(BattleLesson[])world.Lessons.Owned.Clone();
        foreach(var rank in world.Relations.ProvisionalRanking(world))state.NewspaperRanks[rank.Ruler]=rank.Rank;
        state.BuildTerrain(world.Nodes.All[e.Target].Terrain==TravelTerrain.Sea?BattleTerrain.Sea:e.Kind==BattleKind.Node?BattleTerrain.Fort:BattleTerrain.Field);return state;
    }
    private void AddReinforcement(WorldSetup world,RobotEncounter e,int owner,int home,List<Robot> robots,bool queen)
    {
        if(robots.Count==0 && !queen)return;
        var state=e.Battles.FirstOrDefault(b=>b.Armies.Any(a=>a.Owner==owner));
        if(state==null)
        {
            state=e.Battles.FirstOrDefault(b=>!b.Finished && b.Armies.Count<b.EntryCapacity);
            if(state==null){state=NewState(world,e);e.Battles.Add(state);}
            state.AddArmy(owner,home,robots,queen);return;
        }
        var army=state.Army(owner);
        state.Finished=false; // Will is reset only when a unit actually enters the board.
        foreach(var robot in robots)army.Waiting.Enqueue(new(){Id=robot.Id,Owner=owner,Robot=robot,Daggers=robot.CombatParts(owner),Shields=robot.CombatParts(owner),Rifles=robot.NativeParts(owner)==3?4:robot.CombatParts(owner),Role=robot.Role==RobotRole.Captain?BattleRole.Captain:BattleRole.Soldier});
        if(queen && !state.Units.Concat(state.Armies.SelectMany(a=>a.Waiting)).Any(u=>u.Id==-(owner+1)))army.Waiting.Enqueue(new(){Id=-(owner+1),Owner=owner,Role=BattleRole.Queen,Daggers=0,Rifles=0});
    }
    private void RunSegment(WorldSetup world,RobotEncounter e)
    {
        foreach(var state in e.Battles){state.Lessons=(BattleLesson[])world.Lessons.Owned.Clone();if(state.GlobalTurn!=world.Population.Turn)state.PersuadedUnits.Clear();state.GlobalTurn=world.Population.Turn;state.Events.Clear();state.Frames.Clear();state.Attacks.Clear();state.Persuasions.Clear();}
        foreach(var state in e.Battles)foreach(var army in state.Armies.Where(a=>e.WithdrawalOwners.Contains(a.Owner))){
            army.Withdrawing=true;
            foreach(var unit in state.Units.Where(u=>u.Owner==army.Owner && u.Active))unit.Retreating=true;
            while(army.Waiting.TryDequeue(out var unit)){unit.Retreated=true;state.Units.Add(unit);}
        }
        e.WithdrawalOwners.Clear();
        int budget=Rules.SegmentTurns;
        while(budget>0)
        {
            var active=e.Battles.Where(b=>!b.Finished).ToArray();
            foreach(var state in active)
            {
                if(!Scenes.Contains(state))Scenes.Add(state);
                BattleTurnResolver.Advance(state);
                if(e.Kind==BattleKind.Edge && LoseHomes(world,e,state)){state.Capture(BattlePhase.Fall);BattleTurnResolver.Finish(state);state.Capture(BattlePhase.Result);}
            }
            budget--;
            if(e.Battles.Any(b=>!b.Finished))continue;
            if(e.Battles.Count>1 && Merge(world,e))continue;
            Complete(world,e);return;
        }
    }
    private bool LoseHomes(WorldSetup world,RobotEncounter encounter,BattleState state)
    {
        bool changed=false;
        foreach(var army in state.Armies)
        {
            if(encounter.LostHomes.ContainsKey(army.Owner) || world.Relations.Allied(world.Nodes.All[army.HomeNode].Owner,army.Owner))continue;
            encounter.LostHomes[army.Owner]=world.Nodes.All[army.HomeNode].Owner;changed=true;
            foreach(var unit in state.Units.Where(u=>u.Owner==army.Owner))unit.Fallen=true;
            while(army.Waiting.TryDequeue(out var unit)){unit.Fallen=true;state.Units.Add(unit);}
        }
        return changed;
    }
    private bool Merge(WorldSetup world,RobotEncounter e)
    {
        var survivors=e.Battles.SelectMany(b=>b.Units.Concat(b.Armies.SelectMany(a=>a.Waiting))).Where(u=>!u.Fallen && !u.Retreated).GroupBy(u=>u.Owner).ToArray();
        if(survivors.Length<=1)
        {
            int winner=survivors.FirstOrDefault()?.Key??-1;
            foreach(var b in e.Battles)b.Winner=winner;
            return false;
        }
        var old=e.Battles.ToArray();
        var groups=survivors.OrderBy(g=>g.Key).ToArray();e.Battles.Clear();
        for(int i=0;i<groups.Length;i+=4)
        {
            var state=NewState(world,e);state.InheritHistory(old);
            foreach(var g in groups.Skip(i).Take(4))
            {
                var previous=old.SelectMany(b=>b.Armies).First(a=>a.Owner==g.Key);
                var army=new BattleArmy{Owner=g.Key,HomeNode=previous.HomeNode,Side=state.Armies.Count,Limit=previous.Limit,Wanderer=previous.Wanderer,WandererPolicy=previous.WandererPolicy,Withdrawing=previous.Withdrawing};
                if(state.Armies.Count==1)army.Side=2;state.Armies.Add(army);
                foreach(var unit in g.OrderBy(u=>u.Role==BattleRole.Queen?1:0).ThenBy(u=>u.Id))army.Waiting.Enqueue(unit);
            }
            state.Deploy();e.Battles.Add(state);
        }
        PrepareSplitBoards(e);
        // Failed/retreated individuals are returned now; active survivors are only in the new boards.
        foreach(var state in old)ReturnUnits(world,e,state,state.Units.Where(u=>u.Fallen || u.Retreated));
        return true;
    }
    private void Complete(WorldSetup world,RobotEncounter e)
    {
        e.Finished=true;var state=e.Battles[0];int winner=state.Winner;
        if(e.Kind==BattleKind.Node)SetOwner(world,world.Nodes.All[e.Target],winner);
        var returns=e.Battles.Select(b=>(Board:b,Units:b.Units.Concat(b.Armies.SelectMany(a=>a.Waiting)).ToArray())).ToArray();
        // Restore destination residents before choosing which residents the invaders bring home.
        var residentIds=returns.SelectMany(r=>r.Units).Where(u=>u.Robot!=null && _homes.TryGetValue(u.Id,out var h) && ReferenceEquals(h.Store,world.Robots.Nodes[e.Target])).Select(u=>u.Id).ToHashSet();
        e.IncomingWinners.Clear();e.IncomingWinners.UnionWith(returns.SelectMany(r=>r.Units).Where(u=>u.Robot!=null && !residentIds.Contains(u.Id) && u.Active && winner>=0 && world.Relations.Allied(u.Owner,winner)).Select(u=>u.Id));
        foreach(var batch in returns)ReturnUnits(world,e,batch.Board,batch.Units.Where(u=>residentIds.Contains(u.Id)));
        foreach(var batch in returns)ReturnUnits(world,e,batch.Board,batch.Units.Where(u=>!residentIds.Contains(u.Id)));
        if(e.Kind==BattleKind.Node && winner>=0)
        foreach(int ruler in Enumerable.Range(0,world.ActiveCount))
            if(world.Nodes.Current(ruler).Id==e.Target && !world.Relations.Allied(ruler,winner))world.Relations.Capture(world,ruler,winner,e.Target);
        if(e.Kind==BattleKind.Edge && winner>=0)
        {
            var army=state.Army(winner);int next=army.HomeNode==e.Source?e.Target:e.Source;
            // The edge survivor continues into the opposing node, using returned survivors only.
            var survivors=state.Units.Concat(state.Armies.SelectMany(a=>a.Waiting)).Where(u=>u.Active && u.Owner==winner && u.Robot!=null).ToArray();
            var ids=survivors.Select(u=>u.Id).ToHashSet();var robots=survivors.Select(u=>u.Robot with {Owner=winner}).ToArray();
            foreach(var store in world.Robots.Nodes.Concat(world.Robots.Retinues))foreach(var r in store.Robots.Where(r=>ids.Contains(r.Id)).ToArray())
            {
                int home=Array.IndexOf(world.Robots.Nodes,store);
                Depart(world,home>=0?home:army.HomeNode,r,home<0);
            }
            Reserves.RemoveAll(r=>ids.Contains(r.Robot.Id));
            var node=world.Nodes.All[next];var residents=world.Robots.Nodes[next].Robots.Where(r=>r.CanFight && !r.FactoryWorker && (!r.DefenseOnly || world.Robots.Training[r.Owner].Eligible(r))).ToArray();
            foreach(var r in residents)Depart(world,next,r);
            var armies=new List<(int,int,List<Robot>,bool)>{(winner,army.HomeNode,robots.ToList(),state.Units.Any(u=>u.Active && u.Owner==winner && u.Role==BattleRole.Queen))};
            foreach(var group in residents.GroupBy(r=>r.Owner))
            {if(group.Key==winner)armies[0].Item3.AddRange(group);else armies.Add((group.Key,next,group.ToList(),false));}
            if(node.Owner>=0 && !armies.Any(a=>a.Item1==node.Owner))armies.Add((node.Owner,next,new(),false));
            if(armies.Count==1){SetOwner(world,node,winner);foreach(var r in armies[0].Item3.OrderBy(r=>ids.Contains(r.Id)?1:0).ThenBy(r=>r.Id))PlaceWinner(world,next,r,army.HomeNode,ids);if(armies[0].Item4)Move(world,winner,next);}
            else
            {
                var continuation=CreateEncounter(world,BattleKind.Node,next,next,node.Owner,armies.ToArray());
                foreach(var unit in continuation.Battles.SelectMany(b=>b.Units.Concat(b.Armies.SelectMany(a=>a.Waiting))))
                {
                    var prior=survivors.FirstOrDefault(u=>u.Id==unit.Id);if(prior==null)continue;
                    unit.Daggers=prior.Daggers;unit.Rifles=prior.Rifles;unit.Shields=prior.Shields;unit.Role=prior.Role;
                }
                Encounters.Add(continuation);JoinWanderers(world,continuation);RunSegment(world,continuation);ReturnRetreatedWanderers(world,continuation);
            }
        }
    }
    private void ReturnRetreatedWanderers(WorldSetup world,RobotEncounter encounter)
    {
        if(encounter.Finished)return;
        foreach(var state in encounter.Battles)
        {
            var escaped=state.Units.Where(u=>u.Retreated && state.Army(u.Owner).Wanderer && !encounter.ReturnedWanderers.Contains(u.Id)).ToArray();
            ReturnUnits(world,encounter,state,escaped);
            foreach(var unit in escaped)encounter.ReturnedWanderers.Add(unit.Id);
        }
    }
    private void ReturnUnits(WorldSetup world,RobotEncounter e,BattleState state,IEnumerable<BattleUnit> units)
    {
        foreach(var unit in units.OrderBy(u=>u.Active && state.Winner>=0 && world.Relations.Allied(u.Owner,state.Winner)?1:0).ThenBy(u=>u.Id))
        {
            if(e.ReturnedWanderers.Contains(unit.Id))continue;
            world.Lessons.BringHome(unit);
            var army=state.Army(unit.Owner);int home=army.HomeNode;
            if(unit.Role==BattleRole.Queen)
            {
                if(unit.Fallen){int victor=e.LostHomes.GetValueOrDefault(unit.Owner,state.Winner);if(victor>=0)world.Relations.Capture(world,unit.Owner,victor,e.Target);continue;}
                int location=e.Kind==BattleKind.Node && state.Winner>=0 && world.Relations.Allied(unit.Owner,state.Winner)?e.Target:home;
                if(army.Wanderer && unit.Retreated && location==e.Target)
                {
                    var escape=world.Routes.Neighbors(world.Nodes.All[e.Target]).Select(r=>world.Nodes.At(r.Target,r.Entry)).Where(n=>n.Id!=e.Target).OrderBy(n=>LockedNode(n.Id) || LockedEdge(e.Target,n.Id)?1:0).ThenBy(n=>n.Id).FirstOrDefault();
                    if(escape!=null)location=escape.Id;
                }
                Move(world,unit.Owner,location);continue;
            }
            if(unit.Robot==null)continue;
            var robot=unit.Robot.Materialize() with {Owner=unit.Owner};
            if(unit.Fallen)
            {
                // Sample a physical part, including already damaged parts. No part is destroyed.
                var parts=new[]{RobotParts.Head,RobotParts.Body,RobotParts.Legs}.Where(p=>(robot.Parts&p)!=0).ToArray();
                var hit=parts[state.Random.Next(parts.Length)];
                robot=robot with {DamagedParts=robot.DamagedParts|hit};
                state.Events.Add(new(state.Turn,BattlePhase.Result,unit.Id,$"帰還：損傷判定 {hit}"));
            }
            if(unit.Fallen || unit.Retreated || state.Winner<0 || !world.Relations.Allied(unit.Owner,state.Winner))
                ReturnHome(world,robot,home);
            else if(e.Kind==BattleKind.Node)PlaceWinner(world,e.Target,robot,home,e.IncomingWinners);
            else ReturnHome(world,robot,home);
        }
    }
    private static void SetOwner(WorldSetup world,Node node,int owner)
    {if(node.Owner==owner)return;node.Owner=owner;Array.Clear(node.Population.Conversion);Array.Clear(node.Population.Migration);}
    private static void Move(WorldSetup world,int owner,int node)
    {world.ConquerorLocations[owner]=world.Nodes.All[node].Cell;world.ConquerorPoints[owner]=world.Nodes.All[node].Center;world.Relations.Released[owner]=false;}
    private void ReturnHome(WorldSetup world,Robot robot,int fallback)
    {
        var home=ReleaseHome(world,robot,fallback);_retinueIds.Remove(robot.Id);
        if(!home.Store.HasSpace){Reserves.Add((home.Node,robot));return;}
        home.Store.Add(robot);
    }
    private void PlaceWinner(WorldSetup world,int target,Robot robot,int fallback,HashSet<long> staying=null)
    {
        var home=ReleaseHome(world,robot,fallback);var destination=world.Robots.Nodes[target];
        // Pocket robots retain their own hangar; their conqueror's location can change separately.
        if(_retinueIds.Remove(robot.Id) || ReferenceEquals(home.Store,destination)){home.Store.Add(robot);return;}
        if(destination.HasSpace){destination.Add(robot);return;}
        var swapped=destination.Robots.Where(r=>staying?.Contains(r.Id)!=true && !r.Stationed && world.Robots.Workshops[target].DisposalTarget!=r.Id && !world.Robots.PendingEdits.Any(e=>e.Ids.Contains(r.Id)))
            .OrderByDescending(r=>r.NativeParts(robot.Owner)).ThenBy(r=>r.Id).FirstOrDefault();
        if(swapped==null){home.Store.Add(robot);return;}
        destination.Remove(swapped.Id);
        home.Store.Add(swapped.Materialize() with {Owner=robot.Owner});destination.Add(robot);
    }
    public void Check(WorldSetup world)
    {
        world.Robots.Check();
        var stored=world.Robots.Nodes.Concat(world.Robots.Retinues).SelectMany(s=>s.Robots);
        var fighting=Encounters.Where(e=>!e.Finished).SelectMany(e=>e.Battles.SelectMany(b=>b.Units.Concat(b.Armies.SelectMany(a=>a.Waiting))).Where(u=>u.Robot!=null && !e.ReturnedWanderers.Contains(u.Id))).Select(u=>u.Robot);
        var all=stored.Concat(fighting).Concat(Reserves.Select(r=>r.Robot)).ToArray();
        if(all.Select(r=>r.Id).Distinct().Count()!=all.Length)throw new InvalidOperationException("Robot duplicated between world and battlefield.");
    }
}

