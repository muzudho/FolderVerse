namespace FolderVerse;
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;

public sealed partial class RobotWorld
{
    public static readonly IReadOnlyList<RawMaterial> RawMaterials = Array.AsReadOnly(Enum.GetValues<RawMaterial>());
    public static string RawName(RawMaterial material)=>material switch
    {
        RawMaterial.Styrene=>"スチレン系樹脂",
        RawMaterial.Vinyl=>"軟質ビニール原料",
        RawMaterial.PP=>"PP樹脂ペレット",
        RawMaterial.Silicone=>"シリコーン生地",
        _=>"楓の木"
    };

    internal bool OtherRawSupplyEnabled=>_rawDeposits!=null;
    public int RawDeposit(int node,RawMaterial material)=>material==RawMaterial.Maple
        ?MapleDeposit(node):_rawDeposits?[(int)material-1][node]??0;

    internal bool HadRawDeposit(WorldSetup world,Node node,RawMaterial material)
    {
        if(material==RawMaterial.Maple)return (_mapleHarvested?[node.Id]??0)>0;
        var cell=world.Cells[node.Cell];
        return CubeNet.ResourceSites(world,cell,world.Terrain).Any(site=>
            SiteMaterial(site.Kind)==material && SiteNode(world,cell,site.Position)?.Id==node.Id);
    }

    internal static RawMaterial SiteMaterial(CubeNet.ResourceSiteKind kind)=>kind switch
    {
        CubeNet.ResourceSiteKind.Salt=>RawMaterial.Vinyl,
        CubeNet.ResourceSiteKind.Oil=>RawMaterial.Styrene,
        CubeNet.ResourceSiteKind.Gas=>RawMaterial.PP,
        _=>RawMaterial.Silicone
    };

    internal static Node SiteNode(WorldSetup world,SurfaceCell cell,Vector3 position)
    {
        var local=position-cell.Origin;
        return world.Nodes.At(cell.Id,new Point((int)(Vector3.Dot(local,cell.U)*10),(int)(Vector3.Dot(local,cell.V)*10)));
    }

    private void InitializeRawDeposits(WorldSetup world)
    {
        _rawDeposits=RawMaterials.Skip(1).Select(_=>new int[Nodes.Length]).ToArray();
        var terrain=world.Terrain;
        foreach(var cell in world.Cells)
        foreach(var site in CubeNet.ResourceSites(world,cell,terrain))
        {
            var node=SiteNode(world,cell,site.Position);
            if(node==null)continue;
            var material=SiteMaterial(site.Kind);
            _rawDeposits[(int)material-1][node.Id]+=material switch
            {
                RawMaterial.Vinyl=>Math.Clamp((int)MathF.Round(site.Size*48),6,10),
                RawMaterial.PP=>8,
                _=>6
            };
        }
    }

    internal static int RawCost(Robot product,RawMaterial material)=>material==RawMaterial.Maple?MapleCost(product):
        RobotMaterials.Parts.Where(p=>(product.Parts&p)!=0).Sum(p=>(material,product.Material(p)) switch
        {
            (RawMaterial.Styrene,RobotMaterial.Foam)=>2,
            (RawMaterial.Styrene,RobotMaterial.Polystyrene)=>8,
            (RawMaterial.Vinyl,RobotMaterial.SoftVinyl)=>4,
            (RawMaterial.PP,RobotMaterial.Polypropylene)=>8,
            (RawMaterial.Silicone,RobotMaterial.Silicone)=>8,
            _=>0
        });

    internal bool RawRequired(RawMaterial material)=>material==RawMaterial.Maple?MapleSupplyEnabled:OtherRawSupplyEnabled;
}
