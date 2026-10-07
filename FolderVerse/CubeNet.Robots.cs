namespace FolderVerse;
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;

public sealed record RobotMapBadge(int Node,Rectangle Bounds,bool Detailed);
public sealed partial class CubeNet
{
    private readonly List<RobotMapBadge> _robotBadges=new();
    public IReadOnlyList<RobotMapBadge> RobotBadges=>_robotBadges;
    public int HitRobotBadge(Point pointer)=>_robotBadges.Where(b=>b.Bounds.Contains(pointer)).Select(b=>b.Node).DefaultIfEmpty(-1).First();
    public const float RobotDetailScale=400;
    // Alternating rows of 2, 3, 2, 3, 2 pins, as in the compact dozen arrangement.
    private static readonly Point[] RobotPinSlots={new(12,4),new(24,4),new(6,13),new(18,13),new(30,13),new(12,22),new(24,22),new(6,31),new(18,31),new(30,31),new(12,40),new(24,40)};
    private static Rectangle RobotPinBounds(Point at,RobotParts parts)
    {
        Rectangle bounds=Rectangle.Empty;
        void Include(Rectangle part)=>bounds=bounds==Rectangle.Empty?part:Rectangle.Union(bounds,part);
        if((parts&RobotParts.Head)!=0)Include(new(at.X-3,at.Y-1,7,7));
        if((parts&RobotParts.Body)!=0)Include(new(at.X-1,at.Y+5,2,6));
        if((parts&RobotParts.Legs)!=0)Include(new(at.X-3,at.Y+10,7,3));
        return bounds;
    }
    private void DrawRobotLayer(UiPainter ui,WorldSetup world,Rectangle panel,Func<Node,Vector2?> project,float scale)
    {
        _robotBadges.Clear();
        if(!world.Campaign.UseRobotCombat)return;
        bool detailed=scale>=RobotDetailScale;
        var occupied=new List<Rectangle>();
        // Give the current node first choice of space, then populated nodes in stable ID order.
        int current=world.Nodes.Current(world.PlayerSlot)?.Id??-1;
        foreach(var node in world.Nodes.All.OrderBy(n=>n.Id==current?0:1).ThenBy(n=>n.Id))
        {
            var store=world.Robots.Nodes[node.Id];if(store.Count==0)continue;
            var point=project(node);if(point==null || !panel.Contains(point.Value.ToPoint()))continue;
            var at=point.Value;
            // Anchor the visible drawing, rather than an empty twelve-slot frame.
            // Sparse groups consequently stay as close to their node as a full dozen.
            Rectangle pinBounds=Rectangle.Empty;
            if(!detailed)for(int slot=0;slot<store.Count;slot++)
            {
                var pin=RobotPinBounds(RobotPinSlots[slot],store.Robots[slot].Parts);
                pinBounds=slot==0?pin:Rectangle.Union(pinBounds,pin);
            }
            int width=detailed?100:pinBounds.Width,height=detailed?104:pinBounds.Height;
            int gap=detailed?(node.IsHarbor?24:16):(node.IsHarbor?12:6);
            var bounds=new Rectangle((int)at.X-width/2,(int)at.Y-gap-height,width,height);
            if(!panel.Contains(bounds) || occupied.Any(r=>r.Intersects(bounds)))continue;
            occupied.Add(bounds);_robotBadges.Add(new(node.Id,bounds,detailed));
            if(detailed)
            {
                ui.Box(bounds,new Color(10,25,34,240));
                ui.Box(new(bounds.X,bounds.Y,bounds.Width,2),world.OwnerColor(node.Owner));
            }
            ui.Box(new((int)at.X-1,bounds.Bottom,2,gap),new Color(208,219,223));
            if(!detailed)
            {
                for(int slot=0;slot<store.Count;slot++)
                {
                    var robot=store.Robots[slot];var offset=RobotPinSlots[slot];
                    DrawRobotPin(ui,new(bounds.X+offset.X-pinBounds.X,bounds.Y+offset.Y-pinBounds.Y),world.OwnerColor(robot.Owner),robot.Parts);
                }
                continue;
            }
            for(int slot=0;slot<12;slot++)
            {
                var card=new Rectangle(bounds.X+5+(slot%4)*23,bounds.Y+13+(slot/4)*28,21,25);
                ui.Box(card,new Color(44,62,73));
                if(slot>=store.Count)continue;
                var robot=store.Robots[slot];
                DrawMapRobot(ui,new(card.X+3,card.Y+1,15,21),world.OwnerColor(robot.Owner),robot.Parts);
            }
        }
    }
    private static void DrawRobotPin(UiPainter ui,Point at,Color color,RobotParts parts)
    {
        if((parts&RobotParts.Head)!=0)Disc(ui,new(at.X,at.Y+2),2.5f,color);
        if((parts&RobotParts.Body)!=0)ui.Box(new(at.X-1,at.Y+5,2,6),color);
        if((parts&RobotParts.Legs)!=0)
        {
            ui.Box(new(at.X-1,at.Y+10,2,3),color);
            ui.Box(new(at.X-3,at.Y+12,7,1),color);
        }
    }
    private static void DrawMapRobot(UiPainter ui,Rectangle r,Color color,RobotParts parts)
    {
        void Box(int x,int y,int width,int height,Color tint)=>ui.Box(new(r.X+x*r.Width/9,r.Y+y*r.Height/13,Math.Max(1,width*r.Width/9),Math.Max(1,height*r.Height/13)),tint);
        if((parts&RobotParts.Head)!=0)
        {
            Box(2,0,5,4,new Color(255,227,189));Box(1,0,7,2,color);
        }
        if((parts&RobotParts.Body)!=0)Box(1,5,7,4,color);
        if((parts&RobotParts.Legs)!=0)
        {
            Box(1,10,3,3,color);Box(5,10,3,3,color);
        }
    }
}
