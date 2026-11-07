namespace FolderVerse;
using System.Collections.Generic;
using System.Linq;

// Consolidate a sealed friendly pocket into its entrance, never across a second frontier.
public static class ComputerFunctionTransfers
{
    public static void Plan(WorldSetup world,bool automaticAll=false)
    {
        if(world.Campaign.Phase!=CampaignPhase.Domestic)return;
        var manual=world.ManualRulers().ToHashSet();
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
                    if(!EnclosedPocket(graph,source,target.Id,id=>world.Nodes.All[id].Owner==owner && !world.Campaign.RobotBattles.LockedNode(id)))continue;
                    if(world.Campaign.FunctionTransfers.ReserveComputer(world,source,target.Id,owner)){reserved=true;break;}
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
