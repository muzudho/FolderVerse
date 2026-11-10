namespace FolderVerse;
using System;
using System.Collections.Generic;
using System.Linq;
internal static class ComputerRecoveryMoves
{
    internal static void Plan(WorldSetup w,List<March> orders,bool automaticAll)
    {
        if(orders.Any(o=>!o.Peaceful))return;
        var manual=w.ManualRulers().ToHashSet();var stock=w.Robots;var battle=w.Campaign.RobotBattles;
        foreach(int owner in Enumerable.Range(0,w.ActiveCount).Where(o=>w.Relations.CanAct(o) && (automaticAll || !manual.Contains(o)))){
            if(orders.Any(o=>o.Ruler==owner && o.Queen))continue;
            var home=w.Nodes.Current(owner);if(home==null || battle.LockedNode(home.Id))continue;
            int Score(int node)=>w.Nodes.All[node].Owner!=owner?0:stock.Nodes[node].Robots
                .Where(r=>r.PersuadableParts(owner)!=0 && !stock.AssemblyLocked(node,r))
                .Sum(r=>r.Parts==RobotParts.Complete?4:1);
            if(Score(home.Id)>0)continue;
            var candidates=w.Nodes.All.Where(n=>n.Functional && n.Owner==owner && !battle.LockedNode(n.Id))
                .Select(n=>new{Node=n.Id,Score=Score(n.Id)}).Where(n=>n.Score>=2)
                .Select(n=>new{n.Node,n.Score,Path=RobotLongDelivery.Path(w,home.Id,n.Node,owner)})
                .Where(n=>n.Path.Length>1 && n.Path.All(p=>w.Nodes.All[p].Owner>=0 && w.Relations.Allied(owner,w.Nodes.All[p].Owner)) && battle.CanMarch(w,owner,n.Path[1]))
                .OrderByDescending(n=>n.Score).ThenBy(n=>n.Path.Length).ThenBy(n=>n.Node).FirstOrDefault();
            if(candidates!=null)orders.Add(new(owner,candidates.Path[1],0,home.Id,Queen:true,PocketOnly:true,Peaceful:true));
        }
    }
}
