namespace FolderVerse;
using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;

public sealed record RobotMapBadge(int Node,Rectangle Bounds,bool Detailed);
public sealed partial class CubeNet
{
    private readonly List<RobotMapBadge> _robotBadges=new();
    public IReadOnlyList<RobotMapBadge> RobotBadges=>_robotBadges;
    public void ClearGlobeRobotBadges(){_badgePanel=null;_robotBadges.Clear();}
    public void AddGlobeRobotBadge(int node,Rectangle bounds)=>_robotBadges.Add(new(node,bounds,false));
    public int HitRobotBadge(Point pointer){if(_badgePanel.HasValue)pointer=UnrotateScreen(pointer,_badgePanel.Value);return _robotBadges.AsEnumerable().Reverse().Where(b=>b.Bounds.Contains(pointer)).Select(b=>b.Node).DefaultIfEmpty(-1).First();}
    public const float RobotDetailScale=400;
    public void DrawMapObjects(UiPainter ui,WorldSetup world,Rectangle panel,float pulse)
    {
        _badgePanel=null;
        if(ShowRoutes)foreach(var cell in world.Cells)
            DrawNodeMarkers(ui,world,cell.Id,p=>AnimatedSurfacePosition(cell.Id,p,world,panel),pulse);
        DrawBatteries(ui,world,panel,r=>AnimatedSurfacePosition(world.ConquerorLocations[r],world.Routes.Position(world.ConquerorLocations[r],world.ConquerorPoints[r]),world,panel),pulse);
        DrawRobotLayer(ui,world,panel,n=>AnimatedNodePosition(world,n,panel),Fit(panel).Scale);
    }
    public void DrawGlobeRobots(UiPainter ui,WorldSetup world,WorldPreview globe,Rectangle panel,float yaw,float pitch)
    {
        _badgePanel=null;
        DrawRobotLayer(ui,world,panel,n=>{
            var cell=world.Cells[n.Cell];
            return globe.ProjectVisible(world.Routes.Position(n.Cell,n.Center),cell.Normal,yaw,pitch,panel,out var at)?at:null;
        },1);
    }
    // Even spacing around the node keeps its center and connected roads visible.
    public static Vector2 RobotRingSlot(int index,int count)
    {
        float angle=-MathHelper.PiOver2+index*MathHelper.TwoPi/Math.Max(1,count);
        float radius=Math.Max(48,count*30/MathHelper.TwoPi);
        return new Vector2(MathF.Cos(angle),MathF.Sin(angle))*radius;
    }
    private static int MapPartCount(RobotParts parts)=>
        ((parts&RobotParts.Head)!=0?1:0)+((parts&RobotParts.Body)!=0?1:0)+((parts&RobotParts.Legs)!=0?1:0);
    private void DrawRobotLayer(UiPainter ui,WorldSetup world,Rectangle panel,Func<Node,Vector2?> project,float scale)
    {
        _robotBadges.Clear();
        if(!ShowRobots || !world.Campaign.UseRobotCombat)return;
        int current=world.Nodes.Current(world.PlayerSlot)?.Id??-1;
        foreach(var node in world.Nodes.All.OrderBy(n=>EmphasizedNode(n.Id)?2:n.Id==current?1:0).ThenBy(n=>n.Id))
        {
            if(!NodeVisible(world,node))continue;
            var robots=world.Robots.Nodes[node.Id].Robots.Where(r=>ShowOtherCountries || world.Relations.Controls(r.Owner)).OrderByDescending(r=>MapPartCount(r.Parts)).ToArray();
            if(robots.Length==0)continue;
            var point=project(node);if(point==null)continue;
            var at=point.Value;float zoom=ShowRoutes && EmphasizedNode(node.Id)?2:1;
            var cards=robots.Select((robot,i)=>{
                var offset=RobotRingSlot(i,robots.Length)*zoom;
                return new Rectangle((int)(at.X+offset.X-12*zoom),(int)(at.Y+offset.Y-MapPartCount(robot.Parts)*6*zoom),(int)(24*zoom),(int)(MapPartCount(robot.Parts)*12*zoom));
            }).ToArray();
            var bounds=cards.Aggregate(Rectangle.Union);
            if(!MapViewport(panel).Intersects(bounds))continue;
            _robotBadges.Add(new(node.Id,bounds,true));
            for(int i=0;i<robots.Length;i++)
            {
                var robot=robots[i];var card=cards[i];
                int row=0;
                foreach(var part in new[]{RobotParts.Head,RobotParts.Body,RobotParts.Legs})
                {
                    if((robot.Parts&part)==0)continue;
                    // Each present part touches the next, including head + legs.
                    var full=new Rectangle(card.X,card.Y+(int)((row*12-(part==RobotParts.Head?0:part==RobotParts.Body?12:24))*zoom),(int)(24*zoom),(int)(36*zoom));
                    DrawMapRobot(ui,full,world.OwnerColor(robot.Origin(part)),part,robot.IsDamaged(part),robot.IsCheap(part));
                    row++;
                }
            }
        }
    }
    public void DrawMapRobot(UiPainter ui,Rectangle r,Color color,RobotParts parts,bool damaged=false,bool cheap=false)
    {
        _mapRobotTexture ??= LoadMapRobot(ui.Batch.GraphicsDevice);
        var tint=damaged?new Color(136,137,127):Color.Lerp(Color.White,color,.48f);
        for(int part=0;part<3;part++)
        {
            if((parts&(RobotParts)(4>>part))==0)continue;
            int top=_mapRobotTexture.Height*part/3,bottom=_mapRobotTexture.Height*(part+1)/3;
            var source=new Rectangle(0,top,_mapRobotTexture.Width,bottom-top);
            int y=r.Y+r.Height*part/3,next=r.Y+r.Height*(part+1)/3;
            if(!cheap)ui.Batch.Draw(_mapRobotTexture,new Rectangle(r.X,y,r.Width,Math.Max(1,next-y)),source,tint);
            else for(int row=y;row<next;row++)
            {
                int sy=top+(row-y)*(bottom-top)/Math.Max(1,next-y);
                ui.Batch.Draw(_mapRobotTexture,new Rectangle(r.X,row,r.Width,1),new Rectangle(0,sy,source.Width,1),(row-y)%4<2?tint:damaged?new Color(165,162,146):Color.White);
            }
        }
    }
    private Texture2D _mapRobotTexture;
    public void DisposeRobotTexture(){_mapRobotTexture?.Dispose();_mapRobotTexture=null;}
    private static Texture2D LoadMapRobot(GraphicsDevice device)
    {
        using var stream=File.OpenRead(Path.Combine(AppContext.BaseDirectory,"Content","Images","robot-map-toy-v001.png"));
        var texture=Texture2D.FromStream(device,stream);
        var pixels=new Color[texture.Width*texture.Height];texture.GetData(pixels);
        for(int i=0;i<pixels.Length;i++){var c=pixels[i];pixels[i]=new Color(c.R*c.A/255,c.G*c.A/255,c.B*c.A/255,(int)c.A);}
        texture.SetData(pixels);return texture;
    }
}
