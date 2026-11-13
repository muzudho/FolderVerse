namespace FolderVerse;
using System;
using System.Linq;
using Microsoft.Xna.Framework;

public partial class Game1
{
    private RobotWorld PlanningStock=>_computerAssemblyStock ?? (DomesticOperationsAllowed?_setup.Robots.PartPlanning.Preview(_setup):_setup.Robots);
    private bool InspectableNode(int node)=>node>=0 && node<_setup.Nodes.All.Length && _setup.Nodes.All[node].Functional;
    private int HangarPlanningNode=>DomesticOperationsAllowed
        ? _setup.Campaign.FunctionTransfers.Orders.FirstOrDefault(o=>o.Source==_populationNode && _setup.Campaign.FunctionTransfers.CanReserve(_setup,o.Source,o.Target))?.Target??_populationNode
        : _populationNode;
    private bool PlanningFunctional(int node)
    {
        if(node<0 || node>=_setup.Nodes.All.Length)return false;
        return DomesticOperationsAllowed
        ? !_setup.Campaign.FunctionTransfers.Orders.Any(o=>o.Source==node) && (_setup.Nodes.All[node].Functional || _setup.Campaign.FunctionTransfers.Orders.Any(o=>o.Target==node))
        :_setup.Nodes.All[node].Functional;
    }
    private static readonly Rectangle HangarUndoButton=new(606,252,258,24);
    private int[] PlanningFactorySources(int node)=>DomesticOperationsAllowed?_setup.Campaign.FunctionTransfers.Orders.Where(o=>o.Target==node).Select(o=>o.Source).Prepend(node).ToArray():new[]{node};
    private RobotWorkshop PlanningWorkshop(int node)=>PlanningFactorySources(node).Length==1?_setup.Robots.Workshops[node]:RobotWorkshop.PlanningView(PlanningFactorySources(node).Select(n=>_setup.Robots.Workshops[n]));
    private bool PlanningCanAttackFrom(int owner,int source,int target)=>PlanningFunctional(source) && _setup.Nodes.All[source].Owner==owner && _setup.Campaign.RobotBattles.MarchTargets(_setup,source).Contains(target);
    private int PlanningMoveSource(PlannedPartMove order)
    {
        int source=Array.FindIndex(_setup.Robots.Nodes,s=>s.Robots.Any(r=>r.Id==order.RobotId));
        if(source<0)source=order.Source;
        return _setup.Campaign.FunctionTransfers.Orders.FirstOrDefault(o=>o.Source==source)?.Target??source;
    }
    private void SetPlanningReservedSlots(int node,int count)
    {
        foreach(int source in PlanningFactorySources(node)){
            var shop=_setup.Robots.Workshops[source];int assigned=Math.Min(count,shop.ReservedSlotLimit);shop.SetReservedSlots(assigned);count-=assigned;
        }
    }
    private void UndoHangarPlan()
    {
        var latest=_setup.Robots.PartPlanning.Orders.LastOrDefault(o=>CanManageNodeFactory(o.Owner));
        bool undone=latest!=null && _setup.Robots.PartPlanning.Undo(latest.Owner);_hangarDrag=null;
        _hangarMessage=undone?"直前の組替・直送・仕事の予約を取り消しました":"取り消せる予約はありません";
    }
    private void DrawHangarUndo()
    {
        _ui.Button(HangarUndoButton,"直前の予約を取り消す",Muted,.32f);
    }
    private string HangarPlanningFailure
    {
        get{
            var plans=_setup.Robots.PartPlanning;
            var failure=plans.PreviewResults.LastOrDefault(r=>r.Target==_populationNode && r.Status!="実施予定" && CanManageNodeFactory(r.Owner));
            if(failure!=null)return "実施できない予約："+failure.Status;
            failure=plans.LastResults.LastOrDefault(r=>r.Target==_populationNode && r.Status!="実施完了" && CanManageNodeFactory(r.Owner));
            return failure==null?"":"前回の予約："+failure.Status;
        }
    }
}
