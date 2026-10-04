namespace FolderVerse;
using System;
using System.Linq;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

public sealed class Node
{
    public int Id,Cell,Owner;
    public Point Center;
    public Point[] Points;
    public TravelTerrain Terrain;
    public bool IsHarbor;
    public string Name;
    public CellPopulation Population=new();
}
public sealed class WorldNodes
{
    public Node[] All {get;private set;}=Array.Empty<Node>();
    private int[][] _regions;
    private WorldSetup _world;
    public void Initialize(WorldSetup world)
    {
        _world=world;var result=new List<Node>();_regions=world.Cells.Select(_=>Enumerable.Repeat(-1,100).ToArray()).ToArray();
        var harbors=world.Cells.SelectMany(c=>world.Routes.Harbors(c.Id))
            .SelectMany(h=>new[]{(Cell:h.Cell,Point:h.Point),(Cell:h.Target,Point:h.Other)})
            .Where(h=>world.Routes.Land(h.Cell,h.Point)).ToHashSet();
        foreach(var cell in world.Cells)
        {
            var links=world.Routes.Segments(cell.Id).ToArray();
            for(int i=0;i<100;i++)
            {
                var start=new Point(i%10,i/10);if(_regions[cell.Id][i]>=0 || !world.Routes.Walkable(cell.Id,start))continue;
                bool harbor=harbors.Contains((cell.Id,start));
                var terrain=world.Routes.Terrain(cell.Id,start);var points=new List<Point>{start};var queue=new Queue<Point>();if(!harbor)queue.Enqueue(start);_regions[cell.Id][i]=result.Count;
                while(queue.Count>0)
                {
                    var point=queue.Dequeue();foreach(var edge in links.Where(e=>e.A==point || e.B==point))
                    {
                        var next=edge.A==point?edge.B:edge.A;int index=next.Y*10+next.X;
                        if(_regions[cell.Id][index]>=0 || harbors.Contains((cell.Id,next)) || world.Routes.Terrain(cell.Id,next)!=terrain)continue;
                        _regions[cell.Id][index]=result.Count;points.Add(next);queue.Enqueue(next);
                    }
                }
                var centroid=new Vector2((float)points.Average(p=>p.X),(float)points.Average(p=>p.Y));
                var center=points.OrderBy(p=>Vector2.DistanceSquared(new Vector2(p.X,p.Y),centroid)).ThenBy(p=>p.Y*10+p.X).First();
                result.Add(new(){Id=result.Count,Cell=cell.Id,Owner=world.Owners[cell.Id],Center=center,Points=points.ToArray(),Terrain=terrain,IsHarbor=harbor,
                    Name=harbor?CoastalNames.For(world.WorldSeed,cell.Id,center):null,Population=new(){Land=terrain!=TravelTerrain.Sea}});
            }
        }
        All=result.ToArray();
        foreach(var cell in world.Cells)
        {
            var land=InCell(cell.Id).Where(p=>p.Population.Land).ToArray();int area=land.Sum(p=>p.Points.Length);
            var remaining=(long[])world.Population.Cells[cell.Id].People.Clone();
            for(int i=0;i<land.Length;i++)for(int role=0;role<3;role++)
            {
                long count=i==land.Length-1?remaining[role]:world.Population.Cells[cell.Id].People[role]*land[i].Points.Length/Math.Max(1,area);
                land[i].Population.People[role]=count;remaining[role]-=count;
            }
        }
        Recount();
    }
    public IEnumerable<Node> InCell(int cell)=>All.Where(p=>p.Cell==cell);
    public Node At(int cell,Point point)
    {
        if(_regions==null || cell<0 || cell>=_regions.Length || point.X<0 || point.X>9 || point.Y<0 || point.Y>9)return null;
        int id=_regions[cell][point.Y*10+point.X];return id<0?null:All[id];
    }
    public Node Current(int ruler)=>At(_world.ConquerorLocations[ruler],_world.ConquerorPoints[ruler]);
    public void Recount()
    {
        Array.Clear(_world.TerritoryCounts);
        foreach(var cell in _world.Cells)
        {
            var posts=InCell(cell.Id).ToArray();foreach(int ruler in posts.Select(p=>p.Owner).Distinct())_world.TerritoryCounts[ruler]++;
            if(posts.Length>0 && !posts.Any(p=>p.Owner==_world.Owners[cell.Id]))
                _world.Owners[cell.Id]=posts.GroupBy(p=>p.Owner).OrderByDescending(g=>g.Sum(p=>p.Points.Length)).ThenBy(g=>g.Key).First().Key;
            for(int role=0;role<3;role++)_world.Population.Cells[cell.Id].People[role]=posts.Sum(p=>p.Population.People[role]);
        }
        for(int ruler=0;ruler<_world.ActiveCount;ruler++)
            if(_world.TerritoryCounts[ruler]>0 && !InCell(_world.Capitals[ruler]).Any(p=>p.Owner==ruler))
                _world.Capitals[ruler]=All.Where(p=>p.Owner==ruler).OrderByDescending(p=>p.Population.Total).ThenBy(p=>p.Id).First().Cell;
    }
    public string Label(Node p)
    {
        var coordinate=WorldCoordinates.At(_world,_world.Cells[p.Cell]);
        string name=p.IsHarbor?p.Name+$"（{coordinate.X},{coordinate.Y}）":WorldCoordinates.Label(_world,p.Cell);
        return name+" / "+(p.IsHarbor?"海港":TerrainRoutes.TerrainName(p.Terrain))+"拠点・"+_world.Routes.PointName(p.Cell,p.Center)+$" [Node {p.Id+1}]";
    }
    public int FullOwner(int cell)
    {
        var owners=InCell(cell).Select(p=>p.Owner).Distinct().ToArray();return owners.Length==1?owners[0]:-1;
    }
    public string Pattern(Node p)
    {
        var exits=_world.Routes.Neighbors(p);
        string shape=exits.Length switch{0=>"孤立",1=>"行き止まり",_=>$"{exits.Length} 接続"};
        return shape+(exits.Length==0?"":"（"+string.Join("・",exits.Select(r=>_world.Routes.DirectionLabel(p,r)).Distinct())+"）");
    }
    public string CellPattern(int cell)=>string.Join(" / ",InCell(cell).Select(p=>TerrainRoutes.TerrainName(p.Terrain)+"："+Pattern(p)).Distinct());
    public string Shares(int cell)
    {
        var posts=InCell(cell).ToArray();int total=posts.Sum(p=>p.Points.Length);
        return string.Join(" / ",posts.GroupBy(p=>p.Owner).OrderByDescending(g=>g.Sum(p=>p.Points.Length)).Select(g=>_world.ConquerorNames[g.Key]+$" {100m*g.Sum(p=>p.Points.Length)/Math.Max(1,total):0}%"));
    }
}
