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
        foreach(int owner in Enumerable.Range(0,w.ActiveCount).Where(o=>w.Relations.CanAct(o) && (automaticAll || !manual.Contains(o)))){
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
            int shipped=0;
            foreach(var node in nodes.OrderByDescending(n=>stock.Nodes[n.Id].Count/(double)Math.Max(1,stock.Nodes[n.Id].StorageLimit)).ThenBy(n=>n.Id)){
                int source=node.Id;var store=stock.Nodes[source];var shop=stock.Workshops[source];
                if(shop.SupervisorLeaveRemaining>0)continue;
                bool pressure=store.Count*4>=store.StorageLimit*3 || shop.ProductionLines.Any(l=>l.Waiting);
                if(pressure && !store.Robots.Any(r=>r.Disposal) && shop.DisposalFacilities.Any(f=>f.HasDisposalFactory) &&
                    shop.ProductionLines.Any(l=>l.Waiting && l.WaitingProductionTurns>=IndustryTime.Whole(20))){
                    var excess=store.Robots.Where(r=>r.Parts!=RobotParts.Complete && r.NativeParts(owner)>0 && r.EffectiveDamagedParts==0 &&
                        !stock.AssemblyLocked(source,r) && store.Robots.Count(p=>p.Parts==r.Parts && !p.Disposal)>=3 &&
                        !store.Robots.Any(p=>p.Id!=r.Id && (p.Parts&r.Parts)==0 && !stock.AssemblyLocked(source,p)))
                        .OrderBy(r=>r.NativeParts(owner)).ThenBy(r=>r.Id).FirstOrDefault();
                    if(excess!=null)stock.ToggleReservation(source,excess.Id,HangarReservation.Disposal,owner);
                }
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
