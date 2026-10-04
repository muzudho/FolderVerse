namespace FolderVerse;
using System;
using System.Linq;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

public sealed partial class CubeNet
{
    public bool ShowFlags {get;set;}=true;
    public bool ShowBatteries {get;set;}=true;
    public bool ShowRoutes {get;set;}
    public HashSet<int> MovementTargets {get;}=new();
    public int SelectedTarget {get;set;}=-1;
    public Vector2 NodePosition(WorldSetup world,Node node,Rectangle panel)
    {
        var fit=Fit(panel);var face=Faces.First(f=>f.Face==world.Cells[node.Cell].Face);
        return face.Project(world.Routes.Position(node.Cell,node.Center))*fit.Scale+fit.Origin;
    }
    public int HitNode(WorldSetup world,Rectangle panel,Point pointer,IEnumerable<int> candidates=null)
    {
        if(IsAnimating || !panel.Contains(pointer))return -1;
        var fit=Fit(panel);float radius=MathHelper.Clamp(fit.Scale*.06f,4,10)+4;
        var point=new Vector2(pointer.X,pointer.Y);
        return (candidates??world.Nodes.All.Select(n=>n.Id)).Select(id=>(Id:id,Distance:Vector2.DistanceSquared(NodePosition(world,world.Nodes.All[id],panel),point)))
            .Where(n=>n.Distance<=radius*radius).OrderBy(n=>n.Distance).ThenBy(n=>n.Id).Select(n=>n.Id).DefaultIfEmpty(-1).First();
    }
    public Point MicroPoint(WorldSetup world,int cell,Rectangle panel,Point pointer)
    {
        var fit=Fit(panel);var face=Faces.First(f=>f.Face==world.Cells[cell].Face);
        var p=(new Vector2(pointer.X,pointer.Y)-fit.Origin)/fit.Scale-face.Center;
        var surface=world.Cells[cell];float normalDistance=Vector3.Dot(surface.Origin,surface.Normal);
        var physical=surface.Normal*normalDistance+face.Right*p.X-face.Up*p.Y;
        return new(Math.Clamp((int)MathF.Floor(Vector3.Dot(physical-surface.Origin,surface.U)*10),0,9),Math.Clamp((int)MathF.Floor(Vector3.Dot(physical-surface.Origin,surface.V)*10),0,9));
    }
    private void DrawCellRoutes(UiPainter ui,WorldSetup world,int cell,Func<Vector3,Vector2> project,float pulse)
    {
        foreach(var peak in world.Routes.Peaks(cell))
        {
            var point=world.Routes.Position(cell,peak);var surface=world.Cells[cell];
            var triangle=new[]{project(point-surface.V*.035f),project(point+surface.U*.028f+surface.V*.024f),project(point-surface.U*.028f+surface.V*.024f)};
            for(int edge=0;edge<3;edge++){var a=triangle[edge];var b=triangle[(edge+1)%3];var delta=b-a;ui.Tile((a+b)/2,new Vector2(delta.Length(),2),MathF.Atan2(delta.Y,delta.X),new Color(245,246,249));}
        }
        if(ShowRoutes)
        {
            var active=world.ConquerorLocations.Select((c,r)=>(Cell:c,Ruler:r)).Where(p=>p.Cell==cell)
                .Where(p=>world.Nodes.Current(p.Ruler)?.Center!=world.ConquerorPoints[p.Ruler])
                .SelectMany(p=>Enumerable.Range(0,4).Select(d=>world.Routes.ForRuler(p.Ruler,d))).Where(r=>r!=null);
            foreach(var route in world.Routes.DisplayPaths(cell).Concat(active))
            {
                var curve=world.Routes.Curve(cell,route);var offset=WorldTerrain.Offset(world.WorldSeed);
                for(int i=1;i<curve.Length;i++)
                {
                    var a=project(curve[i-1]);var b=project(curve[i]);var delta=b-a;
                    float height=WorldTerrain.Elevation((curve[i-1]+curve[i])/2,offset);
                    var color=height<.52f?new Color(111,228,255):height>=WorldTerrain.MountainHeight?new Color(255,163,70):new Color(255,230,140);
                    ui.Tile((a+b)/2,new Vector2(delta.Length()+1,4),MathF.Atan2(delta.Y,delta.X),new Color(13,36,44));
                    ui.Tile((a+b)/2,new Vector2(delta.Length()+1,2),MathF.Atan2(delta.Y,delta.X),color);
                }
            }
            foreach(var post in world.Nodes.InCell(cell))
            {
                var position=world.Routes.Position(cell,post.Center);var at=project(position);
                float radius=MathHelper.Clamp(Vector2.Distance(at,project(position+world.Cells[cell].U))*.06f,4,10);
                if(MovementTargets.Contains(post.Id) && MathF.Sin(pulse*6)>=0)
                {
                    Disc(ui,at,radius+7,new Color(255,219,96));
                    Disc(ui,at,radius+5,new Color(13,36,44));
                }
                if(SelectedTarget==post.Id)
                {
                    Disc(ui,at,radius+4,Color.White);
                    Disc(ui,at,radius+2,new Color(13,36,44));
                }
                Disc(ui,at,radius+2,new Color(13,36,44));
                Disc(ui,at,radius,world.OwnerColor(post.Owner));
            }
            foreach(var harbor in world.Routes.Harbors(cell))
            {
                var at=project(harbor.Position);var gold=new Color(255,204,83);
                ui.Tile(at,new Vector2(12,14),0,new Color(13,36,44));
                ui.Tile(at+new Vector2(0,-1),new Vector2(2,10),0,gold);
                ui.Tile(at+new Vector2(0,-3),new Vector2(7,2),0,gold);
                ui.Tile(at+new Vector2(0,4),new Vector2(9,2),0,gold);
                ui.Tile(at+new Vector2(-4,2),new Vector2(2,4),0,gold);
                ui.Tile(at+new Vector2(4,2),new Vector2(2,4),0,gold);
            }
        }
        if(ShowFlags)foreach(var post in world.Nodes.InCell(cell))
        {
            var point=project(world.Routes.Position(cell,post.Center));
            ui.Tile(point+new Vector2(0,-3),new Vector2(2,13),0,Color.White);
            ui.Tile(point+new Vector2(4,-6),new Vector2(8,5),0,world.OwnerColor(post.Owner));
        }
    }
    private static void Disc(UiPainter ui,Vector2 center,float radius,Color color)
    {
        for(float y=-radius+.5f;y<radius;y++)
        {
            float width=2*MathF.Sqrt(Math.Max(0,radius*radius-y*y));
            ui.Tile(center+new Vector2(0,y),new Vector2(width,1.1f),0,color);
        }
    }
}
