namespace FolderVerse;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Microsoft.Xna.Framework;

public sealed partial class TerrainRoutes
{
    private static readonly ConditionalWeakTable<float[][],bool[][]> OceanByTerrain=new();

    internal bool IsOpenOcean(int cell,Point point)=>
        OceanByTerrain.GetValue(_heights,_=>FindOcean())[cell][Id(point)];

    private bool[][] FindOcean()
    {
        // On a closed cube the largest connected body of water is the ocean.
        // Use terrain pixels across the shared edges, regardless of route or ice closures.
        var across=new Dictionary<(int Cell,int Pixel),List<(int Cell,int Pixel)>>();
        foreach(var cell in _world.Cells)
        foreach(int neighbor in cell.Neighbors.Where(id=>id>cell.Id))
        {
            var edge=cell.Corners.Intersect(_world.Cells[neighbor].Corners).ToArray();
            for(int i=0;i<Detail;i++)
            {
                var sample=Vector3.Lerp(edge[0],edge[1],(i+.5f)/Detail);
                var a=(cell.Id,Id(EdgePoint(cell.Id,sample)));
                var b=(neighbor,Id(EdgePoint(neighbor,sample)));
                Add(a,b);
                Add(b,a);
            }
        }

        var visited=_heights.Select(cell=>new bool[cell.Length]).ToArray();
        var largest=new List<(int Cell,int Pixel)>();
        var queue=new Queue<(int Cell,int Pixel)>();
        for(int cell=0;cell<_heights.Length;cell++)
        for(int pixel=0;pixel<_heights[cell].Length;pixel++)
        {
            Enqueue((cell,pixel));
            if(queue.Count==0)continue;
            var component=new List<(int Cell,int Pixel)>();
            while(queue.TryDequeue(out var at))
            {
                component.Add(at);
                foreach(int next in Nearby(at.Pixel))Enqueue((at.Cell,next));
                if(across.TryGetValue(at,out var neighbors))
                    foreach(var next in neighbors)Enqueue(next);
            }
            if(component.Count>largest.Count)largest=component;
        }
        var ocean=_heights.Select(cell=>new bool[cell.Length]).ToArray();
        foreach(var (cell,pixel) in largest)ocean[cell][pixel]=true;
        return ocean;

        void Enqueue((int Cell,int Pixel) at)
        {
            if(visited[at.Cell][at.Pixel] || Land(at.Cell,PointAt(at.Pixel)))return;
            visited[at.Cell][at.Pixel]=true;
            queue.Enqueue(at);
        }

        void Add((int,int) from,(int,int) to)
        {
            if(!across.TryGetValue(from,out var links))across[from]=links=new();
            links.Add(to);
        }
    }
}
