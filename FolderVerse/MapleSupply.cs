namespace FolderVerse;
using System;
using System.Collections.Generic;
using System.Linq;

public sealed record MaplePackage(long Id,int Units);
public sealed record MapleShipment(int Owner,int Source,int Target,long PackageId);

public sealed partial class RobotWorld
{
    public const int MaplePackageSize=16;
    private int[] _mapleDeposits;
    private List<MapleShipment> _mapleShipments;
    public bool MapleSupplyEnabled=>_mapleDeposits!=null;
    public IReadOnlyList<MapleShipment> MapleShipments=>_mapleShipments??(IReadOnlyList<MapleShipment>)Array.Empty<MapleShipment>();
    public int MapleDeposit(int node)=>_mapleDeposits?[node]??0;

    internal void InitializeMapleSupply(WorldSetup world)
    {
        if(world.EquipmentMode!=RobotEquipmentMode.Materials)return;
        _mapleDeposits=new int[Nodes.Length];
        _mapleShipments=new();
        var offset=WorldTerrain.Offset(world.WorldSeed);
        var woodland=world.Nodes.All.Where(n=>n.Terrain==TravelTerrain.Land && !n.IsHarbor).Where(node=>
        {
            var cell=world.Cells[node.Cell];
            return node.Points.Any(p=>WorldTerrain.Kind(WorldTerrain.Elevation(
                cell.Origin+cell.U*((p.X+.5f)/10)+cell.V*((p.Y+.5f)/10),offset))==TerrainKind.Grassland);
        }).ToArray();
        foreach(var node in woodland)
        {
            var random=new SeedRandom(world.WorldSeed^node.Id*7919^0x4d41504c);
            if(random.Next(3)==0)_mapleDeposits[node.Id]=4+random.Next(9);
        }
        if(woodland.Length>0 && !_mapleDeposits.Any(p=>p>0))_mapleDeposits[woodland[0].Id]=4;
    }

    public int MapleAvailableToPlan(int node,RobotWorkshop editing)
        =>Math.Max(0,MapleDeposit(node)-Workshops[node].ProductionLines.Where(l=>!ReferenceEquals(l,editing)).Sum(l=>l.MapleRemaining));

    internal bool CanHarvestMaple(int node)
        =>MapleDeposit(node)>0 && Nodes[node].HasSpace &&
            Nodes[node].Count+ProductionIntakes.Count(i=>(i.Target??i.Source)==node)<Nodes[node].StorageLimit-Workshops[node].ReservedSlots;

    internal bool HarvestMaple(int node)
    {
        if(!CanHarvestMaple(node))return false;
        var store=Nodes[node];
        int reserved=ProductionIntakes.Count(i=>(i.Target??i.Source)==node)+Workshops[node].ReservedSlots;
        if(!store.TryReceiveMaple(new(_nextId,MaplePackageSize),reserved,id=>MapleShipping(node,id)))return false;
        _nextId++;
        _mapleDeposits[node]--;
        return true;
    }

    internal void TransferMapleFunctions(int source,int target)
    {
        Nodes[target].SetMaplePackages(Nodes[target].MaplePackages.Concat(Nodes[source].MaplePackages).ToArray());
        Nodes[source].SetMaplePackages(Array.Empty<MaplePackage>());
        if(!MapleSupplyEnabled)return;
        _mapleDeposits[source]=0;
        _mapleShipments.RemoveAll(o=>o.Source==source || o.Target==source);
    }

    public static int MapleCost(Robot product)=>RobotMaterials.Parts.Where(p=>(product.Parts&p)!=0).Sum(p=>product.Material(p) switch
    {
        RobotMaterial.Paper=>1,
        RobotMaterial.Cardboard=>4,
        RobotMaterial.Maple=>MaplePackageSize,
        _=>0
    });

    internal int AvailableMaple(int node)=>Nodes[node].MapleUnits-(_mapleShipments?
        .Where(o=>o.Source==node).Sum(o=>Nodes[node].MaplePackages.FirstOrDefault(p=>p.Id==o.PackageId)?.Units??0)??0);

    private bool MapleShipping(int node,long id)=>_mapleShipments.Any(o=>o.Source==node && o.PackageId==id);

    internal void SpendMaple(int node,int units)
    {
        var store=Nodes[node];
        var packages=store.MaplePackages.ToArray();
        for(int i=0;i<packages.Length && units>0;i++)
        {
            var pack=packages[i];
            if(MapleShipping(node,pack.Id))continue;
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
        if(!world.Relations.Controls(owner) || !CanDeliverMaple(world,owner,source,target) || !Nodes[source].MaplePackages.Any(p=>p.Id==packageId))return false;
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
            int reserved=ProductionIntakes.Count(i=>(i.Target??i.Source)==order.Target)+Workshops[order.Target].ReservedSlots;
            if(!target.TryReceiveMaple(pack,reserved,id=>MapleShipping(order.Target,id)))continue;
            source.SetMaplePackages(source.MaplePackages.Where(p=>p.Id!=pack.Id).ToArray());
            _mapleShipments.Remove(order);
        }
    }
}
