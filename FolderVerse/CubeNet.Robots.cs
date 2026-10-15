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
    public void ClearGlobeRobotBadges()=>_robotBadges.Clear();
    public void AddGlobeRobotBadge(int node,Rectangle bounds)=>_robotBadges.Add(new(node,bounds,false));
    public int HitRobotBadge(Point pointer)=>_robotBadges.Where(b=>b.Bounds.Contains(pointer)).Select(b=>b.Node).DefaultIfEmpty(-1).First();
    public const float RobotDetailScale=400;
    // Hexagonal packing, filled from the center outward.
    private static readonly Point[] RobotHexSlots=(from q in Enumerable.Range(-2,5)
        from r in Enumerable.Range(-2,5)
        where Math.Max(Math.Abs(q),Math.Max(Math.Abs(r),Math.Abs(q+r)))<=2
        let x=q*28+r*14
        let y=r*38
        orderby Math.Max(Math.Abs(q),Math.Max(Math.Abs(r),Math.Abs(q+r))),x*x+y*y,Math.Atan2(y,x)
        select new Point(x,y)).Take(12).ToArray();
    private static int MapPartCount(RobotParts parts)=>
        ((parts&RobotParts.Head)!=0?1:0)+((parts&RobotParts.Body)!=0?1:0)+((parts&RobotParts.Legs)!=0?1:0);
    private void DrawRobotLayer(UiPainter ui,WorldSetup world,Rectangle panel,Func<Node,Vector2?> project,float scale)
    {
        _robotBadges.Clear();
        if(!world.Campaign.UseRobotCombat)return;
        var occupied=new List<Rectangle>();
        int current=world.Nodes.Current(world.PlayerSlot)?.Id??-1;
        foreach(var node in world.Nodes.All.OrderBy(n=>n.Id==current?0:1).ThenBy(n=>n.Id))
        {
            var robots=world.Robots.Nodes[node.Id].Robots.OrderByDescending(r=>MapPartCount(r.Parts)).ToArray();
            if(robots.Length==0)continue;
            var point=project(node);if(point==null || !panel.Contains(point.Value.ToPoint()))continue;
            var cards=robots.Select((robot,i)=>new Rectangle(RobotHexSlots[i].X-12,RobotHexSlots[i].Y-MapPartCount(robot.Parts)*6,24,MapPartCount(robot.Parts)*12)).ToArray();
            var local=cards.Aggregate(Rectangle.Union);
            int gap=node.IsHarbor?24:16;
            var at=point.Value;
            var origin=new Point((int)at.X-local.Center.X,(int)at.Y-gap-local.Bottom);
            var bounds=new Rectangle(local.X+origin.X,local.Y+origin.Y,local.Width,local.Height);
            if(!panel.Contains(bounds) || occupied.Any(r=>r.Intersects(bounds)))continue;
            occupied.Add(bounds);_robotBadges.Add(new(node.Id,bounds,true));
            ui.Box(new((int)at.X-1,bounds.Bottom,2,gap),new Color(208,219,223));
            for(int i=0;i<robots.Length;i++)
            {
                var robot=robots[i];var card=cards[i];card.Offset(origin);
                int row=0;
                foreach(var part in new[]{RobotParts.Head,RobotParts.Body,RobotParts.Legs})
                {
                    if((robot.Parts&part)==0)continue;
                    // Each present part touches the next, including head + legs.
                    var full=new Rectangle(card.X,card.Y+row*12-(part==RobotParts.Head?0:part==RobotParts.Body?12:24),24,36); if(ShowRoutes && HoveredNode==node.Id)full=new(bounds.Center.X+(full.X-bounds.Center.X)*2,bounds.Bottom+(full.Y-bounds.Bottom)*2,full.Width*2,full.Height*2);
                    DrawMapRobot(ui,full,world.OwnerColor(robot.Affiliation(part)),part);
                    row++;
                }
            }
        }
    }
    public void DrawMapRobot(UiPainter ui,Rectangle r,Color color,RobotParts parts)
    {
        _mapRobotTexture ??= LoadMapRobot(ui.Batch.GraphicsDevice);
        var tint=Color.Lerp(Color.White,color,.48f);
        for(int part=0;part<3;part++)
        {
            if((parts&(RobotParts)(4>>part))==0)continue;
            int top=_mapRobotTexture.Height*part/3,bottom=_mapRobotTexture.Height*(part+1)/3;
            var source=new Rectangle(0,top,_mapRobotTexture.Width,bottom-top);
            int y=r.Y+r.Height*part/3,next=r.Y+r.Height*(part+1)/3;
            ui.Batch.Draw(_mapRobotTexture,new Rectangle(r.X,y,r.Width,Math.Max(1,next-y)),source,tint);
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