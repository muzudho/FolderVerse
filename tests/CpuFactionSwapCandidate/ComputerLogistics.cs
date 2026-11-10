namespace FolderVerse;
using System;
using System.Collections.Generic;
using System.Linq;

// Uses ordinary reservations: no free disposal, teleportation, or additional capacity.
public static class ComputerLogistics
{
    public static void Plan(WorldSetup w,bool automaticAll=false)
    {
        if(w.Campaign.Phase!=CampaignPhase.Domestic)return;
        var manual=w.ManualRulers().ToHashSet();var stock=w.Robots;var battles=w.Campaign.RobotBattles;
        bool Busy(int node)=>battles.LockedNode(node) || w.Campaign.FunctionTransfers.Orders.Any(o=>o.Source==node || o.Target==node) ||
            stock.PartPlanning.Orders.Any(o=>o.Source==node || o.Target==node) || stock.PendingEdits.Any(o=>o.Node==node) ||
            stock.DirectDelivery.Orders.Any(o=>o.Source==node || o.Target==node) || w.Campaign.PlannedOrders.Any(o=>o.SourceNode==node || o.TargetNode==node);
        bool AnyViableAttack(int root){
            var automated=Enumerable.Range(0,w.ActiveCount).Where(o=>w.Relations.CanAct(o) && w.Relations.Root(o)==root && (automaticAll || !manual.Contains(o))).ToHashSet();
            if(w.Campaign.PlannedOrders.Any(o=>automated.Contains(o.Ruler) && !o.Peaceful && (w.Nodes.All[o.TargetNode].Owner<0 || !w.Relations.Allied(o.Ruler,w.Nodes.All[o.TargetNode].Owner))))return true;
            foreach(var family in automated.GroupBy(w.Relations.Root)){
                var members=family.ToHashSet();
                var sources=w.Nodes.All.Where(n=>n.Functional && members.Contains(n.Owner) && !Busy(n.Id)).Select(n=>{
                    int count=battles.SelectableRobots(w,n.Owner,n.Id,false).Length;
                    bool guard=stock.Nodes[n.Id].Robots.Any(r=>r.Owner==n.Owner && r.DefenseOnly && r.CanFight);
                    return new{Node=n.Id,Owner=n.Owner,Count=Math.Max(0,count-(!guard && count>1?1:0))};
                }).Where(s=>s.Count>0).ToArray();
                var queens=family.Where(o=>w.Nodes.Current(o) is {} n && !battles.LockedNode(n.Id)).ToArray();
                var targets=sources.SelectMany(s=>battles.MarchTargets(w,s.Node)).Concat(queens.SelectMany(o=>battles.MarchTargets(w,w.Nodes.Current(o)!.Id))).Distinct();
                foreach(int target in targets.Where(t=>!battles.LockedNode(t) && (w.Nodes.All[t].Owner<0 || !w.Relations.Allied(family.Key,w.Nodes.All[t].Owner)))){
                    int defense=battles.DefendingRobots(w,family.Key,target);
                    if(w.Nodes.All[target].Owner<0 && defense==0 && queens.Any(o=>battles.CanMarch(w,o,target)))return true;
                    int ground=sources.Where(s=>battles.CanAttackFrom(w,s.Owner,s.Node,target)).Sum(s=>s.Count);
                    int pockets=queens.Where(o=>battles.CanSortie(w,o,target)).Sum(o=>battles.SelectableRobots(w,o,w.Nodes.Current(o)!.Id,true).Length);
                    if(ground+pockets>=defense+Math.Max(1,(defense+2)/3))return true;
                }
            }
            return false;
        }
        var stalledFactions=Enumerable.Range(0,w.ActiveCount).Where(w.Relations.CanAct).Select(w.Relations.Root).Distinct().ToDictionary(root=>root,root=>!AnyViableAttack(root));
        foreach(int owner in Enumerable.Range(0,w.ActiveCount).Where(o=>w.Relations.CanAct(o) && (automaticAll || !manual.Contains(o)))){
            bool stalled=stalledFactions[w.Relations.Root(owner)];
            var nodes=w.Nodes.All.Where(n=>n.Functional && n.Owner==owner && !Busy(n.Id)).ToArray();
            var front=nodes.Where(n=>battles.MarchTargets(w,n.Id).Any(t=>w.Nodes.All[t].Owner>=0 && !w.Relations.Allied(owner,w.Nodes.All[t].Owner))).Select(n=>n.Id).ToHashSet();
            // Stage surplus troops closer to a front even when its hangars are full.
            // Reverse delivery edges keep this distance valid for the shipping direction.
            var incoming=new Dictionary<int,List<int>>();
            foreach(var at in w.Nodes.All.Where(n=>n.Functional && n.Owner>=0 && w.Relations.Allied(owner,n.Owner)))
                foreach(int next in RobotLongDelivery.Neighbors(w,at.Id,owner)){
                    if(!incoming.TryGetValue(next,out var list))incoming[next]=list=new();list.Add(at.Id);
                }
            var frontDistance=front.ToDictionary(n=>n,n=>0);var frontier=new Queue<int>(front);
            while(frontier.TryDequeue(out int at))if(incoming.TryGetValue(at,out var preceding))
                foreach(int next in preceding)if(frontDistance.TryAdd(next,frontDistance[at]+1))frontier.Enqueue(next);
            foreach(var target in nodes.Where(n=>stalled && front.Contains(n.Id) && !stock.Nodes[n.Id].HasSpace && battles.AvailableFrom(w,owner,n.Id)<2).OrderBy(n=>n.Id)){
                if(Busy(target.Id) || stock.Workshops[target.Id].SupervisorLeaveRemaining>0)continue;
                var partial=stock.Nodes[target.Id].Robots.Where(r=>r.Owner==owner && r.Parts!=RobotParts.Complete && !stock.AssemblyLocked(target.Id,r)).OrderBy(r=>r.NativeParts(owner)).ThenBy(r=>r.Id).FirstOrDefault();
                if(partial==null)continue;
                bool exchanged=false;
                foreach(var source in nodes.Where(n=>!front.Contains(n.Id) && !Busy(n.Id) && stock.Workshops[n.Id].SupervisorLeaveRemaining==0 && FunctionalDeliveryRoutes.Connected(w,n.Id,target.Id)).OrderBy(n=>n.Id)){
                    if(stock.Nodes[source.Id].Robots.Count(r=>r.Owner==owner && r.CanFight)<2)continue;
                    var ready=stock.Training[owner].Order(battles.SelectableRobots(w,owner,source.Id,false).Where(r=>!stock.AssemblyLocked(source.Id,r))).FirstOrDefault();
                    if(ready==null)continue;
                    try{stock.DirectDelivery.ReserveComputerExchange(w,source.Id,target.Id,ready.Id,partial.Id,owner);exchanged=true;break;}
                    catch(InvalidOperationException){continue;}
                }
                if(exchanged)break;
            }
            int shipped=0;
            foreach(var node in nodes.OrderByDescending(n=>stock.Nodes[n.Id].Count/(double)Math.Max(1,stock.Nodes[n.Id].StorageLimit)).ThenBy(n=>n.Id)){
                int source=node.Id;var store=stock.Nodes[source];var shop=stock.Workshops[source];
                if(Busy(source))continue;
                if(shop.SupervisorLeaveRemaining>0)continue;
                bool pressure=store.Count*4>=store.StorageLimit*3 || shop.ProductionLines.Any(l=>l.Waiting);
                foreach(var robot in store.Robots.ToArray()){
                    if(stock.AssemblyLocked(source,robot) || robot.Disposal || stock.LongDelivery.Orders.Any(o=>o.Receiver==robot.Id))continue;
                    if(pressure && stock.ReserveComputerReturnHome(w,source,robot,owner))continue;
                    if(pressure && (robot.Parts&~robot.EffectiveDamagedParts)==0 && shop.DisposalFacilities.Any(f=>f.HasDisposalFactory)){
                        stock.ToggleReservation(source,robot.Id,HangarReservation.Disposal,owner);continue;
                    }
                    if(shipped>=2 || robot.Owner!=owner || !stock.CanShip(source,robot))continue;
                    RobotParts damaged=robot.Parts&robot.EffectiveDamagedParts;
                    bool garbage=pressure && damaged!=0 && robot.CanSeparate(damaged);
                    bool rearSoldier=!garbage && robot.CanFight && !front.Contains(source) && store.Robots.Count(r=>r.Owner==owner && r.CanFight && !r.Disposal && !r.LongDeliveryNumber.HasValue)>1;
                    bool partial=robot.Parts!=RobotParts.Complete && !garbage;
                    if(!garbage && !rearSoldier && !partial)continue;
                    var mask=garbage?damaged:robot.Parts;
                    var previous=new Dictionary<int,int>{{source,-1}};var distance=new Dictionary<int,int>{{source,0}};var queue=new Queue<int>();queue.Enqueue(source);
                    while(queue.TryDequeue(out int at))foreach(int next in RobotLongDelivery.Neighbors(w,at,owner))if(previous.TryAdd(next,at)){distance[next]=distance[at]+1;queue.Enqueue(next);}
                    bool ClearRelay(int target){
                        // Reserve only a route whose transit hangars can accept cargo.
                        // A final receiver can merge parts even in a full destination.
                        for(int at=previous[target];at!=source;at=previous[at])if(!stock.Nodes[at].HasSpace)return false;
                        return true;
                    }
                    var candidates=new List<(int Node,long? Receiver,int Slot,int Score,int Distance)>();
                    foreach(var destination in nodes.Where(n=>n.Id!=source && previous.ContainsKey(n.Id) && ClearRelay(n.Id))){
                        int target=destination.Id;var other=stock.Nodes[target];
                        if(garbage && !stock.Workshops[target].DisposalFacilities.Any(f=>f.HasDisposalFactory))continue;
                        var receiver=partial?other.Robots.Where(r=>r.Owner==owner && r.Parts!=RobotParts.Complete && (r.Parts&mask)==0 && !stock.AssemblyLocked(target,r) && ((r.Parts|mask)!=RobotParts.Complete || r.Merge(robot.Slice(mask),owner).AssemblyCredit(owner)>=stock.Training[owner].RequiredNative))
                            .OrderByDescending(r=>(r.Parts|mask)==RobotParts.Complete).ThenBy(r=>r.Id).FirstOrDefault():null;
                        int slot=Enumerable.Range(other.Count,Math.Max(0,other.StorageLimit-other.Count)).FirstOrDefault(i=>!stock.LongDelivery.Orders.Any(o=>o.Target==target && !o.Receiver.HasValue && o.TargetSlot==i),-1);
                        if(receiver==null && slot<0 || partial && receiver==null || rearSoldier && frontDistance.GetValueOrDefault(target,int.MaxValue)>=frontDistance.GetValueOrDefault(source,int.MaxValue))continue;
                        if(receiver!=null && stock.LongDelivery.Orders.Any(o=>o.Target==target && o.Receiver==receiver.Id && (o.Parts&mask)!=0))continue;
                        candidates.Add((target,receiver?.Id,Math.Max(0,slot),receiver!=null?100:rearSoldier?50-Math.Min(40,frontDistance[target]):front.Contains(target)?50:0,distance[target]));
                    }
                    var best=candidates.OrderByDescending(c=>c.Score).ThenBy(c=>c.Distance).ThenBy(c=>c.Node).FirstOrDefault();
                    if(candidates.Count==0)continue;
                    try{stock.LongDelivery.ReserveComputer(w,source,best.Node,robot.Id,mask,best.Receiver,owner,best.Slot);shipped++;}
                    catch(InvalidOperationException){continue;}
                }
            }
        }
    }
}
