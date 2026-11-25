namespace FolderVerse;
using System;
using System.Linq;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

public sealed partial class CubeNet
{
    private ScenicRoutes _scenicRoutes;
    private void RefreshScenicRoutes(WorldSetup world)
    {
        if (ShowRoutes && !DirectoryOnly && (_scenicRoutes == null || !_scenicRoutes.Matches(world)))
            _scenicRoutes = new(world);
    }
    public HashSet<int> VisibleOperationNodes {get;set;}
    public int[] NodeOwnerSnapshot {get;set;}
    public int DisplayedNodeOwner(Node node)=>NodeOwnerSnapshot!=null && node.Id<NodeOwnerSnapshot.Length?NodeOwnerSnapshot[node.Id]:node.Owner;
    public bool DirectoryOnly {get;set;}
    public bool AnimateFlags {get;set;}=true;
    public float? DawnMeridian {get;set;}
    public bool ShowSmoke {get;set;}=true;
    public bool ShowAdjacentObjects {get;set;}=true;
    public bool ShowMapSymbols {get;set;}=true;
    public bool ShowEquator {get;set;}=true;
    public bool ShowRobots {get;set;}=true;
    public int HiddenMapFilters {get;set;}
    public bool ShowOtherCountries
    {
        get=>MapFilterEnabled(2);
        set=>SetMapFilter(2,value);
    }
    internal bool MapFilterEnabled(int index)=>(HiddenMapFilters&(1<<index))==0;
    internal bool MapFilterDanger(int index)=>(HiddenMapFilters&(1<<(16+index)))!=0;
    internal int MapFilterMode(int index)=>!MapFilterEnabled(index)?2:MapFilterDanger(index)?1:0;
    internal void CycleMapFilter(int index)
    {
        if(index!=5 && index!=7 && index!=8){SetMapFilter(index,!MapFilterEnabled(index));return;}
        int next=(MapFilterMode(index)+1)%3;
        HiddenMapFilters&=~((1<<index)|(1<<(16+index)));
        if(next==1)HiddenMapFilters|=1<<(16+index);
        if(next==2)HiddenMapFilters|=1<<index;
    }
    internal void SetMapFilter(int index,bool enabled)
    {
        if(enabled)HiddenMapFilters&=~(1<<index);
        else HiddenMapFilters|=1<<index;
    }
    public bool ShowBatteries {get;set;}=true;
    public bool ShowCountryColors {get;set;}
    public bool ControlledCountryColors {get;set;}
    public Color TerrainColor(WorldSetup world,int cell,float height,int x=0,int y=0)
    {
        int owner=ShowCountryColors?world.Nodes.FullOwner(cell):-1;
        if(ControlledCountryColors && !world.Relations.Controls(owner))owner=-1;
        var color=owner<0?WorldTerrain.MapColorAt(height,world.Cells[cell].Normal,world.PolarAxis,world.WorldSeed):WorldPreview.LandTerritoryColor(world.CountryMapColor(owner,x,y),WorldTerrain.Kind(height));
        color=WorldTerrain.WaterSurfaceColor(world,cell,height,x,y,color);
        var surface=world.Cells[cell];
        var point=surface.Origin+surface.U*((x+.5f)/TerrainRoutes.Detail)+surface.V*((y+.5f)/TerrainRoutes.Detail);
        if(WorldTerrain.IsValley(world,surface,point,height))color=WorldTerrain.ValleyColor(color);
        color=MapTerrainColors.ForTile(color,HighlightTiles.Contains(new(cell,x,y)) || SelectedCell==cell);
        if(DawnMeridian is {} dawn)color=WorldDayCycle.Tint(color,WorldDayCycle.Hour(world,world.Cells[cell],dawn));
        return color;
    }
    public bool ShowRoutes {get;set;}=true; public int HoveredNode {get;set;}=-1; public int InspectedNode {get;set;}=-1;
    public bool AttackTargetMarkers {get;set;}
    public bool RestrictNodeActions {get;set;}
    public int HoverSuppressedNode {get;set;}=-1;
    public HashSet<int> VisibleOperationBatteries {get;set;}
    public bool RevealMovementTargets {get;set;}
    private static int MapOwnerGroup(WorldSetup world,int owner)
    {
        int player=world.PlayerSlot;
        if(owner<0 || owner>=world.ActiveCount || player<0)return 2;
        if(owner==player)return 0;
        var parents=world.Relations.Superiors;
        for(int i=0;i<parents.Length && owner>=0;i++,owner=parents[owner])
            if(owner==player)return 1;
        return 2;
    }
    public bool NodeVisible(WorldSetup world,Node node)=>
        (VisibleOperationNodes==null || VisibleOperationNodes.Contains(node.Id)) &&
        (HiddenMapFilters==0 || MapFilterEnabled(MapOwnerGroup(world,DisplayedNodeOwner(node))) ||
         RevealMovementTargets && MovementTargets.Contains(node.Id));

    private int MapEdgeRow(WorldSetup world,Node a,Node b)
    {
        if(a==null || b==null || a.Id==b.Id)return -1;
        int start=MapOwnerGroup(world,DisplayedNodeOwner(a)),end=MapOwnerGroup(world,DisplayedNodeOwner(b));
        int low=Math.Min(start,end),high=Math.Max(start,end);
        int row=low switch { 0=>3+high,1=>5+high,_=>8 };
        return row;
    }
    internal bool MapEdgeVisible(WorldSetup world,Node a,Node b){int row=MapEdgeRow(world,a,b);return row<0 || MapFilterEnabled(row);}
    internal bool MapEdgeDanger(WorldSetup world,Node a,Node b){int row=MapEdgeRow(world,a,b);return row>=0 && row!=8 && MapFilterEnabled(row) && MapFilterDanger(row);}
    internal bool MapEdgeDark(WorldSetup world,Node a,Node b)=>MapEdgeRow(world,a,b)==8 && MapFilterMode(8)==1;
    internal static Color DarkRoadColor(bool sea)=>sea?new Color(57,76,91):new Color(103,97,61);
    public bool EmphasizedNode(int id)=>false; // Hover is a HUD ring, never a change in object geometry.
    public HashSet<int> MovementTargets {get;}=new();
    public HashSet<int> ActionTargetNodes {get;}=new();
    public int SelectedTarget {get;set;}=-1;
    public Vector2 NodePosition(WorldSetup world,Node node,Rectangle panel)
    {
        var fit=Fit(panel);var face=Faces.First(f=>f.Face==world.Cells[node.Cell].Face);
        var at=face.Project(SiteDisplayPosition(world,node,ResourceClearance(world,node.Cell)))*fit.Scale+fit.Origin;
        return _drawing?at:RotateScreen(at,panel);
    }
    public int HitNode(WorldSetup world,Rectangle panel,Point pointer,IEnumerable<int> candidates=null)
    {
        if(IsAnimating || !panel.Contains(pointer))return -1;
        var fit=Fit(panel);
        var point=pointer.ToVector2();
        var clearances=new System.Collections.Generic.Dictionary<int,(Vector3 Position,float Radius)[]>();
        int hit=world.Nodes.All.Where(n=>NodeVisible(world,n)).Select(n=>
        {
            float puckRadius=MapPuckRadius(n,fit.Scale);
            float radius=puckRadius*(ShowRoutes && HasFacilityPlatform(n)?FacilityPlatformScale:1)+4;
            if(!clearances.TryGetValue(n.Cell,out var obstacles))
                clearances[n.Cell]=obstacles=ResourceClearance(world,n.Cell);
            var display=SiteDisplayPosition(world,n,obstacles);
            float distance=Vector2.DistanceSquared(AnimatedSurfacePosition(n.Cell,display,world,panel),point);
            bool pier=false;
            if(ShowRoutes && !DirectoryOnly && n.IsHarbor && n.Functional && TryHarborFrame(world,n,out var shore,out var seaward)){
                var face=Faces.First(f=>f.Face==world.Cells[n.Cell].Face);
                var at=face.Project(shore)*fit.Scale+fit.Origin;
                var forward=Vector2.Normalize((face.Project(shore+seaward*.1f)-face.Project(shore))*fit.Scale);
                var across=new Vector2(forward.Y,-forward.X);
                pier=HarborPierContains(point,(x,y,z)=>RotateScreen(FlatHarborPoint(at,forward,across,puckRadius,x,y),panel),FacilityDevelopment.Grade(world,n));
            }
            return (n.Id,Pier:pier,Distance:pier || distance<=radius*radius?distance:float.PositiveInfinity);
        }).Where(n=>float.IsFinite(n.Distance)).OrderByDescending(n=>n.Pier).ThenBy(n=>n.Distance).ThenBy(n=>n.Id)
            .Select(n=>n.Id).DefaultIfEmpty(-1).First();        return hit>=0 && (candidates==null || candidates.Contains(hit))?hit:-1;
    }
    public Point MicroPoint(WorldSetup world,int cell,Rectangle panel,Point pointer)
    {
        var fit=Fit(panel);var face=Faces.First(f=>f.Face==world.Cells[cell].Face);
        pointer=UnrotateScreen(pointer,panel);
        var p=(new Vector2(pointer.X,pointer.Y)-fit.Origin)/fit.Scale-face.Center;
        var surface=world.Cells[cell];float normalDistance=Vector3.Dot(surface.Origin,surface.Normal);
        var physical=surface.Normal*normalDistance+face.Right*p.X-face.Up*p.Y;
        return new(Math.Clamp((int)MathF.Floor(Vector3.Dot(physical-surface.Origin,surface.U)*10),0,9),Math.Clamp((int)MathF.Floor(Vector3.Dot(physical-surface.Origin,surface.V)*10),0,9));
    }
    private void DrawCellRoutes(UiPainter ui,WorldSetup world,int cell,Func<Vector3,Vector2> project,float pulse,bool drawNodes=true,Func<Vector2,Vector2> focusPoint=null,bool globeRibbon=false,Action<RoutePortal,bool,bool,Func<Color,Vector2,Color>> joinPortal=null)
    {
        Color SearchTint(Color color,Vector2 at)=>color;
        foreach(var peak in world.Routes.Peaks(cell))
        {
            var point=world.Routes.Position(cell,peak);var surface=world.Cells[cell];
            var triangle=new[]{project(point-surface.V*.035f),project(point+surface.U*.028f+surface.V*.024f),project(point-surface.U*.028f+surface.V*.024f)};
            for(int edge=0;edge<3;edge++){var a=triangle[edge];var b=triangle[(edge+1)%3];var delta=b-a;ui.Tile((a+b)/2,new Vector2(delta.Length(),2),MathF.Atan2(delta.Y,delta.X),SearchTint(new Color(245,246,249),(a+b)/2));}
        }
        if(ShowRoutes)
        {
            var active=world.ConquerorLocations.Select((c,r)=>(Cell:c,Ruler:r)).Where(p=>p.Cell==cell)
                .Where(p=>world.Nodes.Current(p.Ruler)?.Center!=world.ConquerorPoints[p.Ruler])
                .SelectMany(p=>Enumerable.Range(0,4).Select(d=>world.Routes.ForRuler(p.Ruler,d))).Where(r=>r!=null);
            var ribbons = globeRibbon ? new List<(Vector2 Center, float Length, float Angle, bool Sea, bool Dark)>() : null;
            var vehicles = new List<ScenicRoutes.Vehicle>();
            var offset=world.Terrain;
            foreach(var route in world.Routes.DisplayPaths(cell).Concat(active))
            {
                if(HiddenMapFilters>>3!=0 && route.Path.Length>0 &&
                    !MapEdgeVisible(world,world.Nodes.At(cell,route.Path[0]),world.Nodes.At(route.Target,route.Entry)))continue;
                bool dark=route.Path.Length>0 && MapEdgeDark(world,world.Nodes.At(cell,route.Path[0]),world.Nodes.At(route.Target,route.Entry));
                Color RouteTint(Color ink,Vector2 at)=>dark?DarkRoadColor(route.Portal?.Terrain==TravelTerrain.Sea):SearchTint(ink,at);
                bool danger=route.Path.Length>0 && MapEdgeDanger(world,world.Nodes.At(cell,route.Path[0]),world.Nodes.At(route.Target,route.Entry));
                if (!danger && !dark && !DirectoryOnly && _scenicRoutes != null &&
                    _scenicRoutes.TryDraw(route,pulse,DrawScenicDash,out var vehicle))
                {
                    if (vehicle is { } toy) vehicles.Add(toy);
                    if(route.Portal!=null)joinPortal?.Invoke(route.Portal,true,false,SearchTint);
                    continue;
                }
                var curve=world.Routes.Curve(cell,route);
                if(curve.Length>0 && route.Portal!=null)joinPortal?.Invoke(route.Portal,false,danger,RouteTint);
                float dangerPhase=0;
                for(int i=1;i<curve.Length;i++)
                {
                    var a=project(curve[i-1]);var b=project(curve[i]);var delta=b-a;
                    if(danger){DrawDangerRoad(ui,a,b,ref dangerPhase,globeRibbon);continue;}
                    float height=offset.Elevation((curve[i-1]+curve[i])/2);
                    if (ribbons != null)
                    {
                        if (delta.LengthSquared() > .001f)
                            ribbons.Add(((a+b)/2, delta.Length()+2, MathF.Atan2(delta.Y,delta.X), height<.52f,dark));
                        continue;
                    }
                    var color=height<.52f?new Color(111,228,255):height>=WorldTerrain.MountainHeight?new Color(255,163,70):new Color(255,230,140);
                    ui.Tile((a+b)/2,new Vector2(delta.Length()+1,4),MathF.Atan2(delta.Y,delta.X),SearchTint(new Color(13,36,44),(a+b)/2));
                    ui.Tile((a+b)/2,new Vector2(delta.Length()+1,2),MathF.Atan2(delta.Y,delta.X),SearchTint(dark?DarkRoadColor(height<.52f):color,(a+b)/2));
                }
            }
            if (ribbons != null)
            {
                // Draw all sides before the top so adjoining segments form one ribbon.
                foreach (var r in ribbons)
                    ui.Tile(r.Center+new Vector2(0,2),new(r.Length,7),r.Angle,
                        SearchTint(r.Dark ? Color.Lerp(DarkRoadColor(r.Sea),Color.Black,.25f) : r.Sea ? new Color(39,107,137) : new Color(145,99,33),r.Center));
                foreach (var r in ribbons)
                    ui.Tile(r.Center,new(r.Length,6),r.Angle,
                        SearchTint(r.Dark ? DarkRoadColor(r.Sea) : r.Sea ? new Color(108,213,245) : new Color(255,216,99),r.Center));
            }
            foreach (var vehicle in vehicles)
                DrawScenicVehicle(ui,world.Cells[cell],vehicle,project,globeRibbon,
                    color=>SearchTint(color,project(vehicle.At)));
            if(drawNodes)DrawNodeMarkers(ui,world,cell,project,pulse);

            void DrawScenicDash(Vector3 from, Vector3 to, bool sea)
            {
                var a = project(from); var b = project(to);
                var delta = b-a;
                if (delta.LengthSquared() < .001f) return;
                var center = (a+b)/2;
                var angle = MathF.Atan2(delta.Y,delta.X);
                var ink = sea ? new Color(111,228,255) : new Color(255,230,140);
                ui.Tile(center,new(delta.Length()+.5f,globeRibbon ? 5 : 4),angle,SearchTint(new Color(38,53,56),center));
                ui.Tile(center,new(delta.Length(),globeRibbon ? 3 : 2),angle,SearchTint(ink,center));
            }
        }
    }
    private void DrawNodeMarkers(UiPainter ui,WorldSetup world,int cell,Func<Vector3,Vector2> project,float pulse)
    {
        if (DirectoryOnly) return;
        DrawResourceSites(ui,world,cell,project,globe:false);
        DrawLandmarks(ui,world,cell,p=>project(p),pulse);
        var obstacles=ResourceClearance(world,cell);
        foreach (var node in world.Nodes.InCell(cell).Where(n=>NodeVisible(world,n)))
        {
            DrawMapleDeposit(ui,world,node,project,globe:false);
            var position=SiteDisplayPosition(world,node,obstacles);
            var at=project(position);
            float radius=MapPuckRadius(node,Vector2.Distance(at,project(position+world.Cells[cell].U)));
            DrawFlatPuck(ui,world,node,at,radius,pulse,color=>NodeTint(node.Id,color),project,color=>NodeFlagTint(node.Id,color));
        }
    }
    private static void DrawHarborBadge(UiPainter ui,Vector2 at,float radius,Func<Color,Color> tint,float rotation=0)
    {
        void Stroke(Vector2 a,Vector2 b,float width,Color color)
        {
            a=at+RotatePoint(a*radius,-rotation);b=at+RotatePoint(b*radius,-rotation);var delta=b-a;
            ui.Tile((a+b)/2,new(delta.Length()+width,width),MathF.Atan2(delta.Y,delta.X),color);
        }
        void Shape(float width,Color tint)
        {
            Stroke(new(0,-.55f),new(0,.7f),width,tint);
            Stroke(new(-.48f,-.2f),new(.48f,-.2f),width,tint);
            Stroke(new(0,.7f),new(-.7f,.35f),width,tint);Stroke(new(0,.7f),new(.7f,.35f),width,tint);
            Stroke(new(-.7f,.35f),new(-.7f,.05f),width,tint);Stroke(new(.7f,.35f),new(.7f,.05f),width,tint);
        }
        var dark=tint(new Color(13,36,44));var light=tint(new Color(247,239,216));
        Disc(ui,at,radius+3,dark);Disc(ui,at,radius+1,light);
        Shape(2,dark);
        var eye=at+RotatePoint(new Vector2(0,-.65f)*radius,-rotation);
        Disc(ui,eye,radius*.26f,dark);Disc(ui,eye,radius*.12f,light);
    }
    private static void Disc(UiPainter ui,Vector2 center,float radius,Color color,bool dim=false)
    {
        for(float y=-radius+.5f;y<radius;y++)
        {
            float width=2*MathF.Sqrt(Math.Max(0,radius*radius-y*y));
            ui.Tile(center+new Vector2(0,y),new Vector2(width,1.1f),0,color);
        }
    }
}



