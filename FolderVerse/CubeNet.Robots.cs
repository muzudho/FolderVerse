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
        bool detailed=true;
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
            int width=detailed?90:pinBounds.Width,height=detailed?160:pinBounds.Height;
            int gap=detailed?(node.IsHarbor?24:16):(node.IsHarbor?12:6);
            var bounds=new Rectangle((int)at.X-width/2,(int)at.Y-gap-height,width,height);
            if(!panel.Contains(bounds) || occupied.Any(r=>r.Intersects(bounds)))continue;
            occupied.Add(bounds);_robotBadges.Add(new(node.Id,bounds,detailed));
            if(detailed)
            {
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
                var card=new Rectangle(bounds.X+3+(slot%3)*29,bounds.Y+4+(slot/3)*39,26,36);
                if(slot>=store.Count)continue;
                var robot=store.Robots[slot];
                foreach(var part in new[]{RobotParts.Head,RobotParts.Body,RobotParts.Legs})
                    if((robot.Parts&part)!=0)DrawMapRobot(ui,new(card.X,card.Y,24,36),world.OwnerColor(robot.Origin(part)),part);
            }
        }
    }
    private void DrawRobotPin(UiPainter ui,Point at,Color color,RobotParts parts)
    {
        DrawMapRobot(ui,new(at.X-5,at.Y-1,10,15),color,parts);
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