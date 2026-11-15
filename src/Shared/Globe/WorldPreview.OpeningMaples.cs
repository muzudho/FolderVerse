namespace FolderVerse;

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

public sealed partial class WorldPreview
{
    internal int OpeningMapleLoads {get;private set;}
    private (int Seed,int Width,int Height,int Depth,int PolarAxis,TerrainSampler Terrain) _maplePreviewKey;
    private readonly List<VertexPositionColorNormal> _maplePreviewMesh=new();
    private int _maplePreviewLoads;

    private void AddPreviewMaples(List<VertexPositionColorNormal> mesh,WorldSetup setup,TerrainSampler terrain)
    {
        if(setup.EquipmentMode!=RobotEquipmentMode.Materials)return;
        var key=(setup.WorldSeed,setup.Width,setup.Height,setup.Depth,setup.PolarAxis,terrain);
        if(_maplePreviewKey!=key)
        {
            _maplePreviewMesh.Clear();
            _maplePreviewLoads=BuildPreviewMaples(_maplePreviewMesh,setup,terrain);
            _maplePreviewKey=key;
        }
        OpeningMapleLoads=_maplePreviewLoads;
        mesh.AddRange(_maplePreviewMesh);
    }

    private static int BuildPreviewMaples(List<VertexPositionColorNormal> mesh,WorldSetup setup,TerrainSampler terrain)
    {
        var world=setup.CreateGeographyPreview();
        world.Routes.Initialize(world);
        world.Nodes.Initialize(world,geographyOnly:true);
        var deposits=RobotWorld.GenerateMapleDeposits(world);
        foreach(var node in world.Nodes.All)
        {
            int loads=deposits[node.Id];
            if(loads==0)continue;
            var cell=world.Cells[node.Cell];
            var center=world.Routes.Position(node.Cell,CubeNet.MapleGrovePoint(world,node));
            int count=loads<4?loads:Math.Min(9,loads);
            for(int i=0;i<count;i++)
            {
                float angle=i*2.4f;
                float spread=.025f*MathF.Sqrt(i);
                var root=center+(cell.U*MathF.Cos(angle)+cell.V*MathF.Sin(angle))*spread;
                root+=cell.Normal*WorldTerrain.ReliefAt(world,cell,root,terrain);
                var collar=root+cell.Normal*.058f;
                var crown=root+cell.Normal*(.12f+(i%3)*.009f);
                for(int side=0;side<6;side++)
                {
                    var a=Radial(side);
                    var b=Radial(side+1);
                    Triangle(crown,collar+a*.042f,collar+b*.042f,
                        side%2==0?new Color(244,127,42):new Color(208,67,36));
                    Triangle(collar+a*.042f,root+cell.Normal*.023f,collar+b*.042f,new Color(175,64,29));
                    Triangle(root,root+cell.Normal*.055f+a*.008f,root+cell.Normal*.055f+b*.008f,new Color(106,65,39));
                }

                Vector3 Radial(int side)
                {
                    float radians=side*MathHelper.TwoPi/6;
                    return cell.U*MathF.Cos(radians)+cell.V*MathF.Sin(radians);
                }
            }
        }
        foreach(var cell in world.Cells)
        foreach(var site in CubeNet.ResourceSites(world,cell,terrain))
        {
            if(RobotWorld.SiteNode(world,cell,site.Position)==null)continue;
            var origin=site.Position+cell.Normal*WorldTerrain.ReliefAt(world,cell,site.Position,terrain);
            CubeNet.DrawResourceSite(site.Kind,true,(color,vertices)=>
            {
                for(int i=1;i+1<vertices.Length;i++)
                    Triangle(Position(vertices[0]),Position(vertices[i]),Position(vertices[i+1]),color);
            });
            Vector3 Position(Vector3 p)=>origin+site.Size*(cell.U*p.X+cell.V*p.Y+cell.Normal*p.Z);
        }
        return deposits.Sum();

        void Triangle(Vector3 a,Vector3 b,Vector3 c,Color color)
        {
            var normal=Vector3.Normalize(Vector3.Cross(b-a,c-a));
            mesh.Add(new(a,color,normal));mesh.Add(new(b,color,normal));mesh.Add(new(c,color,normal));
        }
    }
}
