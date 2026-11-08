namespace FolderVerse;
using System;
using System.Collections.Generic;
using System.Linq;

public sealed record LongPartOrder(long Number,int Commander,int Source,int Target,long RobotId,RobotParts Parts,long? Receiver)
{
    public int Node {get;set;}
    public long CargoId {get;set;}
    public bool Started {get;set;}
    public int TargetSlot {get;init;}
    public string Status {get;set;}="出荷予約";
}
public sealed class RobotLongDelivery
{
    private long _next=1;
    private Dictionary<(int Node,int Slot,int Owner),long> _slotArrivals=new();
    public List<LongPartOrder> Orders {get;}=new();
    public List<LongPartOrder> Results {get;}=new();
    public void Reset(){_next=1;Orders.Clear();Results.Clear();_slotArrivals.Clear();}
    public long SlotArrival(int node,int slot,int owner)=>_slotArrivals.GetValueOrDefault((node,slot,owner));
    public bool HoldsSource(int node,long id)=>Orders.Any(o=>!o.Started && o.Node==node && o.CargoId==id);
    internal void RestoreLegacySlots(RobotWorld world)
    {
        for(int i=0;i<Orders.Count;i++)if(Orders[i].TargetSlot<0){
            var order=Orders[i];int slot=Enumerable.Range(0,world.Nodes[order.Target].TotalCapacity).FirstOrDefault(s=>!Orders.Any(o=>o.Target==order.Target && !o.Receiver.HasValue && o.TargetSlot==s),-1);
            Orders[i]=order with {TargetSlot=slot};
        }
    }
    public static int[] Neighbors(WorldSetup w,int source,int owner,bool unlocked=false)
    {
        var seen=new HashSet<int>{source};var queue=new Queue<int>();var result=new HashSet<int>();queue.Enqueue(source);
        while(queue.TryDequeue(out int at))foreach(var route in w.Routes.Neighbors(w.Nodes.All[at])){
            int next=w.Nodes.At(route.Target,route.Entry).Id,country=w.Nodes.All[next].Owner;
            if(country>=0 && !w.Relations.Allied(owner,country) || unlocked && (w.Campaign.RobotBattles.LockedNode(next) || w.Campaign.RobotBattles.LockedEdge(at,next)) || !seen.Add(next))continue;
            if(w.Nodes.All[next].Functional)result.Add(next);else queue.Enqueue(next);
        }
        return result.OrderBy(n=>n).ToArray();
    }
    public static int[] Path(WorldSetup w,int source,int target,int owner)
    {
        if(source<0 || target<0 || source>=w.Nodes.All.Length || target>=w.Nodes.All.Length)return Array.Empty<int>();
        if(w.Nodes.All[source].Owner>=0 && !w.Relations.Allied(owner,w.Nodes.All[source].Owner))return Array.Empty<int>();
        var previous=new Dictionary<int,int>{{source,-1}};var queue=new Queue<int>();queue.Enqueue(source);
        while(queue.TryDequeue(out int at)){
            if(at==target){var path=new List<int>();for(int node=at;node>=0;node=previous[node])path.Add(node);path.Reverse();return path.ToArray();}
            foreach(int next in Neighbors(w,at,owner))if(previous.TryAdd(next,at))queue.Enqueue(next);
        }
        return Array.Empty<int>();
    }
    public LongPartOrder Reserve(WorldSetup w,int source,int target,long id,RobotParts parts,long? receiver,int commander,int targetSlot=0)
        =>ReserveCore(w,source,target,id,parts,receiver,commander,targetSlot,true);
    internal LongPartOrder ReserveComputer(WorldSetup w,int source,int target,long id,RobotParts parts,long? receiver,int commander,int targetSlot=0)
        =>ReserveCore(w,source,target,id,parts,receiver,commander,targetSlot,false);
    private LongPartOrder ReserveCore(WorldSetup w,int source,int target,long id,RobotParts parts,long? receiver,int commander,int targetSlot,bool authority)
    {
        var world=w.Robots;
        bool Controls(int owner)=>owner>=0 && (authority?w.Relations.Controls(owner):owner==commander);
        if(w.Campaign.Phase!=CampaignPhase.Domestic || source==target || source<0 || target<0 || source>=world.Nodes.Length || target>=world.Nodes.Length ||
            !w.Nodes.All[source].Functional || !w.Nodes.All[target].Functional || w.Nodes.All[source].Owner<0 || w.Nodes.All[target].Owner<0 || !Controls(w.Nodes.All[source].Owner) || !Controls(w.Nodes.All[target].Owner))throw new InvalidOperationException("操作できる元地点と先地点を選んでください。");
        var robot=world.Nodes[source].Robots.SingleOrDefault(r=>r.Id==id);
        if(robot==null || !Controls(robot.Owner) || world.AssemblyLocked(source,robot) && !HoldsSource(source,id) || parts==0 || (parts&robot.Parts)!=parts)throw new InvalidOperationException("配送できるパーツを選んでください。");
        parts=robot.ConnectedParts(parts);
        if(Orders.Any(o=>o.Node==source && o.CargoId==id && (o.Parts&parts)!=0))throw new InvalidOperationException("その部位は長距離便で予約済みです。");
        if(Path(w,source,target,robot.Owner).Length<2)throw new InvalidOperationException("先地点へつながる経路がありません。");
        if(targetSlot<0 || targetSlot>=world.Nodes[target].TotalCapacity || !receiver.HasValue && Orders.Any(o=>o.Target==target && !o.Receiver.HasValue && o.TargetSlot==targetSlot && (o.Parts&parts)!=0))throw new InvalidOperationException("その受取部位は予約済みです。");
        if(!receiver.HasValue && Orders.Any(o=>o.Target==target && !o.Receiver.HasValue && o.TargetSlot==targetSlot && world.Nodes[o.Node].Robots.Any(r=>r.Id==o.CargoId && r.Owner!=robot.Owner)))throw new InvalidOperationException("異なる国のロボットを同じ格納枠へ組み立てることはできません。");
        if(receiver.HasValue){var other=world.Nodes[target].Robots.SingleOrDefault(r=>r.Id==receiver);
            if(other==null || other.Owner!=robot.Owner || (other.Parts&parts)!=0 || world.AssemblyLocked(target,other) || Orders.Any(o=>o.Target==target && o.Receiver==receiver && (o.Parts&parts)!=0))throw new InvalidOperationException("受取先のロボットにその部位を追加できません。");}
        if(!receiver.HasValue && !Orders.Any(o=>o.Target==target && !o.Receiver.HasValue && o.TargetSlot==targetSlot))_slotArrivals.Remove((target,targetSlot,robot.Owner));
        var order=new LongPartOrder(_next++,commander,source,target,id,parts,receiver){Node=source,CargoId=id,TargetSlot=targetSlot};
        Orders.Add(order);world.Nodes[source].Replace(world.Nodes[source].Robots.Select(r=>r.Id==id?r with {LongDeliveryNumber=r.LongDeliveryNumber??order.Number}:r));world.PartPlanning.Invalidate();return order;
    }
    public bool Cancel(RobotWorld world,long number,string reason="予約を取り消しました")
    {
        var order=Orders.SingleOrDefault(o=>o.Number==number);if(order==null)return false;
        Orders.Remove(order);
        foreach(var store in world.Nodes)store.Replace(store.Robots.Select(r=>r.LongDeliveryNumber.HasValue?r with {LongDeliveryNumber=Orders.Where(o=>o.CargoId==r.Id).Select(o=>(long?)o.Number).FirstOrDefault()}:r));
        order.Status=reason;Results.Add(order);if(Results.Count>64)Results.RemoveAt(0);world.PartPlanning.Invalidate();return true;
    }
    internal void Relocate(int source,int target,int offset=0)
    {
        foreach(var entry in _slotArrivals.Where(p=>p.Key.Node==source).ToArray()){_slotArrivals.Remove(entry.Key);_slotArrivals[(target,entry.Key.Slot+offset,entry.Key.Owner)]=entry.Value;}
        for(int i=0;i<Orders.Count;i++){
            var order=Orders[i];Orders[i]=order with {Source=order.Source==source?target:order.Source,Target=order.Target==source?target:order.Target,TargetSlot=order.Target==source?order.TargetSlot+offset:order.TargetSlot,Node=order.Node==source?target:order.Node};
        }
    }
    internal void Advance(WorldSetup w)
    {
        var world=w.Robots;var playback=new List<(RobotShipment Shipment,int Owner)>();
        foreach(var order in Orders.ToArray()){
            var store=world.Nodes[order.Node];var robot=store.Robots.SingleOrDefault(r=>r.Id==order.CargoId);
            if(robot==null || !robot.LongDeliveryNumber.HasValue){Cancel(world,order.Number,"荷物が見つからないため解除");continue;}
            var path=Path(w,order.Node,order.Target,robot.Owner);
            if(path.Length<2){Cancel(world,order.Number,path.Length==1?"到着地点が合流したため配送を終了":"先地点へつながらなくなったため解除 / 荷物は現在地点に残ります");continue;}
            int next=path[1];
            if(!Neighbors(w,order.Node,robot.Owner,true).Contains(next) || w.Campaign.RobotBattles.LockedNode(order.Node) || w.Campaign.RobotBattles.LockedNode(next) || w.Campaign.RobotBattles.LockedEdge(order.Node,next)){order.Status="経路上の戦闘が終わるまで待機";continue;}
            var destination=world.Nodes[next];Robot receiver=null;
            long? receiverId=order.Receiver;
            if(next==order.Target && order.TargetSlot>=0 && !receiverId.HasValue && _slotArrivals.TryGetValue((order.Target,order.TargetSlot,robot.Owner),out long arrived))receiverId=arrived;
            if(next==order.Target && receiverId.HasValue){
                receiver=destination.Robots.SingleOrDefault(r=>r.Id==receiverId);
                if(receiver==null || receiver.Owner!=robot.Owner || (receiver.Parts&order.Parts)!=0){Cancel(world,order.Number,"受取先が変わったため解除 / 荷物は現在地点に残ります");continue;}
                if(world.AssemblyLocked(next,receiver)){order.Status="受取先の予約が終わるまで待機";continue;}
            }
            if(receiver==null && !destination.HasSpace){order.Status="次の地点の格納庫が満杯のため待機";continue;}
            var cargo=(order.Started?robot:robot.Slice(order.Parts)) with {LongDeliveryNumber=order.Number};
            if(!order.Started && order.Parts!=robot.Parts){
                cargo=cargo with {Id=world.Create(robot.Owner,cargo.Parts).Id};
                long? remainingOrder=Orders.Where(o=>o.Number!=order.Number && o.CargoId==robot.Id && o.Node==order.Node).Select(o=>(long?)o.Number).FirstOrDefault();
                store.Replace(store.Robots.Select(r=>r.Id==robot.Id?r.Slice(r.Parts^order.Parts) with {LongDeliveryNumber=remainingOrder}:r));
            }else store.Remove(robot.Id);
            if(next==order.Target){
                cargo=cargo with {LongDeliveryNumber=null};
                if(receiver==null)destination.Add(cargo);else destination.Replace(destination.Robots.Select(r=>r.Id==receiver.Id?r.Merge(cargo,r.Owner):r));
                if(order.TargetSlot>=0 && !order.Receiver.HasValue)_slotArrivals[(order.Target,order.TargetSlot,cargo.Owner)]=receiver?.Id??cargo.Id;
                Orders.Remove(order);order.Status="配達完了";Results.Add(order);if(Results.Count>64)Results.RemoveAt(0);
            }else destination.Add(cargo);
            playback.Add((new(order.Node,next,cargo.Id,cargo.Parts),cargo.Owner));order.Node=next;order.CargoId=cargo.Id;order.Started=true;
            if(Orders.Contains(order))order.Status="中継地点で次の配送を待っています";
        }
        if(_slotArrivals.Count>1024)foreach(var key in _slotArrivals.Keys.Where(k=>!Orders.Any(o=>o.Target==k.Node && o.TargetSlot==k.Slot)).Take(_slotArrivals.Count-1024).ToArray())_slotArrivals.Remove(key);
        world.Transport.AppendPlannedTransfers(playback);world.PartPlanning.Invalidate();world.Check();
    }
}
public sealed partial class RobotWorld
{
    public RobotLongDelivery LongDelivery {get;}=new();
}
