namespace FolderVerse;
using System;
using System.Collections.Generic;
using System.Linq;

public sealed record SubordinateDomesticResult(int Turn,int Controller,int Owner,int Node,string Action);
public static class SubordinateDomestic
{
    public static int[] Pending(WorldSetup w,int controller,IEnumerable<int> checks)
    {
        if(controller<0 || controller>=w.ActiveCount || w.ControllerOf(controller)!=controller)return Array.Empty<int>();
        var marked=checks.ToHashSet();
        return w.Nodes.All.Where(n=>n.Functional && n.Owner>=0 && n.Owner!=controller && w.ControllerOf(n.Owner)==controller &&
            w.Relations.CanAct(n.Owner) && !marked.Contains(n.Id) && !w.Campaign.RobotBattles.LockedNode(n.Id) &&
            !w.Campaign.SubordinateResults.Any(r=>r.Turn==w.Population.Turn && r.Controller==controller && r.Node==n.Id))
            .OrderBy(n=>n.Owner).ThenBy(n=>n.Id).Select(n=>n.Id).ToArray();
    }
    private static bool Busy(WorldSetup w,int id)=>
        w.Campaign.FunctionTransfers.Orders.Any(o=>o.Source==id || o.Target==id) ||
        w.Robots.DirectDelivery.Orders.Any(o=>o.Source==id || o.Target==id) ||
        w.Robots.PartPlanning.Orders.Any(o=>o.Source==id || o.Target==id) ||
        w.Robots.PendingEdits.Any(o=>o.Node==id) ||
        w.Campaign.PlannedOrders.Any(o=>o.SourceNode==id || o.TargetNode==id || o.SourceNode<0 && w.Nodes.Current(o.Ruler)?.Id==id);
    public static SubordinateDomesticResult Operate(WorldSetup w,int controller,int node,IReadOnlySet<int> allowed)
    {
        var prior=w.Campaign.SubordinateResults.FirstOrDefault(r=>r.Turn==w.Population.Turn && r.Controller==controller && r.Node==node);
        if(prior!=null)return prior;
        if(w.Campaign.Phase!=CampaignPhase.Domestic || !allowed.Contains(node) || !Pending(w,controller,Array.Empty<int>()).Contains(node))
            throw new InvalidOperationException("手下に任せる地点ではありません。");
        var site=w.Nodes.All[node];var shop=w.Robots.Workshops[node];int owner=site.Owner;
        var actions=new List<string>();
        if(Busy(w,node))actions.Add("手動の予約を維持");
        else if(shop.SupervisorLeaveRemaining>0)actions.Add("監督の休暇を維持");
        else{
            w.Robots.AdjustNode(node,owner,(a,b)=>false,policyOverride:NodeAutomation.Auto,adjustTransport:false);
            w.Robots.PartPlanning.Invalidate();
            int before=w.Robots.PartPlanning.Orders.Count;
            var complete=w.Robots.PartPlanning.Preview(w).Nodes[node].Robots.Where(r=>r.Owner==owner && r.Parts==RobotParts.Complete && !r.Stationed && !r.Disposal).Select(r=>r.Id).ToArray();
            foreach(long id in complete)w.Robots.PartPlanning.EnsureForecastRobot(w,id);
            actions.Add(w.Robots.PartPlanning.Orders.Count>before?"製造を調整・組立を予約":"製造・格納庫を確認");
            var soldiers=w.Campaign.DeliverySortieRobots(w,owner,node).Where(r=>!r.Stationed && !r.Disposal).Take(13).ToArray();
            // Keep one complete robot home. Only attack a weaker visible neighboring force.
            var outgoing=soldiers.Take(Math.Min(12,Math.Max(0,soldiers.Length-1))).Select(r=>r.Id).ToArray();
            int target=w.Campaign.RobotBattles.MarchTargets(w,node).Where(id=>{
                var enemy=w.Nodes.All[id];
                if(enemy.Owner<0 || w.Relations.Allied(owner,enemy.Owner) || w.Campaign.RobotBattles.LockedNode(id) || w.Campaign.PlannedOrders.Any(o=>o.TargetNode==id))return false;
                int defense=w.Robots.Nodes[id].Robots.Count(r=>r.Parts==RobotParts.Complete && !r.Disposal);
                foreach(int ruler in Enumerable.Range(0,w.ActiveCount).Where(r=>w.Nodes.Current(r)?.Id==id && w.Relations.Allied(enemy.Owner,r)))defense+=w.Robots.Retinues[ruler].Robots.Count(r=>r.Parts==RobotParts.Complete);
                return outgoing.Length>defense;
            }).OrderBy(id=>id).FirstOrDefault(-1);
            if(outgoing.Length>0 && target>=0){
                w.Campaign.PlanDeliverySortie(w,new(owner,target,outgoing.Length,node,Queen:false,RobotIds:outgoing));actions.Add("開戦を予約");
            }else if(outgoing.Length>1){
                int destination=FunctionalDeliveryRoutes.Targets(w,node).Where(id=>allowed.Contains(id) && w.Nodes.All[id].Owner==owner && !Busy(w,id))
                    .OrderBy(id=>w.Robots.PartPlanning.Preview(w).Nodes[id].Count).FirstOrDefault(-1);
                if(destination>=0 && w.Robots.PartPlanning.Preview(w).Nodes[destination].Count<w.Robots.PartPlanning.Preview(w).Nodes[node].Count-1){
                    try{w.Robots.PartPlanning.Reserve(w,node,destination,outgoing[0],RobotParts.Complete,null,owner);actions.Add("直送便を予約");}
                    catch(InvalidOperationException){actions.Add("直送便は空きを待機");}
                }
            }
        }
        w.Campaign.SubordinateResults.RemoveAll(r=>r.Turn<w.Population.Turn-1);
        var result=new SubordinateDomesticResult(w.Population.Turn,controller,owner,node,string.Join(" / ",actions));
        w.Campaign.SubordinateResults.Add(result);return result;
    }
}
