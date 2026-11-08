namespace FolderVerse;
using System;
using System.Collections.Generic;
using System.Linq;

public static class ComputerJointAttacks
{
    public static void Plan(WorldSetup w,List<March> orders,bool automaticAll=false)
    {
        var manual=w.ManualRulers().ToHashSet();var battle=w.Campaign.RobotBattles;var stock=w.Robots;
        var owners=Enumerable.Range(0,w.ActiveCount).Where(o=>w.Relations.CanAct(o) && (automaticAll || !manual.Contains(o))).ToHashSet();
        var used=orders.SelectMany(o=>o.RobotIds??Array.Empty<long>()).ToHashSet();
        var busy=orders.Where(o=>!o.PocketOnly).Select(o=>o.SourceNode>=0?o.SourceNode:w.Nodes.Current(o.Ruler)?.Id??-1).ToHashSet();
        foreach(int owner in owners)BattleLessons.ReserveHomeGuards(w,owner);
        foreach(var faction in owners.GroupBy(w.Relations.Root).OrderBy(g=>g.Key)){
            var family=faction.ToHashSet();
            var sources=w.Nodes.All.Where(n=>n.Functional && family.Contains(n.Owner) && !busy.Contains(n.Id) && !battle.LockedNode(n.Id))
                .ToDictionary(n=>n.Id,n=>{
                    var robots=battle.SelectableRobots(w,n.Owner,n.Id,false).Where(r=>!used.Contains(r.Id)).ToArray();
                    bool guard=stock.Nodes[n.Id].Robots.Any(r=>r.Owner==n.Owner && r.DefenseOnly && r.CanFight);
                    return stock.Training[n.Owner].Order(robots).Take(Math.Max(0,robots.Length-(!guard && robots.Length>1?1:0))).ToArray();
                });
            var targets=sources.Where(p=>p.Value.Length>0).SelectMany(p=>battle.MarchTargets(w,p.Key)).Distinct()
                .Where(t=>!battle.LockedNode(t) && (w.Nodes.All[t].Owner<0 || w.Relations.Root(w.Nodes.All[t].Owner)!=faction.Key))
                .Select(t=>{
                    var attackers=sources.Where(p=>p.Value.Length>0 && battle.CanAttackFrom(w,w.Nodes.All[p.Key].Owner,p.Key,t)).Select(p=>p.Key).ToArray();
                    int defense=battle.DefendingRobots(w,faction.Key,t),required=defense+Math.Max(1,(defense+2)/3);
                    int captives=Enumerable.Range(0,w.ActiveCount).Count(o=>w.Relations.Powered[o] && w.Relations.Root(o)!=faction.Key && w.Nodes.Current(o)?.Id==t);
                    return new{Target=t,Sources=attackers,Required=required,Score=captives*1000-defense*10+(w.Nodes.All[t].Owner<0?0:50)};
                }).Where(t=>t.Sources.Sum(s=>sources[s].Length)>=t.Required).OrderByDescending(t=>t.Score).ThenBy(t=>t.Target).ToArray();
            foreach(var target in targets){
                var available=target.Sources.Where(s=>!busy.Contains(s)).OrderByDescending(s=>sources[s].Length).ThenBy(s=>s).ToArray();
                if(available.Sum(s=>sources[s].Length)<target.Required)continue;
                int needed=target.Required;
                foreach(int source in available){
                    var robots=sources[source].Take(needed).ToArray();if(robots.Length==0)continue;
                    int owner=w.Nodes.All[source].Owner;
                    orders.Add(new(owner,target.Target,robots.Length,source,Queen:false,RobotIds:robots.Select(r=>r.Id).ToArray()));
                    busy.Add(source);foreach(var r in robots)used.Add(r.Id);needed-=robots.Length;if(needed<=0)break;
                }
            }
            // Move queens through friendly territory toward a front, rather than
            // making endless peaceful visits to an enemy that cannot be conquered.
            var distance=new Dictionary<int,int>();var queue=new Queue<int>();
            foreach(var node in w.Nodes.All.Where(n=>n.Functional && n.Owner>=0 && w.Relations.Root(n.Owner)==faction.Key))
                if(battle.MarchTargets(w,node.Id).Any(t=>w.Nodes.All[t].Owner<0 || w.Relations.Root(w.Nodes.All[t].Owner)!=faction.Key)){distance[node.Id]=0;queue.Enqueue(node.Id);}
            while(queue.TryDequeue(out int at))foreach(int next in RobotLongDelivery.Neighbors(w,at,faction.Key))if(distance.TryAdd(next,distance[at]+1))queue.Enqueue(next);
            foreach(int owner in faction.OrderBy(o=>o)){
                if(orders.Any(o=>o.Ruler==owner && o.Queen))continue;var home=w.Nodes.Current(owner);if(home==null || battle.LockedNode(home.Id))continue;
                var choices=battle.MarchTargets(w,home.Id).Where(t=>battle.CanMarch(w,owner,t)).ToArray();
                var retinue=battle.SelectableRobots(w,owner,home.Id,true).Where(r=>!used.Contains(r.Id)).ToArray();
                int hostile=choices.Where(t=>w.Nodes.All[t].Owner>=0 && w.Relations.Root(w.Nodes.All[t].Owner)!=faction.Key && battle.DefendingRobots(w,owner,t)<retinue.Length)
                    .OrderBy(t=>battle.DefendingRobots(w,owner,t)).ThenBy(t=>t).FirstOrDefault(-1);
                if(hostile>=0){orders.Add(new(owner,hostile,retinue.Length,home.Id,Queen:true,PocketOnly:true,RobotIds:retinue.Select(r=>r.Id).ToArray()));continue;}
                int neutral=choices.Where(t=>w.Nodes.All[t].Owner<0).OrderBy(t=>t).FirstOrDefault(-1);
                if(neutral>=0){orders.Add(new(owner,neutral,0));continue;}
                int move=choices.Where(t=>w.Nodes.All[t].Owner>=0 && w.Relations.Root(w.Nodes.All[t].Owner)==faction.Key && distance.GetValueOrDefault(t,int.MaxValue)<distance.GetValueOrDefault(home.Id,int.MaxValue))
                    .OrderBy(t=>distance[t]).ThenBy(t=>t).FirstOrDefault(-1);
                if(move>=0)orders.Add(new(owner,move,0,home.Id,Queen:true,PocketOnly:true,Peaceful:true));
            }
        }
    }
}
