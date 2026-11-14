namespace FolderVerse;
using System;
using System.Collections.Generic;
using System.Linq;

public sealed record DeliverySortieMember(long RobotId,long[] DeliveryNumbers)
{
    public bool Cancelled {get;init;}
    public long[] PartNumbers {get;init;}=Array.Empty<long>();
}
public sealed record CancelledWar(int Turn,int Ruler,int Target,string Reason);

public sealed partial class ConquestCampaign
{
    public List<CancelledWar> LastCancelledWars {get;}=new();

    public Robot[] DeliverySortieRobots(WorldSetup world,int ruler,int node)
    {
        if(node<0 || node>=world.Robots.Nodes.Length || RobotBattles.LockedNode(node))return Array.Empty<Robot>();
        return world.Robots.PartPlanning.Preview(world).Nodes[node].Robots.Where(r=>r.Owner==ruler &&
            world.Robots.Training[ruler].Eligible(r with {Disposal=false,DefenseOnly=false,InterceptOnly=false,FactoryWorker=false}) &&
            !world.Robots.Workshops[node].IsDisposing(r.Id) && !world.Robots.PendingEdits.Any(e=>e.Ids.Contains(r.Id))).ToArray();
    }

    public void PlanDeliverySortie(WorldSetup world,March order)
    {
        var ids=order.RobotIds??Array.Empty<long>();
        var available=DeliverySortieRobots(world,order.Ruler,order.SourceNode);
        if(ids.Distinct().Count()!=ids.Length || ids.Any(id=>!available.Any(r=>r.Id==id)))
            throw new InvalidOperationException("配送後の出撃メンバーを選択してください。");
        foreach(long id in ids)world.Robots.PartPlanning.EnsureForecastRobot(world,id);
        foreach(long id in ids)world.Robots.PartPlanning.EnsureArrival(world,order.SourceNode,id,order.Ruler);
        var members=ids.Select(id=>new DeliverySortieMember(id,world.Robots.DirectDelivery.ArrivalOrders(world.Robots,order.SourceNode,id)){PartNumbers=world.Robots.PartPlanning.Dependencies(id)}).ToArray();
        var present=members.Where(m=>m.DeliveryNumbers.Length==0 && m.PartNumbers.Length==0 && world.Robots.Nodes[order.SourceNode].Robots.Any(r=>r.Id==m.RobotId)).Select(m=>m.RobotId).ToArray();
        if(present.Length>0 && !world.Robots.PrepareSortieReservation(order.SourceNode,present))
            throw new InvalidOperationException("出撃メンバーを予約できません。");
        Plan(order with {Fighters=ids.Length,DeliveryMembers=members});
    }

    private int WarGroup(WorldSetup world,March order)
        =>world.ControllerOf(order.Ruler)>=0?world.ControllerOf(order.Ruler):world.ActiveCount+order.Ruler;

    private void CancelWarGroup(WorldSetup world,List<March> orders,March failed,string reason)
    {
        int group=WarGroup(world,failed);
        orders.RemoveAll(o=>!o.Peaceful && o.TargetNode==failed.TargetNode && WarGroup(world,o)==group);
        LastCancelledWars.Add(new(world.Population.Turn,failed.Ruler,failed.TargetNode,reason));
    }

    private void ResolveDeliverySorties(WorldSetup world)
    {
        var orders=PlannedOrders.ToList();
        foreach(var order in orders.ToArray()){
            if(!orders.Contains(order) || order.DeliveryMembers==null || order.PocketOnly)continue;
            var ids=new List<long>();string reason=null;
            foreach(var member in order.DeliveryMembers){
                if(member.Cancelled){reason="予定メンバーの出撃予約が解除されました";break;}
                if(member.PartNumbers.Any(n=>!world.Robots.PartPlanning.LastResults.Any(r=>r.Number==n && r.Status=="実施完了"))){reason="組替・入荷の予約が取り消された、または実施できませんでした";break;}
                long id=member.RobotId;
                if(RobotPartPlanning.PlannedId(id)){
                    if(!world.Robots.PartPlanning.CompletedRobots.TryGetValue(id,out id)){reason="組立予定のロボットが完成しませんでした";break;}
                }
                if(member.DeliveryNumbers.Length>0){
                    var arrivals=world.Robots.DirectDelivery.CompletedArrivals;
                    if(member.DeliveryNumbers.Any(n=>!arrivals.ContainsKey(n))){
                        long number=member.DeliveryNumbers.First(n=>!arrivals.ContainsKey(n));
                        var delivery=world.Robots.DirectDelivery;
                        string status=delivery.Orders.Concat(delivery.Failures).FirstOrDefault(o=>o.Number==number)?.Status??"予約取消・荷物の変更";
                        reason=$"直送便 #{number} が未到着（{status}）のため、予定メンバーが完成しませんでした";break;
                    }
                    id=arrivals[member.DeliveryNumbers[0]];
                    if(member.DeliveryNumbers.Any(n=>arrivals[n]!=id)){reason="直送便の受取先が変わり、予定のロボットを組み立てられませんでした";break;}
                }
                var robot=world.Robots.Nodes[order.SourceNode].Robots.FirstOrDefault(r=>r.Id==id && r.Owner==order.Ruler);
                if(robot==null || robot.Parts!=RobotParts.Complete){reason=$"ロボット ID {id} が完成体にならなかった、または出撃元にいませんでした";break;}
                var sortie=robot with {Disposal=false,DefenseOnly=false,InterceptOnly=false,FactoryWorker=false,DefenseFlag=null};
                if(!world.Robots.Training[order.Ruler].Eligible(sortie) || world.Robots.DirectDelivery.LockedParts(order.SourceNode,id)!=0 ||
                    world.Robots.Workshops[order.SourceNode].IsDisposing(id) || world.Robots.PendingEdits.Any(e=>e.Ids.Contains(id))){reason="予定メンバーが出撃条件を満たしませんでした";break;}
                ids.Add(id);
            }
            if(reason!=null){CancelWarGroup(world,orders,order,reason+" / 開戦予定全体を中止");continue;}
            // Change roles only after every member has passed validation.
            world.Robots.Nodes[order.SourceNode].Replace(world.Robots.Nodes[order.SourceNode].Robots.Select(r=>ids.Contains(r.Id)?r with {Disposal=false,DefenseOnly=false,InterceptOnly=false,FactoryWorker=false,DefenseFlag=null}:r).ToArray());
            world.Robots.SyncWorkers(order.SourceNode);
            orders[orders.IndexOf(order)]=order with {RobotIds=ids.ToArray(),Fighters=ids.Count,DeliveryMembers=ids.Select(id=>new DeliverySortieMember(id,Array.Empty<long>())).ToArray()};
        }
        _plannedOrders.Clear();_partyOrders.Clear();_plannedOrders.AddRange(orders);
    }

    private void CancelInvalidWars(WorldSetup world,List<March> orders)
    {
        foreach(var order in orders.ToArray()){
            if(!orders.Contains(order))continue;
            if(order.Peaceful){
                if(!world.Relations.CanAct(order.Ruler) || !RobotBattles.CanMarch(world,order.Ruler,order.TargetNode))
                    orders.Remove(order);
                continue;
            }
            int source=order.SourceNode>=0?order.SourceNode:world.Nodes.Current(order.Ruler)?.Id??-1;
            string reason=null;
            if(order.DeliveryMembers?.Any(m=>m.Cancelled)==true)reason="予定メンバーの出撃予約が解除されました";
            else if(!world.Relations.CanAct(order.Ruler) || !(order.Queen || order.PocketOnly?RobotBattles.CanSortie(world,order.Ruler,order.TargetNode):RobotBattles.CanAttackFrom(world,order.Ruler,source,order.TargetNode)))
                reason="出撃元・目標・経路が開戦条件を満たさなくなりました";
            else if(order.RobotIds!=null){
                var available=RobotBattles.SelectableRobots(world,order.Ruler,source,order.PocketOnly).Select(r=>r.Id).ToHashSet();
                if(order.RobotIds.Any(id=>!available.Contains(id)))reason="予定メンバーのうち１体以上が出撃できませんでした";
            }
            if(reason!=null)CancelWarGroup(world,orders,order,reason+" / 開戦予定全体を中止");
        }
    }
}
