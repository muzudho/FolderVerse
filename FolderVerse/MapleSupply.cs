namespace FolderVerse;
using System;
using System.Collections.Generic;
using System.Linq;

public enum RawMaterial { Maple, Styrene, Vinyl, PP, Silicone }

public sealed record MaplePackage(long Id,int Units)
{
    public RawMaterial Material {get;init;}
    public const int Capacity=48;
    public int Sections=>(Units+RobotWorld.MaplePackageSize-1)/RobotWorld.MaplePackageSize;
}
public sealed record MapleShipment(int Owner,int Source,int Target,long PackageId);

public sealed partial class RobotWorld
{
    public const int MaplePackageSize=16;
    private int[] _mapleDeposits;
    private int[][] _rawDeposits;
    private int[] _mapleHarvested;
    private List<MapleShipment> _mapleShipments;
    public bool MapleSupplyEnabled=>_mapleDeposits!=null;
    public IReadOnlyList<MapleShipment> MapleShipments=>_mapleShipments??(IReadOnlyList<MapleShipment>)Array.Empty<MapleShipment>();
    public int MapleDeposit(int node)=>_mapleDeposits?[node]??0;
    public int MaplePlantingCount(int node)=>(_mapleHarvested?[node]??0) switch
    {
        0=>0,
        <4=>1,
        <8=>2,
        _=>3
    };

    internal void InitializeMapleSupply(WorldSetup world)
    {
        if(world.EquipmentMode!=RobotEquipmentMode.Materials)return;
        _mapleDeposits=GenerateMapleDeposits(world);
        _mapleShipments=new();
        InitializeRawDeposits(world);
    }

    internal static int[] GenerateMapleDeposits(WorldSetup world)
    {
        var deposits=new int[world.Nodes.All.Length];
        var offset=world.Terrain;
        var woodland=world.Nodes.All.Where(n=>n.Terrain==TravelTerrain.Land && !n.IsHarbor).Where(node=>
        {
            var cell=world.Cells[node.Cell];
            return node.Points.Any(p=>WorldTerrain.Kind(offset.Elevation(
                cell.Origin+cell.U*((p.X+.5f)/10)+cell.V*((p.Y+.5f)/10)))==TerrainKind.Grassland);
        }).ToArray();
        foreach(var node in woodland)
        {
            var random=new SeedRandom(world.WorldSeed^node.Id*7919^0x4d41504c);
            bool scattered=random.Next(3)==0;
            int baseline=scattered?4+random.Next(9):0;
            var cell=world.Cells[node.Cell];
            bool coolValley=node.Points.Any(p=>WorldTerrain.IsCoolValley(world,cell,world.Routes.Position(node.Cell,p)));
            bool equatorial=WorldTerrain.RelativeLatitude(world,world.Routes.Position(node.Cell,node.Center))<.25f;
            deposits[node.Id]=coolValley?13+random.Next(4):equatorial?Math.Min(baseline,1+baseline%3):baseline;
        }
        if(woodland.Length>0 && !deposits.Any(p=>p>0))deposits[woodland[0].Id]=3;
        return deposits;
    }

    public int MapleAvailableToPlan(int node,RobotWorkshop editing,RawMaterial material=RawMaterial.Maple)
        =>Math.Max(0,RawDeposit(node,material)-Workshops[node].ProductionLines
            .Where(l=>!ReferenceEquals(l,editing) && l.HarvestMaterial==material).Sum(l=>l.MapleRemaining));

    private int MapleReservedSlots(int node)=>ProductionIntakes.Count(i=>(i.Target??i.Source)==node)+Workshops[node].ReservedSlots;
    internal bool CanHarvestMaple(int node,int loads,RawMaterial material=RawMaterial.Maple)
        =>RawDeposit(node,material)>=loads && Nodes[node].HasMapleSpace(new MaplePackage(0,loads*MaplePackageSize){Material=material},MapleReservedSlots(node));

    internal bool HarvestMaple(int node,int loads,RawMaterial material=RawMaterial.Maple)
    {
        if(!CanHarvestMaple(node,loads,material))return false;
        var store=Nodes[node];
        int reserved=ProductionIntakes.Count(i=>(i.Target??i.Source)==node)+Workshops[node].ReservedSlots;
        if(!store.TryReceiveMaple(new MaplePackage(_nextId,loads*MaplePackageSize){Material=material},reserved,id=>MapleShipping(node,id)))return false;
        _nextId++;
        if(material!=RawMaterial.Maple){_rawDeposits[(int)material-1][node]-=loads;return true;}
        _mapleDeposits[node]-=loads;
        (_mapleHarvested??=new int[Nodes.Length])[node]+=loads;
        return true;
    }

    internal void TransferMapleFunctions(int source,int target)
    {
        Nodes[target].SetMaplePackages(Nodes[target].MaplePackages.Concat(Nodes[source].MaplePackages).ToArray());
        Nodes[source].SetMaplePackages(Array.Empty<MaplePackage>());
        if(!MapleSupplyEnabled)return;
        _mapleDeposits[source]=0;
        if(_rawDeposits!=null)foreach(var deposits in _rawDeposits)deposits[source]=0;
        _mapleShipments.RemoveAll(o=>o.Source==source || o.Target==source);
    }

    public static int MapleCost(Robot product)=>RobotMaterials.Parts.Where(p=>(product.Parts&p)!=0).Sum(p=>product.Material(p) switch
    {
        RobotMaterial.Paper=>1,
        RobotMaterial.Cardboard=>4,
        RobotMaterial.Maple=>MaplePackageSize,
        _=>0
    });

    internal int AvailableMaple(int node)=>AvailableRaw(node,RawMaterial.Maple);
    internal int AvailableRaw(int node,RawMaterial material)=>Nodes[node].MaplePackages.Where(p=>p.Material==material).Sum(p=>p.Units)-(_mapleShipments?
        .Where(o=>o.Source==node).Sum(o=>Nodes[node].MaplePackages.FirstOrDefault(p=>p.Id==o.PackageId && p.Material==material)?.Units??0)??0);

    private bool MapleShipping(int node,long id)=>_mapleShipments.Any(o=>o.Source==node && o.PackageId==id);

    internal bool RearrangeMaple(WorldSetup world,int node,long sourceId,long? targetId,bool split)
    {
        if(!MapleSupplyEnabled || world.Campaign.Phase!=CampaignPhase.Domestic ||
            !world.Relations.Controls(world.Nodes.All[node].Owner) || world.Campaign.RobotBattles.LockedNode(node))return false;
        var store=Nodes[node];
        var source=store.MaplePackages.FirstOrDefault(p=>p.Id==sourceId);
        if(source==null || source.Sections!=1 || MapleShipping(node,sourceId))return false;
        var target=store.MaplePackages.FirstOrDefault(p=>p.Id==targetId);
        if(split){
            if(targetId.HasValue || source.Units<2 || !store.HasMapleSpace(new MaplePackage(0,1){Material=source.Material},MapleReservedSlots(node)))return false;
            int moved=source.Units/2;
            store.SetMaplePackages(store.MaplePackages.Select(p=>p.Id==sourceId?p with {Units=p.Units-moved}:p)
                .Append(new MaplePackage(_nextId++,moved){Material=source.Material}).ToArray());
            return true;
        }
        if(target==null || target.Id==sourceId || target.Material!=source.Material || target.Sections!=1 || MapleShipping(node,target.Id))return false;
        int added=Math.Min(source.Units,MaplePackageSize-target.Units);
        if(added==0)return false;
        store.SetMaplePackages(store.MaplePackages.Select(p=>p.Id==target.Id?p with {Units=p.Units+added}:
            p.Id==sourceId?p with {Units=p.Units-added}:p).Where(p=>p.Units>0).ToArray());
        return true;
    }

    internal void SpendMaple(int node,int units,RawMaterial material=RawMaterial.Maple)
    {
        var store=Nodes[node];
        var packages=store.MaplePackages.ToArray();
        for(int i=0;i<packages.Length && units>0;i++)
        {
            var pack=packages[i];
            if(pack.Material!=material || MapleShipping(node,pack.Id))continue;
            int spent=Math.Min(pack.Units,units);
            packages[i]=pack with {Units=pack.Units-spent};
            units-=spent;
        }
        if(units!=0)throw new InvalidOperationException("Insufficient maple stock.");
        store.SetMaplePackages(packages.Where(p=>p.Units>0).ToArray());
    }

    public bool ReserveMapleShipment(WorldSetup world,int source,int target,long packageId)
    {
        if(!MapleSupplyEnabled || world.Campaign.Phase!=CampaignPhase.Domestic || source<0 || source>=Nodes.Length || target<0 || target>=Nodes.Length)return false;
        int owner=world.Nodes.All[source].Owner;
        var package=Nodes[source].MaplePackages.FirstOrDefault(p=>p.Id==packageId);
        if(!world.Relations.Controls(owner) || !CanDeliverMaple(world,owner,source,target) || package==null)return false;
        if(package.Sections>1){
            var targetStore=Nodes[target].PlanningCopy();
            foreach(var order in MapleShipments.Where(o=>o.Target==target && o.PackageId!=packageId)){
                var pending=Nodes[order.Source].MaplePackages.FirstOrDefault(p=>p.Id==order.PackageId);
                if(pending!=null)targetStore.TryReceiveMaple(pending,MapleReservedSlots(target),id=>MapleShipping(target,id));
            }
            if(!targetStore.HasMapleSpace(package,MapleReservedSlots(target)))return false;
        }
        CancelMapleShipment(source,packageId);
        _mapleShipments.Add(new(owner,source,target,packageId));
        return true;
    }

    public bool CancelMapleShipment(int source,long packageId)=>_mapleShipments?.RemoveAll(o=>o.Source==source && o.PackageId==packageId)>0;

    private static bool CanDeliverMaple(WorldSetup world,int owner,int source,int target)=>
        source!=target && owner>=0 && world.Nodes.All[source].Owner==owner && world.Nodes.All[target].Owner>=0 &&
        !world.Campaign.RobotBattles.LockedNode(source) && FunctionalDeliveryRoutes.Connected(world,source,target);

    internal void ResolveMapleShipments(WorldSetup world)
    {
        if(!MapleSupplyEnabled)return;
        foreach(var order in _mapleShipments.ToArray())
        {
            var source=Nodes[order.Source];var target=Nodes[order.Target];
            var pack=source.MaplePackages.FirstOrDefault(p=>p.Id==order.PackageId);
            if(pack==null || world.Nodes.All[order.Source].Owner!=order.Owner){_mapleShipments.Remove(order);continue;}
            if(!CanDeliverMaple(world,order.Owner,order.Source,order.Target))continue;
            if(!target.TryReceiveMaple(pack,MapleReservedSlots(order.Target),id=>MapleShipping(order.Target,id)))continue;
            source.SetMaplePackages(source.MaplePackages.Where(p=>p.Id!=pack.Id).ToArray());
            _mapleShipments.Remove(order);
        }
    }

    internal (int Blocked,MaplePackage[] Packages) PreviewMapleArrivals(WorldSetup world,int node,RobotStore forecast)
    {
        var target=forecast.PlanningCopy();
        int blocked=0;
        // The hangar forecast already includes manufactured arrivals.
        int reserved=Workshops[node].ReservedSlots;
        foreach(var order in MapleShipments.Where(o=>o.Target==node)){
            var package=Nodes[order.Source].MaplePackages.FirstOrDefault(p=>p.Id==order.PackageId);
            if(package==null || !CanDeliverMaple(world,order.Owner,order.Source,node) ||
                !target.TryReceiveMaple(package,reserved,id=>MapleShipping(node,id)))blocked++;
        }
        return (blocked,target.MaplePackages.ToArray());
    }
}
