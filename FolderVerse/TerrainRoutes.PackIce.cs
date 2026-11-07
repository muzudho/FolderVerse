namespace FolderVerse;
using System;
using System.Linq;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

public sealed record SeaIceWall(int Cell,Vector3 A,Vector3 B);
public sealed partial class TerrainRoutes
{
    private bool[][] _packIce;
    private SeaIceWall[] _iceWalls=Array.Empty<SeaIceWall>();
    public SeaIceWall[] IceWalls=>_iceWalls??Array.Empty<SeaIceWall>();
    public bool PackIceAt(int cell,Point point)=>_packIce!=null && cell>=0 && cell<_packIce.Length && point.X>=0 && point.X<10 && point.Y>=0 && point.Y<10 && _packIce[cell][Id(point)];

    private void BuildPolarPackIce()
    {
        if(_world.TutorialStep!=0)return;
        var originalLinks=_links.Select(c=>c.Select(p=>p.ToList()).ToArray()).ToArray();
        var originalPortals=_portals.Select(c=>c.Select(p=>p.ToArray()).ToArray()).ToArray();
        int count=_world.Cells.Length*100;var parent=Enumerable.Range(0,count).ToArray();
        int Root(int at){while(parent[at]!=at){parent[at]=parent[parent[at]];at=parent[at];}return at;}
        void Join(int a,int b){a=Root(a);b=Root(b);if(a!=b)parent[b]=a;}
        foreach(var cell in _world.Cells)for(int at=0;at<100;at++)foreach(int next in _links[cell.Id][at])if(NavigableLink(cell.Id,at,next))Join(cell.Id*100+at,cell.Id*100+next);
        var edges=new List<(int Cell,RoutePortal Portal,int A,int B)>();
        foreach(var cell in _world.Cells)foreach(var portal in _portals[cell.Id].SelectMany(p=>p).Where(p=>p.Open && cell.Id<p.Target))
            edges.Add((cell.Id,portal,Root(cell.Id*100+Id(portal.Exit)),Root(portal.Target*100+Id(portal.Entry))));
        var graph=new Dictionary<int,List<int>>();
        for(int i=0;i<edges.Count;i++)foreach(int end in new[]{edges[i].A,edges[i].B}){if(!graph.TryGetValue(end,out var list))graph[end]=list=new();list.Add(i);}
        var removed=new HashSet<int>();
        bool Alternative(int index){
            var edge=edges[index];var seen=new HashSet<int>{edge.A};var queue=new Queue<int>();queue.Enqueue(edge.A);
            while(queue.TryDequeue(out int at)){
                if(at==edge.B)return true;
                if(!graph.TryGetValue(at,out var adjacent))continue;
                foreach(int number in adjacent){if(number==index || removed.Contains(number))continue;var e=edges[number];int next=e.A==at?e.B:e.A;if(seen.Add(next))queue.Enqueue(next);}
            }
            return false;
        }
        bool SeaEdge(int index){var e=edges[index];return Terrain(e.Cell,e.Portal.Exit)==TravelTerrain.Sea && Terrain(e.Portal.Target,e.Portal.Entry)==TravelTerrain.Sea;}
        int SnakeIndex(SurfaceCell cell){int width=_world.Cells.Where(c=>c.Face==cell.Face).Max(c=>c.X)+1;return cell.Y*width+(cell.Y%2==0?cell.X:width-1-cell.X);}
        var preferredExits=new HashSet<int>();
        foreach(int face in _world.Cells.Where(c=>_world.IsPolarFace(c.Face)).Select(c=>c.Face).Distinct()){
            var exits=Enumerable.Range(0,edges.Count).Where(i=>SeaEdge(i) && ((_world.Cells[edges[i].Cell].Face==face)!=(_world.Cells[edges[i].Portal.Target].Face==face)))
                .OrderBy(i=>SnakeIndex(_world.Cells[_world.Cells[edges[i].Cell].Face==face?edges[i].Cell:edges[i].Portal.Target])).ThenBy(i=>i).ToArray();
            if(exits.Length>0){preferredExits.Add(exits[0]);preferredExits.Add(exits[^1]);}
        }
        int Priority(int i){
            var e=edges[i];var a=_world.Cells[e.Cell];var b=_world.Cells[e.Portal.Target];
            if(!SeaEdge(i) || !_world.IsPolarFace(a.Face) && !_world.IsPolarFace(b.Face))return -1;
            if(a.Face!=b.Face)return preferredExits.Contains(i)?0:3;
            int width=_world.Cells.Where(c=>c.Face==a.Face).Max(c=>c.X)+1;
            bool straight=a.Y==b.Y;
            bool turn=a.X==b.X && a.X==(Math.Min(a.Y,b.Y)%2==0?width-1:0);
            return straight || turn?0:2;
        }
        var walls=new List<SeaIceWall>();
        foreach(int i in Enumerable.Range(0,edges.Count).Where(i=>Priority(i)>0).OrderByDescending(Priority).ThenBy(i=>i)){
            if(!Alternative(i))continue;removed.Add(i);var e=edges[i];
            CloseIcePortal(e.Cell,e.Portal);
            int polar=_world.IsPolarFace(_world.Cells[e.Cell].Face)?e.Cell:e.Portal.Target;
            var shared=_world.Cells[e.Cell].Corners.Intersect(_world.Cells[e.Portal.Target].Corners).OrderBy(p=>p.X).ThenBy(p=>p.Y).ThenBy(p=>p.Z).ToArray();
            walls.Add(new(polar,Vector3.Lerp(shared[0],shared[1],e.Portal.First/10f),Vector3.Lerp(shared[0],shared[1],(e.Portal.Last+1)/10f)));
        }
        _packIce=_world.Cells.Select(_=>new bool[100]).ToArray();
        foreach(var cell in _world.Cells.Where(c=>_world.IsPolarFace(c.Face))){
            var visited=new HashSet<int>();var keep=new HashSet<int>();
            for(int start=0;start<100;start++){
                if(!Walkable(cell.Id,PointAt(start)) || Terrain(cell.Id,PointAt(start))!=TravelTerrain.Sea || !visited.Add(start))continue;
                var component=new List<int>{start};var discover=new Queue<int>();discover.Enqueue(start);
                while(discover.TryDequeue(out int at))foreach(int next in _links[cell.Id][at])if(Terrain(cell.Id,PointAt(next))==TravelTerrain.Sea && visited.Add(next)){component.Add(next);discover.Enqueue(next);}
                int root=component.OrderBy(p=>Vector2.DistanceSquared(new(p%10,p/10),new(4.5f))).ThenBy(p=>p).First();
                var previous=new Dictionary<int,int>{{root,-1}};var queue=new Queue<int>();queue.Enqueue(root);
                while(queue.TryDequeue(out int at))foreach(int next in _links[cell.Id][at])if(Terrain(cell.Id,PointAt(next))==TravelTerrain.Sea && previous.TryAdd(next,at))queue.Enqueue(next);
                var anchors=_portals[cell.Id].SelectMany(p=>p).Where(p=>p.Open).Select(p=>Id(p.Exit))
                    .Concat(component.Where(p=>_links[cell.Id][p].Any(next=>Terrain(cell.Id,PointAt(next))!=TravelTerrain.Sea && NavigableLink(cell.Id,p,next)))).Append(root);
                foreach(int anchor in anchors.Where(previous.ContainsKey))for(int at=anchor;at>=0;at=previous[at])keep.Add(at);
            }
            // Three pixels of open water make the corridor readable; the closed
            // crossings remain ice walls, rather than becoming hidden shortcuts.
            var wide=keep.Concat(keep.SelectMany(Nearby)).ToHashSet();
            for(int at=0;at<100;at++)if(Terrain(cell.Id,PointAt(at))==TravelTerrain.Sea && Walkable(cell.Id,PointAt(at)) && !wide.Contains(at))_packIce[cell.Id][at]=true;
        }
        foreach(var cell in _world.Cells)for(int at=0;at<100;at++)_links[cell.Id][at].RemoveAll(next=>PackIceAt(cell.Id,PointAt(at)) || PackIceAt(cell.Id,PointAt(next)));
        _iceWalls=walls.ToArray();
        if(!IceNetworkConnected() || _world.Cells.Any(c=>!Enumerable.Range(0,100).Any(i=>Walkable(c.Id,PointAt(i))))){
            _links=originalLinks;_portals=originalPortals;_packIce=null;_iceWalls=Array.Empty<SeaIceWall>();
        }
        _nodeCache.Clear();_cache.Clear();_curves.Clear();
    }
    private void CloseIcePortal(int cell,RoutePortal portal)
    {
        for(int d=0;d<4;d++)for(int i=0;i<_portals[cell][d].Length;i++)if(_portals[cell][d][i]==portal)_portals[cell][d][i]=portal with {Open=false};
        for(int d=0;d<4;d++)for(int i=0;i<_portals[portal.Target][d].Length;i++){
            var back=_portals[portal.Target][d][i];if(back.Target==cell && back.Position==portal.Position)_portals[portal.Target][d][i]=back with {Open=false};
        }
    }
    private bool IceNetworkConnected()
    {
        var required=Enumerable.Range(0,_world.Cells.Length*100).Where(i=>Walkable(i/100,PointAt(i%100))).ToArray();
        if(required.Length==0)return false;var seen=new HashSet<int>{required[0]};var queue=new Queue<int>();queue.Enqueue(required[0]);
        while(queue.TryDequeue(out int current)){
            int cell=current/100,at=current%100;
            foreach(int next in _links[cell][at])if(NavigableLink(cell,at,next) && seen.Add(cell*100+next))queue.Enqueue(cell*100+next);
            foreach(var p in _portals[cell].SelectMany(p=>p).Where(p=>p.Open && Id(p.Exit)==at))if(seen.Add(p.Target*100+Id(p.Entry)))queue.Enqueue(p.Target*100+Id(p.Entry));
        }
        return seen.Count==required.Length;
    }
}
