namespace FolderVerse;
using System;
using System.Collections.Generic;
using System.Linq;

[Flags]
public enum RobotParts { Legs=1, Body=2, Head=4, Complete=7 }
public enum RobotRole { Soldier, Captain }
public sealed record RobotWorkshopEdit(int Node,long[] Ids,RobotParts? SplitPart,int? Owner=null);
public sealed record DefenseFlagReservation(int Commander,int Node,int FlagOwner);
public sealed partial record Robot(long Id,int Owner,RobotParts Parts,RobotRole Role=RobotRole.Soldier,bool Disposal=false)
{
    public bool SalvageReserved {get;init;}
    public int? FunctionDisplayNode {get;init;}
    public bool InterceptOnly {get;init;}
    public bool DefenseOnly {get;init;}
    public DefenseFlagReservation DefenseFlag {get;init;}
    public int ShoeOwner=>Parts==RobotParts.Complete && DefenseOnly?DefenseFlag?.Commander??Owner:-1;
    public Robot WithoutDefenseReservation()=>this with {DefenseOnly=false,DefenseFlag=null};
    public bool WarehouseKeeper {get;init;}
    public bool FactoryWorker {get;init;}
    public bool HarvestAssignment {get;init;}
    public bool HarvestWorker=>FactoryWorker && HarvestAssignment;
    public long? LongDeliveryNumber {get;init;}
    public int? ReturnHomeCommander {get;init;}
    public string ReturnHomeStatus {get;init;}
    public bool Stationed=>SalvageReserved || WarehouseKeeper || DefenseOnly || InterceptOnly || FactoryWorker || LongDeliveryNumber.HasValue;
    public RobotParts DamagedParts {get;init;}=0;
    public RobotParts CheapParts {get;init;}=0;
    public RobotParts PremiumParts {get;init;}=0;
    public RobotMaterial HeadMaterial {get;init;}
    public RobotMaterial BodyMaterial {get;init;}
    public RobotMaterial LegsMaterial {get;init;}
    public RobotMaterial Material(RobotParts part)=>part switch
    {
        RobotParts.Head=>HeadMaterial,
        RobotParts.Body=>BodyMaterial,
        RobotParts.Legs=>LegsMaterial,
        _=>throw new ArgumentOutOfRangeException(nameof(part))
    };
    // A normal multi-part package stays separable; only this mask denotes a physical joint.
    public RobotParts IntegratedParts {get;init;}=0;
    public static bool ValidIntegratedParts(RobotParts parts)=>parts==0 || parts==(RobotParts.Head|RobotParts.Body) || parts==(RobotParts.Body|RobotParts.Legs);
    public bool ValidIntegration=>ValidIntegratedParts(IntegratedParts) && (IntegratedParts&Parts)==IntegratedParts;
    public RobotParts ConnectedParts(RobotParts selected)=>(selected&IntegratedParts)!=0?selected|IntegratedParts:selected;
    public bool CanSeparate(RobotParts selected)=>(selected&Parts)==selected && ConnectedParts(selected)==selected;
    // Preserve actual impact locations for future repair; failure of the joined unit is derived.
    public RobotParts EffectiveDamagedParts=>(DamagedParts&IntegratedParts)!=0?DamagedParts|IntegratedParts:DamagedParts;
    public Robot Slice(RobotParts selected)
    {
        if(selected==0 || !CanSeparate(selected))throw new InvalidOperationException("Integrated parts cannot be separated.");
        return Materialize() with {ReturnHomeCommander=null,ReturnHomeStatus=null,Parts=selected,DamagedParts=DamagedParts&selected,CheapParts=CheapParts&selected&~PremiumParts,PremiumParts=PremiumParts&selected,IntegratedParts=IntegratedParts&selected,DefenseFlag=selected==RobotParts.Complete?DefenseFlag:null,
            HeadMaterial=(selected&RobotParts.Head)!=0?HeadMaterial:default,
            BodyMaterial=(selected&RobotParts.Body)!=0?BodyMaterial:default,
            LegsMaterial=(selected&RobotParts.Legs)!=0?LegsMaterial:default,
            HeadWork=(selected&RobotParts.Head)!=0?HeadWork:null,BodyWork=(selected&RobotParts.Body)!=0?BodyWork:null,LegsWork=(selected&RobotParts.Legs)!=0?LegsWork:null};
    }
    public int LastBattlePersuasionTurn {get;init;}=-1;
    public int LastDomesticPersuasionTurn {get;init;}=-1;
    public RobotParts PersuadableParts(int owner)=>Parts&~EffectiveDamagedParts &
        ((Origin(RobotParts.Head)!=owner?RobotParts.Head:0)|(Origin(RobotParts.Body)!=owner?RobotParts.Body:0)|(Origin(RobotParts.Legs)!=owner?RobotParts.Legs:0));
    public Robot Persuaded(int owner,RobotParts parts,bool changeOwner)
    {
        var robot=Materialize();parts&=Parts&~EffectiveDamagedParts;
        return robot with {Owner=changeOwner?owner:Owner,
            HeadOrigin=(parts&RobotParts.Head)!=0?owner:robot.HeadOrigin,
            BodyOrigin=(parts&RobotParts.Body)!=0?owner:robot.BodyOrigin,
            LegsOrigin=(parts&RobotParts.Legs)!=0?owner:robot.LegsOrigin};
    }
    public bool IsCheap(RobotParts part)=>(Parts&part)!=0 && (CheapParts&~PremiumParts&part)!=0;
    public bool IsPremium(RobotParts part)=>(Parts&PremiumParts&part)!=0;
    public int PremiumBonus=>CanFight && (Parts&PremiumParts&~EffectiveDamagedParts)!=0?1:0;
    public bool IsDamaged(RobotParts part)=>(Parts&part)!=0 && (EffectiveDamagedParts&part)!=0;
    // Origin retains the country even while damage makes a part unaffiliated.
    public int Affiliation(RobotParts part)=>(Parts&part)==0 || IsDamaged(part)?-1:Origin(part);
    public int HeadOrigin {get;init;}=-1;
    public int BodyOrigin {get;init;}=-1;
    public int LegsOrigin {get;init;}=-1;
    public int Origin(RobotParts part)=>part switch{RobotParts.Head=>HeadOrigin==-1?Owner:Math.Max(-1,HeadOrigin),RobotParts.Body=>BodyOrigin==-1?Owner:Math.Max(-1,BodyOrigin),_=>LegsOrigin==-1?Owner:Math.Max(-1,LegsOrigin)};
    public bool HasAffiliatedParts=>new[]{RobotParts.Head,RobotParts.Body,RobotParts.Legs}.Any(p=>(Parts&p)!=0 && Affiliation(p)>=0);
    public int LargestFlagGroup=>new[]{RobotParts.Head,RobotParts.Body,RobotParts.Legs}.Select(Affiliation).Where(o=>o>=0).GroupBy(o=>o).Select(g=>g.Count()).DefaultIfEmpty(0).Max();
    public int NativeParts(int owner)=>new[]{RobotParts.Head,RobotParts.Body,RobotParts.Legs}.Count(p=>(Parts&p)!=0 && owner>=0 && Affiliation(p)==owner);
    public int RegularNativeParts(int owner)=>new[]{RobotParts.Head,RobotParts.Body,RobotParts.Legs}.Count(p=>(Parts&p)!=0 && !IsCheap(p) && Origin(p)==owner);
    public int AssemblyCredit(int owner)=>NativeParts(owner);
    public int CombatParts(int owner)=>Math.Max(NativeParts(owner),CanFight?1:0)+PremiumBonus;
    public int CombatRifles(int owner)=>(NativeParts(owner)==3?4:Math.Max(NativeParts(owner),CanFight?1:0))+PremiumBonus;
    public RobotEquipment Equipment(int owner,RobotEquipmentMode mode)
    {
        if(IsWorkpiece)return new(0,0,0);
        if(mode==RobotEquipmentMode.Legacy)
        {
            int melee=CombatParts(owner);
            return new(melee,melee,CombatRifles(owner));
        }
        int shields=0,daggers=0,rifles=0;
        foreach(var part in RobotMaterials.Parts)
        {
            if((Parts&part)==0 || IsDamaged(part))continue;
            var amount=RobotMaterials.Thirds(Material(part));
            shields+=amount.Shields;daggers+=amount.Daggers;rifles+=amount.Rifles;
        }
        return new(shields/3,daggers/3,rifles/3);
    }
    internal int StoredOrigin(RobotParts part)=>Origin(part)<0?-2:Origin(part);
    public Robot Materialize()=>this with {HeadOrigin=StoredOrigin(RobotParts.Head),BodyOrigin=StoredOrigin(RobotParts.Body),LegsOrigin=StoredOrigin(RobotParts.Legs),DamagedParts=DamagedParts&Parts,CheapParts=CheapParts&Parts&~PremiumParts,PremiumParts=PremiumParts&Parts};
    public Robot Merge(Robot other,int owner)
    {
        if(!ValidIntegration || !other.ValidIntegration || !CanSeparate(other.Parts&Parts))throw new InvalidOperationException("Cannot replace only half of integrated parts.");
        var result=Materialize() with {Parts=Parts|other.Parts,LastBattlePersuasionTurn=Math.Max(LastBattlePersuasionTurn,other.LastBattlePersuasionTurn),LastDomesticPersuasionTurn=Math.Max(LastDomesticPersuasionTurn,other.LastDomesticPersuasionTurn),DamagedParts=(DamagedParts&Parts&~other.Parts)|(other.DamagedParts&other.Parts),CheapParts=(CheapParts&Parts&~other.Parts)|(other.CheapParts&other.Parts),HeadOrigin=(other.Parts&RobotParts.Head)!=0?other.StoredOrigin(RobotParts.Head):StoredOrigin(RobotParts.Head),BodyOrigin=(other.Parts&RobotParts.Body)!=0?other.StoredOrigin(RobotParts.Body):StoredOrigin(RobotParts.Body),LegsOrigin=(other.Parts&RobotParts.Legs)!=0?other.StoredOrigin(RobotParts.Legs):StoredOrigin(RobotParts.Legs)};
        result=result with {IntegratedParts=(IntegratedParts&~other.Parts)|other.IntegratedParts};
        result=result with {PremiumParts=(PremiumParts&Parts&~other.Parts)|(other.PremiumParts&other.Parts),
            HeadMaterial=(other.Parts&RobotParts.Head)!=0?other.HeadMaterial:HeadMaterial,
            BodyMaterial=(other.Parts&RobotParts.Body)!=0?other.BodyMaterial:BodyMaterial,
            LegsMaterial=(other.Parts&RobotParts.Legs)!=0?other.LegsMaterial:LegsMaterial,
            HeadWork=(other.Parts&RobotParts.Head)!=0?other.HeadWork:HeadWork,
            BodyWork=(other.Parts&RobotParts.Body)!=0?other.BodyWork:BodyWork,
            LegsWork=(other.Parts&RobotParts.Legs)!=0?other.LegsWork:LegsWork};
        if(!result.ValidIntegration)throw new InvalidOperationException("Incompatible integrated parts.");
        return result with {Owner=result.AssemblyCredit(owner)>0?owner:Owner};
    }
    public bool CanFight=>RecoveredRaw==null && !IsWorkpiece && !IsDismantling && !WarehouseKeeper && !LongDeliveryNumber.HasValue && Parts==RobotParts.Complete && !Disposal && AssemblyCredit(Owner)>0;
}
public sealed class RobotStore
{
    public const int Capacity=12;
    private int? _capacity;
    private int? _warehouseLimit;
    private bool _logisticsRules,_relayWarehouse;
    private bool _flagWarehouseRules,_materialWarehouseRules;
    internal void EnableFlagWarehouseRules()=>_flagWarehouseRules=true;
    internal void SetWarehouseEquipmentMode(RobotEquipmentMode mode)=>_materialWarehouseRules=mode==RobotEquipmentMode.Materials;
    internal bool UsesWarehouseMaterials=>_flagWarehouseRules && _materialWarehouseRules;
    internal int WarehouseContributionThirds(Robot robot)
    {
        if(!_flagWarehouseRules)return WarehouseExpansion*3;
        int flags=robot.LargestFlagGroup;
        int thirds=3*(1+(_relayWarehouse?2:1)*flags);
        if(!UsesWarehouseMaterials || flags==0 || robot.Parts!=RobotParts.Complete)return thirds;
        // Paper preserves the original flag bonus. Better materials add to it in thirds.
        return thirds+RobotMaterials.Parts.Sum(part=>RobotMaterials.WarehouseThirds(robot.Material(part))-3);
    }
    internal int DevelopedWarehouseCapacity(IEnumerable<Robot> robots,int limit)=>
        Math.Min(limit,1+robots.Where(r=>r.WarehouseKeeper).Sum(WarehouseContributionThirds)/3);
    public int WarehouseExpansion=>_relayWarehouse?7:4;
    internal int WarehouseMaximum=>_logisticsRules && !_relayWarehouse?30:60;
    internal bool LogisticsRules=>_logisticsRules;
    internal void SetLogisticsRules(bool relay){_logisticsRules=true;_relayWarehouse=relay;if(_warehouseLimit is int limit)_warehouseLimit=Math.Min(limit,WarehouseMaximum);}
    public int? WarehouseLimit=>_warehouseLimit;
    public int TotalCapacity=>CapacityFor(_robots);
    internal void SetWarehouseLimit(int limit)=>_warehouseLimit=limit;
    internal int CapacityFor(IEnumerable<Robot> robots)=>_warehouseLimit is int limit && _capacity!=0
        ?DevelopedWarehouseCapacity(robots,limit):_capacity??Capacity;
    internal bool Fits(IEnumerable<Robot> robots,IEnumerable<MaplePackage> packages,int reserved,int? capacity=null)
        =>OccupiedSlots(robots,packages)+reserved<=(capacity??CapacityFor(robots));
    internal void SetCapacity(int capacity){if(capacity<Count+ReservedReturns)throw new InvalidOperationException("Occupied hangar capacity.");_capacity=capacity;}
    private readonly List<Robot> _robots=new();
    public IReadOnlyList<Robot> Robots=>_robots;
    internal int ScrapNode=-1,ScrapTurn;
    private MaplePackage[] _maplePackages;
    public IReadOnlyList<MaplePackage> MaplePackages=>_maplePackages??Array.Empty<MaplePackage>();
    public int MapleUnits=>MaplePackages.Where(p=>p.Material==RawMaterial.Maple).Sum(p=>p.Units);
    public int Count=>MaplePackages.Count==0?_robots.Count:OccupiedSlots(_robots);
    internal int OccupiedSlots(IEnumerable<Robot> robots)=>OccupiedSlots(robots,MaplePackages);
    private static int OccupiedSlots(IEnumerable<Robot> robots,IEnumerable<MaplePackage> packages)
    {
        int scrap=packages.Where(p=>p.Material==RawMaterial.Scrap).Sum(p=>p.Sections);
        packages=packages.Where(p=>p.Material!=RawMaterial.Scrap);
        int singles=0,pairs=0,whole=0;
        foreach(var package in packages)
            switch(package.Sections){case 1:singles++;break;case 2:pairs++;break;case 3:whole++;break;}
        int count=0,openSections=0;
        foreach(var robot in robots){
            count++;
            int mask=(int)robot.Parts;
            openSections+=3-System.Numerics.BitOperations.PopCount((uint)mask);
            if(pairs>0 && robot.Parts is RobotParts.Head or RobotParts.Legs){pairs--;openSections-=2;}
        }
        return count+whole+pairs+(Math.Max(0,singles-openSections-pairs)+2)/3+(scrap+2)/3;
    }
    internal bool HasMapleSpace(MaplePackage package,int reserved)=>
        OccupiedSlots(_robots,MaplePackages.Append(package))<=StorageLimit-reserved;
    private readonly HashSet<long> _returnReservations=new();
    public int ReservedReturns=>_returnReservations.Count;
    public int StorageLimit=>TotalCapacity-ReservedReturns;
    public bool HasSpace=>Count<StorageLimit;
    internal Robot Depart(long id){var robot=Remove(id);_returnReservations.Add(id);return robot;}
    internal void ReleaseReturn(long id)=>_returnReservations.Remove(id);
    internal void ReserveStorage(long token){if(!HasSpace)throw new InvalidOperationException("Pocket capacity is already in use.");_returnReservations.Add(token);}
    public void Add(Robot robot)
    {
        if(!robot.ValidIntegration)throw new InvalidOperationException("Invalid integrated parts.");
        RobotWorld.Validate(robot.Owner,robot.Parts);
        if(Count>=StorageLimit || _robots.Any(r=>r.Id==robot.Id))throw new InvalidOperationException("Robot store is full or ID is duplicated.");
        _robots.Add(robot);
    }
    public Robot Remove(long id)
    {
        var robot=_robots.Single(r=>r.Id==id);
        if(robot.WarehouseKeeper)throw new InvalidOperationException("Release the warehouse keeper reservation first.");
        _robots.Remove(robot);return robot;
    }
    internal void Replace(IEnumerable<Robot> robots)
    {
        var next=robots.ToArray();
        if(!Fits(next,MaplePackages,ReservedReturns) || next.Any(r=>!r.ValidIntegration) || next.Select(r=>r.Id).Distinct().Count()!=next.Length)throw new InvalidOperationException("Invalid robot store.");
        _robots.Clear();_robots.AddRange(next);
    }
    internal RobotStore PlanningCopy()
    {
        var result=new RobotStore{_capacity=_capacity,_warehouseLimit=_warehouseLimit,_logisticsRules=_logisticsRules,_relayWarehouse=_relayWarehouse,_flagWarehouseRules=_flagWarehouseRules,_materialWarehouseRules=_materialWarehouseRules,ScrapNode=ScrapNode,ScrapTurn=ScrapTurn};foreach(long id in _returnReservations)result._returnReservations.Add(id);result.Replace(Robots);result._maplePackages=_maplePackages;return result;
    }
    internal void SetMaplePackages(MaplePackage[] packages)=>_maplePackages=packages;
    internal bool TryReceiveMaple(MaplePackage package,int reserved,Func<long,bool> shipping)
    {
        package=ScrapCargo.Arrive(package,ScrapNode,ScrapTurn);
        var packages=MaplePackages.ToArray();
        int remaining=package.Units;
        for(int i=0;i<packages.Length && remaining>0;i++)
        {
            var current=packages[i];
            if(package.Material!=current.Material || package.Sections>1 || current.Sections>1 || shipping(current.Id))continue;
            int added=Math.Min(current.MergeCapacity-current.Units,remaining);
            packages[i]=package.Material==RawMaterial.Scrap?ScrapCargo.Join(current,ScrapCargo.Portion(package,package.Units-remaining,added)):current with {Units=current.Units+added};
            remaining-=added;
        }
        if(remaining>0 && !HasMapleSpace(package with {Units=remaining},reserved))return false;
        SetMaplePackages(remaining==0?packages:packages.Append(package.Material==RawMaterial.Scrap?ScrapCargo.Portion(package,package.Units-remaining,remaining):package with {Units=remaining}).ToArray());
        return true;
    }
}
public sealed partial class RobotWorld
{
    private long _nextId=1;
    public RobotStore[] Nodes {get;private set;}=Array.Empty<RobotStore>();
    public RobotStore[] Retinues {get;private set;}=Array.Empty<RobotStore>();
    public RobotWorkshop[] Workshops {get;private set;}=Array.Empty<RobotWorkshop>();
    public RobotTransport Transport {get;}=new();
    public RobotDirectDelivery DirectDelivery {get;private set;}=new();
    public RobotTraining[] Training {get;private set;}=Array.Empty<RobotTraining>();
    public List<RobotWorkshopEdit> PendingEdits {get;}=new();
    public List<(int Node,string Message)> WorkshopFailures {get;}=new();
    public static void Validate(int owner,RobotParts parts)
    {
        if(owner<0 || owner>=GameLimits.MaxConquerors || (int)parts<1 || (int)parts>7)throw new ArgumentOutOfRangeException();
    }
    public Robot Create(int owner,RobotParts parts,RobotRole role=RobotRole.Soldier,bool disposal=false)
    {Validate(owner,parts);return new(_nextId++,owner,parts,role,disposal);}
    public void Initialize(int nodeCount,int rulerCount)
    {
        _mapleHarvested=null;
        Discarding.Reset();ScrapRulesEnabled=false;LongDelivery.Reset();ReturningRobots.Clear();_workerPlanWorldSeed=0;DirectDelivery=new();Reservations=null;ReservationSource=null;IndustryTurn=0;DomesticPersuasionTurns.Clear();
        _domesticPreparedTurn=-1;Training=Enumerable.Range(0,rulerCount).Select(_=>new RobotTraining()).ToArray();_nextId=1;Nodes=Enumerable.Range(0,nodeCount).Select(_=>new RobotStore()).ToArray();
        Retinues=Enumerable.Range(0,rulerCount).Select(_=>new RobotStore()).ToArray();
        Workshops=Enumerable.Range(0,nodeCount).Select(_=>new RobotWorkshop()).ToArray();Transport.Reset();PendingEdits.Clear();WorkshopFailures.Clear();
        PartPlanning.Reset();ProductionIntakes.Clear();ProductionIntakeEnabled=false;IntakeAdjacent=null;IntakeAllowed=null;
        _mapleDeposits=null;_mapleShipments=null;_rawDeposits=null;
    }
    public void QueueAssembly(int node,IEnumerable<long> ids,int? owner=null)=>QueueEdit(new(node,ids.ToArray(),null,owner));
    public void QueueSplit(int node,long id,RobotParts part)=>QueueEdit(new(node,new[]{id},part));
    private void QueueEdit(RobotWorkshopEdit edit)
    {
        if(edit.Node<0 || edit.Node>=Nodes.Length || edit.Ids.Length<(edit.SplitPart==null?2:1) || edit.Ids.Distinct().Count()!=edit.Ids.Length)throw new ArgumentException("Invalid workshop order.");
        var robots=edit.Ids.Select(id=>Nodes[edit.Node].Robots.Single(r=>r.Id==id)).ToArray();
        if(robots.Any(r=>AssemblyLocked(edit.Node,r)))throw new InvalidOperationException("Robot is already reserved or belongs to another owner.");
        if(edit.SplitPart is RobotParts part)
        {Validate(robots[0].Owner,part);if(!robots[0].CanSeparate(part) || robots[0].Parts==part)throw new InvalidOperationException("Invalid split parts.");}
        else{var parts=(RobotParts)0;foreach(var robot in robots){if((parts&robot.Parts)!=0)throw new InvalidOperationException("Duplicate parts.");parts|=robot.Parts;}}
        PendingEdits.Add(edit);
    }
    public Robot Assemble(int node,IEnumerable<long> ids,int? owner=null)
    {
        var store=Nodes[node];var selected=ids.ToArray();
        if(selected.Length<2 || selected.Distinct().Count()!=selected.Length)throw new ArgumentException("Select distinct robots.");
        var robots=selected.Select(id=>store.Robots.Single(r=>r.Id==id)).ToArray();var parts=(RobotParts)0;
        if(robots.Any(r=>AssemblyLocked(node,r)))throw new InvalidOperationException("Robot is reserved.");
        foreach(var robot in robots)
        {if(robot.Disposal!=robots[0].Disposal || (parts&robot.Parts)!=0)throw new InvalidOperationException("Parts must be disjoint and have the same owner.");parts|=robot.Parts;}
        int affiliation=owner??robots[0].Owner;var merged=robots.OrderBy(r=>r.Id).Aggregate((a,b)=>a.Merge(b,affiliation)) with {Role=robots.Any(r=>r.Role==RobotRole.Captain)?RobotRole.Captain:RobotRole.Soldier};
        store.Replace(store.Robots.Where(r=>!selected.Contains(r.Id)).Append(merged));return merged;
    }
    public void Split(int node,long id,RobotParts first)
    {
        var store=Nodes[node];var robot=store.Robots.Single(r=>r.Id==id);Validate(robot.Owner,first);
        if(AssemblyLocked(node,robot))throw new InvalidOperationException("Robot is reserved.");
        if(!robot.CanSeparate(first) || first==robot.Parts || !store.HasSpace)throw new InvalidOperationException("Cannot split this robot.");
        var remainder=robot.Slice(robot.Parts^first) with {Id=Create(robot.Owner,robot.Parts^first,disposal:robot.Disposal).Id};
        store.Replace(store.Robots.Where(r=>r.Id!=id).Append(robot.Slice(first)).Append(remainder));
    }
    public int IndustryTurn {get;private set;}
    // One simulation step is 1/3 displayed industry turn.
    public void AdvanceIndustry(Func<int,int> owner,Func<int,bool> locked=null)
    {
        _collectInterrupted?.Invoke();
        for(int n=0;n<Nodes.Length;n++)if(locked?.Invoke(n)!=true)SyncWorkers(n);
        for(int n=0;n<Nodes.Length;n++)if(locked?.Invoke(n)!=true){Workshops[n].StartDisposal(Nodes[n],id=>PendingEdits.Any(e=>e.Ids.Contains(id)) || DirectDelivery.LockedParts(n,id)!=0 || PartPlanning.ReservesRobot(id) || Discarding.Reserved(id));Workshops[n].CompleteDisposal(Nodes[n],this,n);}
        for(int n=0;n<Nodes.Length;n++)if(locked?.Invoke(n)!=true){Workshops[n].CompleteProduction(this,n,owner(n));Workshops[n].StartDisposal(Nodes[n],id=>PendingEdits.Any(e=>e.Ids.Contains(id)) || DirectDelivery.LockedParts(n,id)!=0 || PartPlanning.ReservesRobot(id) || Discarding.Reserved(id));if(owner(n)>=0 && !Workshops[n].AutoAssemblyDisabled)AutoAssemble(n,owner(n));}
        for(int n=0;n<Nodes.Length;n++)if(locked?.Invoke(n)!=true)SyncWorkers(n);IndustryTurn++;Check();
    }
    public void Advance(Func<int,int,bool> connected,Func<int,int> owner,int seed,Func<int,bool> locked=null,Func<int,int,bool> canEnter=null,bool industry=true,Func<int,bool> directLocked=null)
    {
        if(industry)for(int n=0;n<Nodes.Length;n++)if(locked?.Invoke(n)!=true){Workshops[n].StartDisposal(Nodes[n],id=>PendingEdits.Any(e=>e.Ids.Contains(id)) || DirectDelivery.LockedParts(n,id)!=0 || PartPlanning.ReservesRobot(id) || Discarding.Reserved(id));Workshops[n].CompleteDisposal(Nodes[n],this,n);}
        WorkshopFailures.Clear();
        foreach(var edit in PendingEdits.ToArray())
        {
            if(locked?.Invoke(edit.Node)==true)continue;
            PendingEdits.Remove(edit);
            try{if(edit.SplitPart is RobotParts part)Split(edit.Node,edit.Ids.Single(),part);else Assemble(edit.Node,edit.Ids,edit.Owner);}
            catch(InvalidOperationException error){WorkshopFailures.Add((edit.Node,error.Message));}
        }
        var arrivals=DirectDelivery.Resolve(this,connected,directLocked??locked,canEnter);
        // Long-term transport chooses the stock remaining after direct deliveries and assembly.
        _arrivalLocked=id=>arrivals.Contains(id);
        try{
            for(int n=0;n<Nodes.Length;n++)if(locked?.Invoke(n)!=true && owner(n)>=0 && !Workshops[n].AutoAssemblyDisabled)AutoAssemble(n,owner(n));
            Transport.ResolvePlans(this,connected,seed,locked,canEnter);
        }finally{_arrivalLocked=null;}
        if(industry)for(int n=0;n<Nodes.Length;n++)if(locked?.Invoke(n)!=true)Workshops[n].CompleteProduction(this,n,owner(n));
        if(industry)for(int n=0;n<Nodes.Length;n++)if(locked?.Invoke(n)!=true)Workshops[n].StartDisposal(Nodes[n],id=>PendingEdits.Any(e=>e.Ids.Contains(id)) || DirectDelivery.LockedParts(n,id)!=0 || PartPlanning.ReservesRobot(id) || Discarding.Reserved(id));
        for(int n=0;n<Nodes.Length;n++)if(locked?.Invoke(n)!=true && owner(n)>=0 && !Workshops[n].AutoAssemblyDisabled)AutoAssemble(n,owner(n));
        for(int n=0;n<Nodes.Length;n++)if(locked?.Invoke(n)!=true)SyncWorkers(n);
        if(industry)IndustryTurn++;Check();
    }
    private void AutoAssemble(int node,int owner)
    {
        while(AssemblySelection(node,owner) is {} selection)Assemble(node,selection,owner);
    }
    internal bool TryStoreProduction(int node,int owner,Robot product,int reservedSlots)
    {
        var store=Nodes[node];int limit=store.StorageLimit-reservedSlots;
        bool StoreComplete(Robot[] partners){
            var merged=partners.Aggregate((a,b)=>a.Merge(b,owner)).Merge(product,owner);
            if(merged.Parts!=RobotParts.Complete || merged.AssemblyCredit(owner)<Training[owner].RequiredNative)return false;
            merged=merged with {Role=partners.Any(r=>r.Role==RobotRole.Captain)?RobotRole.Captain:RobotRole.Soldier};
            var ids=partners.Select(r=>r.Id).ToHashSet();
            var next=store.Robots.Where(r=>!ids.Contains(r.Id)).Append(merged).ToArray();
            if(store.OccupiedSlots(next)>limit)return false;
            store.Replace(next);
            return true;
        }
        if(!Workshops[node].AutoAssemblyDisabled && product.Parts!=RobotParts.Complete){
            var parts=store.Robots.Where(r=>r.Parts!=RobotParts.Complete && (r.Parts&product.Parts)==0 && !AssemblyLocked(node,r)).OrderBy(r=>r.Id).ToArray();
            for(int a=0;a<parts.Length;a++){
                if((parts[a].Parts|product.Parts)==RobotParts.Complete && StoreComplete(new[]{parts[a]}))return true;
                for(int b=a+1;b<parts.Length;b++)
                    if((parts[a].Parts&parts[b].Parts)==0 && (parts[a].Parts|parts[b].Parts|product.Parts)==RobotParts.Complete && StoreComplete(new[]{parts[a],parts[b]}))return true;
            }
        }
        if(store.Count>=limit)return false;
        store.Add(product with {Id=Create(owner,product.Parts).Id});return true;
    }
    public long[] AssemblySelection(int node,int owner,bool deployable=false)
    {
        var policy=Training[owner];
            var parts=Nodes[node].Robots.Where(r=>r.Parts!=RobotParts.Complete && !AssemblyLocked(node,r)).OrderBy(r=>r.Id).ToArray();
            long[] selection=null;
            for(int a=0;a<parts.Length && selection==null;a++)for(int b=a+1;b<parts.Length && selection==null;b++){
                if((parts[a].Parts&parts[b].Parts)!=0)continue;
                var merged=parts[a].Merge(parts[b],owner);
                if(merged.Parts==RobotParts.Complete && merged.AssemblyCredit(owner)>=policy.RequiredNative && (!deployable || policy.Eligible(merged))){selection=new[]{parts[a].Id,parts[b].Id};break;}
                for(int c=b+1;c<parts.Length;c++)if((merged.Parts&parts[c].Parts)==0){var complete=merged.Merge(parts[c],owner);if(complete.Parts==RobotParts.Complete && complete.AssemblyCredit(owner)>=policy.RequiredNative && (!deployable || policy.Eligible(complete))){selection=new[]{parts[a].Id,parts[b].Id,parts[c].Id};break;}}
            }
            return selection;
    }
    public void Check()
    {
        var all=Nodes.Concat(Retinues).SelectMany(s=>s.Robots).Concat(ProductionIntakes.Select(o=>o.Product)).Concat(ReturningRobots.Select(j=>j.Robot)).ToArray();
        if(all.Select(r=>r.Id).Distinct().Count()!=all.Length)throw new InvalidOperationException("Robot has multiple locations.");
        if(MapleSupplyEnabled){
            var ids=all.Select(r=>r.Id).ToHashSet();
            foreach(var package in Nodes.SelectMany(s=>s.MaplePackages))
                if(package.Units<1 || package.Units>package.UnitCapacity || package.Material==RawMaterial.Scrap && ScrapCargo.Lots(package).Sum(l=>l.Units)!=package.Units || !ids.Add(package.Id))throw new InvalidOperationException("Invalid maple cargo.");
        }
        foreach(var store in Nodes.Concat(Retinues))if(store.Count>store.StorageLimit)throw new InvalidOperationException("Hangar return capacity exceeded.");
        foreach(var robot in all){Validate(robot.Owner,robot.Parts);if(!robot.ValidIntegration)throw new InvalidOperationException("Invalid integrated parts.");}
    }
}
