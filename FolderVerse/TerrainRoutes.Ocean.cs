namespace FolderVerse;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Microsoft.Xna.Framework;

public sealed partial class TerrainRoutes
{
    private sealed record WaterRegions(bool[][] Seas,bool[][] Largest);
    private static readonly ConditionalWeakTable<float[][],WaterRegions> OceanByTerrain=new();
    private static readonly ConditionalWeakTable<SurfaceCell[],WaterRegions> DisplayWaterByCells=new();

    internal bool IsOpenOcean(int cell,Point point,bool useLargestWaterBody=false)
    {
        var regions=OceanByTerrain.GetValue(_heights,heights=>FindWaterRegions(_world.Cells,heights));
        return (useLargestWaterBody?regions.Largest:regions.Seas)[cell][Id(point)];
    }

    internal static bool IsSeaForDisplay(WorldSetup world,int cell,int x,int y)
    {
        var regions=DisplayWaterByCells.GetValue(world.Cells,cells=>
        {
            var terrain=world.Terrain;
            var heights=cells.Select(c=>Enumerable.Range(0,Detail*Detail).Select(i=>
                terrain.Elevation(c.Origin+c.U*((i%Detail+.5f)/Detail)+c.V*((i/Detail+.5f)/Detail))).ToArray()).ToArray();
            return FindWaterRegions(cells,heights);
        });
        return regions.Seas[cell][y*Detail+x];
    }

    private static WaterRegions FindWaterRegions(SurfaceCell[] cells,float[][] heights)
    {
        // Use terrain pixels across the shared edges, regardless of route or ice closures.
        var across=new Dictionary<(int Cell,int Pixel),List<(int Cell,int Pixel)>>();
        foreach(var cell in cells)
        foreach(int neighbor in cell.Neighbors.Where(id=>id>cell.Id))
        {
            var edge=cell.Corners.Intersect(cells[neighbor].Corners).ToArray();
            for(int i=0;i<Detail;i++)
            {
                var sample=Vector3.Lerp(edge[0],edge[1],(i+.5f)/Detail);
                var a=(cell.Id,Id(EdgePoint(cell,sample)));
                var b=(neighbor,Id(EdgePoint(cells[neighbor],sample)));
                Add(a,b);
                Add(b,a);
            }
        }

        var visited=heights.Select(cell=>new bool[cell.Length]).ToArray();
        var seas=heights.Select(cell=>new bool[cell.Length]).ToArray();
        int landArea=heights.Sum(cell=>cell.Count(height=>Classify(height)!=TravelTerrain.Sea));
        var largest=new List<(int Cell,int Pixel)>();
        var queue=new Queue<(int Cell,int Pixel)>();
        for(int cell=0;cell<heights.Length;cell++)
        for(int pixel=0;pixel<heights[cell].Length;pixel++)
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
            if(component.Count>=landArea)
                foreach(var (region,index) in component)seas[region][index]=true;
            if(component.Count>largest.Count)largest=component;
        }
        var ocean=heights.Select(cell=>new bool[cell.Length]).ToArray();
        foreach(var (cell,pixel) in largest)ocean[cell][pixel]=true;
        return new(seas,ocean);

        void Enqueue((int Cell,int Pixel) at)
        {
            if(visited[at.Cell][at.Pixel] || Classify(heights[at.Cell][at.Pixel])!=TravelTerrain.Sea)return;
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
