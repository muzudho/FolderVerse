namespace FolderVerse;
using System;
using System.Collections.Generic;
using System.Linq;

// Consolidate a sealed friendly pocket into its entrance, never across a second frontier.
public static class ComputerFunctionTransfers
{
    public static void Plan(WorldSetup world,bool automaticAll=false)
    {
        if(world.Campaign.Phase!=CampaignPhase.Domestic)return;
        var manual=world.ManualRulers().ToHashSet();
        var w=world;var stock=w.Robots;var battles=w.Campaign.RobotBattles;
        bool AttackBusy(int node)=>battles.LockedNode(node) || w.Campaign.FunctionTransfers.Orders.Any(o=>o.Source==node || o.Target==node) ||
            stock.PartPlanning.Orders.Any(o=>o.Source==node || o.Target==node) || stock.PendingEdits.Any(o=>o.Node==node) ||
            stock.DirectDelivery.Orders.Any(o=>o.Source==node || o.Target==node) || w.Campaign.PlannedOrders.Any(o=>o.SourceNode==node || o.TargetNode==node);
        bool AnyViableAttack(){
            var automated=Enumerable.Range(0,w.ActiveCount).Where(o=>w.Relations.CanAct(o) && (automaticAll || !manual.Contains(o))).ToHashSet();
            if(w.Campaign.PlannedOrders.Any(o=>automated.Contains(o.Ruler) && !o.Peaceful && (w.Nodes.All[o.TargetNode].Owner<0 || !w.Relations.Allied(o.Ruler,w.Nodes.All[o.TargetNode].Owner))))return true;
            foreach(var family in automated.GroupBy(w.Relations.Root)){
                var members=family.ToHashSet();
                var sources=w.Nodes.All.Where(n=>n.Functional && members.Contains(n.Owner) && !AttackBusy(n.Id)).Select(n=>{
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
        bool recovery=!AnyViableAttack(),recoveryReserved=false;
        var graph=world.Nodes.All.Select(n=>world.Routes.Neighbors(n).Select(r=>world.Nodes.At(r.Target,r.Entry).Id).Distinct().ToArray()).ToArray();
        for(int owner=0;owner<world.ActiveCount;owner++){
            if(!world.Relations.CanAct(owner) || !automaticAll && manual.Contains(owner))continue;
            if(world.Campaign.FunctionTransfers.Orders.Any(o=>o.Owner==owner))continue;
            bool Busy(int id)=>world.Campaign.FunctionTransfers.Orders.Any(o=>o.Source==id || o.Target==id) ||
                world.Robots.PartPlanning.Orders.Any(o=>o.Source==id || o.Target==id) ||
                world.Robots.DirectDelivery.Orders.Any(o=>o.Source==id || o.Target==id) ||
                world.Robots.LongDelivery.Orders.Any(o=>o.Source==id || o.Target==id || o.Node==id) ||
                world.Robots.PendingEdits.Any(o=>o.Node==id) ||
                world.Campaign.PlannedOrders.Any(o=>o.SourceNode==id || o.TargetNode==id);
            var targets=world.Nodes.All.Where(n=>n.Owner==owner && n.Functional && !Busy(n.Id))
                .Where(n=>world.Campaign.RobotBattles.MarchTargets(world,n.Id).Any(id=>world.Nodes.All[id].Owner>=0 && !world.Relations.Allied(owner,world.Nodes.All[id].Owner)))
                .OrderByDescending(n=>n.FunctionUnits).ThenBy(n=>n.Id);
            bool reserved=false;
            foreach(var target in targets){
                foreach(int source in graph[target.Id].OrderBy(id=>id)){
                    var node=world.Nodes.All[source];
                    if(!node.Functional || node.Owner!=owner || Busy(source))continue;
                    bool enclosed=EnclosedPocket(graph,source,target.Id,id=>world.Nodes.All[id].Owner==owner && !world.Campaign.RobotBattles.LockedNode(id));
                    bool local=recovery && !recoveryReserved && !battles.MarchTargets(world,source).Any(id=>world.Nodes.All[id].Owner>=0 && !world.Relations.Allied(owner,world.Nodes.All[id].Owner)) &&
                        graph[source].All(id=>world.Nodes.All[id].Owner>=0 && world.Relations.Allied(owner,world.Nodes.All[id].Owner) && !battles.LockedNode(id)) &&
                        stock.Nodes[source].Robots.Any(r=>r.Owner==owner && stock.Training[owner].Eligible(r) && !r.FactoryWorker);
                    if(!enclosed && !local)continue;
                    if(world.Campaign.FunctionTransfers.ReserveComputer(world,source,target.Id,owner)){
                        if(!enclosed)recoveryReserved=true;
                        reserved=true;break;
                    }
                }
                if(reserved)break;
            }
        }
    }
    // Removing the entrance must disconnect the entire source side from all foreign land.
    public static bool EnclosedPocket(int[][] graph,int source,int entrance,System.Func<int,bool> safe)
    {
        var seen=new HashSet<int>{entrance,source};var queue=new Queue<int>();queue.Enqueue(source);
        while(queue.TryDequeue(out int at)){
            if(!safe(at))return false;
            foreach(int next in graph[at])if(seen.Add(next))queue.Enqueue(next);
        }
        return true;
    }
}
