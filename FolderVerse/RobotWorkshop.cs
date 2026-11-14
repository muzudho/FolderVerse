namespace FolderVerse;
using System;
using System.Linq;

public enum NodeAutomation { Auto, Gentle, Manual }
public sealed partial class RobotWorkshop
{
    public RobotParts Product {get;private set;}=RobotParts.Complete;
    public int ProductionPeriod {get;private set;}=12;
    public int ProductionAge {get;private set;}
    public bool Manufacturing {get;private set;}
    public bool Paused {get;set;}
    public bool DoNotRepeat {get;private set;}
    public bool CycleFinished {get;private set;}
    public bool HarvestOnly {get;private set;}
    public int MapleRemaining {get;private set;}
    public int MapleProgress {get;private set;}
    public void SelectMapleHarvest(int loads)
    {
        if(loads is <1 or >3)throw new ArgumentOutOfRangeException(nameof(loads));
        SelectPlan(3);
        MapleRemaining=loads;MapleProgress=0;Paused=false;
    }
    public void SetDoNotRepeat(bool value)
    {
        DoNotRepeat=value;
        if(!value)CycleFinished=false;
    }
    public NodeAutomation Automation {get;set;}=NodeAutomation.Manual;
    public int SupervisorLeaveRemaining {get;private set;}
    public NodeAutomation SupervisorReturnPolicy {get;private set;}=NodeAutomation.Gentle;
    public void GiveSupervisorLeave(int turns,NodeAutomation returnPolicy)
    {
        if(turns<1 || turns>99 || returnPolicy==NodeAutomation.Manual)throw new ArgumentOutOfRangeException();
        SupervisorLeaveRemaining=turns;SupervisorReturnPolicy=returnPolicy;Automation=NodeAutomation.Manual;
    }
    public void RecallSupervisor(NodeAutomation policy)
    {
        if(policy==NodeAutomation.Manual)throw new ArgumentOutOfRangeException(nameof(policy));
        SupervisorLeaveRemaining=0;SupervisorReturnPolicy=policy;Automation=policy;
    }
    internal void AdvanceSupervisorLeave()
    {
        if(SupervisorLeaveRemaining>0 && --SupervisorLeaveRemaining==0)Automation=SupervisorReturnPolicy;
    }
    public bool AutoAssemblyDisabled {get;set;}
    private int? _lastEffectiveProductionPeriod;
    public bool Waiting=>Manufacturing && ProductionAge>=(_lastEffectiveProductionPeriod??ProductionPeriod);
    internal static int PurposeDuration(int units,NodePurpose purpose,bool harvest)
    {
        int rate=purpose switch {
            NodePurpose.Harvest=>harvest?9:11,
            NodePurpose.Manufacturing=>harvest?11:9,
            _=>10
        };
        return (int)Math.Max(1,((long)units*rate+5)/10);
    }
    internal int EffectiveProductionPeriod(NodePurpose purpose)=>PurposeDuration(ProductionPeriod,purpose,false);
    internal int EffectiveHarvestPeriod(NodePurpose purpose)=>PurposeDuration(MapleRemaining*IndustryTime.UnitsPerTurn,purpose,true);
    public int WaitingProductionTurns {get;private set;}
    public int DisposalPeriod {get;private set;}
    public bool HasDisposalFactory {get;private set;}
    public int DisposalAge {get;private set;}
    public long? DisposalTarget {get;private set;}
    public void Configure(RobotParts product,int period)
    {RobotWorld.Validate(0,product);if(period<1)throw new ArgumentOutOfRangeException(nameof(period));MapleRemaining=MapleProgress=0;Product=product;_selectedProductPlan=null;CheapProduct=false;IntegratedProduct=0;ProductionPeriod=period;ProductionAge=WaitingProductionTurns=0;_lastEffectiveProductionPeriod=null;Manufacturing=true;CycleFinished=false;SelectedPlan=-1;RestEnabled=false;RestRemaining=ProducedSinceRest=0;}
    public void Dispose(RobotStore store,long id)
    {if(store.Robots.Single(r=>r.Id==id).Stationed)throw new InvalidOperationException("Stationed robot is locked.");if(DisposalTarget.HasValue)throw new InvalidOperationException("Disposal lane is busy.");if(!HasDisposalFactory)throw new InvalidOperationException("This node has no disposal factory.");int duration=DisposalTurns(store.Robots.Single(r=>r.Id==id).Parts);if(duration<1 || !store.Robots.Any(r=>r.Id==id))throw new ArgumentException("Invalid disposal order.");DisposalTarget=id;DisposalAge=0;DisposalPeriod=duration;}
    public void ConfigureDisposal()
    {HasDisposalFactory=true;}
    private long[] _disposalOrder=Array.Empty<long>();
    public System.Collections.Generic.IReadOnlyList<long> DisposalOrder=>_disposalOrder;
    public void SetDisposalOrder(System.Collections.Generic.IEnumerable<long> ids)
    {var order=ids.ToArray();if(order.Distinct().Count()!=order.Length)throw new ArgumentException("Duplicate disposal priority.");_disposalOrder=order;}
    public static int DisposalTurns(RobotParts parts)=>IndustryTime.Whole(((parts&RobotParts.Head)!=0?1:0)+((parts&RobotParts.Body)!=0?2:0)+((parts&RobotParts.Legs)!=0?2:0));
    internal void StartDisposal(RobotStore store,Func<long,bool> reserved)
    {foreach(var facility in DisposalFacilities)facility.StartSingleDisposal(store,id=>reserved(id) || IsDisposing(id));}
    private void StartSingleDisposal(RobotStore store,Func<long,bool> reserved)
    {
        if(!HasDisposalFactory || DisposalTarget.HasValue)return;
        int Rank(long id){int rank=Array.IndexOf(_disposalOrder,id);return rank<0?int.MaxValue:rank;}
        var robot=store.Robots.Where(r=>r.Disposal && !r.Stationed && !reserved(r.Id)).OrderBy(r=>Rank(r.Id)).ThenBy(r=>r.Id).FirstOrDefault();
        if(robot!=null)Dispose(store,robot.Id);
    }
    internal void CompleteDisposal(RobotStore store)
    {foreach(var facility in DisposalFacilities)facility.CompleteSingleDisposal(store);}
    private void CompleteSingleDisposal(RobotStore store)
    {
        if(DisposalTarget is not long id)return;
        if(!store.Robots.Any(r=>r.Id==id)){DisposalTarget=null;DisposalAge=0;return;}
        if(++DisposalAge<DisposalPeriod)return;
        store.Remove(id);DisposalTarget=null;DisposalAge=0;
    }
    internal void CompleteProduction(RobotWorld world,int node,int owner)
    {
        foreach(var line in ProductionLines){line.CompleteSingleProduction(world,node,owner,ReservedSlots);}
    }
    public bool WaitingForMaple(RobotWorld world,int node)=>world.MapleSupplyEnabled && Manufacturing &&
        !Paused && !CycleFinished && RestRemaining==0 && RobotWorld.MapleCost(CurrentProductionPlan.Preview(0))>world.AvailableMaple(node);
    private void CompleteSingleProduction(RobotWorld world,int node,int owner,int reservedSlots)
    {
        var purpose=world.PurposeAt(node);
        if(MapleRemaining>0)
        {
            if(Paused || owner<0 || !world.CanHarvestMaple(node,MapleRemaining))return;
            if(++MapleProgress>=EffectiveHarvestPeriod(purpose) && world.HarvestMaple(node,MapleRemaining)){
                MapleRemaining=0;MapleProgress=0;
            }
            return;
        }
        if(HarvestOnly || !Manufacturing || Paused || CycleFinished || owner<0)return;
        if(RestEnabled && RestRemaining>0){if(--RestRemaining==0 && DoNotRepeat)CycleFinished=true;return;}
        if(WaitingForMaple(world,node))return;
        int period=EffectiveProductionPeriod(purpose);
        _lastEffectiveProductionPeriod=period;
        ProductionAge=ProductionAge>=period?period:ProductionAge+1;
        if(!Waiting)return;
        var product=CurrentProductionPlan.Preview(owner) with {FunctionDisplayNode=FunctionOriginNode};
        int cost=world.MapleSupplyEnabled?RobotWorld.MapleCost(product):0;
        var store=world.Nodes[node];
        var before=cost>0?store.MaplePackages.ToArray():null;
        if(cost>0)world.SpendMaple(node,cost);
        if(!(world.ProductionIntakeEnabled?world.QueueProduction(this,node,owner,product,reservedSlots):world.TryStoreProduction(node,owner,product,reservedSlots))){
            if(before!=null)store.SetMaplePackages(before);
            if(WaitingProductionTurns<int.MaxValue)WaitingProductionTurns++;
            return;
        }
        ProductionAge=WaitingProductionTurns=0;
        if(RestEnabled && ++ProducedSinceRest>=RestEvery){ProducedSinceRest=0;RestRemaining=IndustryTime.Whole(RestTurns);}
        if(!RestEnabled && DoNotRepeat)CycleFinished=true;
    }
}

