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
            int Score(int node){
                if(w.Nodes.All[node].Owner!=owner)return 0;
                var robots=stock.Nodes[node].Robots;
                int persuasion=robots.Where(r=>r.PersuadableParts(owner)!=0 && !stock.AssemblyLocked(node,r)).Sum(r=>r.Parts==RobotParts.Complete?4:1);
                int needed=Math.Max(0,Math.Min(4,stock.Retinues[owner].StorageLimit)-stock.Retinues[owner].Robots.Count(stock.Training[owner].Eligible));
                int surplus=Math.Min(robots.Count(r=>r.Owner==owner && stock.Training[owner].Eligible(r) && !stock.AssemblyLocked(node,r)),
                    Math.Max(0,robots.Count(r=>r.Owner==owner && r.CanFight && !r.FactoryWorker)-1));
                return persuasion+4*Math.Min(needed,surplus);
            }
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
