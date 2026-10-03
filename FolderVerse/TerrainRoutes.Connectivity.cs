namespace FolderVerse;
using System;
using System.Linq;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

public sealed record Harbor(int Cell,Point Point,int Target,Point Other,Vector3 Position);
public sealed partial class TerrainRoutes
{
    private readonly HashSet<(int Cell,int A,int B)> _harborEdges=new();
    private readonly List<Harbor> _harbors=new();
    private readonly Dictionary<(int Cell,int Point,int Direction),RouteStep[]> _arrivalCache=new();
    public int ComponentsBeforeRepair {get;private set;}
    public int ComponentsAfterRepair {get;private set;}
    public IEnumerable<Harbor> Harbors(int cell)=>_harbors.Where(h=>h.Cell==cell);
    public bool IsHarbor(int cell,Point a,Point b)=>_harborEdges.Contains((cell,Math.Min(Id(a),Id(b)),Math.Max(Id(a),Id(b))));
    private bool NavigableLink(int cell,int a,int b)=>_links[cell][a].Contains(b) &&
        ((Terrain(cell,PointAt(a))==TravelTerrain.Sea)==(Terrain(cell,PointAt(b))==TravelTerrain.Sea) || IsHarbor(cell,PointAt(a),PointAt(b)));
    private void AddConnection(int cell,int a,int b)
    {
        if(!_links[cell][a].Contains(b)){_links[cell][a].Add(b);_links[cell][b].Add(a);}
        if((Terrain(cell,PointAt(a))==TravelTerrain.Sea)!=(Terrain(cell,PointAt(b))==TravelTerrain.Sea) &&
           _harborEdges.Add((cell,Math.Min(a,b),Math.Max(a,b))))
            _harbors.Add(new(cell,PointAt(a),cell,PointAt(b),(Position(cell,PointAt(a))+Position(cell,PointAt(b)))/2));
    }
    private IEnumerable<(int Node,RoutePortal Portal)> PhysicalNeighbors(int node)
    {
        int cell=node/100,point=node%100;
        foreach(int next in Nearby(point))yield return (cell*100+next,null);
        foreach(var portal in _portals[cell].SelectMany(p=>p).Where(p=>Id(p.Exit)==point))yield return (portal.Target*100+Id(portal.Entry),portal);
    }
    private void RepairConnectivity()
    {
        _harborEdges.Clear();_harbors.Clear();_arrivalCache.Clear();
        int count=_world.Cells.Length*100;var parent=Enumerable.Range(0,count).ToArray();
        int Root(int n){while(parent[n]!=n){parent[n]=parent[parent[n]];n=parent[n];}return n;}
        void Join(int a,int b){a=Root(a);b=Root(b);if(a!=b)parent[Math.Max(a,b)]=Math.Min(a,b);}
        bool Required(int n)=>Walkable(n/100,PointAt(n%100));
        foreach(var cell in _world.Cells)
        {
            for(int a=0;a<100;a++)foreach(int b in _links[cell.Id][a])if(NavigableLink(cell.Id,a,b))Join(cell.Id*100+a,cell.Id*100+b);
            foreach(var portal in _portals[cell.Id].SelectMany(p=>p).Where(p=>p.Open))
            {
                Join(cell.Id*100+Id(portal.Exit),portal.Target*100+Id(portal.Entry));
                if((Terrain(cell.Id,portal.Exit)==TravelTerrain.Sea)!=(Terrain(portal.Target,portal.Entry)==TravelTerrain.Sea))
                    _harbors.Add(new(cell.Id,portal.Exit,portal.Target,portal.Entry,portal.Position));
            }
        }
        int Groups()=>Enumerable.Range(0,count).Where(Required).Select(Root).Distinct().Count();
        ComponentsBeforeRepair=Groups();
        // Add only coasts that connect separated components. A lake normally needs just one harbour.
        var coasts=new List<(int Cell,int A,int B,float Slope)>();
        foreach(var cell in _world.Cells)for(int a=0;a<100;a++)foreach(int b in Nearby(a).Where(b=>b>a))
            if(Required(cell.Id*100+a) && Required(cell.Id*100+b) &&
               (Terrain(cell.Id,PointAt(a))==TravelTerrain.Sea)!=(Terrain(cell.Id,PointAt(b))==TravelTerrain.Sea))
                coasts.Add((cell.Id,a,b,Math.Abs(_heights[cell.Id][a]-_heights[cell.Id][b])));
        foreach(var coast in coasts.OrderBy(c=>c.Slope).ThenBy(c=>c.Cell).ThenBy(c=>c.A).ThenBy(c=>c.B))
        {
            int a=coast.Cell*100+coast.A,b=coast.Cell*100+coast.B;if(Root(a)==Root(b))continue;
            AddConnection(coast.Cell,coast.A,coast.B);Join(a,b);
        }
        // Remaining islands may be enclosed by mountains or a closed cell edge. Open the shortest
        // connector, strongly preferring water/land and existing passes over excavating a mountain.
        while(Groups()>1)
        {
            var groups=Enumerable.Range(0,count).Where(Required).GroupBy(Root).OrderBy(g=>g.Count()).ThenBy(g=>g.Key).ToArray();
            int sourceRoot=groups[0].Key;var distance=Enumerable.Repeat(float.PositiveInfinity,count).ToArray();
            var previous=Enumerable.Repeat(-1,count).ToArray();var crossings=new RoutePortal[count];
            var queue=new PriorityQueue<int,(float Cost,int Node)>();
            foreach(int node in groups[0]){distance[node]=0;queue.Enqueue(node,(0,node));}
            int destination=-1;
            while(queue.TryDequeue(out int current,out var priority))
            {
                if(priority.Cost!=distance[current])continue;
                if(Required(current) && Root(current)!=sourceRoot){destination=current;break;}
                foreach(var edge in PhysicalNeighbors(current))
                {
                    int next=edge.Node;float cost=distance[current]+1+(Required(next)?0:100)+
                        (edge.Portal!=null && !edge.Portal.Open?4:0);
                    if(cost>=distance[next])continue;distance[next]=cost;previous[next]=current;crossings[next]=edge.Portal;queue.Enqueue(next,(cost,next));
                }
            }
            if(destination<0)throw new InvalidOperationException("No physical connector for an isolated outpost.");
            var path=new List<int>();for(int n=destination;n>=0;n=previous[n])path.Add(n);path.Reverse();
            foreach(int node in path)if(!Required(node))_passes[node/100][node%100]=true;
            for(int i=1;i<path.Count;i++)
            {
                int a=path[i-1],b=path[i],cell=a/100;
                if(cell==b/100)AddConnection(cell,a%100,b%100);
                else
                {
                    var bridge=crossings[b];
                    for(int d=0;d<4;d++)for(int p=0;p<_portals[cell][d].Length;p++)if(_portals[cell][d][p]==bridge)_portals[cell][d][p]=bridge with{Open=true};
                    for(int d=0;d<4;d++)for(int p=0;p<_portals[b/100][d].Length;p++)
                    {
                        var back=_portals[b/100][d][p];if(back.Target==cell && back.Position==bridge.Position)_portals[b/100][d][p]=back with{Open=true};
                    }
                    if((Terrain(cell,PointAt(a%100))==TravelTerrain.Sea)!=(Terrain(b/100,PointAt(b%100))==TravelTerrain.Sea))
                    {
                        _harbors.Add(new(cell,PointAt(a%100),b/100,PointAt(b%100),bridge.Position));
                        _harbors.Add(new(b/100,PointAt(b%100),cell,PointAt(a%100),bridge.Position));
                    }
                }
                Join(a,b);
            }
        }
        ComponentsAfterRepair=Groups();_cache.Clear();
    }
    public RouteStep[] ArrivalChoices(int ruler,int direction)
    {
        int cell=_world.ConquerorLocations[ruler];var start=_world.ConquerorPoints[ruler];
        var key=(cell,Id(start),direction);if(_arrivalCache.TryGetValue(key,out var cached))return cached;
        var reachable=Reach(cell,start,false,true);var choices=new List<RouteStep>();
        foreach(var portal in Portals(cell,direction).Where(p=>p.Open && reachable.ContainsKey(Id(p.Exit))).OrderBy(p=>Trace(reachable,Id(p.Exit)).Length))
        {
            var path=Trace(reachable,Id(portal.Exit));var arrival=Reach(portal.Target,portal.Entry,false,true);
            var entryPost=_world.Outposts.At(portal.Target,portal.Entry);
            foreach(var post in _world.Outposts.InCell(portal.Target).Where(p=>arrival.ContainsKey(Id(p.Center))).OrderBy(p=>p.Id==entryPost.Id?0:1).ThenBy(p=>p.Id))
            {
                if(choices.Any(r=>_world.Outposts.At(r.Target,r.Entry).Id==post.Id))continue;
                var point=post.Id==entryPost.Id?portal.Entry:post.Center;
                choices.Add(new(portal.Target,portal.Exit,point,path){Portal=portal,ArrivalPath=Trace(arrival,Id(point))});
            }
        }
        return _arrivalCache[key]=choices.ToArray();
    }
    public RouteStep ForMarch(int ruler,int direction,int outpost)
    {
        if(outpost<0)return ForRuler(ruler,direction);
        return ArrivalChoices(ruler,direction).FirstOrDefault(r=>_world.Outposts.At(r.Target,r.Entry).Id==outpost);
    }
}
