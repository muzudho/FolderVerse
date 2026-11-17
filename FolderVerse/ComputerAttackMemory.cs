namespace FolderVerse;

using System.Collections.Generic;
using System.Linq;

public sealed record ComputerAttackFailure(int Ruler,int Source,int Target,int Force,int Turn,int TargetOwner,int TargetRoot);

// Reconsider failed computer sorties without changing battle rules or learned tactics.
public sealed class ComputerAttackMemory
{
    public const int RetentionTurns=5;
    public List<ComputerAttackFailure> Failures {get;}=new();
    public List<March> PendingOrders {get;}=new();

    private static (int Ruler,int Source,int Target) Key(WorldSetup world,March order)=>
        (order.Ruler,order.SourceNode>=0?order.SourceNode:world.Nodes.Current(order.Ruler)?.Id??-1,order.TargetNode);

    internal void Adjust(WorldSetup world,List<March> orders,IReadOnlyList<March> submitted)
    {
        PendingOrders.Clear();
        var protectedKeys=submitted.Select(o=>Key(world,o)).ToHashSet();
        bool Eligible(March o)=>!o.Queen && !o.PocketOnly && !o.Peaceful && o.SourceNode>=0 &&
            o.RobotIds!=null && !protectedKeys.Contains(Key(world,o));
        int turn=world.Population.Turn+1;
        for(int i=orders.Count-1;i>=0;i--){
            var order=orders[i];if(!Eligible(order))continue;
            var prior=Recent(world,order.Ruler,order.SourceNode,order.TargetNode,turn);
            if(prior==null || order.Fighters>prior.Force)continue;
            var stock=world.Robots;
            var used=orders.Where((other,index)=>index!=i).SelectMany(o=>o.RobotIds??System.Array.Empty<long>()).ToHashSet();
            var pool=world.Campaign.RobotBattles.SelectableRobots(world,order.Ruler,order.SourceNode,false)
                .Where(r=>!used.Contains(r.Id));
            var ready=(stock.Training[order.Ruler].DeploymentPolicy==3
                ?pool.OrderByDescending(r=>r.Equipment(order.Ruler,world.EquipmentMode).Total).ThenBy(r=>r.Id)
                :stock.Training[order.Ruler].Order(pool)).ToArray();
            bool guard=stock.Nodes[order.SourceNode].Robots.Any(r=>r.Owner==order.Ruler && r.DefenseOnly && r.CanFight);
            int keep=world.RulerComputerPolicy(order.Ruler).GuardCount(ready.Length,guard);
            int needed=prior.Force+1;
            if(ready.Length-keep<needed){orders.RemoveAt(i);continue;}
            orders[i]=order with {Fighters=needed,RobotIds=ready.Take(needed).Select(r=>r.Id).ToArray()};
        }
        PendingOrders.AddRange(orders.Where(Eligible));
    }

    internal void Observe(WorldSetup world,IReadOnlyList<March> resolved)
    {
        foreach(var order in PendingOrders){
            if(!resolved.Any(o=>Key(world,o)==Key(world,order) && o.Fighters==order.Fighters &&
                o.RobotIds!=null && o.RobotIds.SequenceEqual(order.RobotIds)))continue;
            int owner=world.Nodes.All[order.TargetNode].Owner;
            if(owner>=0 && world.Relations.Allied(owner,order.Ruler)){
                Failures.RemoveAll(f=>f.Ruler==order.Ruler && f.Source==order.SourceNode && f.Target==order.TargetNode);
                continue;
            }
            if(world.Campaign.RobotBattles.Encounters.Where(e=>e.Target==order.TargetNode).SelectMany(e=>e.Battles)
                .Any(b=>b.Finished && (b.Winner<0 || !world.Relations.Allied(b.Winner,order.Ruler))))
                Record(world,order.Ruler,order.SourceNode,order.TargetNode,(int)order.Fighters,world.Population.Turn);
        }
        PendingOrders.Clear();
    }

    internal ComputerAttackFailure Recent(WorldSetup world,int ruler,int source,int target,int turn)
    {
        var failure=Failures.LastOrDefault(f=>f.Ruler==ruler && f.Source==source && f.Target==target);
        if(failure==null)return null;
        int owner=world.Nodes.All[target].Owner;
        int root=owner<0?-1:world.Relations.Root(owner);
        if(turn-failure.Turn>RetentionTurns || owner!=failure.TargetOwner || root!=failure.TargetRoot){
            Failures.RemoveAll(f=>f.Ruler==ruler && f.Source==source && f.Target==target);
            return null;
        }
        return failure;
    }

    internal void Record(WorldSetup world,int ruler,int source,int target,int force,int turn)
    {
        if(force<1)return;
        var prior=Recent(world,ruler,source,target,turn);
        int owner=world.Nodes.All[target].Owner;
        var failure=new ComputerAttackFailure(ruler,source,target,
            System.Math.Max(force,prior?.Force??0),turn,owner,owner<0?-1:world.Relations.Root(owner));
        Failures.RemoveAll(f=>f.Ruler==ruler && f.Source==source && f.Target==target);
        Failures.Add(failure);
    }

    public void Reset(){Failures.Clear();PendingOrders.Clear();}
}
