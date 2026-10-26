namespace FolderVerse;
using System;
using System.Linq;

public sealed partial class RobotWorld
{
    // Move exactly one physical part. Validate the entire change before replacing either robot.
    public long MovePart(int node,long sourceId,RobotParts part,long? targetId,int? owner=null)
    {
        if(part==0 || (part&~RobotParts.Complete)!=0)throw new ArgumentException("Select one part.");
        var store=Nodes[node];var source=store.Robots.Single(r=>r.Id==sourceId);
        part=source.ConnectedParts(part);
        if(part is not (RobotParts.Head or RobotParts.Body or RobotParts.Legs) && source.IntegratedParts!=part)throw new ArgumentException("Select one physical part.");
        if((source.Parts&part)==0)throw new InvalidOperationException("Part is absent.");
        if(targetId==sourceId)return sourceId;
        bool Reserved(long id)=>PartLocked(node,store.Robots.Single(r=>r.Id==id),part);
        if(Reserved(sourceId) || targetId.HasValue && Reserved(targetId.Value))throw new InvalidOperationException("Robot is reserved.");
        var target=targetId.HasValue?store.Robots.Single(r=>r.Id==targetId.Value):null;
        if(target!=null && target.Disposal!=source.Disposal)throw new InvalidOperationException("Incompatible target.");
        if(target!=null && (!target.CanSeparate(target.Parts&part) || (target.Parts&part)!=0 && (target.Parts&part)!=part))throw new InvalidOperationException("Integrated target is incompatible.");
        if(target!=null && (target.Parts&part)!=0)
        {
            Robot Slice(Robot robot)=>robot.Slice(part);
            var exchangedSource=source.Merge(Slice(target),owner??source.Owner);
            var exchangedTarget=target.Merge(Slice(source),owner??target.Owner);
            store.Replace(store.Robots.Select(r=>r.Id==sourceId?exchangedSource:r.Id==target.Id?exchangedTarget:r));
            Check();return target.Id;
        }
        var remaining=source.Parts^part;
        if(remaining==0 && DirectDelivery.LockedParts(node,sourceId)!=0)throw new InvalidOperationException("Arrival anchor must remain in its hangar.");
        if(target==null && remaining!=0 && !store.HasSpace)throw new InvalidOperationException("Hangar is full.");
        var destination=target!=null?target.Merge(source.Slice(part),owner??target.Owner):
            remaining==0?source.Materialize():source.Slice(part) with {Id=Create(source.Owner,part,disposal:source.Disposal).Id};
        if(owner.HasValue && destination.AssemblyCredit(owner.Value)>0)destination=destination.Materialize() with {Owner=owner.Value};
        var next=store.Robots.Where(r=>r.Id!=sourceId && r.Id!=targetId).ToList();
        if(remaining!=0)next.Add(source.Slice(remaining));
        next.Add(destination);store.Replace(next);Check();return destination.Id;
    }
}
