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
    public void ShowLandConquest(WorldSetup setup,bool exclusive=false)
    {
        Generate(setup.Width,setup.Height,setup.Depth,setup.WorldSeed,setup.PolarAxis);
        var mesh=new List<VertexPositionColorNormal>();
        var offset=WorldTerrain.Offset(setup.WorldSeed);
        foreach(var cell in setup.Cells){
            int owner=PresentationOwner.HasValue?ConquestPresentation.ExactCellOwner(setup,cell.Id):exclusive?setup.Nodes.FullOwner(cell.Id):setup.Owners[cell.Id];
            if(PresentationOwner.HasValue && (owner!=PresentationOwner.Value || PresentationCells!=null && !PresentationCells.Contains(cell.Id)))owner=-1;
            if(exclusive && ControlledCountryColors && !setup.Relations.Controls(owner))owner=-1;
        for(int y=0;y<10;y++)for(int x=0;x<10;x++)
        {
            var a=cell.Origin+cell.U*(x/10f)+cell.V*(y/10f);
            var b=a+cell.U/10;var c=b+cell.V/10;var d=a+cell.V/10;
            float height=WorldTerrain.Elevation((a+c)/2,offset);
            var color=owner<0?WorldTerrain.ColorAt(height,cell.Normal,setup.PolarAxis):LandTerritoryColor(setup.OwnerColor(owner),WorldTerrain.Kind(height));
            if(x==0 || y==0)color=Color.Lerp(color,new Color(17,47,62),.18f);
            foreach(var point in new[]{a,b,c,a,c,d})mesh.Add(new(point,color,cell.Normal));
        }
        }
        _vertices.Dispose();
        _vertices=new VertexBuffer(_device,VertexPositionColorNormal.VertexDeclaration,mesh.Count,BufferUsage.WriteOnly);
        _vertices.SetData(mesh.ToArray());_primitiveCount=mesh.Count/3;
    }
}

