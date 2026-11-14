namespace FolderVerse;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

public sealed partial class WorldPreview
{
    public static Color LandTerritoryColor(Color nation,TerrainKind terrain)=>terrain switch
    {
        TerrainKind.Sea=>Color.Lerp(nation,new Color(78,88,104),.55f),
        TerrainKind.Mountain=>Color.Lerp(nation,new Color(250,247,240),.72f),
        _=>Color.Lerp(Color.Lerp(nation,new Color(190,190,180),.22f),new Color(250,247,240),.32f)
    };

    public int? PresentationOwner {get;set;}
    public HashSet<int> PresentationCells {get;set;}
    public bool CountryColors {get;set;}
    public bool ControlledCountryColors {get;set;}
    public void ShowLandConquest(WorldSetup setup,bool exclusive=false,bool dimClaimed=false)
    {
        Generate(setup.Width,setup.Height,setup.Depth,setup.WorldSeed,setup.PolarAxis);
        var mesh=new List<VertexPositionColorNormal>();
        var offset=WorldTerrain.Offset(setup.WorldSeed);
        foreach(var cell in setup.Cells){
            int owner=PresentationOwner.HasValue?ConquestPresentation.ExactCellOwner(setup,cell.Id):exclusive?setup.Nodes.FullOwner(cell.Id):setup.Owners[cell.Id];
            if(PresentationOwner.HasValue && (owner!=PresentationOwner.Value || PresentationCells!=null && !PresentationCells.Contains(cell.Id)))owner=-1;
            if(exclusive && ControlledCountryColors && !setup.Relations.Controls(owner))owner=-1;
            AddFace(mesh,cell.Origin,cell.U,cell.V,cell.Normal,1,1,offset,(height,x,y)=>
                exclusive && CountryColors && owner>=0
                    ? LandTerritoryColor(setup.CountryMapColor(owner,x,y),WorldTerrain.Kind(height))
                    : WorldTerrain.ColorAt(height,cell.Normal,setup.PolarAxis),
                brightness:dimClaimed && setup.Owners[cell.Id]>=0?.35f:1f);
        }
        _vertices.Dispose();
        _vertices=new VertexBuffer(_device,VertexPositionColorNormal.VertexDeclaration,mesh.Count,BufferUsage.WriteOnly);
        _vertices.SetData(mesh.ToArray());_primitiveCount=mesh.Count/3;
        BuildWhirlpoolMesh(setup);
    }
}

