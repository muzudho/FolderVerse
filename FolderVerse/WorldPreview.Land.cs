namespace FolderVerse;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

public sealed partial class WorldPreview
{
    private void DrawLandBacklight(IEnumerable<SurfaceCell> selection,float time)
    {
        // Premultiplied overlay: at the trough, black at 25% leaves 75% of the terrain.
        // At the crest, the colored light also reads against white ice and bright water.
        float pulse=.5f+.5f*System.MathF.Sin(time*MathHelper.TwoPi/2.4f);
        Color tint=Color.Lerp(Color.Black*.25f,new Color(64,219,235)*.62f,pulse);
        _device.BlendState=BlendState.AlphaBlend;
        _device.DepthStencilState=DepthStencilState.DepthRead;
        _effect.LightingEnabled=false;
        foreach(var cell in selection)
        {
            var a=cell.Origin+cell.Normal*(WorldTerrain.MaxRelief+.003f);
            var b=a+cell.U;var c=b+cell.V;var d=a+cell.V;
            _selectionVertices[0]=new(a,tint,cell.Normal);
            _selectionVertices[1]=new(b,tint,cell.Normal);
            _selectionVertices[2]=new(c,tint,cell.Normal);
            _selectionVertices[3]=new(a,tint,cell.Normal);
            _selectionVertices[4]=new(c,tint,cell.Normal);
            _selectionVertices[5]=new(d,tint,cell.Normal);
            foreach(var pass in _effect.CurrentTechnique.Passes)
            {
                pass.Apply();
                _device.DrawUserPrimitives(PrimitiveType.TriangleList,_selectionVertices,0,2);
            }
        }
        _effect.LightingEnabled=true;
        _device.BlendState=BlendState.Opaque;
        _device.DepthStencilState=DepthStencilState.Default;
    }

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
        Generate(setup.Width,setup.Height,setup.Depth,setup.WorldSeed,setup.PolarAxis,setup.Terrain.ElevationShift);
        var mesh=new List<VertexPositionColorNormal>();
        var offset=setup.Terrain;
        float Brightness(int cell)=>dimClaimed && setup.Owners[cell]>=0?.35f:1f;
        foreach(var cell in setup.Cells){
            int owner=PresentationOwner.HasValue?ConquestPresentation.ExactCellOwner(setup,cell.Id):exclusive?setup.Nodes.FullOwner(cell.Id):setup.Owners[cell.Id];
            if(PresentationOwner.HasValue && (owner!=PresentationOwner.Value || PresentationCells!=null && !PresentationCells.Contains(cell.Id)))owner=-1;
            if(exclusive && ControlledCountryColors && !setup.Relations.Controls(owner))owner=-1;
            AddFace(mesh,cell.Origin,cell.U,cell.V,cell.Normal,1,1,offset,(height,x,y)=>
                exclusive && CountryColors && owner>=0
                    ? LandTerritoryColor(setup.CountryMapColor(owner,x,y),WorldTerrain.Kind(height))
                    : WorldTerrain.ColorAt(height,cell.Normal,setup.PolarAxis),
                brightness:Brightness(cell.Id));
        }
        _vertices.Dispose();
        _vertices=new VertexBuffer(_device,VertexPositionColorNormal.VertexDeclaration,mesh.Count,BufferUsage.WriteOnly);
        _vertices.SetData(mesh.ToArray());_primitiveCount=mesh.Count/3;
        BuildWhirlpoolMesh(setup,Brightness);
    }
}

