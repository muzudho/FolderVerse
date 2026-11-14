namespace FolderVerse;

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

public sealed partial class WorldPreview
{
    internal int OpeningMapleLoads {get;private set;}

    private void AddOpeningMaples(List<VertexPositionColorNormal> mesh,WorldSetup setup,TerrainSampler terrain)
    {
        if(setup.EquipmentMode!=RobotEquipmentMode.Materials)return;
        var world=setup.CreateGeographyPreview();
        world.Routes.Initialize(world);
        world.Nodes.Initialize(world,geographyOnly:true);
        var deposits=RobotWorld.GenerateMapleDeposits(world);
        OpeningMapleLoads=deposits.Sum();
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
                root+=cell.Normal*WorldTerrain.ReliefAt(cell,root,terrain);
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

        void Triangle(Vector3 a,Vector3 b,Vector3 c,Color color)
        {
            var normal=Vector3.Normalize(Vector3.Cross(b-a,c-a));
            mesh.Add(new(a,color,normal));mesh.Add(new(b,color,normal));mesh.Add(new(c,color,normal));
        }
    }
}
