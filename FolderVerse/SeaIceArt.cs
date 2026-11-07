namespace FolderVerse;
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;

public static class SeaIceArt
{
    private static List<Vector2> Clip(List<Vector2> polygon,Vector2 normal,float limit)
    {
        var result=new List<Vector2>();if(polygon.Count==0)return result;
        var previous=polygon[^1];float before=Vector2.Dot(previous,normal)-limit;
        foreach(var current in polygon){float after=Vector2.Dot(current,normal)-limit;
            if((before<=0)!=(after<=0))result.Add(Vector2.Lerp(previous,current,before/(before-after)));
            if(after<=0)result.Add(current);previous=current;before=after;
        }
        return result;
    }
    public static IEnumerable<(Vector2[] Points,Color Color)> Floes(WorldSetup world,int cell)
    {
        if(!Enumerable.Range(0,100).Any(i=>world.Routes.PackIceAt(cell,new(i%10,i/10))))yield break;
        float Noise(int x,int y,int salt){uint h=unchecked((uint)(cell*73856093+x*19349663+y*83492791+salt*1640531527));h^=h>>13;h*=1274126177;h^=h>>16;return (h&65535)/65535f-.5f;}
        Vector2 Seed(int x,int y)=>new(x+.5f+Noise(x,y,1)*.85f,y+.5f+Noise(x,y,2)*.85f);
        // Voronoi floes have irregular outlines and unequal sizes. Clip against the
        // existing ice mask so their artwork cannot cover a navigable water lane.
        for(int y=0;y<10;y++)for(int x=0;x<10;x++){
            var seed=Seed(x,y);var polygon=new List<Vector2>{new(0,0),new(10,0),new(10,10),new(0,10)};
            for(int ny=Math.Max(0,y-2);ny<=Math.Min(9,y+2);ny++)for(int nx=Math.Max(0,x-2);nx<=Math.Min(9,x+2);nx++){
                if(nx==x && ny==y)continue;var other=Seed(nx,ny);
                polygon=Clip(polygon,other-seed,(other.LengthSquared()-seed.LengthSquared())/2);
            }
            for(int i=0;i<polygon.Count;i++)polygon[i]=Vector2.Lerp(polygon[i],seed,.035f);
            int left=Math.Max(0,(int)MathF.Floor(polygon.Min(p=>p.X))),right=Math.Min(9,(int)MathF.Floor(polygon.Max(p=>p.X)));
            int top=Math.Max(0,(int)MathF.Floor(polygon.Min(p=>p.Y))),bottom=Math.Min(9,(int)MathF.Floor(polygon.Max(p=>p.Y)));
            for(int iy=top;iy<=bottom;iy++)for(int ix=left;ix<=right;ix++)if(world.Routes.PackIceAt(cell,new(ix,iy))){
                var fragment=Clip(polygon,Vector2.UnitX,ix+1);fragment=Clip(fragment,-Vector2.UnitX,-ix);
                fragment=Clip(fragment,Vector2.UnitY,iy+1);fragment=Clip(fragment,-Vector2.UnitY,-iy);
                if(fragment.Count>=3)yield return (fragment.ToArray(),ColorAt(cell,new(x,y)));
            }
        }
    }
    public static Color ColorAt(int cell,Point point)=>((cell*17+point.X*7+point.Y*11)%5) switch {
        0=>new(195,229,242),1=>new(219,243,249),_=>new(238,251,252)};
}
