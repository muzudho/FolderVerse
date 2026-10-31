namespace FolderVerse;
using System;
using System.Linq;

public sealed partial class RobotWorld
{
    // Move exactly one physical part. Validate the entire change before replacing either robot.
    public long MovePart(int node,long sourceId,RobotParts part,long? targetId,int? owner=null)
    {
        RevalidateWorkReservations();
        if(part==0 || (part&~RobotParts.Complete)!=0)throw new ArgumentException("Select one part.");
        var store=Nodes[node];var source=store.Robots.Single(r=>r.Id==sourceId);
        part=source.ConnectedParts(part);
        if(part is not (RobotParts.Head or RobotParts.Body or RobotParts.Legs) && source.IntegratedParts!=part)throw new ArgumentException("Select one physical part.");
        if((source.Parts&part)==0)throw new InvalidOperationException("Part is absent.");
        if(targetId==sourceId)return sourceId;
        var target=targetId.HasValue?store.Robots.Single(r=>r.Id==targetId.Value):null;
        int country=owner??source.Owner;
        if(!CanEditReservedPart(node,source,country) || target!=null && !CanEditReservedPart(node,target,country))throw new InvalidOperationException("Robot has a foreign work reservation or is busy.");
        if(target!=null && target.Disposal!=source.Disposal)throw new InvalidOperationException("Incompatible target.");
        if(target!=null && (!target.CanSeparate(target.Parts&part) || (target.Parts&part)!=0 && (target.Parts&part)!=part))throw new InvalidOperationException("Integrated target is incompatible.");
        if(target!=null && (target.Parts&part)!=0)
        {
            // Exchanging indistinguishable parts is a true no-op, including robot ownership and history.
            if(EquivalentPartState(source,target,part))return target.Id;
            Robot Slice(Robot robot)=>robot.Slice(part);
            var exchangedSource=source.Merge(Slice(target),owner??source.Owner);
            var exchangedTarget=target.Merge(Slice(source),owner??target.Owner);
            exchangedSource=AfterPartEdit(node,source,exchangedSource,true);exchangedTarget=AfterPartEdit(node,target,exchangedTarget,true);
            var swapped=store.Robots.Select(r=>r.Id==sourceId?exchangedSource:r.Id==target.Id?exchangedTarget:r).ToArray();
            CancelChangedDeliveries(node,new[]{source,target},swapped);store.Replace(swapped);SyncWorkers(node);
            Check();return target.Id;
        }
        var remaining=source.Parts^part;
        if(target==null && remaining!=0 && !store.HasSpace)throw new InvalidOperationException("Hangar is full.");
        var destination=target!=null?target.Merge(source.Slice(part),owner??target.Owner):
            remaining==0?source.Materialize():source.Slice(part) with {Id=Create(source.Owner,part,disposal:source.Disposal).Id};
        if(owner.HasValue && destination.AssemblyCredit(owner.Value)>0)destination=destination.Materialize() with {Owner=owner.Value};
        var next=store.Robots.Where(r=>r.Id!=sourceId && r.Id!=targetId).ToList();
        if(remaining!=0)next.Add(AfterPartEdit(node,source,source.Slice(remaining),false));else CancelRobotWorkOrders(source);
        if(target!=null)destination=AfterPartEdit(node,target,destination,false);
        next.Add(destination);CancelChangedDeliveries(node,target==null?new[]{source}:new[]{source,target},next);store.Replace(next);SyncWorkers(node);Check();return destination.Id;
    }
}
