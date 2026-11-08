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
            int shipped=0;
            foreach(var node in nodes.OrderByDescending(n=>stock.Nodes[n.Id].Count/(double)Math.Max(1,stock.Nodes[n.Id].StorageLimit)).ThenBy(n=>n.Id)){
                int source=node.Id;var store=stock.Nodes[source];var shop=stock.Workshops[source];
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
                    bool rearSoldier=!garbage && robot.CanFight && !front.Contains(source) && store.Robots.Count(r=>r.Owner==owner && r.CanFight && !r.Disposal)>1;
                    bool partial=robot.Parts!=RobotParts.Complete && !garbage;
                    if(!garbage && !rearSoldier && !partial)continue;
                    var mask=garbage?damaged:robot.Parts;
                    var previous=new Dictionary<int,int>{{source,-1}};var distance=new Dictionary<int,int>{{source,0}};var queue=new Queue<int>();queue.Enqueue(source);
                    while(queue.TryDequeue(out int at))foreach(int next in RobotLongDelivery.Neighbors(w,at,owner))if(previous.TryAdd(next,at)){distance[next]=distance[at]+1;queue.Enqueue(next);}
                    var candidates=new List<(int Node,long? Receiver,int Slot,int Score,int Distance)>();
                    foreach(var destination in nodes.Where(n=>n.Id!=source && previous.ContainsKey(n.Id))){
                        int target=destination.Id;var other=stock.Nodes[target];
                        if(garbage && !stock.Workshops[target].DisposalFacilities.Any(f=>f.HasDisposalFactory))continue;
                        var receiver=partial?other.Robots.Where(r=>r.Owner==owner && r.Parts!=RobotParts.Complete && (r.Parts&mask)==0 && !stock.AssemblyLocked(target,r) && ((r.Parts|mask)!=RobotParts.Complete || r.Merge(robot.Slice(mask),owner).AssemblyCredit(owner)>=stock.Training[owner].RequiredNative))
                            .OrderByDescending(r=>(r.Parts|mask)==RobotParts.Complete).ThenBy(r=>r.Id).FirstOrDefault():null;
                        int slot=Enumerable.Range(other.Count,Math.Max(0,other.StorageLimit-other.Count)).FirstOrDefault(i=>!stock.LongDelivery.Orders.Any(o=>o.Target==target && !o.Receiver.HasValue && o.TargetSlot==i),-1);
                        if(receiver==null && slot<0 || partial && receiver==null || rearSoldier && !front.Contains(target))continue;
                        if(receiver!=null && stock.LongDelivery.Orders.Any(o=>o.Target==target && o.Receiver==receiver.Id && (o.Parts&mask)!=0))continue;
                        candidates.Add((target,receiver?.Id,Math.Max(0,slot),receiver!=null?100:front.Contains(target)?50:0,distance[target]));
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
