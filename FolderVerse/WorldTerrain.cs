namespace FolderVerse;
using System;
using Microsoft.Xna.Framework;

public enum TerrainKind { Sea,Coast,Grassland,Hill,Mountain }
public enum PlanetHeadSystem { Fish=0,Monkey=1,Dog=2 }

public readonly record struct TerrainSampler(Vector3 Offset,float ElevationShift)
{
    public float Elevation(Vector3 point)=>WorldTerrain.Elevation(point,Offset)+ElevationShift;
}

public static class WorldTerrain
{
    public const float MountainHeight=0.72f;
    internal const float MaxRelief=.20f;
    public static Vector3 Offset(int seed)
    {
        var random=new SeedRandom(seed);return new Vector3(random.Unit(),random.Unit(),random.Unit())*100;
    }
    public static float Elevation(Vector3 center,Vector3 offset)
    {
        var sample=center*0.72f+offset;
        return Noise(sample)*0.68f+Noise(sample*2.07f)*0.23f+Noise(sample*4.13f)*0.09f;
    }
    public static TerrainKind Kind(float height)=>height<0.52f?TerrainKind.Sea:height<0.55f?TerrainKind.Coast:height<0.70f?TerrainKind.Grassland:height<MountainHeight?TerrainKind.Hill:TerrainKind.Mountain;
    internal static bool IsValley(TerrainSampler terrain,Vector3 point,Vector3 u,Vector3 v,Vector3 half,float height)
    {
        if(Kind(height)!=TerrainKind.Grassland)return false;
        return BetweenSlopes(u,v) || BetweenSlopes(v,u) ||
            BetweenSlopes((u+v)*.7071f,(u-v)*.7071f) ||
            BetweenSlopes((u-v)*.7071f,(u+v)*.7071f);

        bool BetweenSlopes(Vector3 across,Vector3 along)
        {
            const float rise=.035f;
            if(Sample(point+across*.35f)<height+rise || Sample(point-across*.35f)<height+rise)return false;
            float upstream=Sample(point+along*.2f),downstream=Sample(point-along*.2f);
            return upstream>=.52f && downstream>=.52f && Math.Min(upstream,downstream)<height+rise;
        }

        float Sample(Vector3 p)=>Math.Abs(p.X)>half.X+.001f || Math.Abs(p.Y)>half.Y+.001f ||
            Math.Abs(p.Z)>half.Z+.001f ? 0 : terrain.Elevation(p);
    }

    internal static Color ValleyColor(Color color)=>Color.Lerp(color,new Color(29,69,68),.35f);

    internal static bool IsValley(WorldSetup world,SurfaceCell cell,Vector3 point,float height)=>
        IsValley(world.Terrain,point,cell.U,cell.V,new Vector3(world.Width,world.Height,world.Depth)*.5f,height);

    internal static float RelativeLatitude(WorldSetup world,Vector3 point)
    {
        var dimensions=new Vector3(world.Width,world.Height,world.Depth);
        return 2*Math.Abs(PolarComponent(point,world.PolarAxis))/PolarComponent(dimensions,world.PolarAxis);
    }

    internal static bool IsCoolValley(WorldSetup world,SurfaceCell cell,Vector3 point)
    {
        float latitude=RelativeLatitude(world,point);
        return latitude>=.45f && latitude<.9f && IsValley(world,cell,point,world.Terrain.Elevation(point));
    }
    // Display-only relief in world units. One terrain sample is 0.1 units wide.
    public static float Relief(float height,bool valley=false)
    {
        if(height<.52f)return 0;
        float relief=height switch
        {
            <.55f=>MathHelper.Lerp(.008f,.024f,(height-.52f)/.03f),
            <MountainHeight=>MathHelper.Lerp(.024f,.080f,(height-.55f)/.17f),
            _=>MathHelper.Lerp(.080f,MaxRelief,MathHelper.Clamp((height-MountainHeight)/.20f,0,1))
        };
        if(valley)relief-=.022f;
        // Keep the relief terraced so nearby tiles can share the same level.
        return Math.Max(.005f,MathF.Round(relief/.005f)*.005f);
    }
    public static float ReliefAt(WorldSetup world,SurfaceCell cell,Vector3 point,TerrainSampler terrain)
    {
        var local=point-cell.Origin;
        float u=(MathF.Floor(Vector3.Dot(local,cell.U)*10)+.5f)/10;
        float v=(MathF.Floor(Vector3.Dot(local,cell.V)*10)+.5f)/10;
        var sample=cell.Origin+cell.U*u+cell.V*v;
        float height=terrain.Elevation(sample);
        return Relief(height,IsValley(terrain,sample,cell.U,cell.V,new Vector3(world.Width,world.Height,world.Depth)*.5f,height));
    }
    public static Color ColorAt(float height)=>height<0.47f?new(32,112,182):height<0.52f?new(53,155,207):height<0.55f?new(237,211,140):height<0.70f?new(85,164,103):height<MountainHeight?new(167,188,124):height<.77f?new(111,101,96):new(227,235,242);
    public static Color ColorAt(float height,Vector3 normal,int polarAxis=1)
    {
        if(Math.Abs(PolarComponent(normal,polarAxis))<.5f)return ColorAt(height);
        if(height<.47f)return new(139,185,214);
        if(height<.52f)return new(181,216,234);
        return height>=MountainHeight?new(224,234,243):new(245,249,255);
    }
    internal static float PolarComponent(Vector3 point,int polarAxis)=>polarAxis switch
    {
        0=>point.X,
        2=>point.Z,
        _=>point.Y
    };

    public static PlanetHeadSystem HeadSystemForArea(int seaSamples,int totalSamples)
    {
        if(totalSamples<=0)throw new ArgumentOutOfRangeException(nameof(totalSamples));
        if(seaSamples<0 || seaSamples>totalSamples)throw new ArgumentOutOfRangeException(nameof(seaSamples));
        if(3L*seaSamples>=2L*totalSamples)return PlanetHeadSystem.Fish;
        if(3L*seaSamples<totalSamples)return PlanetHeadSystem.Dog;
        return PlanetHeadSystem.Monkey;
    }

    private static float[] SurfaceElevations(SurfaceCell[] cells,TerrainSampler terrain)
    {
        const int detail=TerrainRoutes.Detail;
        var heights=new float[cells.Length*detail*detail];
        int index=0;
        foreach(var cell in cells)
            for(int y=0;y<detail;y++)
                for(int x=0;x<detail;x++)
                    heights[index++]=terrain.Elevation(cell.Origin+cell.U*((x+.5f)/detail)+cell.V*((y+.5f)/detail));
        return heights;
    }

    internal static PlanetHeadSystem HeadSystem(SurfaceCell[] cells,TerrainSampler terrain)
    {
        // Pack ice forms on sea tiles and is already included in their area.
        var heights=SurfaceElevations(cells,terrain);
        return HeadSystemForArea(Array.FindAll(heights,h=>Kind(h)==TerrainKind.Sea).Length,heights.Length);
    }

    internal static bool TryFitSeaRange(SurfaceCell[] cells,int seed,double minimum,double maximum,out float shift)
    {
        if(!(0<=minimum && minimum<=maximum && maximum<=1))
            throw new ArgumentOutOfRangeException(nameof(minimum),"Sea bounds must be ordered fractions between zero and one.");
        var heights=SurfaceElevations(cells,new(Offset(seed),0));
        Array.Sort(heights);
        int total=heights.Length;
        double target=minimum+(maximum-minimum)*new SeedRandom(seed^0x534541).Unit();
        int desired=(int)Math.Round(total*target);
        int min=(int)Math.Ceiling(total*Math.Max(minimum,target-.01));
        int max=(int)Math.Floor(total*Math.Min(maximum,target+.01));
        if(min/(double)total<minimum)min++;
        if(max/(double)total>maximum)max--;
        for(int distance=0;distance<=Math.Max(desired-min,max-desired);distance++)
        {
            if(TryCount(desired-distance,out shift) || distance>0 && TryCount(desired+distance,out shift))return true;
        }
        shift=0;
        return false;

        bool TryCount(int count,out float candidate)
        {
            candidate=0;
            if(count<min || count>max)return false;
            if(count>0 && count<total && heights[count-1]==heights[count])return false;
            float waterline=count==0?heights[0]-.001f:count==total?heights[^1]+.001f:
                (float)(((double)heights[count-1]+heights[count])/2);
            candidate=.52f-waterline;
            // Check the actual float comparisons on either side of the split.
            return (count==0 || heights[count-1]+candidate<.52f) &&
                   (count==total || heights[count]+candidate>=.52f);
        }
    }

    public static TerrainKind CellKind(SurfaceCell cell,Vector3 offset)=>CellKind(cell,new TerrainSampler(offset,0));
    public static TerrainKind CellKind(SurfaceCell cell,TerrainSampler terrain)
    {
        int[] counts=new int[5];
        for(int y=0;y<10;y++)for(int x=0;x<10;x++)
            counts[(int)Kind(terrain.Elevation(cell.Origin+cell.U*((x+0.5f)/10)+cell.V*((y+0.5f)/10)))]++;
        int best=0;for(int i=1;i<counts.Length;i++)if(counts[i]>counts[best])best=i;
        return (TerrainKind)best;
    }
    // Tutorial variety uses the same majority terrain classification as cells in the game.
    public static bool HasSeaAndLand(SurfaceCell[] cells,int seed)
    {
        bool sea=false,land=false;var offset=Offset(seed);
        foreach(var cell in cells){if(CellKind(cell,offset)==TerrainKind.Sea)sea=true;else land=true;if(sea && land)return true;}
        return false;
    }
    private static float Noise(Vector3 p)
    {
        int x = (int)MathF.Floor(p.X), y = (int)MathF.Floor(p.Y), z = (int)MathF.Floor(p.Z);
        float tx = Smooth(p.X - x), ty = Smooth(p.Y - y), tz = Smooth(p.Z - z);
        float bottom = MathHelper.Lerp(MathHelper.Lerp(Hash(x,y,z), Hash(x+1,y,z), tx), MathHelper.Lerp(Hash(x,y+1,z), Hash(x+1,y+1,z), tx), ty);
        float top = MathHelper.Lerp(MathHelper.Lerp(Hash(x,y,z+1), Hash(x+1,y,z+1), tx), MathHelper.Lerp(Hash(x,y+1,z+1), Hash(x+1,y+1,z+1), tx), ty);
        return MathHelper.Lerp(bottom, top, tz);
    }
    private static float Smooth(float x) => x * x * (3 - 2 * x);
    private static float Hash(int x, int y, int z)
    {
        uint h = unchecked((uint)x * 374761393u + (uint)y * 668265263u + (uint)z * 2147483647u);
        h = (h ^ (h >> 13)) * 1274126177u;
        return (h ^ (h >> 16)) / (float)uint.MaxValue;
    }

}
