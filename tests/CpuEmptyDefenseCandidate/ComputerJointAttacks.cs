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
        Robot[] Prefer(int owner,IEnumerable<Robot> robots)=>stock.Training[owner].DeploymentPolicy==3?robots.OrderByDescending(r=>2*r.CombatParts(owner)+r.CombatRifles(owner)).ThenBy(r=>r.Id).ToArray():stock.Training[owner].Order(robots).ToArray();
        // A nominally empty target can still field overflow reserve defenders.
        // Restrict this experiment to the false-empty case; mixed garrisons are unchanged.
        int Defense(int attacker,int target){
            int count=battle.DefendingRobots(w,attacker,target);
            return count!=0?count:battle.Reserves.Count(r=>r.Node==target && r.Robot.CanFight && !w.Relations.Allied(attacker,r.Robot.Owner));
        }
        var used=orders.SelectMany(o=>o.RobotIds??Array.Empty<long>()).ToHashSet();
        var busy=orders.Where(o=>!o.PocketOnly).Select(o=>o.SourceNode>=0?o.SourceNode:w.Nodes.Current(o.Ruler)?.Id??-1).ToHashSet();
        foreach(int owner in owners)BattleLessons.ReserveHomeGuards(w,owner);
        foreach(var faction in owners.GroupBy(w.Relations.Root).OrderBy(g=>g.Key)){
            var family=faction.ToHashSet();
            var sources=w.Nodes.All.Where(n=>n.Functional && family.Contains(n.Owner) && !busy.Contains(n.Id) && !battle.LockedNode(n.Id))
                .ToDictionary(n=>n.Id,n=>{
                    var robots=battle.SelectableRobots(w,n.Owner,n.Id,false).Where(r=>!used.Contains(r.Id)).ToArray();
                    bool guard=stock.Nodes[n.Id].Robots.Any(r=>r.Owner==n.Owner && r.DefenseOnly && r.CanFight);
                    return Prefer(n.Owner,robots).Take(Math.Max(0,robots.Length-(!guard && robots.Length>1?1:0))).ToArray();
                });
            int FailedForce(int target)=>battle.Encounters.Where(e=>e.Finished && e.Target==target)
                .SelectMany(e=>e.Battles).Where(b=>b.Finished && (b.Winner<0 || !w.Relations.Allied(b.Winner,faction.Key)) &&
                    b.Armies.Any(a=>family.Contains(a.Owner) && a.HomeNode!=target && a.Withdrawing && a.NoWillCount>=b.Rules.NoWillTurns))
                .Select(b=>b.Units.Count(u=>family.Contains(u.Owner) && u.Robot!=null)+b.Armies.Where(a=>family.Contains(a.Owner)).Sum(a=>a.Waiting.Count(u=>u.Robot!=null))).DefaultIfEmpty(0).Max();
            var targets=sources.Where(p=>p.Value.Length>0).SelectMany(p=>battle.MarchTargets(w,p.Key)).Distinct()
                .Where(t=>!battle.LockedNode(t) && (w.Nodes.All[t].Owner<0 || w.Relations.Root(w.Nodes.All[t].Owner)!=faction.Key))
                .Select(t=>{
                    var attackers=sources.Where(p=>p.Value.Length>0 && battle.CanAttackFrom(w,w.Nodes.All[p.Key].Owner,p.Key,t)).Select(p=>p.Key).ToArray();
                    int defense=Defense(faction.Key,t),required=defense+Math.Max(1,(defense+2)/3);
                    required=Math.Max(required,FailedForce(t)+1);
                    int captives=Enumerable.Range(0,w.ActiveCount).Count(o=>w.Relations.Powered[o] && w.Relations.Root(o)!=faction.Key && w.Nodes.Current(o)?.Id==t);
                    return new{Target=t,Sources=attackers,Required=required,Score=captives*1000-defense*10+(w.Nodes.All[t].Owner<0?0:50)};
                }).Where(t=>t.Sources.Sum(s=>sources[s].Length)>=t.Required).OrderByDescending(t=>t.Score).ThenBy(t=>t.Target).ToArray();
            foreach(var target in targets){
                var available=target.Sources.Where(s=>!busy.Contains(s)).OrderByDescending(s=>sources[s].Length).ThenBy(s=>s).ToArray();
                if(available.Sum(s=>sources[s].Length)<target.Required)continue;
                // Retry a failed assault only with a larger force; fresh targets use the minimum.
                int needed=target.Required;
                foreach(int source in available){
                    var robots=sources[source].Take(needed).ToArray();if(robots.Length==0)continue;
                    int owner=w.Nodes.All[source].Owner;
                    orders.Add(new(owner,target.Target,robots.Length,source,Queen:false,RobotIds:robots.Select(r=>r.Id).ToArray()));
                    busy.Add(source);foreach(var r in robots)used.Add(r.Id);needed-=robots.Length;if(needed<=0)break;
                }
            }
            // Nearby rulers of one faction may combine their pocket escorts.
            // Individually weak retinues must not hide a jointly viable assault.
            var escorts=faction.Where(o=>!orders.Any(m=>m.Ruler==o && m.Queen) && w.Nodes.Current(o) is {} home && !battle.LockedNode(home.Id))
                .ToDictionary(o=>o,o=>Prefer(o,battle.SelectableRobots(w,o,w.Nodes.Current(o)!.Id,true).Where(r=>!used.Contains(r.Id))));
            var escortTargets=escorts.Where(p=>p.Value.Length>0).SelectMany(p=>battle.MarchTargets(w,w.Nodes.Current(p.Key)!.Id)).Distinct()
                .Where(t=>w.Nodes.All[t].Owner>=0 && !w.Relations.Allied(faction.Key,w.Nodes.All[t].Owner) && !battle.LockedNode(t) && !orders.Any(o=>o.TargetNode==t && family.Contains(o.Ruler)))
                .Select(t=>new{Target=t,Members=escorts.Where(p=>p.Value.Length>0 && battle.CanSortie(w,p.Key,t)).Select(p=>p.Key).ToArray(),Defense=Defense(faction.Key,t)})
                .OrderBy(t=>t.Defense).ThenBy(t=>t.Target).ToArray();
            foreach(var target in escortTargets){
                var members=target.Members.Where(o=>!orders.Any(m=>m.Ruler==o && m.Queen)).OrderByDescending(o=>escorts[o].Length).ThenBy(o=>o).ToArray();
                int needed=Math.Max(target.Defense+Math.Max(1,(target.Defense+2)/3),FailedForce(target.Target)+1);
                if(members.Length<2 || members.Sum(o=>escorts[o].Length)<needed)continue;
                foreach(int owner in members){
                    var robots=escorts[owner].Take(needed).ToArray();if(robots.Length==0)continue;
                    orders.Add(new(owner,target.Target,robots.Length,w.Nodes.Current(owner)!.Id,Queen:true,PocketOnly:true,RobotIds:robots.Select(r=>r.Id).ToArray()));
                    foreach(var robot in robots)used.Add(robot.Id);needed-=robots.Length;if(needed<=0)break;
                }
            }
            // Combine hangar troops and pocket escorts when neither force is sufficient alone.
            var mixedTargets=sources.Where(p=>p.Value.Length>0 && !busy.Contains(p.Key)).SelectMany(p=>battle.MarchTargets(w,p.Key)).Distinct()
                .Where(t=>w.Nodes.All[t].Owner>=0 && !w.Relations.Allied(faction.Key,w.Nodes.All[t].Owner) && !battle.LockedNode(t) && !orders.Any(o=>o.TargetNode==t && family.Contains(o.Ruler)))
                .OrderByDescending(t=>Enumerable.Range(0,w.ActiveCount).Count(o=>w.Relations.Powered[o] && !w.Relations.Allied(o,faction.Key) && w.Nodes.Current(o)?.Id==t)).ThenBy(t=>t).ToArray();
            foreach(int target in mixedTargets){
                var ground=sources.Where(p=>!busy.Contains(p.Key) && p.Value.Length>0 && battle.CanAttackFrom(w,w.Nodes.All[p.Key].Owner,p.Key,target)).OrderByDescending(p=>p.Value.Length).ThenBy(p=>p.Key).ToArray();
                var members=escorts.Where(p=>p.Value.Length>0 && !orders.Any(o=>o.Ruler==p.Key && o.Queen) && battle.CanSortie(w,p.Key,target)).OrderByDescending(p=>p.Value.Length).ThenBy(p=>p.Key).ToArray();
                int defense=Defense(faction.Key,target),needed=Math.Max(defense+Math.Max(1,(defense+2)/3),FailedForce(target)+1);
                int g=ground.Sum(p=>p.Value.Length),pocket=members.Sum(p=>p.Value.Length);
                if(g==0 || pocket==0 || g>=needed || pocket>=needed || g+pocket<needed)continue;
                foreach(var source in ground){
                    var robots=source.Value.Take(needed).ToArray();int owner=w.Nodes.All[source.Key].Owner;
                    orders.Add(new(owner,target,robots.Length,source.Key,Queen:false,RobotIds:robots.Select(r=>r.Id).ToArray()));
                    busy.Add(source.Key);foreach(var robot in robots)used.Add(robot.Id);needed-=robots.Length;
                }
                foreach(var member in members){
                    var robots=member.Value.Take(needed).ToArray();if(robots.Length==0)continue;
                    orders.Add(new(member.Key,target,robots.Length,w.Nodes.Current(member.Key)!.Id,Queen:true,PocketOnly:true,RobotIds:robots.Select(r=>r.Id).ToArray()));
                    foreach(var robot in robots)used.Add(robot.Id);needed-=robots.Length;if(needed<=0)break;
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
                var retinue=Prefer(owner,battle.SelectableRobots(w,owner,home.Id,true).Where(r=>!used.Contains(r.Id)));
                int hostile=choices.Where(t=>w.Nodes.All[t].Owner>=0 && w.Relations.Root(w.Nodes.All[t].Owner)!=faction.Key && Defense(owner,t)<retinue.Length && retinue.Length>FailedForce(t))
                    .OrderBy(t=>Defense(owner,t)).ThenBy(t=>t).FirstOrDefault(-1);
                if(hostile>=0){orders.Add(new(owner,hostile,retinue.Length,home.Id,Queen:true,PocketOnly:true,RobotIds:retinue.Select(r=>r.Id).ToArray()));continue;}
                int neutral=choices.Where(t=>w.Nodes.All[t].Owner<0).OrderBy(t=>t).FirstOrDefault(-1);
                if(neutral>=0){orders.Add(new(owner,neutral,0));continue;}
                int move=choices.Where(t=>w.Nodes.All[t].Owner>=0 && w.Relations.Root(w.Nodes.All[t].Owner)==faction.Key && distance.GetValueOrDefault(t,int.MaxValue)<distance.GetValueOrDefault(home.Id,int.MaxValue))
                    .OrderBy(t=>distance[t]).ThenBy(t=>t).FirstOrDefault(-1);
                if(move>=0)orders.Add(new(owner,move,0,home.Id,Queen:true,PocketOnly:true,Peaceful:true));
            }
        }
        ComputerRecoveryMoves.Plan(w,orders,automaticAll);
    }
}
