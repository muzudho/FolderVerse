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
        foreach(int owner in WorldDayCycle.Order(world,Enumerable.Range(0,world.ActiveCount))){
            if(!world.Relations.CanAct(owner) || !automaticAll && manual.Contains(owner))continue;
            if(world.Campaign.FunctionTransfers.Orders.Any(o=>o.Owner==owner))continue;
            bool Busy(int id)=>ComputerStratagems.ProtectedSite(world,id) || world.Campaign.FunctionTransfers.Orders.Any(o=>o.Source==id || o.Target==id) ||
                world.Robots.PartPlanning.WorkpieceResumes.Any(o=>o.Node==id) || world.Robots.PartPlanning.Orders.Any(o=>o.Source==id || o.Target==id) ||
                world.Robots.DirectDelivery.Orders.Any(o=>o.Source==id || o.Target==id) ||
                world.Robots.LongDelivery.Orders.Any(o=>o.Source==id || o.Target==id || o.Node==id) ||
                world.Robots.PendingEdits.Any(o=>o.Node==id) ||
                world.Campaign.PlannedOrders.Any(o=>o.SourceNode==id || o.TargetNode==id);
            if(world.RelayLogisticsEnabled && PlanRelayNetwork(world,owner,graph,Busy))continue;
            var targets=world.Nodes.All.Where(n=>n.Owner==owner && n.Functional && !Busy(n.Id) && world.Robots.Workshops[n.Id].Automation!=NodeAutomation.Manual)
                .Where(n=>world.Campaign.RobotBattles.MarchTargets(world,n.Id).Any(id=>world.Nodes.All[id].Owner>=0 && !world.Relations.Allied(owner,world.Nodes.All[id].Owner)))
                .OrderByDescending(n=>n.FunctionUnits).ThenBy(n=>n.Id);
            bool reserved=false;
            foreach(var target in targets){
                foreach(int source in graph[target.Id].OrderBy(id=>id)){
                    var node=world.Nodes.All[source];
                    if(!node.Functional || node.Owner!=owner || Busy(source) || world.Robots.Workshops[source].Automation==NodeAutomation.Manual)continue;
                    if(!EnclosedPocket(graph,source,target.Id,id=>world.Nodes.All[id].Owner==owner && !world.Campaign.RobotBattles.LockedNode(id)))continue;
                    bool leaveRelay=world.RelayLogisticsEnabled && node.Purpose!=NodePurpose.Relay &&
                        world.Robots.Workshops[source].LineLimit>0 && graph[source].Count(id=>world.Nodes.All[id].Functional && world.Nodes.All[id].Owner==owner)>=3 &&
                        !RobotWorld.RawMaterials.Any(m=>world.Robots.RawDeposit(source,m)>0) &&
                        world.Robots.Nodes[source].MaplePackages.Count>0;
                    if(world.Campaign.FunctionTransfers.ReserveComputer(world,source,target.Id,owner,leaveRelay)){reserved=true;break;}
                }
                if(reserved)break;
            }
        }
    }
    private static bool PlanRelayNetwork(WorldSetup world,int owner,int[][] graph,System.Func<int,bool> busy)
    {
        var r=world.Robots;
        int Connections(int id)=>graph[id].Count(n=>world.Nodes.All[n].Functional && world.Nodes.All[n].Owner==owner && world.Nodes.All[n].Purpose!=NodePurpose.Relay);
        // Prefer a freight hub beside more factories/end users. Stable IDs break equal ties;
        // merging removes one active delivery stop, while the vacated site remains traversable.
        foreach(var target in world.Nodes.All.Where(n=>n.Functional && n.Owner==owner && n.Purpose==NodePurpose.Relay && !busy(n.Id))
            .OrderByDescending(n=>Connections(n.Id)).ThenBy(n=>n.Id)){
            if(Connections(target.Id)==0)continue;
            foreach(int source in graph[target.Id].OrderBy(n=>n)){
                var from=world.Nodes.All[source];
                if(!from.Functional || from.Owner!=owner || from.Purpose!=NodePurpose.Relay || busy(source) ||
                    r.Workshops[source].Automation==NodeAutomation.Manual || r.Workshops[target.Id].Automation==NodeAutomation.Manual ||
                    Connections(source)>Connections(target.Id) || Connections(source)==Connections(target.Id) && source<target.Id)continue;
                if(world.Campaign.FunctionTransfers.ReserveComputer(world,source,target.Id,owner))return true;
            }
        }
        // Move an unused factory out of a safe freight junction into an operating factory.
        // Keep the junction and its raw stock: moving everything would erase this storage stop.
        foreach(var source in world.Nodes.All.Where(n=>n.Functional && n.Owner==owner && n.Purpose!=NodePurpose.Relay && !busy(n.Id))){
            int id=source.Id;
            if(r.Workshops[id].Automation==NodeAutomation.Manual || r.Workshops[id].LineLimit==0 ||
                r.Workshops[id].ProductionLines.Length!=0 || r.Nodes[id].MaplePackages.Count==0 ||
                RobotWorld.RawMaterials.Any(m=>r.RawDeposit(id,m)>0) || Connections(id)<2 ||
                graph[id].Any(n=>world.Nodes.All[n].Owner>=0 && !world.Relations.Allied(owner,world.Nodes.All[n].Owner)))continue;
            foreach(int target in graph[id].OrderByDescending(n=>r.Workshops[n].ProductionLines.Length).ThenBy(n=>n)){
                if(world.Nodes.All[target].Owner!=owner || !world.Nodes.All[target].Functional || world.Nodes.All[target].Purpose==NodePurpose.Relay || busy(target) ||
                    r.Workshops[target].Automation==NodeAutomation.Manual || !r.Nodes[target].Robots.Any(robot=>robot.FactoryWorker))continue;
                if(world.Campaign.FunctionTransfers.ReserveComputer(world,id,target,owner,true))return true;
            }
        }
        return false;
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
