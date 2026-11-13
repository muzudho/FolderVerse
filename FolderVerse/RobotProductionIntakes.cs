namespace FolderVerse;
using System;
using System.Collections.Generic;
using System.Linq;

public sealed record ProductionIntake(int Source,int? Target,Robot Product,RobotWorkshop Line)
{
    public string Status {get;set;}="入荷予約";
}
public sealed partial class RobotWorkshop
{
    public int? IntakeTarget {get;set;}
    public string IntakeFailure {get;set;}="";
}
public sealed partial class RobotWorld
{
    public bool ProductionIntakeEnabled {get;set;}
    public List<ProductionIntake> ProductionIntakes {get;}=new();
    public Func<int,int,bool> IntakeAdjacent;
    public Func<int,int,bool> IntakeAllowed;
    private Func<IEnumerable<(RobotShipment Shipment,int Owner)>> _intakePlayback;
    internal IEnumerable<(RobotShipment Shipment,int Owner)> IntakePlayback=>_intakePlayback?.Invoke()??Array.Empty<(RobotShipment,int)>();
    internal bool QueueProduction(RobotWorkshop line,int source,int owner,Robot product,int reservedSlots)
    {
        int target=line.IntakeTarget??source;
        line.IntakeFailure="";
        if(target<0 || target>=Nodes.Length || source!=target && IntakeAdjacent?.Invoke(source,target)!=true){line.IntakeFailure="入荷先が同地点・隣接地点ではありません";return false;}
        if(IntakeAllowed?.Invoke(owner,target)==false){line.IntakeFailure="入荷先の機能・所有国を確認してください";return false;}
        int reserved=target==source?reservedSlots:Workshops[target].ReservedSlots;
        var planned=product with {Id=_nextId};
        var store=Nodes[target].PlanningCopy();
        bool Locked(Robot robot)=>IntakeReceiverLocked(target,robot);
        bool merge=!Workshops[target].AutoAssemblyDisabled;
        foreach(var incoming in ProductionIntakes.Where(i=>(i.Target??i.Source)==target))
            PlaceIntake(store,incoming.Product,reserved,merge,Locked);
        if(!PlaceIntake(store,planned,reserved,merge,Locked)){line.IntakeFailure="入荷先の格納枠が不足しています";return false;}
        Create(owner,product.Parts);ProductionIntakes.Add(new(source,line.IntakeTarget,planned,line));return true;
    }
    private int ProductionSource(ProductionIntake intake)=>intake.Source;
    internal void RelocateProductionIntakes(int source,int target)
    {for(int i=0;i<ProductionIntakes.Count;i++)if(ProductionIntakes[i].Source==source)ProductionIntakes[i]=ProductionIntakes[i] with {Source=target,Product=ProductionIntakes[i].Product with {FunctionDisplayNode=ProductionIntakes[i].Product.FunctionDisplayNode??source}};}
    public void SetIntakeTarget(RobotWorkshop line,int? target)
    {
        line.IntakeTarget=target;
        line.IntakeFailure="";
        for(int i=0;i<ProductionIntakes.Count;i++)if(ReferenceEquals(ProductionIntakes[i].Line,line))ProductionIntakes[i]=ProductionIntakes[i] with {Target=target,Status="入荷予約"};
    }
    internal void PreviewProductionIntakes(WorldSetup world,RobotWorld copy,int[] hosts)
    {
        foreach(var intake in ProductionIntakes){
            int source=hosts[ProductionSource(intake)],target=intake.Target??source;
            if(target>=0 && target<copy.Nodes.Length)
                PlaceIntake(copy.Nodes[target],intake.Product,copy.Workshops[target].ReservedSlots,!copy.Workshops[target].AutoAssemblyDisabled,r=>IntakeReceiverLocked(target,r) || copy.IntakeReceiverLocked(target,r));
        }
    }
    internal void ResolveProductionIntakes(WorldSetup world)
    {
        var playback=new List<(RobotShipment,int)>();_intakePlayback=()=>playback;
        foreach(var intake in ProductionIntakes.ToArray()){
            int source=ProductionSource(intake),target=intake.Target??source;
            bool connected=source==target || world.Routes.Neighbors(world.Nodes.All[source]).Any(r=>world.Nodes.At(r.Target,r.Entry).Id==target);
            if(!connected || !world.Nodes.All[target].Functional || !world.Relations.Allied(intake.Product.Owner,world.Nodes.All[target].Owner) || world.Campaign.RobotBattles.LockedNode(target) || world.Campaign.RobotBattles.LockedEdge(source,target)){
                intake.Status="入荷待機：経路・所有国・戦闘状況";continue;
            }
            if(!PlaceIntake(Nodes[target],intake.Product,Workshops[target].ReservedSlots,!Workshops[target].AutoAssemblyDisabled,r=>IntakeReceiverLocked(target,r))){intake.Status="入荷待機：格納枠不足";continue;}
            if(source!=target)playback.Add((new(source,target,intake.Product.Id,intake.Product.Parts),intake.Product.Owner));
            ProductionIntakes.Remove(intake);
        }
    }
    private bool IntakeReceiverLocked(int node,Robot robot)=>robot.Disposal || robot.Stationed || Workshops[node].IsDisposing(robot.Id) ||
        PendingEdits.Any(e=>e.Ids.Contains(robot.Id)) ||
        DirectDelivery.LockedParts(node,robot.Id)!=0 || PartPlanning.Orders.Any(o=>
            (o.RobotId==robot.Id && (o.ExpectedSource==0 || o.ExpectedSource==robot.Parts)) ||
            (o.TargetRobotId==robot.Id && (o.ExpectedTarget==0 || o.ExpectedTarget==robot.Parts)));

    private bool PlaceIntake(RobotStore store,Robot product,int reservedSlots,bool merge,Func<Robot,bool> locked)
    {
        if(store.Count<store.StorageLimit-reservedSlots){store.Add(product);return true;}
        if(store.HasSpace || !merge)return false;
        var receiver=store.Robots.FirstOrDefault(r=>r.Owner==product.Owner && (r.Parts&product.Parts)==0 && !locked(r) &&
            ((r.Parts|product.Parts)!=RobotParts.Complete || r.Merge(product,r.Owner).AssemblyCredit(r.Owner)>=Training[r.Owner].RequiredNative));
        if(receiver==null)return false;
        var merged=receiver.Merge(product,receiver.Owner);
        var next=store.Robots.Select(r=>r.Id==receiver.Id?merged:r).ToArray();
        if(store.OccupiedSlots(next)>store.StorageLimit)return false;
        store.Replace(next);
        return true;
    }
}
