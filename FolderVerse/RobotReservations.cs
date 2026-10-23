namespace FolderVerse;
using System;
using System.Linq;

public enum HangarReservation { None,Disposal,Defense,Worker }
public sealed partial class RobotWorld
{
    public ConquestCampaign Reservations {get;set;}
    public Func<int,int> ReservationSource;
    public bool MarchReserved(int node,Robot robot)=>Reservations?.PlannedOrders.Any(o=>!o.PocketOnly && o.Ruler==robot.Owner && (o.SourceNode>=0?o.SourceNode:ReservationSource?.Invoke(o.Ruler)??node)==node && (o.RobotIds?.Contains(robot.Id)??Training[o.Ruler].Order(Nodes[node].Robots.Where(r=>r.Owner==o.Ruler && Training[r.Owner].Eligible(r) && !r.Stationed)).Take((int)Math.Min(12,o.Fighters)).Any(r=>r.Id==robot.Id)))==true;
    public bool BaseAssemblyLocked(int node,Robot robot)=>robot.Disposal || robot.Stationed || Workshops[node].DisposalTarget==robot.Id || PendingEdits.Any(e=>e.Ids.Contains(robot.Id)) || MarchReserved(node,robot);
    public bool AssemblyLocked(int node,Robot robot)=>BaseAssemblyLocked(node,robot) || DirectDelivery.LockedParts(node,robot.Id)!=0;
    public bool PartLocked(int node,Robot robot,RobotParts part)=>BaseAssemblyLocked(node,robot) || (DirectDelivery.LockedParts(node,robot.Id)&part)!=0;
    public bool CanShip(int node,Robot robot)=>!robot.Stationed && Workshops[node].DisposalTarget!=robot.Id && !PendingEdits.Any(e=>e.Ids.Contains(robot.Id)) && !MarchReserved(node,robot) && DirectDelivery.LockedParts(node,robot.Id)==0;
    private bool ReservationBusy(int node,long id)=>Reservations?.RobotBattles.LockedNode(node)==true || Workshops[node].DisposalTarget==id || PendingEdits.Any(e=>e.Ids.Contains(id)) || DirectDelivery.LockedParts(node,id)!=0;
    private Robot WithoutRole(Robot robot)=>robot with {Disposal=false,DefenseOnly=false,FactoryWorker=false};
    public bool CanReserveSortie(int node,Robot robot)=>!ReservationBusy(node,robot.Id) && Training[robot.Owner].Eligible(WithoutRole(robot));
    private void RemoveMarchReservation(int node,Robot robot)
    {
        Reservations?.RewriteReservations(order=>{
            if(order.PocketOnly || order.Ruler!=robot.Owner || (order.SourceNode>=0?order.SourceNode:ReservationSource?.Invoke(order.Ruler)??node)!=node)return order;
            var ids=order.RobotIds??Training[order.Ruler].Order(Nodes[node].Robots.Where(r=>r.Owner==order.Ruler && Training[r.Owner].Eligible(r) && !r.Stationed)).Take((int)Math.Min(12,order.Fighters)).Select(r=>r.Id).ToArray();
            if(!ids.Contains(robot.Id))return order;
            ids=ids.Where(id=>id!=robot.Id).ToArray();
            return ids.Length==0 && !order.Queen?null:order with {RobotIds=ids,Fighters=ids.Length};
        });
    }
    public bool PrepareSortieReservation(int node,long[] ids)
    {
        var store=Nodes[node];var robots=store.Robots.Where(r=>ids.Contains(r.Id)).ToArray();
        if(ids.Distinct().Count()!=ids.Length || robots.Length!=ids.Length || robots.Any(r=>!CanReserveSortie(node,r)))return false;
        foreach(var robot in robots)RemoveMarchReservation(node,robot);
        store.Replace(store.Robots.Select(r=>ids.Contains(r.Id)?WithoutRole(r):r).ToArray());SyncWorkers(node);return true;
    }
    public void ConfigureFactories(WorldSetup world)
    {
        Reservations=world.Campaign;
        ReservationSource=owner=>world.Nodes.Current(owner)?.Id??-1;
        foreach(var node in world.Nodes.All){Workshops[node.Id].LimitLines(node.Terrain==TravelTerrain.Sea?1:3);SyncWorkers(node.Id);}
    }
    public void SyncWorkers(int node)
    {
        var workshop=Workshops[node];
        var ids=Nodes[node].Robots.Where(r=>r.FactoryWorker && Training[r.Owner].Eligible(r)).OrderBy(r=>r.Id).Take(Math.Max(0,workshop.LineLimit-workshop.BaseLineCount)).Select(r=>r.Id).ToArray();
        foreach(var robot in Nodes[node].Robots.Where(r=>r.FactoryWorker && !ids.Contains(r.Id)).ToArray())Nodes[node].Replace(Nodes[node].Robots.Select(r=>r.Id==robot.Id?r with {FactoryWorker=false}:r).ToArray());
        workshop.SetWorkers(ids);
    }
    public bool CanSetReservation(int node,Robot robot,HangarReservation kind)
    {
        var workshop=Workshops[node];
        if(kind is <HangarReservation.Disposal or >HangarReservation.Worker || ReservationBusy(node,robot.Id))return false;
        bool remove=kind==HangarReservation.Disposal?robot.Disposal:kind==HangarReservation.Defense?robot.DefenseOnly:kind==HangarReservation.Worker && robot.FactoryWorker;
        if(!remove){
            if(kind is HangarReservation.Defense or HangarReservation.Worker && !Training[robot.Owner].Eligible(WithoutRole(robot)))return false;
            if(kind==HangarReservation.Worker && workshop.ProductionLines.Length>=workshop.LineLimit)return false;
        }
        return true;
    }
    public bool ToggleReservation(int node,long id,HangarReservation kind)
    {
        var store=Nodes[node];var robot=store.Robots.Single(r=>r.Id==id);
        if(!CanSetReservation(node,robot,kind))return false;
        bool remove=kind==HangarReservation.Disposal?robot.Disposal:kind==HangarReservation.Defense?robot.DefenseOnly:robot.FactoryWorker;
        RemoveMarchReservation(node,robot);
        store.Replace(store.Robots.Select(r=>r.Id!=id?r:r with {Disposal=kind==HangarReservation.Disposal && !remove,DefenseOnly=kind==HangarReservation.Defense && !remove,FactoryWorker=kind==HangarReservation.Worker && !remove}).ToArray());
        SyncWorkers(node);return true;
    }
}
