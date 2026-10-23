namespace FolderVerse;
using System;
using System.Linq;

public enum HangarReservation { None,Disposal,Defense,Worker }
public sealed partial class RobotWorld
{
    public ConquestCampaign Reservations {get;set;}
    public Func<int,int> ReservationSource;
    public bool MarchReserved(int node,Robot robot)=>Reservations?.PlannedOrders.Any(o=>!o.PocketOnly && o.Ruler==robot.Owner && (o.SourceNode>=0?o.SourceNode:ReservationSource?.Invoke(o.Ruler)??node)==node && (o.RobotIds?.Contains(robot.Id)??Training[o.Ruler].Order(Nodes[node].Robots.Where(r=>r.Owner==o.Ruler && Training[r.Owner].Eligible(r) && !r.Stationed)).Take((int)Math.Min(12,o.Fighters)).Any(r=>r.Id==robot.Id)))==true;
    public bool AssemblyLocked(int node,Robot robot)=>robot.Disposal || robot.Stationed || Workshops[node].DisposalTarget==robot.Id || PendingEdits.Any(e=>e.Ids.Contains(robot.Id)) || MarchReserved(node,robot);
    public bool CanShip(int node,Robot robot)=>!robot.Stationed && Workshops[node].DisposalTarget!=robot.Id && !PendingEdits.Any(e=>e.Ids.Contains(robot.Id)) && !MarchReserved(node,robot);
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
    public bool ToggleReservation(int node,long id,HangarReservation kind)
    {
        var store=Nodes[node];var robot=store.Robots.Single(r=>r.Id==id);var workshop=Workshops[node];
        if(workshop.DisposalTarget==id || PendingEdits.Any(e=>e.Ids.Contains(id)) || MarchReserved(node,robot))return false;
        bool remove=kind==HangarReservation.Disposal?robot.Disposal:kind==HangarReservation.Defense?robot.DefenseOnly:kind==HangarReservation.Worker && robot.FactoryWorker;
        if(!remove){
            if(AssemblyLocked(node,robot))return false;
            if(kind is HangarReservation.Defense or HangarReservation.Worker && !Training[robot.Owner].Eligible(robot))return false;
            if(kind==HangarReservation.Worker && workshop.ProductionLines.Length>=workshop.LineLimit)return false;
        }
        store.Replace(store.Robots.Select(r=>r.Id!=id?r:r with {Disposal=kind==HangarReservation.Disposal && !remove,DefenseOnly=kind==HangarReservation.Defense && !remove,FactoryWorker=kind==HangarReservation.Worker && !remove}).ToArray());
        SyncWorkers(node);return true;
    }
}
