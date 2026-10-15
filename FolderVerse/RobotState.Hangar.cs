namespace FolderVerse;
using System;
using System.Linq;

public sealed partial class RobotWorld
{
    // Move exactly one physical part. Validate the entire change before replacing either robot.
    public long MovePart(int node,long sourceId,RobotParts part,long? targetId,int? owner=null)
    {
        if(part is not (RobotParts.Head or RobotParts.Body or RobotParts.Legs))throw new ArgumentException("Select one part.");
        var store=Nodes[node];var source=store.Robots.Single(r=>r.Id==sourceId);
        if((source.Parts&part)==0)throw new InvalidOperationException("Part is absent.");
        if(targetId==sourceId)return sourceId;
        bool Reserved(long id)=>Workshops[node].DisposalTarget==id || PendingEdits.Any(e=>e.Ids.Contains(id));
        if(Reserved(sourceId) || targetId.HasValue && Reserved(targetId.Value))throw new InvalidOperationException("Robot is reserved.");
        var target=targetId.HasValue?store.Robots.Single(r=>r.Id==targetId.Value):null;
        if(target!=null && (target.Disposal!=source.Disposal || (target.Parts&part)!=0))throw new InvalidOperationException("Incompatible target.");
        var remaining=source.Parts^part;
        if(target==null && remaining!=0 && !store.HasSpace)throw new InvalidOperationException("Hangar is full.");
        var destination=target!=null?target.Merge(source with {Parts=part,DamagedParts=source.DamagedParts&part},owner??target.Owner):
            remaining==0?source.Materialize():source.Materialize() with {Id=Create(source.Owner,part,disposal:source.Disposal).Id,Parts=part,DamagedParts=source.DamagedParts&part};
        if(owner.HasValue && destination.NativeParts(owner.Value)>0)destination=destination.Materialize() with {Owner=owner.Value};
        var next=store.Robots.Where(r=>r.Id!=sourceId && r.Id!=targetId).ToList();
        if(remaining!=0)next.Add(source with {Parts=remaining,DamagedParts=source.DamagedParts&remaining});
        next.Add(destination);store.Replace(next);Check();return destination.Id;
    }
}
