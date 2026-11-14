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
    public bool ShowMapSymbols {get;set;}=true;
    public bool ShowEquator {get;set;}=true;
    public bool ShowRobots {get;set;}=true;
    public bool ShowOtherCountries {get;set;}=true;
    public bool ShowBatteries {get;set;}=true;
    public bool ShowCountryColors {get;set;}
    public bool ControlledCountryColors {get;set;}
    public Color TerrainColor(WorldSetup world,int cell,float height,int x=0,int y=0)
    {
        int owner=ShowCountryColors?world.Nodes.FullOwner(cell):-1;
        if(ControlledCountryColors && !world.Relations.Controls(owner))owner=-1;
        var color=owner<0?WorldTerrain.ColorAt(height,world.Cells[cell].Normal,world.PolarAxis):WorldPreview.LandTerritoryColor(world.CountryMapColor(owner,x,y),WorldTerrain.Kind(height));
        var surface=world.Cells[cell];
        var point=world.Routes.Position(cell,new Point(x,y));
        if(WorldTerrain.IsValley(world,surface,point,height))color=WorldTerrain.ValleyColor(color);
        return NodeFocus<0 && RegionFocusCell>=0 && cell!=RegionFocusCell?SelectionGlow.Dim(color,FocusBrightness):color;
    }
    public bool ShowRoutes {get;set;}=true; public int HoveredNode {get;set;}=-1; public int InspectedNode {get;set;}=-1;
    public bool AttackTargetMarkers {get;set;}
    public bool AttackTargetSpotlight {get;set;}
    public int HoverSuppressedNode {get;set;}=-1;
    public HashSet<int> VisibleOperationBatteries {get;set;}
    public bool RevealMovementTargets {get;set;}
    public bool NodeVisible(WorldSetup world,Node node)=>(VisibleOperationNodes==null || VisibleOperationNodes.Contains(node.Id)) && (ShowOtherCountries || DisplayedNodeOwner(node)<0 || world.Relations.Controls(DisplayedNodeOwner(node)) || RevealMovementTargets && MovementTargets.Contains(node.Id));
    public bool EmphasizedNode(int id)=>id!=HoverSuppressedNode && (AttackTargetSpotlight?AttackHighlightTargets.Contains(id):CountryFocus<0 && NodeFocus<0) && (HoveredNode==id || InspectedNode==id);
    public HashSet<int> MovementTargets {get;}=new();
    public HashSet<int> AttackHighlightTargets {get;}=new();
    public int SelectedTarget {get;set;}=-1;
    public Vector2 NodePosition(WorldSetup world,Node node,Rectangle panel)
    {
        var fit=Fit(panel);var face=Faces.First(f=>f.Face==world.Cells[node.Cell].Face);
        var at=face.Project(world.Routes.Position(node.Cell,node.Center))*fit.Scale+fit.Origin;
        return _drawing?at:RotateScreen(at,panel);
    }
    public int HitNode(WorldSetup world,Rectangle panel,Point pointer,IEnumerable<int> candidates=null)
    {
        if(IsAnimating || !panel.Contains(pointer))return -1;
        var fit=Fit(panel);
        var point=pointer.ToVector2();
        int hit=world.Nodes.All.Where(n=>NodeVisible(world,n)).Select(n=>
        {
            float radius=MapPuckRadius(n,fit.Scale)+4;
            float distance=Vector2.DistanceSquared(NodePosition(world,n,panel),point);
            return (n.Id,Distance:distance<=radius*radius?distance:float.PositiveInfinity);
        }).Where(n=>float.IsFinite(n.Distance)).OrderBy(n=>n.Distance).ThenBy(n=>n.Id)
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
    private void DrawCellRoutes(UiPainter ui,WorldSetup world,int cell,Func<Vector3,Vector2> project,float pulse,bool drawNodes=true,Func<Vector2,Vector2> focusPoint=null,bool globeRibbon=false,Action<RoutePortal,bool,Func<Color,Vector2,Color>> joinPortal=null)
    {
        Color SearchTint(Color color,Vector2 at){
            if(!FolderSearchFocus && !AttackTargetSpotlight)return color;
            if(RegionFocusCell>=0)return cell==RegionFocusCell?color:SelectionGlow.Dim(color,FocusBrightness);
            return FocusTerrain(color,focusPoint?.Invoke(at)??at,cell);
        }
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
            var ribbons = globeRibbon ? new List<(Vector2 Center, float Length, float Angle, bool Sea)>() : null;
            var vehicles = new List<ScenicRoutes.Vehicle>();
            var offset=world.Terrain;
            foreach(var route in world.Routes.DisplayPaths(cell).Concat(active))
            {
                if (!DirectoryOnly && _scenicRoutes != null &&
                    _scenicRoutes.TryDraw(route,pulse,DrawScenicDash,out var vehicle))
                {
                    if (vehicle is { } toy) vehicles.Add(toy);
                    if(route.Portal!=null)joinPortal?.Invoke(route.Portal,true,SearchTint);
                    continue;
                }
                var curve=world.Routes.Curve(cell,route);
                if(curve.Length>0 && route.Portal!=null)joinPortal?.Invoke(route.Portal,false,SearchTint);
                for(int i=1;i<curve.Length;i++)
                {
                    var a=project(curve[i-1]);var b=project(curve[i]);var delta=b-a;
                    float height=offset.Elevation((curve[i-1]+curve[i])/2);
                    if (ribbons != null)
                    {
                        if (delta.LengthSquared() > .001f)
                            ribbons.Add(((a+b)/2, delta.Length()+2, MathF.Atan2(delta.Y,delta.X), height<.52f));
                        continue;
                    }
                    var color=height<.52f?new Color(111,228,255):height>=WorldTerrain.MountainHeight?new Color(255,163,70):new Color(255,230,140);
                    ui.Tile((a+b)/2,new Vector2(delta.Length()+1,4),MathF.Atan2(delta.Y,delta.X),SearchTint(new Color(13,36,44),(a+b)/2));
                    ui.Tile((a+b)/2,new Vector2(delta.Length()+1,2),MathF.Atan2(delta.Y,delta.X),SearchTint(color,(a+b)/2));
                }
            }
            if (ribbons != null)
            {
                // Draw all sides before the top so adjoining segments form one ribbon.
                foreach (var r in ribbons)
                    ui.Tile(r.Center+new Vector2(0,2),new(r.Length,7),r.Angle,
                        SearchTint(r.Sea ? new Color(39,107,137) : new Color(145,99,33),r.Center));
                foreach (var r in ribbons)
                    ui.Tile(r.Center,new(r.Length,6),r.Angle,
                        SearchTint(r.Sea ? new Color(108,213,245) : new Color(255,216,99),r.Center));
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
        foreach (var node in world.Nodes.InCell(cell).Where(n=>NodeVisible(world,n)))
        {
            DrawMapleDeposit(ui,world,node,project,globe:false);
            var position=world.Routes.Position(cell,node.Center);
            var at=project(position);
            float radius=MapPuckRadius(node,Vector2.Distance(at,project(position+world.Cells[cell].U)));
            DrawFlatPuck(ui,world,node,at,radius,pulse,color=>NodeTint(node.Id,color));
        }
    }
    private static void DrawHarbor(UiPainter ui,Vector2 at,float radius,Color color,bool dim=false)
    {
        void Stroke(Vector2 a,Vector2 b,float width,Color tint)
        {
            a=at+a*radius;b=at+b*radius;var delta=b-a;
            ui.Tile((a+b)/2,new(delta.Length()+width,width),MathF.Atan2(delta.Y,delta.X),tint);
        }
        void Shape(float width,Color tint)
        {
            Stroke(new(0,-.55f),new(0,.7f),width,tint);
            Stroke(new(-.48f,-.2f),new(.48f,-.2f),width,tint);
            Stroke(new(0,.7f),new(-.7f,.35f),width,tint);Stroke(new(0,.7f),new(.7f,.35f),width,tint);
            Stroke(new(-.7f,.35f),new(-.7f,.05f),width,tint);Stroke(new(.7f,.35f),new(.7f,.05f),width,tint);
        }
        var dark=dim?SelectionGlow.Dim(new Color(13,36,44)):new Color(13,36,44);Shape(6,dark);Shape(3,color);
        var eye=at+new Vector2(0,-.65f)*radius;
        Disc(ui,eye,radius*.27f+2,dark);Disc(ui,eye,radius*.27f,color);Disc(ui,eye,radius*.12f,dark);
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



