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
        var rotation=DisplayOrientation(yaw,pitch);
        var anchor=Vector3.Transform(GlobeAnchor??Vector3.Zero,rotation);
        return rotation*Matrix.CreateTranslation(-anchor.X,-anchor.Y,0);
    }
    public void DrawRobotPins(WorldSetup setup,CubeNet net,float yaw,float pitch,Rectangle panel)
    {
        net.ClearGlobeRobotBadges();net.ClearGlobeBatteries();
        if(!setup.Campaign.UseRobotCombat)return;
        var mesh=new List<VertexPositionColorNormal>();
        void Triangle(Vector3 a,Vector3 b,Vector3 c,Color tint)
        {
            var normal=Vector3.Normalize(Vector3.Cross(b-a,c-a));
            mesh.Add(new(a,tint,normal));mesh.Add(new(b,tint,normal));mesh.Add(new(c,tint,normal));
        }
        foreach(var node in setup.Nodes.All)
        {
            bool nodeVisible=net.NodeVisible(setup,node);
            bool ownBattery=Enumerable.Range(0,setup.ActiveCount).Any(r=>net.ShowBatteries && setup.Relations.Powered[r] && setup.Nodes.Current(r)?.Id==node.Id && (net.ShowOtherCountries || setup.Relations.Controls(r)) && (net.VisibleOperationBatteries==null || net.VisibleOperationBatteries.Contains(r)));
            if(!nodeVisible && !ownBattery)continue;
            var cell=setup.Cells[node.Cell];var robots=setup.Robots.Nodes[node.Id].Robots.Where(r=>nodeVisible && net.ShowRobots && (net.ShowOtherCountries || setup.Relations.Controls(r.Owner))).ToList();
            if(net.ShowRobots && net.ShowBatteries && net.HoveredBattery>=0 && setup.Relations.Powered[net.HoveredBattery] && setup.Nodes.Current(net.HoveredBattery)?.Id==node.Id && (net.ShowOtherCountries || setup.Relations.Controls(net.HoveredBattery)))robots.AddRange(setup.Robots.Retinues[net.HoveredBattery].Robots);
            var center=setup.Routes.Position(node.Cell,node.Center);
            if(!ProjectVisible(center,cell.Normal,yaw,pitch,panel,out _))continue;
            Rectangle bounds=Rectangle.Empty;
            float hoverScale=net.ShowRoutes && net.EmphasizedNode(node.Id)?2:1; Vector3 Point(float u,float v,float height)=>center+(cell.U*u+cell.V*v+cell.Normal*height)*hoverScale;
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
                float angle=-MathHelper.PiOver2+slot*MathHelper.TwoPi/robots.Count,radius=Math.Max(.18f,robots.Count*.12f/MathHelper.TwoPi);
                float u=MathF.Cos(angle)*radius,v=MathF.Sin(angle)*radius,z=.008f;var robot=robots[slot];
                void Part(RobotParts part,float x,float y,float height,float width,float depth,float extent){
                    var tint=robot.IsDamaged(part)?new Color(136,137,127):Color.Lerp(Color.White,setup.OwnerColor(robot.Origin(part)),.48f);
                    Box(x,y,height,width,depth,extent,tint);
                    if(robot.IsCheap(part))Box(x,y,height+extent*.4f,width*1.02f,depth*1.02f,extent*.16f,Color.White);
                }
                if((robot.Parts&RobotParts.Legs)!=0){foreach(float side in new[]{-1f,1f})Part(RobotParts.Legs,u+side*.027f,v,z,.038f,.065f,.09f);z+=.1f;}
                if((robot.Parts&RobotParts.Body)!=0){Part(RobotParts.Body,u,v,z,.10f,.065f,.11f);foreach(float side in new[]{-1f,1f})Part(RobotParts.Body,u+side*.068f,v,z+.01f,.026f,.045f,.09f);z+=.12f;}
                if((robot.Parts&RobotParts.Head)!=0)
                {
                    Part(RobotParts.Head,u,v,z,.115f,.08f,.09f);
                    foreach(float side in new[]{-1f,1f})Box(u+side*.025f,v+.043f,z+.05f,.018f,.009f,.018f,new(25,41,49));
                    Box(u,v+.044f,z+.023f,.04f,.008f,.009f,new(230,102,60));Box(u,v,z+.09f,.01f,.01f,.025f,new(83,112,122));
                }
            }
            if(robots.Count>0 && bounds.Intersects(panel))net.AddGlobeRobotBadge(node.Id,bounds);
            foreach(int ruler in Enumerable.Range(0,setup.ActiveCount).Where(r=>net.ShowBatteries && setup.Relations.Powered[r] && setup.Nodes.Current(r)?.Id==node.Id && (net.ShowOtherCountries || setup.Relations.Controls(r)) && (net.VisibleOperationBatteries==null || net.VisibleOperationBatteries.Contains(r)))){
                bounds=Rectangle.Empty;var point=setup.Routes.Position(setup.ConquerorLocations[ruler],setup.ConquerorPoints[ruler]);
                var delta=point-center;int batterySlot=Enumerable.Range(0,ruler).Count(r=>setup.Relations.Powered[r] && setup.Nodes.Current(r)?.Id==node.Id);
                float u=Vector3.Dot(delta,cell.U)+.32f,v=Vector3.Dot(delta,cell.V)+batterySlot*.13f;
                Box(u,v,.005f,.09f,.07f,.18f,setup.OwnerColor(ruler));Box(u,v,.185f,.035f,.035f,.018f,new(240,216,147));
                for(int i=0;i<3;i++)Box(u,v+.038f,.025f+i*.05f,.06f,.009f,.035f,new(45,112,70));
                if(bounds.Intersects(panel))net.AddGlobeBatteryBadge(ruler,bounds);
            }
        }
        if(mesh.Count==0)return;
        _device.BlendState=BlendState.Opaque;_device.DepthStencilState=DepthStencilState.Default;_device.RasterizerState=RasterizerState.CullNone;
        _effect.LightingEnabled=true;_effect.World=GlobeOrientation(yaw,pitch);
        foreach(var pass in _effect.CurrentTechnique.Passes){pass.Apply();_device.DrawUserPrimitives(PrimitiveType.TriangleList,mesh.ToArray(),0,mesh.Count/3);}
    }
}
