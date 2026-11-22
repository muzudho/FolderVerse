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
    private readonly List<(int Node,Rectangle Bounds,int Count)> _globeRobotOverflow=new();
    public IReadOnlyList<RobotMapBadge> RobotBadges=>_robotBadges;
    public void ClearGlobeRobotBadges(){_badgePanel=null;_robotBadges.Clear();_globeRobotOverflow.Clear();}
    public void AddGlobeRobotBadge(int node,Rectangle bounds)=>_robotBadges.Add(new(node,bounds,true));
    internal void AddGlobeRobotOverflow(int node,Vector2 at,int hidden,Rectangle panel)
    {
        var bounds=RobotOverflowBounds(at);
        if(panel.Intersects(bounds))_globeRobotOverflow.Add((node,bounds,hidden));
    }
    private void DrawGlobeRobotOverflow(UiPainter ui)
    {
        if(!ShowRobots || DirectoryOnly)return;
        foreach(var (node,bounds,count) in _globeRobotOverflow)
        {
            DrawRobotOverflow(ui,bounds,count);
            AddGlobeRobotBadge(node,bounds);
        }
    }
    public int HitRobotBadge(Point pointer){if(_badgePanel.HasValue)pointer=UnrotateScreen(pointer,_badgePanel.Value);return _robotBadges.AsEnumerable().Reverse().Where(b=>b.Bounds.Contains(pointer)).Select(b=>b.Node).DefaultIfEmpty(-1).First();}
    public const float RobotDetailScale=400;
    public void DrawMapObjects(UiPainter ui,WorldSetup world,Rectangle panel,float pulse)
    {
        _badgePanel=null;
        DrawWhirlpools(ui,world,panel);
        if(ShowRoutes)foreach(var cell in world.Cells)
            DrawNodeMarkers(ui,world,cell.Id,p=>AnimatedSurfacePosition(cell.Id,p,world,panel),pulse);
        DrawBatteries(ui,world,panel,r=>AnimatedSurfacePosition(world.ConquerorLocations[r],world.Routes.Position(world.ConquerorLocations[r],world.ConquerorPoints[r]),world,panel),pulse);
        DrawRobotLayer(ui,world,panel,n=>AnimatedNodePosition(world,n,panel),Fit(panel).Scale,pulse,avoidResources:true);
    }
    public void DrawGlobeRobots(UiPainter ui,WorldSetup world,WorldPreview globe,Rectangle panel,float yaw,float pitch)
    {
        _whirlpoolScreens.Clear();
        _badgePanel=null;
        DrawRobotLayer(ui,world,panel,n=>{
            var cell=world.Cells[n.Cell];
            return globe.ProjectVisible(world.Routes.Position(n.Cell,n.Center),cell.Normal,yaw,pitch,panel,out var at)?at:null;
        },65*globe.GlobeZoom);
    }
    // Even spacing around the node keeps its center and connected roads visible.
    public static Vector2 RobotRingSlot(int index,int count)
    {
        float angle=-MathHelper.PiOver2+index*MathHelper.TwoPi/Math.Max(1,count);
        float radius=Math.Max(48,count*30/MathHelper.TwoPi);
        return new Vector2(MathF.Cos(angle),MathF.Sin(angle))*radius;
    }
    public static Vector2 RobotSpiralSlot(int index,float spacing)
    {
        const float innerRadius=1.55f;
        const float growth=1.25f/MathHelper.TwoPi;
        // Each slot advances by approximately one spacing along the spiral.
        float radius=MathF.Sqrt(innerRadius*innerRadius+2*growth*index);
        float angle=MathHelper.PiOver2+(radius-innerRadius)/growth;
        return new Vector2(MathF.Cos(angle),MathF.Sin(angle))*(radius*spacing);
    }
    internal static int VisibleRobotCount(int total,float projectedUnit)=>
        Math.Min(total,projectedUnit<65?4:projectedUnit<120?8:projectedUnit<200?16:total);
    public static float MapRobotZoom(float projectedRegionSize)=>MathHelper.Clamp(projectedRegionSize/100f,.08f,3f);
    private static Rectangle RobotOverflowBounds(Vector2 at,float zoom=1)=>new((int)(at.X-22*zoom),(int)(at.Y-11*zoom),Math.Max(1,(int)(44*zoom)),Math.Max(1,(int)(22*zoom)));
    private static void DrawRobotOverflow(UiPainter ui,Rectangle box,int count,float zoom=1)
    {
        ui.Box(box,new Color(16,35,47,230));
        ui.Center($"+{count}",box,.44f*zoom,new Color(255,229,159));
    }
    private static int MapPartCount(RobotParts parts)=>
        ((parts&RobotParts.Head)!=0?1:0)+((parts&RobotParts.Body)!=0?1:0)+((parts&RobotParts.Legs)!=0?1:0);
    public static float MapObjectHop(double seconds,int index)=>6*MathF.Pow(MathF.Max(0,MathF.Sin((float)(seconds*MathHelper.TwoPi/1.2)+index*.45f)),2);
    private void DrawRobotLayer(UiPainter ui,WorldSetup world,Rectangle panel,Func<Node,Vector2?> project,float scale,float pulse=0,bool avoidResources=false)
    {
        using var cost=new RenderFeatureCost.Scope(2,ShowRobots);
        _robotBadges.Clear();
        _globeRobotOverflow.Clear();
        if(!ShowRobots || !world.Campaign.UseRobotCombat)return;
        int current=world.Nodes.Current(world.PlayerSlot)?.Id??-1;
        var groups=world.Robots.FunctionDisplayGroups(world);
        var clearances=new Dictionary<int,(Vector3 Position,float Radius)[]>();
        foreach(var node in world.Nodes.All.OrderBy(n=>EmphasizedNode(n.Id)?2:n.Id==current?1:0).ThenBy(n=>n.Id))
        {
            if(!NodeVisible(world,node))continue;
            var robots=groups.GetValueOrDefault(node.Id,new List<Robot>()).Where(r=>ShowOtherCountries || world.Relations.Controls(r.Owner)).OrderBy(r=>r.Id).ToArray();
            if(robots.Length==0)continue;
            var point=project(node);if(point==null)continue;
            var at=point.Value;float zoom=avoidResources?MapRobotZoom(scale):1;
            bool detailed=zoom>=.65f;
            int visible=VisibleRobotCount(robots.Length,scale);
            if(!clearances.TryGetValue(node.Cell,out var obstacles))
                clearances[node.Cell]=obstacles=avoidResources?ResourceClearance(world,node.Cell):Array.Empty<(Vector3,float)>();
            var cell=world.Cells[node.Cell];
            var center=SiteDisplayPosition(world,node,obstacles);
            int nextSlot=0;
            Vector2 RobotPosition()
            {
                if(obstacles.Length==0)return at+RobotSpiralSlot(nextSlot++,46*zoom);
                var offset=ResourceSafeRobotSlot(cell,center,46*zoom/Math.Max(1,scale),18*zoom/Math.Max(1,scale),obstacles,ref nextSlot);
                return AnimatedSurfacePosition(node.Cell,center+cell.U*offset.X+cell.V*offset.Y,world,panel);
            }
            var cards=robots.Take(visible).Select((robot,i)=>{
                var position=RobotPosition();
                return AvoidWhirlpool(new Rectangle((int)(position.X-12*zoom),(int)(position.Y-MapPartCount(robot.Parts)*6*zoom),Math.Max(1,(int)(24*zoom)),Math.Max(1,(int)(MapPartCount(robot.Parts)*12*zoom))));
            }).ToArray();
            int hidden=robots.Length-visible;
            var overflow=hidden>0?AvoidWhirlpool(RobotOverflowBounds(RobotPosition(),zoom)):Rectangle.Empty;
            var bounds=cards.Aggregate(Rectangle.Union);
            if(hidden>0)bounds=Rectangle.Union(bounds,overflow);
            if(!MapViewport(panel).Intersects(bounds))continue;
            // Keep gaps between robots outside the hit area.
            foreach(var card in cards){var hit=card;if(ShowRoutes && EmphasizedNode(node.Id)){int hop=(int)(6*zoom);hit.Y-=hop;hit.Height+=hop;}_robotBadges.Add(new(node.Functional?node.Id:node.FunctionHost,hit,detailed));}
            for(int i=0;i<visible;i++)
            {
                var robot=robots[i];var card=cards[i];
                if(ShowRoutes && EmphasizedNode(node.Id))card.Offset(0,-(int)(MapObjectHop(pulse,i)*zoom));
                int row=0;
                foreach(var part in new[]{RobotParts.Head,RobotParts.Body,RobotParts.Legs})
                {
                    if((robot.Parts&part)==0)continue;
                    // Each present part touches the next, including head + legs.
                    var full=new Rectangle(card.X,card.Y+(int)((row*12-(part==RobotParts.Head?0:part==RobotParts.Body?12:24))*zoom),(int)(24*zoom),(int)(36*zoom));
                    DrawMapRobot(ui,full,world.OwnerColor(robot.Origin(part)),part,robot.IsDamaged(part),robot.IsCheap(part),DimNode(node.Id),
                        world.CountryPattern(robot.Origin(part)),WorldSetup.Colors[world.CountrySecondaryColor(robot.Origin(part))],robot.IsPremium(part),
                        part==((robot.Parts&RobotParts.Body)!=0?RobotParts.Body:(robot.Parts&RobotParts.Head)!=0?RobotParts.Head:RobotParts.Legs)?world.CountryEmblem(robot.Origin(part)):0,simplified:!detailed);
                    row++;
                }
                if(detailed && robot.ShoeOwner>=0)RobotWorkArt.DrawWindupKey(ui,card);
            }
            if(hidden>0 && MapViewport(panel).Intersects(overflow))
            {
                DrawRobotOverflow(ui,overflow,hidden,zoom);
                _robotBadges.Add(new(node.Functional?node.Id:node.FunctionHost,overflow,true));
            }
        }
    }
    public void DrawMapRobot(UiPainter ui,Rectangle r,Color color,RobotParts parts,bool damaged=false,bool cheap=false,bool dim=false,int pattern=0,Color? secondary=null,bool premium=false,int emblem=0,bool simplified=false)
    {
        _mapRobotTexture ??= LoadMapRobot(ui.Batch.GraphicsDevice);
        var tint=Color.Lerp(Color.White,color,.48f);
        if(dim)tint=SelectionGlow.Dim(tint,FocusBrightness);
        for(int part=0;part<3;part++)
        {
            if((parts&(RobotParts)(4>>part))==0)continue;
            int top=_mapRobotTexture.Height*part/3,bottom=_mapRobotTexture.Height*(part+1)/3;
            var source=new Rectangle(0,top,_mapRobotTexture.Width,bottom-top);
            int y=r.Y+r.Height*part/3,next=r.Y+r.Height*(part+1)/3;
            if(simplified || (!damaged && !cheap && !premium && pattern==0))ui.Batch.Draw(_mapRobotTexture,new Rectangle(r.X,y,r.Width,Math.Max(1,next-y)),source,tint);
            else for(int row=y;row<next;row++)
            {
                int sy=top+(row-y)*(bottom-top)/Math.Max(1,next-y);
                var area=new Rectangle(r.X,y,r.Width,Math.Max(1,next-y));
                var span=RobotPartSilhouette.Row(area,row,cheap,premium);
                for(int x=span.Left;x<span.Right;x++){
                    var ink=pattern!=0 && WorldSetup.CountryPatternSecondary(pattern,(x-r.X)/2,(row-r.Y)/2)?secondary??color:tint;
                    if(premium && (row==y || row==next-1))ink=new Color(255,215,100);
                    if(dim && pattern!=0 && ink!=tint)ink=SelectionGlow.Dim(ink,FocusBrightness);
                    int sx=(x-span.Left)*source.Width/Math.Max(1,span.Width);
                    var shaded=damaged?RobotDamageArt.Tint(area,x,row,ink):ink;
                    ui.Batch.Draw(_mapRobotTexture,new Rectangle(x,row,1,1),new Rectangle(sx,sy,1,1),shaded);
                }
            }

        }
        int emblemSide=Math.Max(1,r.Width/2),emblemTop=(parts&RobotParts.Body)!=0?r.Y+r.Height/3:(parts&RobotParts.Head)!=0?r.Y:r.Y+r.Height*2/3;
        if(!simplified)CountryEmblemArt.Draw(ui,new(r.Center.X-emblemSide/2,emblemTop,emblemSide,emblemSide),emblem,secondary??color,dim?FocusBrightness:1);
    }
    private Texture2D _mapRobotTexture;
    public void DisposeRobotTexture(){DisposeIceTextures();_mapRobotTexture?.Dispose();_mapRobotTexture=null;_mapleMapArt?.Dispose();_mapleMapArt=null;}
    private static Texture2D LoadMapRobot(GraphicsDevice device)
    {
        using var stream=File.OpenRead(Path.Combine(AppContext.BaseDirectory,"Content","Images","robot-map-toy-v001.png"));
        var texture=Texture2D.FromStream(device,stream);
        var pixels=new Color[texture.Width*texture.Height];texture.GetData(pixels);
        for(int i=0;i<pixels.Length;i++){var c=pixels[i];pixels[i]=new Color(c.R*c.A/255,c.G*c.A/255,c.B*c.A/255,(int)c.A);}
        texture.SetData(pixels);return texture;
    }
}
