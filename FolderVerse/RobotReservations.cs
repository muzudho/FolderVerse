namespace FolderVerse;
using System;
using System.Linq;

public enum HangarReservation { None,Disposal,Defense,Worker,Intercept }
public sealed partial class RobotWorld
{
    public ConquestCampaign Reservations {get;set;}
    public Func<int,int> ReservationSource;
    private Func<long,bool> _arrivalLocked;
    private Func<int,int> _reservationFlagOwner;
    public bool MarchReserved(int node,Robot robot)=>MarchReservedCore(node,robot,false);
    public bool CombatMarchReserved(int node,Robot robot)=>MarchReservedCore(node,robot,true);
    private bool MarchReservedCore(int node,Robot robot,bool combatOnly)=>Reservations?.PlannedOrders.Any(o=>(!combatOnly || !o.Peaceful) && !o.PocketOnly && o.Ruler==robot.Owner && (o.SourceNode>=0?o.SourceNode:ReservationSource?.Invoke(o.Ruler)??node)==node && (o.RobotIds?.Contains(robot.Id)??Training[o.Ruler].Order(Nodes[node].Robots.Where(r=>r.Owner==o.Ruler && Training[r.Owner].Eligible(r) && !r.Stationed && DirectDelivery.LockedParts(node,r.Id)==0)).Take((int)Math.Min(12,o.Fighters)).Any(r=>r.Id==robot.Id)))==true;
    public bool BaseAssemblyLocked(int node,Robot robot)=>robot.Disposal || robot.Stationed || Workshops[node].DisposalTarget==robot.Id || PendingEdits.Any(e=>e.Ids.Contains(robot.Id)) || MarchReserved(node,robot);
    public bool AssemblyLocked(int node,Robot robot)=>BaseAssemblyLocked(node,robot) || DirectDelivery.LockedParts(node,robot.Id)!=0 || _arrivalLocked?.Invoke(robot.Id)==true;
    public bool PartLocked(int node,Robot robot,RobotParts part)=>BaseAssemblyLocked(node,robot) || (DirectDelivery.LockedParts(node,robot.Id)&robot.ConnectedParts(part))!=0;
    public bool CanShip(int node,Robot robot)=>!robot.Stationed && Workshops[node].DisposalTarget!=robot.Id && !PendingEdits.Any(e=>e.Ids.Contains(robot.Id)) && !MarchReserved(node,robot) && DirectDelivery.LockedParts(node,robot.Id)==0 && _arrivalLocked?.Invoke(robot.Id)!=true;
    private bool ReservationBusy(int node,long id)=>Reservations?.RobotBattles.LockedNode(node)==true || Workshops[node].DisposalTarget==id || PendingEdits.Any(e=>e.Ids.Contains(id)) || DirectDelivery.LockedParts(node,id)!=0;
    private Robot WithoutRole(Robot robot)=>robot with {Disposal=false,DefenseOnly=false,InterceptOnly=false,FactoryWorker=false,DefenseFlag=null};
    public bool CanReserveSortie(int node,Robot robot)=>!ReservationBusy(node,robot.Id) && Training[robot.Owner].Eligible(WithoutRole(robot));
    private void RemoveMarchReservation(int node,Robot robot)
    {
        Reservations?.RewriteReservations(order=>{
            if(order.PocketOnly || order.Ruler!=robot.Owner || (order.SourceNode>=0?order.SourceNode:ReservationSource?.Invoke(order.Ruler)??node)!=node)return order;
            var ids=order.RobotIds??Training[order.Ruler].Order(Nodes[node].Robots.Where(r=>r.Owner==order.Ruler && Training[r.Owner].Eligible(r) && !r.Stationed && DirectDelivery.LockedParts(node,r.Id)==0)).Take((int)Math.Min(12,order.Fighters)).Select(r=>r.Id).ToArray();
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
        Reservations=world.Campaign;_reservationFlagOwner=node=>world.Nodes.All[node].Owner;
        for(int node=0;node<Nodes.Length;node++){
            int current=node;
            Nodes[node].Replace(Nodes[node].Robots.Select(r=>ValidateDefenseFlag(r,n=>world.Nodes.All[n].Owner)).Select(r=>r.DefenseOnly && r.Parts==RobotParts.Complete && r.DefenseFlag==null?r with {DefenseFlag=new(r.Owner,current,world.Nodes.All[current].Owner)}:r).ToArray());
        }
        ReservationSource=owner=>world.Nodes.Current(owner)?.Id??-1;
        foreach(var node in world.Nodes.All){Workshops[node.Id].LimitLines(node.Terrain==TravelTerrain.Sea?1:3);SyncWorkers(node.Id);}
        RevalidateWorkReservations();
    }
    public void SyncWorkers(int node)
    {
        var workshop=Workshops[node];
        var ids=Nodes[node].Robots.Where(r=>r.FactoryWorker && WorkQualified(r,r.Owner)).OrderBy(r=>r.Id).Take(Math.Max(0,workshop.LineLimit-workshop.BaseLineCount)).Select(r=>r.Id).ToArray();
        foreach(var robot in Nodes[node].Robots.Where(r=>r.FactoryWorker && !ids.Contains(r.Id)).ToArray())Nodes[node].Replace(Nodes[node].Robots.Select(r=>r.Id==robot.Id?r with {FactoryWorker=false}:r).ToArray());
        workshop.SetWorkers(ids);
    }
    public bool CanSetReservation(int node,Robot robot,HangarReservation kind,int? commander=null)
        =>CanSetReservationCore(node,robot,kind,commander,false);
    // Explain training only if relaxing that policy alone would make the command valid.
    public bool ReservationBlockedByTraining(int node,Robot robot,HangarReservation kind,int? commander=null)
        =>!CanSetReservationCore(node,robot,kind,commander,false) && CanSetReservationCore(node,robot,kind,commander,true);
    private bool CanSetReservationCore(int node,Robot robot,HangarReservation kind,int? commander,bool ignoreTraining)
    {
        if(node<0 || node>=Nodes.Length || robot==null)return false;
        int country=commander??robot.Owner;if(country<0 || country>=Training.Length)return false;
        if(robot.DefenseFlag is {} flag && flag.Commander!=country)return false;
        if((robot.Stationed || robot.Disposal || MarchReserved(node,robot)) && robot.Owner!=country)return false;
        var workshop=Workshops[node];
        if(kind is <HangarReservation.Disposal or >HangarReservation.Intercept || ReservationBusy(node,robot.Id))return false;
        bool remove=kind==HangarReservation.Disposal?robot.Disposal:kind==HangarReservation.Defense?robot.DefenseOnly:kind==HangarReservation.Intercept?robot.InterceptOnly:kind==HangarReservation.Worker && robot.FactoryWorker;
        if(!remove){
            if(kind is HangarReservation.Defense or HangarReservation.Worker or HangarReservation.Intercept){
                var candidate=WithoutRole(robot).Materialize() with {Owner=country};
                bool eligible=kind==HangarReservation.Worker?Training[country].WorkerEligible(candidate):Training[country].Eligible(candidate);
                if(!candidate.CanFight || !ignoreTraining && !eligible)return false;
            }
            if(kind==HangarReservation.Defense && robot.NativeParts(country)==0)return false;
            if(kind==HangarReservation.Worker && workshop.ProductionLines.Length>=workshop.LineLimit)return false;
        }
        return true;
    }
    public bool ToggleReservation(int node,long id,HangarReservation kind,int? commander=null)
    {
        var store=Nodes[node];var robot=store.Robots.Single(r=>r.Id==id);
        if(!CanSetReservation(node,robot,kind,commander))return false;
        bool remove=kind==HangarReservation.Disposal?robot.Disposal:kind==HangarReservation.Defense?robot.DefenseOnly:kind==HangarReservation.Intercept?robot.InterceptOnly:robot.FactoryWorker;
        RemoveMarchReservation(node,robot);
        int country=commander??robot.Owner;
        var next=robot.Materialize() with {Owner=!remove && kind==HangarReservation.Defense?country:robot.Owner,
            Disposal=kind==HangarReservation.Disposal && !remove,DefenseOnly=kind==HangarReservation.Defense && !remove,InterceptOnly=kind==HangarReservation.Intercept && !remove,FactoryWorker=kind==HangarReservation.Worker && !remove,
            DefenseFlag=kind==HangarReservation.Defense && !remove?new(country,node,_reservationFlagOwner?.Invoke(node)??country):null};
        store.Replace(store.Robots.Select(r=>r.Id==id?next:r).ToArray());
        SyncWorkers(node);return true;
    }
    public void ReleaseDefenseFlag(int node,int previousOwner)
    {
        foreach(var store in Nodes.Concat(Retinues))store.Replace(store.Robots.Select(r=>r.DefenseFlag is {} flag && flag.Node==node && flag.FlagOwner==previousOwner?r.WithoutDefenseReservation():r).ToArray());
    }
    public static Robot ValidateDefenseFlag(Robot robot,Func<int,int> owner)
        =>robot.DefenseFlag is {} flag && owner(flag.Node)!=flag.FlagOwner?robot.WithoutDefenseReservation():robot;

}
