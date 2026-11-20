namespace FolderVerse;
using System;
using System.Linq;

public sealed partial class RobotWorld
{
    public long ConsolidateWorkpieceParts(int node,long sourceId,long targetId,RobotParts parts,int owner)
    {
        var store=Nodes[node];var source=store.Robots.Single(r=>r.Id==sourceId);var target=store.Robots.Single(r=>r.Id==targetId);
        parts=source.ConnectedParts(parts);
        if(sourceId==targetId || !source.IsWorkpiece || !target.IsWorkpiece || !CanEditReservedPart(node,source,owner) || !CanEditReservedPart(node,target,owner) || !source.CanSeparate(parts) || !target.CanSeparate(parts) || (target.Parts&parts)!=parts || source.IntegratedParts!=target.IntegratedParts)throw new InvalidOperationException("仕掛品の部位と結合を確認してください");
        foreach(var part in RobotMaterials.Parts.Where(p=>(parts&p)!=0)){
            if(source.Material(part)!=target.Material(part) || source.Origin(part)!=target.Origin(part))throw new InvalidOperationException("同じ素材・国籍の仕掛品を選んでください");
            var a=source.Work(part);var b=target.Work(part);int total=a?.Total??b?.Total??1;
            if(a!=null && b!=null && a.Total!=b.Total)throw new InvalidOperationException("必要工数が異なる仕掛品です");
            int cost=RobotWorld.RawMaterials.Sum(m=>RobotWorld.RawCost(source with {Parts=part,IntegratedParts=0},m));
            long done=(long)(a?.Done??total)+(b?.Done??total),paid=(long)(a?.RawPaid??cost)+(b?.RawPaid??cost);
            int intoDone=(int)Math.Min(total,done),intoPaid=(int)Math.Min(cost,paid),restDone=(int)(done-intoDone),restPaid=(int)(paid-intoPaid);
            target=target.WithWork(part,intoDone==total && intoPaid==cost?null:new(intoDone,total,intoPaid));
            source=source.WithWork(part,restDone==total && restPaid==cost?null:new(restDone,total,restPaid));
        }
        store.Replace(store.Robots.Select(r=>r.Id==sourceId?source:r.Id==targetId?target:r));return targetId;
    }
}
