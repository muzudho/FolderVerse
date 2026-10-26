namespace FolderVerse;
using System;
using System.Collections.Generic;
using System.Linq;

public sealed record DirectPartOrder(long Number,int Source,int Target,long RobotId,RobotParts Part,long? TargetRobotId,int TargetSlot,int Owner)
{
    public string Status {get;set;}="予約";
}
public sealed class RobotDirectDelivery
{
    private long _next=1;
    public List<DirectPartOrder> Orders {get;}=new();
    public RobotParts LockedParts(int node,long id)=>Orders.Where(o=>o.Source==node && o.RobotId==id || o.Target==node && o.TargetRobotId==id).Aggregate((RobotParts)0,(mask,o)=>mask|o.Part);
    public bool Cancel(long number)=>Orders.RemoveAll(o=>o.Number==number)>0;
    public void ReserveParts(WorldSetup setup,int source,int target,long id,RobotParts parts,long? targetId,int slot)
    {
        if(parts==0 || (parts&~RobotParts.Complete)!=0)throw new InvalidOperationException("Select physical parts.");
        if(source<0 || source>=setup.Robots.Nodes.Length)throw new InvalidOperationException("Invalid direct delivery source.");
        long start=_next;
        try{
            var robot=setup.Robots.Nodes[source].Robots.Single(r=>r.Id==id);parts=robot.ConnectedParts(parts);
            foreach(var part in new[]{RobotParts.Head,RobotParts.Body,RobotParts.Legs})if((parts&part)!=0){var order=Reserve(setup,source,target,id,part,targetId,slot);parts&=~order.Part;}
        }
        catch{Orders.RemoveAll(o=>o.Number>=start);_next=start;throw;}
    }
    public DirectPartOrder Reserve(WorldSetup setup,int source,int target,long id,RobotParts part,long? targetId,int slot)
    {
        var world=setup.Robots;var battles=setup.Campaign.RobotBattles;
        if(part==0 || (part&~RobotParts.Complete)!=0 || slot<0 || slot>=12 || source<0 || source>=world.Nodes.Length || target<0 || target>=world.Nodes.Length || source==target)throw new InvalidOperationException("Invalid direct delivery.");
        if(battles.LockedNode(source) || battles.LockedNode(target) || battles.LockedEdge(source,target) || !setup.Routes.Neighbors(setup.Nodes.All[source]).Any(r=>setup.Nodes.At(r.Target,r.Entry).Id==target))throw new InvalidOperationException("Route unavailable.");
        var robot=world.Nodes[source].Robots.SingleOrDefault(r=>r.Id==id);
        if(robot!=null){part=robot.ConnectedParts(part);if(part is not (RobotParts.Head or RobotParts.Body or RobotParts.Legs) && robot.IntegratedParts!=part)throw new InvalidOperationException("Select one physical unit.");}
        if(robot==null || !setup.Relations.Controls(robot.Owner) || !setup.Relations.Controls(setup.Nodes.All[source].Owner) || !setup.Relations.Controls(setup.Nodes.All[target].Owner) || (robot.Parts&part)==0 || world.BaseAssemblyLocked(source,robot) || (LockedParts(source,id)&part)!=0)throw new InvalidOperationException("Part unavailable.");
        var outgoing=Orders.Where(o=>o.Source==source && o.RobotId==id).Aggregate(part,(mask,o)=>mask|o.Part);
        if((outgoing&robot.Parts)==robot.Parts && Orders.Any(o=>o.Target==source && o.TargetRobotId==id))throw new InvalidOperationException("Arrival anchor must remain.");
        if(targetId is long receiver){var destination=world.Nodes[target].Robots.SingleOrDefault(r=>r.Id==receiver);if(destination==null || destination.Owner!=robot.Owner || (destination.Parts&part)!=0 || world.BaseAssemblyLocked(target,destination) || (LockedParts(target,receiver)&part)!=0 || Orders.Where(o=>o.Source==target && o.RobotId==receiver).Aggregate((RobotParts)0,(mask,o)=>mask|o.Part)==destination.Parts)throw new InvalidOperationException("Destination part unavailable.");}
        else if(Orders.Any(o=>o.Target==target && o.TargetRobotId==null && o.TargetSlot==slot && ((o.Part&part)!=0 || o.Owner!=robot.Owner)))throw new InvalidOperationException("Arrival slot already reserved.");
        var order=new DirectPartOrder(_next++,source,target,id,part,targetId,slot,robot.Owner);Orders.Add(order);return order;
    }
    internal HashSet<long> Resolve(RobotWorld world,Func<int,int,bool> connected,Func<int,bool> locked,Func<int,int,bool> canEnter)
    {
        var active=new List<DirectPartOrder>();
        foreach(var order in Orders.ToArray()){
            var source=world.Nodes[order.Source].Robots.SingleOrDefault(r=>r.Id==order.RobotId);
            var destination=order.TargetRobotId is long id?world.Nodes[order.Target].Robots.SingleOrDefault(r=>r.Id==id):null;
            if(source==null || source.Owner!=order.Owner || !source.CanSeparate(order.Part) || order.TargetRobotId.HasValue && (destination==null || destination.Owner!=order.Owner || (destination.Parts&order.Part)!=0)){Orders.Remove(order);continue;}
            if(locked?.Invoke(order.Source)==true || locked?.Invoke(order.Target)==true || !connected(order.Source,order.Target) || canEnter?.Invoke(order.Owner,order.Source)==false || canEnter?.Invoke(order.Owner,order.Target)==false || world.BaseAssemblyLocked(order.Source,source) || destination!=null && world.BaseAssemblyLocked(order.Target,destination)){order.Status="継戦・経路待機";continue;}
            order.Status="受取待機";active.Add(order);
        }
        List<Robot>[] Simulate(IReadOnlyList<DirectPartOrder> selected,out Dictionary<(int,int),long> groups,out int invalid)
        {
            var stores=world.Nodes.Select(s=>s.Robots.ToList()).ToArray();groups=new();invalid=-1;
            var cargo=new Dictionary<long,Robot>();
            foreach(var order in selected){var store=stores[order.Source];var robot=store.Single(r=>r.Id==order.RobotId);store.Remove(robot);var remaining=robot.Parts&~order.Part;if(remaining!=0)store.Add(robot.Slice(remaining));cargo[order.Number]=robot.Slice(order.Part) with {Id=remaining==0?robot.Id:-order.Number};}
            foreach(var order in selected){var store=stores[order.Target];var robot=cargo[order.Number];long? targetId=order.TargetRobotId;if(!targetId.HasValue && groups.TryGetValue((order.Target,order.TargetSlot),out long group))targetId=group;
                var target=targetId.HasValue?store.SingleOrDefault(r=>r.Id==targetId):null;
                if(targetId.HasValue && (target==null || (target.Parts&order.Part)!=0)){invalid=selected.ToList().IndexOf(order);return stores;}
                if(target==null){store.Add(robot);groups[(order.Target,order.TargetSlot)]=robot.Id;}
                else{store.Remove(target);store.Add(target.Merge(robot,target.Owner) with {Role=target.Role==RobotRole.Captain || robot.Role==RobotRole.Captain?RobotRole.Captain:RobotRole.Soldier});}
            }
            return stores;
        }
        while(active.Count>0){
            var stores=Simulate(active,out var groups,out int invalid);
            if(invalid>=0){active[invalid].Status="受取部位待機";active.RemoveAt(invalid);continue;}
            int overflow=Enumerable.Range(0,stores.Length).FirstOrDefault(n=>stores[n].Count>world.Nodes[n].StorageLimit,-1);
            if(overflow>=0){var rejected=active.LastOrDefault(o=>o.Target==overflow);if(rejected==null)throw new InvalidOperationException("Direct delivery capacity invariant.");rejected.Status="空き枠待機";active.Remove(rejected);continue;}
            var transfers=active.Select(o=>new RobotShipment(o.Source,o.Target,o.RobotId,o.Part)).ToArray();world.Transport.RecordDirectTransfers(world,transfers);
            var allocated=new Dictionary<long,long>();foreach(var robot in stores.SelectMany(s=>s).Where(r=>r.Id<0))allocated[robot.Id]=world.Create(robot.Owner,robot.Parts).Id;
            for(int n=0;n<stores.Length;n++)world.Nodes[n].Replace(stores[n].Select(r=>r.Id<0?r with {Id=allocated[r.Id]}:r).ToArray());
            foreach(var order in active)Orders.Remove(order);
            for(int i=0;i<Orders.Count;i++){var order=Orders[i];if(order.TargetRobotId==null && groups.TryGetValue((order.Target,order.TargetSlot),out long group))Orders[i]=order with {TargetRobotId=group<0?allocated[group]:group};}
            var arrivals=new HashSet<long>();foreach(var order in active){long id=order.TargetRobotId??groups[(order.Target,order.TargetSlot)];arrivals.Add(id<0?allocated[id]:id);}world.Check();return arrivals;
        }
        return new HashSet<long>();
    }
}
