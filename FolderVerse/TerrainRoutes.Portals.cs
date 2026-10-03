namespace FolderVerse;
using System;
using System.Linq;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

public enum TravelTerrain { Sea,Land,Mountain }
public sealed record RoutePortal(int Target,int Direction,int First,int Last,TravelTerrain Terrain,Vector3 Position,Point Exit,Point Entry,bool Open);
public sealed partial class TerrainRoutes
{
    private bool[][] _passes;
    private RoutePortal[][][] _portals;
    private RouteStep[][] _display;
    private Point[][] _mountainPeaks;
    public Point[] Peaks(int cell)=>_mountainPeaks[cell];
    public TravelTerrain Terrain(int cell,Point p)=>Classify(_heights[cell][Id(p)]);
    private static TravelTerrain Classify(float h)=>h<.52f?TravelTerrain.Sea:h<WorldTerrain.MountainHeight?TravelTerrain.Land:TravelTerrain.Mountain;
    public bool MountainPass(int cell,Point p)=>_passes[cell][Id(p)];
    public bool Walkable(int cell,Point p)=>Terrain(cell,p)!=TravelTerrain.Mountain || MountainPass(cell,p);
    private static IEnumerable<int> Nearby(int id)
    {
        if(id%10>0)yield return id-1;if(id%10<9)yield return id+1;if(id>=10)yield return id-10;if(id<90)yield return id+10;
    }
    private void BuildMountainPasses()
    {
        _passes=_world.Cells.Select(_=>new bool[100]).ToArray();
        _mountainPeaks=new Point[_world.Cells.Length][];
        foreach(var cell in _world.Cells)
        {
            var peaks=new List<Point>();
            var visited=new HashSet<int>();
            for(int i=0;i<100;i++)
            {
                if(Classify(_heights[cell.Id][i])!=TravelTerrain.Mountain || !visited.Add(i))continue;
                var component=new List<int>{i};var queue=new Queue<int>();queue.Enqueue(i);
                while(queue.Count>0)foreach(int j in Nearby(queue.Dequeue()))
                    if(Classify(_heights[cell.Id][j])==TravelTerrain.Mountain && visited.Add(j)){component.Add(j);queue.Enqueue(j);}
                peaks.Add(PointAt(component.OrderByDescending(n=>_heights[cell.Id][n]).ThenBy(n=>n).First()));
                var boundary=component.Where(n=>_heights[cell.Id][n]<.82f && Nearby(n).Any(j=>Classify(_heights[cell.Id][j])==TravelTerrain.Land)).ToArray();
                if(boundary.Length<2 || new SeedRandom(_world.WorldSeed^cell.Id*374761^i*19349663).Next(3)!=0)continue;
                int start=boundary.OrderBy(n=>_heights[cell.Id][n]).ThenBy(n=>n).First();
                int end=boundary.OrderByDescending(n=>Vector2.DistanceSquared(new Vector2(n%10,n/10),new Vector2(start%10,start/10))).ThenBy(n=>n).First();
                var costs=component.ToDictionary(n=>n,_=>float.PositiveInfinity);var previous=new Dictionary<int,int>{{start,-1}};
                costs[start]=0;var remaining=new HashSet<int>(component.Where(n=>_heights[cell.Id][n]<.82f));
                while(remaining.Count>0)
                {
                    int current=remaining.OrderBy(n=>costs[n]).ThenBy(n=>n).First();remaining.Remove(current);
                    if(float.IsPositiveInfinity(costs[current]))break;if(current==end)break;
                    foreach(int next in Nearby(current).Where(remaining.Contains))
                    {
                        float cost=costs[current]+1+(_heights[cell.Id][next]-WorldTerrain.MountainHeight)*40;
                        if(cost>=costs[next])continue;costs[next]=cost;previous[next]=current;
                    }
                }
                if(!previous.ContainsKey(end))continue;
                for(int n=end;n>=0;n=previous[n])_passes[cell.Id][n]=true;
            }
            if(Enumerable.Range(0,100).All(n=>Classify(_heights[cell.Id][n])==TravelTerrain.Mountain) && !_passes[cell.Id].Any(p=>p))_passes[cell.Id][44]=true;
            _mountainPeaks[cell.Id]=peaks.ToArray();
        }
    }
    public RoutePortal[] Portals(int cell,int direction)=>_portals[cell][direction];
    public RouteStep[] DisplayPaths(int cell)=>_display[cell];
    private Point EdgePoint(int cell,Vector3 edgePosition)
    {
        var c=_world.Cells[cell];return new(Math.Clamp((int)MathF.Floor(Vector3.Dot(edgePosition-c.Origin,c.U)*10),0,9),Math.Clamp((int)MathF.Floor(Vector3.Dot(edgePosition-c.Origin,c.V)*10),0,9));
    }
    private void BuildPortals()
    {
        _portals=_world.Cells.Select(_=>new RoutePortal[4][]).ToArray();var offset=WorldTerrain.Offset(_world.WorldSeed);
        foreach(var cell in _world.Cells)for(int d=0;d<4;d++)
        {
            int target=_world.Population.Neighbor(_world,cell.Id,d);
            var shared=cell.Corners.Intersect(_world.Cells[target].Corners).OrderBy(p=>p.X).ThenBy(p=>p.Y).ThenBy(p=>p.Z).ToArray();
            var kinds=Enumerable.Range(0,10).Select(i=>Classify(WorldTerrain.Elevation(Vector3.Lerp(shared[0],shared[1],(i+.5f)/10),offset))).ToArray();
            var portals=new List<RoutePortal>();
            for(int first=0;first<10;)
            {
                int last=first;while(last<9 && kinds[last+1]==kinds[first])last++;
                var position=Vector3.Lerp(shared[0],shared[1],(first+last+1)/20f);
                // Even-length runs have two middle pixels: prefer a passable central pair.
                Point exit=default,entry=default;bool open=false;
                foreach(int middle in new[]{(first+last)/2,(first+last+1)/2}.Distinct())
                {
                    var sample=Vector3.Lerp(shared[0],shared[1],(middle+.5f)/10);
                    var a=EdgePoint(cell.Id,sample);var b=EdgePoint(target,sample);
                    bool allowed=Walkable(cell.Id,a) && Walkable(target,b) && Open(_heights[cell.Id][Id(a)],_heights[target][Id(b)],position);
                    if(!open){exit=a;entry=b;}if(allowed){exit=a;entry=b;open=true;break;}
                }
                portals.Add(new(target,d,first,last,kinds[first],position,exit,entry,open));first=last+1;
            }
            _portals[cell.Id][d]=portals.ToArray();
        }
        _display=_world.Cells.Select(c=>BuildDisplay(c.Id)).ToArray();
    }
    private static Point[] Trace(Dictionary<int,int> previous,int last)
    {
        var path=new List<Point>();for(int n=last;n>=0;n=previous[n])path.Add(PointAt(n));path.Reverse();return path.ToArray();
    }
    private RouteStep[] BuildDisplay(int cell)
    {
        var result=new List<RouteStep>();var covered=new HashSet<int>();
        foreach(var portal in _portals[cell].SelectMany(p=>p).Where(p=>p.Open))
        {
            int id=Id(portal.Exit);if(covered.Contains(id))continue;
            var component=Reach(cell,portal.Exit,false,true);
            foreach(int point in component.Keys)covered.Add(point);
            int root=component.Keys.OrderBy(n=>Vector2.DistanceSquared(new Vector2(n%10,n/10),new Vector2(4.5f))).ThenBy(n=>n).First();
            var previous=Reach(cell,PointAt(root),false,true);
            foreach(var next in _portals[cell].SelectMany(p=>p).Where(p=>p.Open && previous.ContainsKey(Id(p.Exit))))
                result.Add(new(next.Target,next.Exit,next.Entry,Trace(previous,Id(next.Exit))){Portal=next});
        }
        return result.ToArray();
    }
    public static string TerrainName(TravelTerrain terrain)=>terrain switch {TravelTerrain.Sea=>"海",TravelTerrain.Land=>"陸",_=>"山"};
    public Vector3[] Curve(int cell,RouteStep route)
    {
        var result=new List<Vector3>();var path=route.Path;if(path.Length==0)return Array.Empty<Vector3>();
        result.Add(Position(cell,path[0]));
        for(int i=1;i<path.Length-1;i++)
        {
            var a=Position(cell,path[i-1]);var b=Position(cell,path[i]);var c=Position(cell,path[i+1]);
            if(Vector3.Cross(b-a,c-b).LengthSquared()<.000001f){result.Add(b);continue;}
            // Round only inside the middle terrain pixel; never cut across a wall.
            var before=Vector3.Lerp(b,a,.35f);var after=Vector3.Lerp(b,c,.35f);result.Add(before);
            for(int j=1;j<=6;j++){float t=j/6f;result.Add(before*(1-t)*(1-t)+b*2*t*(1-t)+after*t*t);}
        }
        if(path.Length>1)result.Add(Position(cell,path[^1]));
        if(route.Portal!=null)result.Add(route.Portal.Position);return result.ToArray();
    }
}
