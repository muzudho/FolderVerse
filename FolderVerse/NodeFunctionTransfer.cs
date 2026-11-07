namespace FolderVerse;
using System;
using System.Collections.Generic;
using System.Linq;

public sealed record NodeFunctionTransferOrder(int Source,int Target,int Owner)
{
    public string Status {get;set;}="予約";
}
public sealed class NodeFunctionTransfers
{
    public const int MaximumCapacity=60;
    public static int[] Members(WorldSetup w,int host)=>w.Nodes.All.Where(n=>n.Id==host || n.FunctionHost==host).Select(n=>n.Id).ToArray();
    public List<NodeFunctionTransferOrder> Orders {get;}=new();
    public List<NodeFunctionTransferOrder> LastResults {get;}=new();
    public bool Cancel(int source)=>Orders.RemoveAll(o=>o.Source==source)>0;
    public bool CanReserve(WorldSetup w,int source,int target)
        =>Available(w,source,target,true);
    internal IEnumerable<int> ValidSources(WorldSetup w)=>Orders
        .Where(o=>Available(w,o.Source,o.Target,false) && w.Nodes.All[o.Source].Owner==o.Owner)
        .Select(o=>o.Source);
    private bool Available(WorldSetup w,int source,int target,bool authority)
    {
        if(w.Campaign.Phase!=CampaignPhase.Domestic || source<0 || target<0 || source>=w.Nodes.All.Length || target>=w.Nodes.All.Length || source==target)return false;
        var a=w.Nodes.All[source];var b=w.Nodes.All[target];var battles=w.Campaign.RobotBattles;
        long capacity=(long)w.Robots.Nodes[target].TotalCapacity+w.Robots.Nodes[source].TotalCapacity+
            Orders.Where(o=>o.Source!=source && o.Target==target).Sum(o=>(long)w.Robots.Nodes[o.Source].TotalCapacity);
        if(capacity>MaximumCapacity)return false;
        return a.Functional && b.Functional && (!authority || w.Relations.Controls(a.Owner) && w.Relations.Controls(b.Owner)) && w.Relations.Allied(a.Owner,b.Owner) &&
            !battles.LockedNode(source) && !battles.LockedNode(target) && !battles.LockedEdge(source,target) &&
            w.Robots.Nodes[source].ReservedReturns==0 && w.Robots.Nodes[target].ReservedReturns==0 &&
            !battles.Reserves.Any(r=>r.Node==source || r.Node==target) &&
            !Orders.Any(o=>o.Source!=source && (o.Source==target || o.Target==source)) &&
            w.Routes.Neighbors(a).Any(r=>w.Nodes.At(r.Target,r.Entry).Id==target);
    }
    public void Reserve(WorldSetup w,int source,int target)
    {
        if(!CanReserve(w,source,target))throw new InvalidOperationException("Function transfer unavailable.");
        Cancel(source);Orders.Add(new(source,target,w.Nodes.All[source].Owner));
    }
    internal bool ReserveComputer(WorldSetup w,int source,int target,int owner)
    {
        if(w.Nodes.All[source].Owner!=owner || w.Nodes.All[target].Owner!=owner || !Available(w,source,target,false))return false;
        Orders.Add(new(source,target,owner));return true;
    }
    internal void Resolve(WorldSetup w)
    {
        LastResults.Clear();
        foreach(var order in Orders.ToArray()){
            if(w.Nodes.All[order.Source].Owner!=order.Owner || !Available(w,order.Source,order.Target,false)){
                Orders.Remove(order);LastResults.Add(order with {Status="移転失敗"});continue;
            }
            int a=order.Source,b=order.Target,offset=w.Robots.Nodes[b].TotalCapacity;
            var source=w.Robots.Nodes[a];var target=w.Robots.Nodes[b];
            target.SetCapacity(checked(offset+source.TotalCapacity));
            target.Replace(target.Robots.Concat(source.Robots.Select(r=>(r.DefenseOnly?r.WithoutDefenseReservation():r) with {FunctionDisplayNode=r.FunctionDisplayNode??a})).ToArray());
            source.Replace(Array.Empty<Robot>());source.SetCapacity(0);
            w.Robots.Workshops[a].PreserveFunctionOrigin(a);w.Robots.Workshops[b].PreserveFunctionOrigin(b);
            w.Robots.Workshops[b].AbsorbFunctions(w.Robots.Workshops[a]);
            w.Robots.Workshops[a]=RobotWorkshop.EmptyFunctions();
            w.Nodes.All[b].FunctionUnits=checked(w.Nodes.All[b].FunctionUnits+w.Nodes.All[a].FunctionUnits);w.Nodes.All[a].FunctionUnits=0;
            foreach(var node in w.Nodes.All)if(node.FunctionHost==a)node.FunctionHost=b;
            w.Nodes.All[a].FunctionHost=b;w.Nodes.All[b].FunctionHost=-1;
            for(int i=0;i<w.Robots.PendingEdits.Count;i++)if(w.Robots.PendingEdits[i].Node==a)w.Robots.PendingEdits[i]=w.Robots.PendingEdits[i] with {Node=b};
            w.Robots.DirectDelivery.RelocateNode(a,b,offset);
            w.Robots.PartPlanning.RelocateLocalOrders(a,b);
            w.Robots.RelocateProductionIntakes(a,b);
            w.Robots.Transport.RelocateNode(a,b);
            w.Campaign.RewriteReservations(m=>!m.PocketOnly && m.SourceNode==a?m with {SourceNode=b}:m);
            Orders.Remove(order);LastResults.Add(order with {Status="移転完了"});w.Robots.SyncWorkers(b);
        }
        w.Robots.Check();
    }
}

public static class FunctionalDeliveryRoutes
{
    // Intermediate empty nodes do not consume a delivery turn; active nodes terminate each route.
    public static int[] Targets(WorldSetup w,int source,int? owner=null)
    {
        if(source<0 || source>=w.Nodes.All.Length || !w.Nodes.All[source].Functional)return Array.Empty<int>();
        int country=owner??w.Nodes.All[source].Owner;var seen=new HashSet<int>{source};var queue=new Queue<int>();var result=new List<int>();queue.Enqueue(source);
        var battles=w.Campaign.RobotBattles;
        while(queue.Count>0){int at=queue.Dequeue();
            foreach(var route in w.Routes.Neighbors(w.Nodes.All[at])){
                int next=w.Nodes.At(route.Target,route.Entry).Id;
                if(battles.LockedEdge(at,next) || battles.LockedNode(next) || w.Nodes.All[next].Owner>=0 && !w.Relations.Allied(country,w.Nodes.All[next].Owner) || !seen.Add(next))continue;
                if(w.Nodes.All[next].Functional)result.Add(next);else queue.Enqueue(next);
            }
        }
        return result.OrderBy(n=>n).ToArray();
    }
    public static bool Connected(WorldSetup w,int source,int target)=>Targets(w,source).Contains(target);
}
