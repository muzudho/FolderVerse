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
        _display=_world.Cells.Select(_=>Array.Empty<RouteStep>()).ToArray();
    }
    public void RefreshDisplay()
    {
        _curves.Clear();_display=_world.Cells.Select(c=>BuildDisplay(c.Id)).ToArray();
    }
    private static Point[] Trace(Dictionary<int,int> previous,int last)
    {
        var path=new List<Point>();for(int n=last;n>=0;n=previous[n])path.Add(PointAt(n));path.Reverse();return path.ToArray();
    }
    private RouteStep[] BuildDisplay(int cell)
    {
        var result=new List<RouteStep>();var covered=new HashSet<int>();
        foreach(var post in _world.Outposts.InCell(cell))
        {
            int id=Id(post.Center);if(covered.Contains(id))continue;
            var component=Reach(cell,post.Center,false,true);
            foreach(int point in component.Keys)covered.Add(point);
            var nodes=_world.Outposts.InCell(cell).Where(p=>component.ContainsKey(Id(p.Center))).ToArray();
            int root=nodes.OrderBy(p=>Vector2.DistanceSquared(new Vector2(p.Center.X,p.Center.Y),new Vector2(4.5f))).ThenBy(p=>p.Id).Select(p=>Id(p.Center)).First();
            var previous=Reach(cell,PointAt(root),false,true);
            foreach(var node in nodes.Where(p=>Id(p.Center)!=root))
                result.Add(new(cell,node.Center,node.Center,Trace(previous,Id(node.Center))));
            foreach(var next in _portals[cell].SelectMany(p=>p).Where(p=>p.Open && previous.ContainsKey(Id(p.Exit))))
                result.Add(new(next.Target,next.Exit,next.Entry,Trace(previous,Id(next.Exit))){Portal=next});
        }
        return result.ToArray();
    }
    public static string TerrainName(TravelTerrain terrain)=>terrain switch {TravelTerrain.Sea=>"海",TravelTerrain.Land=>"陸",_=>"山"};
    private readonly Dictionary<RouteStep,Vector3[]> _curves=new();
    public Vector3[] Curve(int cell,RouteStep route)
    {
        if(_curves.TryGetValue(route,out var cached))return cached;
        if(route.Path.Length==0)return Array.Empty<Vector3>();
        var surface=_world.Cells[cell];var reachable=Reach(cell,route.Path[0],false,true);
        // Validate shortcuts against the actual connected terrain, including closed edges.
        bool Safe(IEnumerable<Vector3> samples)
        {
            var exact=new List<Vector3>();Vector3? last=null;
            foreach(var point in samples)
            {
                if(last.HasValue)
                {
                    var from=last.Value;var a=from-surface.Origin;var b=point-surface.Origin;
                    float ax=Vector3.Dot(a,surface.U)*10,ay=Vector3.Dot(a,surface.V)*10;
                    float bx=Vector3.Dot(b,surface.U)*10,by=Vector3.Dot(b,surface.V)*10;
                    var cuts=new List<float>{0,1};
                    for(int boundary=1;boundary<10;boundary++)
                    {
                        if(Math.Abs(bx-ax)>.000001f){float t=(boundary-ax)/(bx-ax);if(t>0 && t<1)cuts.Add(t);}
                        if(Math.Abs(by-ay)>.000001f){float t=(boundary-ay)/(by-ay);if(t>0 && t<1)cuts.Add(t);}
                    }
                    cuts.Sort();
                    for(int i=1;i<cuts.Count;i++)exact.Add(Vector3.Lerp(from,point,(cuts[i-1]+cuts[i])/2));
                }
                exact.Add(point);last=point;
            }
            int previous=-1;
            foreach(var position in exact)
            {
                var delta=position-surface.Origin;float x=Vector3.Dot(delta,surface.U)*10,y=Vector3.Dot(delta,surface.V)*10;
                if(x<-.0001f || y<-.0001f || x>10.0001f || y>10.0001f)return false;
                int next=Math.Clamp((int)MathF.Floor(y),0,9)*10+Math.Clamp((int)MathF.Floor(x),0,9);
                if(!reachable.ContainsKey(next))return false;
                if(previous>=0 && next!=previous && !_links[cell][previous].Contains(next))
                {
                    int dx=Math.Abs(next%10-previous%10),dy=Math.Abs(next/10-previous/10);
                    if(dx!=1 || dy!=1)return false;
                    int across=previous/10*10+next%10,down=next/10*10+previous%10;
                    if(!(_links[cell][previous].Contains(across) && _links[cell][across].Contains(next)) ||
                       !(_links[cell][previous].Contains(down) && _links[cell][down].Contains(next)))return false;
                }
                previous=next;
            }
            return true;
        }
        Vector3[] Line(Vector3 a,Vector3 b)
        {
            // Safe checks every crossed pixel boundary exactly; straight lines need only endpoints.
            return new[]{a,b};
        }
        var raw=route.Path.Select(p=>Position(cell,p)).ToList();
        if(route.Portal!=null)raw.Add(route.Portal.Position);
        // Remove the stair steps wherever a direct segment stays in the same traversable region.
        var path=new List<Vector3>{raw[0]};int at=0;
        while(at<raw.Count-1)
        {
            int next=raw.Count-1;while(next>at+1 && !Safe(Line(raw[at],raw[next])))next--;
            path.Add(raw[next]);at=next;
        }
        var result=new List<Vector3>{path[0]};
        for(int i=1;i<path.Count-1;i++)
        {
            var a=path[i-1];var b=path[i];var c=path[i+1];bool rounded=false;
            foreach(float fraction in new[]{.45f,.3f,.15f})
            {
                var before=Vector3.Lerp(b,a,fraction);var after=Vector3.Lerp(b,c,fraction);
                int count=Math.Max(12,(int)MathF.Ceiling((Vector3.Distance(before,b)+Vector3.Distance(b,after))*200));
                var curve=Enumerable.Range(0,count+1).Select(j=>{float t=j/(float)count;return before*(1-t)*(1-t)+b*2*t*(1-t)+after*t*t;}).ToArray();
                if(!Safe(curve))continue;
                result.AddRange(curve);rounded=true;break;
            }
            if(!rounded)result.Add(b);
        }
        if(path.Count>1)
        {
            var end=path[^1];bool joined=false;
            if(route.Portal!=null)
            {
                // Both cells approach the shared edge along its normal, so unfolded joins have one tangent.
                var relative=end-surface.Origin;
                float u=Vector3.Dot(relative,surface.U),v=Vector3.Dot(relative,surface.V);
                var inward=u<.0001f?surface.U:u>.9999f?-surface.U:v<.0001f?surface.V:-surface.V;
                var start=result[^1];var first=Vector3.Lerp(start,end,.33f);
                foreach(float distance in new[]{.06f,.035f,.015f})
                {
                    var second=end+inward*distance;
                    int count=Math.Max(20,(int)MathF.Ceiling(Vector3.Distance(start,end)*250));
                    var join=Enumerable.Range(0,count+1).Select(j=>{float t=j/(float)count,s=1-t;return start*s*s*s+first*3*s*s*t+second*3*s*t*t+end*t*t*t;}).ToArray();
                    if(!Safe(join))continue;
                    result.AddRange(join.Skip(1));joined=true;break;
                }
            }
            if(!joined)result.Add(end);
        }
        return _curves[route]=result.ToArray();
    }
}
