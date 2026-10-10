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
}
public sealed class RobotCampaign
{
    private int _nextBattle=1;
    private readonly HashSet<long> _retinueIds=new();
    public BattleRules Rules {get;set;}=new();
    public List<RobotEncounter> Encounters {get;}=new();
    public List<BattleState> Scenes {get;}=new();
    public List<(int Owner,int Source,int Target)> LastMarches {get;}=new();
    public List<(int Node,Robot Robot)> Reserves {get;}=new();
    public bool LockedNode(int node)=>Encounters.Any(e=>!e.Finished && e.Kind==BattleKind.Node && e.Target==node);
    public bool LockedEdge(int a,int b)=>Encounters.Any(e=>!e.Finished && e.Kind==BattleKind.Edge && (e.Source==a && e.Target==b || e.Source==b && e.Target==a));
    public void Reset(){_nextBattle=1;Encounters.Clear();Scenes.Clear();LastMarches.Clear();Reserves.Clear();_retinueIds.Clear();}
    public int Available(WorldSetup world,int ruler)
    {
        var node=world.Nodes.Current(ruler);return node==null || LockedNode(node.Id)?0:world.Robots.Nodes[node.Id].Robots.Count(r=>r.Owner==ruler && r.CanFight && world.Robots.Workshops[node.Id].DisposalTarget!=r.Id && !world.Robots.PendingEdits.Any(e=>e.Ids.Contains(r.Id)));
    }
    public bool CanMarch(WorldSetup world,int ruler,int target)
    {
        var node=world.Nodes.Current(ruler);
        bool deployed=Encounters.Where(e=>!e.Finished).SelectMany(e=>e.Battles).SelectMany(b=>b.Units.Concat(b.Armies.SelectMany(a=>a.Waiting))).Any(u=>u.Owner==ruler && u.Role==BattleRole.Queen && !u.Fallen && !u.Retreated);
        return node!=null && !deployed && !LockedNode(node.Id) && !LockedEdge(node.Id,target) && world.Routes.ToNode(ruler,target)!=null;
    }
    public void SendReinforcement(WorldSetup world,int owner,int source,int encounterId)
    {
        var encounter=Encounters.Single(e=>e.Id==encounterId && !e.Finished);
        if(source<0 || source>=world.Nodes.All.Length || world.Nodes.All[source].Owner!=owner || LockedNode(source))throw new InvalidOperationException("Reinforcement source is unavailable.");
        bool reachable=encounter.Kind==BattleKind.Edge?source==encounter.Source || source==encounter.Target:
            world.Routes.Neighbors(world.Nodes.All[source]).Any(r=>world.Nodes.At(r.Target,r.Entry).Id==encounter.Target) && !LockedEdge(source,encounter.Target);
        if(!reachable)throw new InvalidOperationException("Reinforcement cannot reach battlefield.");
        var robots=world.Robots.Nodes[source].Robots.Where(r=>r.Owner==owner && r.CanFight && world.Robots.Workshops[source].DisposalTarget!=r.Id && !world.Robots.PendingEdits.Any(e=>e.Ids.Contains(r.Id))).ToList();
        if(robots.Count==0)throw new InvalidOperationException("No complete robots are available for reinforcement.");
        foreach(var robot in robots)world.Robots.Nodes[source].Remove(robot.Id);
        AddReinforcement(world,encounter,owner,source,robots,false);Check(world);
    }
    public void Advance(WorldSetup world,IReadOnlyList<March> orders)
    {
        if(orders.Select(o=>o.Ruler).Distinct().Count()!=orders.Count)throw new ArgumentException("One march per ruler.");
        foreach(var o in orders)if(o.Ruler<0 || o.Ruler>=world.ActiveCount || !world.Relations.Powered[o.Ruler] || world.Relations.Pending.Any(p=>p.Ruler==o.Ruler) || o.Fighters<0 || !CanMarch(world,o.Ruler,o.TargetNode))throw new ArgumentException("Invalid robot march.");
        Scenes.Clear();Encounters.RemoveAll(e=>e.Finished);
        foreach(var reserve in Reserves.ToArray())if(!LockedNode(reserve.Node) && world.Robots.Nodes[reserve.Node].Count<12)
        {world.Robots.Nodes[reserve.Node].Add(reserve.Robot);Reserves.Remove(reserve);}
        // One global update: disposal, queued assembly/splits, simultaneous transport, shipment.
        world.Robots.Advance((a,b)=>!LockedEdge(a,b) && world.Routes.Neighbors(world.Nodes.All[a]).Any(r=>world.Nodes.At(r.Target,r.Entry).Id==b),
            n=>world.Nodes.All[n].Owner,world.PlacementSeed^world.Population.Turn^0x61238,LockedNode,(owner,target)=>world.Nodes.All[target].Owner<0 || world.Relations.Allied(owner,world.Nodes.All[target].Owner));
        world.Population.Advance(world);
        var source=orders.ToDictionary(o=>o.Ruler,o=>world.Nodes.Current(o.Ruler).Id);
        LastMarches.Clear();LastMarches.AddRange(orders.Select(o=>(o.Ruler,source[o.Ruler],o.TargetNode)));
        var troops=new Dictionary<int,List<Robot>>();
        foreach(var o in orders)
        {
            var store=world.Robots.Nodes[source[o.Ruler]];
            var selected=store.Robots.Where(r=>r.Owner==o.Ruler && r.CanFight && world.Robots.Workshops[source[o.Ruler]].DisposalTarget!=r.Id).OrderBy(r=>r.Id).Take((int)Math.Min(12,o.Fighters)).ToArray();
            foreach(var r in selected)store.Remove(r.Id);
            troops[o.Ruler]=selected.ToList();
            // Direct retinue travels with its conqueror and is excluded from transport plans.
            foreach(var r in world.Robots.Retinues[o.Ruler].Robots.Where(r=>r.CanFight).ToArray()){_retinueIds.Add(r.Id);troops[o.Ruler].Add(world.Robots.Retinues[o.Ruler].Remove(r.Id));}
        }
        var stopped=new HashSet<int>();
        foreach(var first in orders.OrderBy(o=>o.Ruler))
        {
            if(stopped.Contains(first.Ruler))continue;
            var second=orders.Where(o=>o.Ruler>first.Ruler && !stopped.Contains(o.Ruler) && source[o.Ruler]==first.TargetNode && o.TargetNode==source[first.Ruler] && !world.Relations.Allied(first.Ruler,o.Ruler)).OrderBy(o=>o.Ruler).FirstOrDefault();
            if(second==null)continue;
            var e=CreateEncounter(world,BattleKind.Edge,source[first.Ruler],first.TargetNode,-1,
                new[]{(first.Ruler,source[first.Ruler],troops[first.Ruler],true),(second.Ruler,source[second.Ruler],troops[second.Ruler],true)});
            Encounters.Add(e);stopped.Add(first.Ruler);stopped.Add(second.Ruler);
        }
        foreach(var group in orders.Where(o=>!stopped.Contains(o.Ruler)).GroupBy(o=>o.TargetNode).OrderBy(g=>g.Key))
        {
            int target=group.Key;var node=world.Nodes.All[target];var existing=Encounters.FirstOrDefault(e=>!e.Finished && e.Kind==BattleKind.Node && e.Target==target);
            if(existing!=null)
            {
                foreach(var o in group)AddReinforcement(world,existing,o.Ruler,source[o.Ruler],troops[o.Ruler],true);
                continue;
            }
            var armies=group.Select(o=>(Owner:o.Ruler,Home:source[o.Ruler],Robots:troops[o.Ruler],Queen:true)).ToList();
            foreach(var ownership in world.Robots.Nodes[target].Robots.Where(r=>r.CanFight).GroupBy(r=>r.Owner).ToArray())
            {
                var robots=ownership.ToList();foreach(var r in robots)world.Robots.Nodes[target].Remove(r.Id);
                int index=armies.FindIndex(a=>a.Owner==ownership.Key);
                if(index>=0)armies[index].Robots.AddRange(robots);else armies.Add((ownership.Key,target,robots,false));
            }
            if(armies.All(a=>world.Relations.Allied(a.Owner,armies[0].Owner)) && (node.Owner<0 || world.Relations.Allied(node.Owner,armies[0].Owner)))
            {
                if(node.Owner<0)SetOwner(world,node,armies[0].Owner);
                foreach(var army in armies){foreach(var r in army.Robots)Store(world,target,r);if(army.Queen)Move(world,army.Owner,target);}
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
        foreach(var encounter in Encounters.Where(e=>!e.Finished).ToArray())RunSegment(world,encounter);
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
            state.Deploy();e.Battles.Add(state);offset+=size;
        }
        return e;
    }
    private BattleState NewState(WorldSetup world,RobotEncounter e)
    {
        var state=new BattleState(world.PlacementSeed^e.Id^_nextBattle,Rules){Id=_nextBattle++,Kind=e.Kind,SourceNode=e.Source,TargetNode=e.Target,Defender=e.Defender,Allied=world.Relations.Allied};
        state.BuildTerrain(world.Nodes.All[e.Target].Terrain==TravelTerrain.Sea?BattleTerrain.Sea:e.Kind==BattleKind.Node?BattleTerrain.Fort:BattleTerrain.Field);return state;
    }
    private void AddReinforcement(WorldSetup world,RobotEncounter e,int owner,int home,List<Robot> robots,bool queen)
    {
        if(robots.Count==0 && !queen)return;
        var state=e.Battles.FirstOrDefault(b=>b.Armies.Any(a=>a.Owner==owner));
        if(state==null)
        {
            state=e.Battles.FirstOrDefault(b=>!b.Finished && b.Armies.Count<4);
            if(state==null){state=NewState(world,e);e.Battles.Add(state);}
            state.AddArmy(owner,home,robots,queen);return;
        }
        var army=state.Army(owner);
        state.Finished=false; // Will is reset only when a unit actually enters the board.
        foreach(var robot in robots)army.Waiting.Enqueue(new(){Id=robot.Id,Owner=owner,Robot=robot,Role=robot.Role==RobotRole.Captain?BattleRole.Captain:BattleRole.Soldier});
        if(queen && !state.Units.Concat(state.Armies.SelectMany(a=>a.Waiting)).Any(u=>u.Id==-(owner+1)))army.Waiting.Enqueue(new(){Id=-(owner+1),Owner=owner,Role=BattleRole.Queen,Daggers=0,Rifles=0});
    }
    private void RunSegment(WorldSetup world,RobotEncounter e)
    {
        foreach(var state in e.Battles){state.Events.Clear();state.Frames.Clear();state.Attacks.Clear();}
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
                var army=new BattleArmy{Owner=g.Key,HomeNode=previous.HomeNode,Side=state.Armies.Count,Limit=previous.Limit};
                if(state.Armies.Count==1)army.Side=2;state.Armies.Add(army);
                foreach(var unit in g.OrderBy(u=>u.Role==BattleRole.Queen?1:0).ThenBy(u=>u.Id))army.Waiting.Enqueue(unit);
            }
            state.Deploy();e.Battles.Add(state);
        }
        // Failed/retreated individuals are returned now; active survivors are only in the new boards.
        foreach(var state in old)ReturnUnits(world,e,state,state.Units.Where(u=>u.Fallen || u.Retreated));
        return true;
    }
    private void Complete(WorldSetup world,RobotEncounter e)
    {
        e.Finished=true;var state=e.Battles[0];int winner=state.Winner;
        if(e.Kind==BattleKind.Node)SetOwner(world,world.Nodes.All[e.Target],winner);
        foreach(var b in e.Battles)ReturnUnits(world,e,b,b.Units.Concat(b.Armies.SelectMany(a=>a.Waiting)));
        if(e.Kind==BattleKind.Node && winner>=0)
        foreach(int ruler in Enumerable.Range(0,world.ActiveCount))
            if(world.Nodes.Current(ruler).Id==e.Target && !world.Relations.Allied(ruler,winner))world.Relations.Capture(world,ruler,winner,e.Target);
        if(e.Kind==BattleKind.Edge && winner>=0)
        {
            var army=state.Army(winner);int next=army.HomeNode==e.Source?e.Target:e.Source;
            // The edge survivor continues into the opposing node, using returned survivors only.
            var survivors=state.Units.Concat(state.Armies.SelectMany(a=>a.Waiting)).Where(u=>u.Active && u.Owner==winner && u.Robot!=null).ToArray();
            var ids=survivors.Select(u=>u.Id).ToHashSet();var robots=survivors.Select(u=>u.Robot with {Owner=winner}).ToArray();
            foreach(var store in world.Robots.Nodes.Concat(world.Robots.Retinues))foreach(var r in store.Robots.Where(r=>ids.Contains(r.Id)).ToArray())store.Remove(r.Id);
            Reserves.RemoveAll(r=>ids.Contains(r.Robot.Id));
            var node=world.Nodes.All[next];var residents=world.Robots.Nodes[next].Robots.Where(r=>r.CanFight).ToArray();
            foreach(var r in residents)world.Robots.Nodes[next].Remove(r.Id);
            var armies=new List<(int,int,List<Robot>,bool)>{(winner,army.HomeNode,robots.ToList(),state.Units.Any(u=>u.Active && u.Owner==winner && u.Role==BattleRole.Queen))};
            foreach(var group in residents.GroupBy(r=>r.Owner))
            {if(group.Key==winner)armies[0].Item3.AddRange(group);else armies.Add((group.Key,next,group.ToList(),false));}
            if(node.Owner>=0 && !armies.Any(a=>a.Item1==node.Owner))armies.Add((node.Owner,next,new(),false));
            if(armies.Count==1){SetOwner(world,node,winner);foreach(var r in robots)Store(world,next,r);Move(world,winner,next);}
            else
            {
                var continuation=CreateEncounter(world,BattleKind.Node,next,next,node.Owner,armies.ToArray());
                foreach(var unit in continuation.Battles.SelectMany(b=>b.Units.Concat(b.Armies.SelectMany(a=>a.Waiting))))
                {
                    var prior=survivors.FirstOrDefault(u=>u.Id==unit.Id);if(prior==null)continue;
                    unit.Daggers=prior.Daggers;unit.Rifles=prior.Rifles;unit.Shields=prior.Shields;unit.Role=prior.Role;
                }
                Encounters.Add(continuation);RunSegment(world,continuation);
            }
        }
    }
    private void ReturnUnits(WorldSetup world,RobotEncounter e,BattleState state,IEnumerable<BattleUnit> units)
    {
        foreach(var unit in units)
        {
            var army=state.Army(unit.Owner);int home=army.HomeNode;
            if(unit.Role==BattleRole.Queen)
            {
                if(unit.Fallen){int victor=e.LostHomes.GetValueOrDefault(unit.Owner,state.Winner);if(victor>=0)world.Relations.Capture(world,unit.Owner,victor,e.Target);continue;}
                int location=e.Kind==BattleKind.Node && state.Winner>=0 && world.Relations.Allied(unit.Owner,state.Winner)?e.Target:home;
                Move(world,unit.Owner,location);continue;
            }
            if(unit.Fallen || unit.Robot==null)continue;
            int target=e.Kind==BattleKind.Node && state.Winner>=0 && world.Relations.Allied(unit.Owner,state.Winner)?e.Target:home;
            if(e.Kind==BattleKind.Edge && !world.Relations.Allied(world.Nodes.All[home].Owner,unit.Owner))continue;
            Store(world,target,unit.Robot with {Owner=unit.Owner});
        }
    }
    private static void SetOwner(WorldSetup world,Node node,int owner)
    {if(node.Owner==owner)return;node.Owner=owner;Array.Clear(node.Population.Conversion);Array.Clear(node.Population.Migration);}
    private static void Move(WorldSetup world,int owner,int node)
    {world.ConquerorLocations[owner]=world.Nodes.All[node].Cell;world.ConquerorPoints[owner]=world.Nodes.All[node].Center;world.Relations.Released[owner]=false;}
    private void Store(WorldSetup world,int node,Robot robot)
    {
        if(_retinueIds.Contains(robot.Id) && world.Robots.Retinues[robot.Owner].Count<12){world.Robots.Retinues[robot.Owner].Add(robot);return;}
        if(world.Robots.Nodes[node].Count<12)world.Robots.Nodes[node].Add(robot);
        else if(world.Robots.Retinues[robot.Owner].Count<12)world.Robots.Retinues[robot.Owner].Add(robot);
        else Reserves.Add((node,robot));
    }
    public void Check(WorldSetup world)
    {
        world.Robots.Check();
        var stored=world.Robots.Nodes.Concat(world.Robots.Retinues).SelectMany(s=>s.Robots);
        var fighting=Encounters.Where(e=>!e.Finished).SelectMany(e=>e.Battles).SelectMany(b=>b.Units.Concat(b.Armies.SelectMany(a=>a.Waiting))).Where(u=>u.Robot!=null).Select(u=>u.Robot);
        var all=stored.Concat(fighting).Concat(Reserves.Select(r=>r.Robot)).ToArray();
        if(all.Select(r=>r.Id).Distinct().Count()!=all.Length)throw new InvalidOperationException("Robot duplicated between world and battlefield.");
    }
}
