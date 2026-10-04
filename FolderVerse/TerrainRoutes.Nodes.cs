namespace FolderVerse;
using System;
using System.Linq;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

public sealed partial class TerrainRoutes
{
    private readonly Dictionary<int,RouteStep[]> _nodeCache=new();
    // Contract the terrain graph to Nodes. A turn crosses exactly one Node boundary.
    private Dictionary<int,int> ReachNode(Node node,Point start)
    {
        var previous=new Dictionary<int,int>{{Id(start),-1}};var queue=new Queue<int>();queue.Enqueue(Id(start));
        while(queue.Count>0)
        {
            int point=queue.Dequeue();
            foreach(int next in _links[node.Cell][point])
                if(NavigableLink(node.Cell,point,next) && _world.Nodes.At(node.Cell,PointAt(next))?.Id==node.Id && previous.TryAdd(next,point))queue.Enqueue(next);
        }
        return previous;
    }
    public RouteStep[] NodeChoices(int ruler)=>Neighbors(_world.Nodes.Current(ruler));
    public RouteStep[] Neighbors(Node source)
    {
        if(source==null)return Array.Empty<RouteStep>();
        if(_nodeCache.TryGetValue(source.Id,out var cached))return cached;
        var previous=ReachNode(source,source.Center);var choices=new Dictionary<int,RouteStep>();
        void Add(Point exit,int cell,Point entry,RoutePortal portal=null)
        {
            var target=_world.Nodes.At(cell,entry);if(target==null || target.Id==source.Id)return;
            var arrival=ReachNode(target,entry);if(!arrival.ContainsKey(Id(target.Center)))return;
            var path=Trace(previous,Id(exit));var tail=Trace(arrival,Id(target.Center));
            RouteStep route=cell==source.Cell?new(cell,target.Center,target.Center,path.Concat(tail).ToArray()):
                new(cell,exit,target.Center,path){Portal=portal,ArrivalPath=tail};
            if(!choices.TryGetValue(target.Id,out var old) || route.Path.Length+route.ArrivalPath.Length<old.Path.Length+old.ArrivalPath.Length)choices[target.Id]=route;
        }
        foreach(int point in previous.Keys)
            foreach(int next in _links[source.Cell][point])
                if(NavigableLink(source.Cell,point,next))Add(PointAt(point),source.Cell,PointAt(next));
        foreach(var portal in _portals[source.Cell].SelectMany(p=>p).Where(p=>p.Open && previous.ContainsKey(Id(p.Exit))))
            Add(portal.Exit,portal.Target,portal.Entry,portal);
        return _nodeCache[source.Id]=choices.Values.OrderBy(r=>Bearing(source,r)).ThenBy(r=>_world.Nodes.At(r.Target,r.Entry).Id).ToArray();
    }
    public RouteStep ToNode(int ruler,int node)=>NodeChoices(ruler).FirstOrDefault(r=>_world.Nodes.At(r.Target,r.Entry).Id==node);
    public int Bearing(Node source,RouteStep route)
    {
        var cell=_world.Cells[source.Cell];var target=Position(route.Target,route.Entry);
        if(route.Portal!=null && _world.Cells[route.Target].Face!=cell.Face)
        {
            // Unfold the neighbouring face into the source face around their shared edge.
            var other=_world.Cells[route.Target];var relative=target-route.Portal.Position;
            target=route.Portal.Position+relative-other.Normal*Vector3.Dot(relative,other.Normal)-cell.Normal*Vector3.Dot(relative,cell.Normal)-other.Normal*Vector3.Dot(relative,cell.Normal);
        }
        var delta=target-Position(source.Cell,source.Center);
        var north=Math.Abs(cell.Normal.Y)>.5f?(cell.Normal.Y>0?Vector3.Backward:Vector3.Forward):Vector3.Up;
        float angle=MathF.Atan2(Vector3.Dot(delta,Vector3.Cross(north,cell.Normal)),Vector3.Dot(delta,north));
        return ((int)MathF.Floor(angle/(MathF.PI/8)+.5f)+16)%16;
    }
    public string DirectionLabel(Node source,RouteStep route)
    {
        string[] names={"北","北北東","北東","東北東","東","東南東","南東","南南東","南","南南西","南西","西南西","西","西北西","北西","北北西"};
        return names[Bearing(source,route)];
    }
}
