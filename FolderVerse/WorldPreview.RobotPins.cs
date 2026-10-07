namespace FolderVerse;
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

public sealed partial class WorldPreview
{
    public float GlobeZoom {get;set;}=1;
    public Vector3? GlobeAnchor {get;set;}
    private float GlobeFieldOfView=>2*MathF.Atan(MathF.Tan(MathHelper.ToRadians(21))/GlobeZoom);
    private Matrix GlobeOrientation(float yaw,float pitch)
    {
        var rotation=Orientation(yaw,pitch);
        var anchor=Vector3.Transform(GlobeAnchor??Vector3.Zero,rotation);
        return rotation*Matrix.CreateTranslation(-anchor.X,-anchor.Y,0);
    }
    public void DrawRobotPins(WorldSetup setup,CubeNet net,float yaw,float pitch,Rectangle panel)
    {
        net.ClearGlobeRobotBadges();if(!setup.Campaign.UseRobotCombat)return;
        var mesh=new List<VertexPositionColorNormal>();
        void Triangle(Vector3 a,Vector3 b,Vector3 c,Color tint)
        {
            var normal=Vector3.Normalize(Vector3.Cross(b-a,c-a));
            mesh.Add(new(a,tint,normal));mesh.Add(new(b,tint,normal));mesh.Add(new(c,tint,normal));
        }
        foreach(var node in setup.Nodes.All)
        {
            var cell=setup.Cells[node.Cell];var robots=setup.Robots.Nodes[node.Id].Robots;
            var center=setup.Routes.Position(node.Cell,node.Center);
            if(robots.Count==0 || !ProjectVisible(center,cell.Normal,yaw,pitch,panel,out var screen) || !panel.Contains(screen.ToPoint()))continue;
            Rectangle bounds=Rectangle.Empty;
            Vector3 Point(float u,float v,float height)=>center+cell.U*u+cell.V*v+cell.Normal*height;
            void Track(Vector3 p)
            {
                ProjectVisible(p,cell.Normal,yaw,pitch,panel,out var at);
                var box=new Rectangle((int)at.X-2,(int)at.Y-2,5,5);bounds=bounds==Rectangle.Empty?box:Rectangle.Union(bounds,box);
            }
            void Box(float u,float v,float z,float width,float depth,float height,Color color)
            {
                var corners=new Vector3[8];
                for(int i=0;i<8;i++){corners[i]=Point(u+((i&1)==0?-1:1)*width/2,v+((i&2)==0?-1:1)*depth/2,z+((i&4)==0?0:height));Track(corners[i]);}
                int[][] quads={new[]{0,1,3,2},new[]{4,6,7,5},new[]{0,4,5,1},new[]{2,3,7,6},new[]{0,2,6,4},new[]{1,5,7,3}};
                foreach(var q in quads){Triangle(corners[q[0]],corners[q[1]],corners[q[2]],color);Triangle(corners[q[0]],corners[q[2]],corners[q[3]],color);}
            }
            for(int slot=0;slot<robots.Count;slot++)
            {
                int row=slot<2?0:slot<5?1:slot<7?2:slot<10?3:4;
                int first=new[]{0,2,5,7,10}[row];int count=row%2==0?2:3;
                float u=(slot-first-(count-1)/2f)*.07f,v=(row-2)*.065f;
                var robot=robots[slot];var tint=setup.OwnerColor(robot.Owner);
                if((robot.Parts&RobotParts.Legs)!=0)Box(u,v,.012f,.055f,.018f,.016f,tint);
                if((robot.Parts&RobotParts.Body)!=0)Box(u,v,.03f,.014f,.014f,.085f,tint);
                if((robot.Parts&RobotParts.Head)!=0)
                {
                    const float radius=.025f;var tip=Point(u,v,.14f);Track(tip+cell.Normal*radius);Track(tip-cell.U*radius);Track(tip+cell.U*radius);
                    var ring=new[]{tip+cell.U*radius,tip+cell.V*radius,tip-cell.U*radius,tip-cell.V*radius};
                    for(int i=0;i<4;i++){Triangle(tip+cell.Normal*radius,ring[i],ring[(i+1)%4],tint);Triangle(tip-cell.Normal*radius,ring[(i+1)%4],ring[i],tint);}
                }
            }
            net.AddGlobeRobotBadge(node.Id,bounds);
        }
        if(mesh.Count==0)return;
        _device.DepthStencilState=DepthStencilState.Default;_device.RasterizerState=RasterizerState.CullNone;
        _effect.LightingEnabled=true;_effect.World=GlobeOrientation(yaw,pitch);
        foreach(var pass in _effect.CurrentTechnique.Passes){pass.Apply();_device.DrawUserPrimitives(PrimitiveType.TriangleList,mesh.ToArray(),0,mesh.Count/3);}
    }
}
