namespace FolderVerse;
using System;
using Microsoft.Xna.Framework;

public enum TerrainKind { Sea,Coast,Grassland,Hill }
public static class WorldTerrain
{
    public static Vector3 Offset(int seed)
    {
        var random=new SeedRandom(seed);return new Vector3(random.Unit(),random.Unit(),random.Unit())*100;
    }
    public static float Elevation(Vector3 center,Vector3 offset)
    {
        var sample=center*0.72f+offset;
        return Noise(sample)*0.68f+Noise(sample*2.07f)*0.23f+Noise(sample*4.13f)*0.09f;
    }
    public static TerrainKind Kind(float height)=>height<0.52f?TerrainKind.Sea:height<0.55f?TerrainKind.Coast:height<0.70f?TerrainKind.Grassland:TerrainKind.Hill;
    public static Color ColorAt(float height)=>height<0.47f?new(32,112,182):height<0.52f?new(53,155,207):height<0.55f?new(237,211,140):height<0.70f?new(85,164,103):new(167,188,124);    public static TerrainKind CellKind(SurfaceCell cell,Vector3 offset)
    {
        int[] counts=new int[4];
        for(int y=0;y<10;y++)for(int x=0;x<10;x++)
            counts[(int)Kind(Elevation(cell.Origin+cell.U*((x+0.5f)/10)+cell.V*((y+0.5f)/10),offset))]++;
        int best=0;for(int i=1;i<4;i++)if(counts[i]>counts[best])best=i;
        return (TerrainKind)best;
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
