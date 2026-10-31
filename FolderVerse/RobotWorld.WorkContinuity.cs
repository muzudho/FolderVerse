namespace FolderVerse;
using System;
using System.Linq;
using System.Collections.Generic;

public sealed partial class RobotWorld
{
    public int WorkCountry(int node,Robot robot)
    {
        if(robot.Parts!=RobotParts.Complete)return -1;
        if(robot.DefenseOnly)return robot.DefenseFlag?.Commander??robot.Owner;
        if(robot.InterceptOnly || robot.FactoryWorker)return robot.Owner;
        var order=Reservations?.PlannedOrders.FirstOrDefault(o=>!o.Peaceful && o.RobotIds?.Contains(robot.Id)==true);
        return order?.Ruler??(MarchReserved(node,robot)?robot.Owner:-1);
    }
    public bool WorkQualified(Robot robot,int country)
        =>country>=0 && country<Training.Length && robot.Parts==RobotParts.Complete && robot.NativeParts(country)>0 && Training[country].Eligible(WithoutRole(robot).Materialize() with {Owner=country});
    private static Robot Barefoot(Robot robot)=>robot with {DefenseOnly=false,DefenseFlag=null,InterceptOnly=false,FactoryWorker=false};
    public Robot ValidateWorkReservation(Robot robot)
    {
        if(!robot.DefenseOnly && robot.DefenseFlag!=null)robot=robot with {DefenseFlag=null};
        if(!robot.Stationed && robot.DefenseFlag==null)return robot;
        int country=robot.DefenseFlag?.Commander??robot.Owner;
        return WorkQualified(robot,country)?robot:Barefoot(robot);
    }
    private RobotStore OrderStore(March order)
    {
        if(order.Ruler<0 || order.Ruler>=Retinues.Length)return null;
        if(order.PocketOnly || order.Queen)return Retinues[order.Ruler];
        int node=order.SourceNode>=0?order.SourceNode:ReservationSource?.Invoke(order.Ruler)??-1;
        return node>=0 && node<Nodes.Length?Nodes[node]:null;
    }
    private long[] LegacyOrderIds(March order,RobotStore store)=>store==null?Array.Empty<long>():Training[order.Ruler].Order(store.Robots.Where(r=>r.Owner==order.Ruler && !r.Stationed && !r.Disposal && WorkQualified(r,order.Ruler))).Take((int)Math.Min(12,order.Fighters)).Select(r=>r.Id).ToArray();
    public void RevalidateWorkReservations()
    {
        for(int node=0;node<Nodes.Length;node++){
            var store=Nodes[node];if(!store.Robots.Any(r=>ValidateWorkReservation(r)!=r))continue;
            store.Replace(store.Robots.Select(ValidateWorkReservation));SyncWorkers(node);
        }
        foreach(var store in Retinues)if(store.Robots.Any(r=>ValidateWorkReservation(r)!=r))store.Replace(store.Robots.Select(ValidateWorkReservation));
        Reservations?.RewriteReservations(order=>{
            if(order.Peaceful || order.Fighters==0 && order.RobotIds==null)return order;
            var store=OrderStore(order);
            var ids=order.RobotIds==null?LegacyOrderIds(order,store):order.RobotIds.Where(id=>store?.Robots.Any(r=>r.Id==id && WorkQualified(r,order.Ruler))==true).ToArray();
            if(ids.Length==0 && !order.Queen)return null;
            if(order.RobotIds==null)return order.Fighters==ids.Length?order:order with {Fighters=ids.Length};
            return ids.SequenceEqual(order.RobotIds)?order:order with {RobotIds=ids,Fighters=ids.Length};
        });
    }
    private void CancelRobotWorkOrders(Robot robot)
    {
        Reservations?.RewriteReservations(order=>{
            if(order.Peaceful)return order;
            var ids=order.RobotIds??LegacyOrderIds(order,OrderStore(order));
            if(!ids.Contains(robot.Id))return order;
            ids=ids.Where(id=>id!=robot.Id).ToArray();
            return ids.Length==0 && !order.Queen?null:order with {RobotIds=ids,Fighters=ids.Length};
        });
    }
    private bool PhysicalEditBusy(int node,Robot robot)=>node<0 || node>=Nodes.Length || Reservations?.RobotBattles.LockedNode(node)==true || Workshops[node].DisposalTarget==robot.Id || PendingEdits.Any(e=>e.Ids.Contains(robot.Id)) || _arrivalLocked?.Invoke(robot.Id)==true;
    public bool CanEditReservedPart(int node,Robot robot,int country)
    {
        if(PhysicalEditBusy(node,robot))return false;
        int work=WorkCountry(node,robot);return work<0 || work==country;
    }
    private Robot AfterPartEdit(int node,Robot before,Robot after,bool swap)
    {
        int country=WorkCountry(node,before);
        if(swap && country>=0 && WorkQualified(after,country))return after;
        CancelRobotWorkOrders(before);return Barefoot(after);
    }
    public static bool EquivalentReservedPart(Robot before,Robot after,RobotParts parts)
    {
        if(before==null || after==null || before.Owner!=after.Owner || before.Role!=after.Role)return false;
        return EquivalentPartState(before,after,parts);
    }
    private static bool EquivalentPartState(Robot before,Robot after,RobotParts parts)
    {
        if((before.Parts&parts)!=(after.Parts&parts))return false;
        foreach(var part in new[]{RobotParts.Head,RobotParts.Body,RobotParts.Legs})if((parts&part)!=0 && (before.Parts&part)!=0){
            if(before.Origin(part)!=after.Origin(part) || before.IsCheap(part)!=after.IsCheap(part) || (before.DamagedParts&part)!=(after.DamagedParts&part) || before.IsDamaged(part)!=after.IsDamaged(part) || before.ConnectedParts(part)!=after.ConnectedParts(part))return false;
        }
        return true;
    }
    private void CancelChangedDeliveries(int node,Robot[] before,IEnumerable<Robot> after)
    {
        var prior=before.ToDictionary(r=>r.Id);var next=after.ToDictionary(r=>r.Id);
        DirectDelivery.Orders.RemoveAll(o=>{
            bool Changed(long id)=>prior.TryGetValue(id,out var old) && !EquivalentReservedPart(old,next.GetValueOrDefault(id),o.Part);
            return o.Source==node && Changed(o.RobotId) || o.Target==node && o.TargetRobotId is long target && Changed(target);
        });
    }
    public bool CanTransferPocket(int node,int country,Robot robot)
        =>country>=0 && country<Retinues.Length && new[]{RobotParts.Head,RobotParts.Body,RobotParts.Legs}.Any(p=>(robot.Parts&p)!=0 && robot.Origin(p)==country) && CanEditReservedPart(node,robot,country);
    public void TransferPocket(int node,int country,long id,bool fromNode)
    {
        RevalidateWorkReservations();
        var source=fromNode?Nodes[node]:Retinues[country];var destination=fromNode?Retinues[country]:Nodes[node];
        var robot=source.Robots.Single(r=>r.Id==id);
        if(!CanTransferPocket(node,country,robot) || !destination.HasSpace || destination.Robots.Any(r=>r.Id==id))throw new InvalidOperationException("Pocket transfer is unavailable.");
        CancelRobotWorkOrders(robot);
        DirectDelivery.Orders.RemoveAll(o=>o.Source==node && o.RobotId==id || o.Target==node && o.TargetRobotId==id);
        source.Remove(id);destination.Add(Barefoot(robot.Materialize()) with {Owner=country,Disposal=false});SyncWorkers(node);Check();
    }
}
