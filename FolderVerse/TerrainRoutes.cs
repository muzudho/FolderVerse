namespace FolderVerse;
using System;
using System.Linq;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

public sealed record RouteStep(int Target,Point Exit,Point Entry,Point[] Path);
public sealed class TerrainRoutes
{
    public const int Detail=10;
    private float[][] _heights;
    private List<int>[][] _links;
    private WorldSetup _world;
    private readonly Dictionary<(int Cell,int Point,int Direction,bool LandOnly),RouteStep> _cache=new();
    public Vector3 Position(int cell,Point point)=>_world.Cells[cell].Origin+_world.Cells[cell].U*((point.X+.5f)/Detail)+_world.Cells[cell].V*((point.Y+.5f)/Detail);
    private static int Id(Point p)=>p.Y*Detail+p.X;
    private static Point PointAt(int id)=>new(id%Detail,id/Detail);
    public bool Land(int cell,Point p)=>_heights[cell][Id(p)]>=.52f;
    private bool Open(float a,float b,Vector3 midpoint)
    {
        if(a>=.79f || b>=.79f)return false;
        if((a>=.52f)==(b>=.52f))return Math.Abs(a-b)<.16f;
        // A gentle coast with a seeded harbour is required to board or land.
        int hash=unchecked((int)MathF.Round(midpoint.X*100)*73856093 ^ (int)MathF.Round(midpoint.Y*100)*19349663 ^ (int)MathF.Round(midpoint.Z*100)*83492791 ^ _world.WorldSeed);
        return Math.Abs(a-b)<.075f && (uint)hash%4==0;
    }
    public void Initialize(WorldSetup world)
    {
        _world=world;_cache.Clear();var offset=WorldTerrain.Offset(world.WorldSeed);
        _heights=world.Cells.Select(c=>Enumerable.Range(0,100).Select(i=>WorldTerrain.Elevation(Position(c.Id,PointAt(i)),offset)).ToArray()).ToArray();
        _links=world.Cells.Select(c=>Enumerable.Range(0,100).Select(_=>new List<int>()).ToArray()).ToArray();
        foreach(var cell in world.Cells)for(int i=0;i<100;i++)
        {
            var p=PointAt(i);
            foreach(var delta in new[]{new Point(1,0),new Point(0,1)})
            {
                var q=p+delta;if(q.X>=10 || q.Y>=10)continue;
                if(!Open(_heights[cell.Id][i],_heights[cell.Id][Id(q)],(Position(cell.Id,p)+Position(cell.Id,q))/2))continue;
                _links[cell.Id][i].Add(Id(q));_links[cell.Id][Id(q)].Add(i);
            }
        }
    }
    public Point Start(int cell)
    {
        bool land=_world.Population.Cells[cell].Land;
        return Enumerable.Range(0,100).Where(i=>_heights[cell][i]<.79f && (_heights[cell][i]>=.52f)==land)
            .DefaultIfEmpty(44).OrderBy(i=>Vector2.DistanceSquared(new Vector2(i%10,i/10),new Vector2(4.5f))).Select(PointAt).First();
    }
    public IEnumerable<(Point A,Point B)> Segments(int cell)
    {
        for(int i=0;i<100;i++)foreach(int j in _links[cell][i])if(j>i)yield return (PointAt(i),PointAt(j));
    }
    private Dictionary<int,int> Reach(int cell,Point start,bool landOnly)
    {
        var previous=new Dictionary<int,int>{{Id(start),-1}};var queue=new Queue<int>();queue.Enqueue(Id(start));
        while(queue.Count>0){int id=queue.Dequeue();foreach(int next in _links[cell][id])if((!landOnly || Land(cell,PointAt(next))) && previous.TryAdd(next,id))queue.Enqueue(next);}
        return previous;
    }
    public RouteStep Find(int cell,Point start,int direction,bool landOnly=false)
    {
        var key=(cell,Id(start),direction,landOnly);if(_cache.TryGetValue(key,out var cached))return cached;
        int target=_world.Population.Neighbor(_world,cell,direction);var source=_world.Cells[cell];var other=_world.Cells[target];
        var shared=source.Corners.Intersect(other.Corners).ToArray();
        var previous=Reach(cell,start,landOnly);RouteStep best=null;
        for(int i=0;i<100;i++)
        {
            if(!previous.ContainsKey(i))continue;var p=PointAt(i);var at=Position(cell,p);
            // The closest micro-points to the shared edge form the crossing ports.
            var edge=shared[1]-shared[0];float t=Vector3.Dot(at-shared[0],edge)/edge.LengthSquared();
            var edgePoint=shared[0]+edge*MathHelper.Clamp(t,0,1);
            if(Vector3.Distance(at,edgePoint)>.051f)continue;
            int j=Math.Clamp((int)MathF.Floor(Vector3.Dot(edgePoint-other.Origin,other.U)*10),0,9)+10*Math.Clamp((int)MathF.Floor(Vector3.Dot(edgePoint-other.Origin,other.V)*10),0,9);
            var q=PointAt(j);
            if(landOnly && (!Land(cell,p) || !Land(target,q)))continue;
            if(!Open(_heights[cell][i],_heights[target][j],edgePoint))continue;
            var path=new List<Point>();for(int k=i;k>=0;k=previous[k])path.Add(PointAt(k));path.Reverse();
            if(best==null || path.Count<best.Path.Length)best=new(target,p,q,path.ToArray());
        }
        _cache[key]=best;return best;
    }
    public RouteStep ForRuler(int ruler,int direction)=>Find(_world.ConquerorLocations[ruler],_world.ConquerorPoints[ruler],direction);
    public string PointName(int cell,Point p)
    {
        var c=_world.Cells[cell];var up=Math.Abs(c.Normal.Y)>.5f?(c.Normal.Y>0?Vector3.Backward:Vector3.Forward):Vector3.Up;
        var delta=Position(cell,p)-c.Center;float n=Vector3.Dot(delta,up),e=Vector3.Dot(delta,Vector3.Cross(up,c.Normal));
        string name=(n>.16f?"北":n<-.16f?"南":"")+(e>.16f?"東":e<-.16f?"西":"");
        return name.Length==0?"中央":name+"側";
    }
    public string LocationLabel(int ruler)
    {
        int cell=_world.ConquerorLocations[ruler];var at=WorldCoordinates.At(_world,_world.Cells[cell]);
        return _world.CityNames[cell]+$"（{at.X},{at.Y},{PointName(cell,_world.ConquerorPoints[ruler])}）";
    }
}
