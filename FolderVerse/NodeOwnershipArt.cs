namespace FolderVerse;
using System;
using Microsoft.Xna.Framework;

// Geometry shared by drawing and pointer picking. Roads keep their original endpoint.
public static class NodeOwnershipArt
{
    public static Vector2 PreviousCenter(Vector2 current,float radius)=>current+new Vector2(-radius-PreviousRadius(radius)-16,-radius*.8f);
    public static float PreviousRadius(float radius)=>Math.Max(3,radius*.55f);
    public static bool HitTwin(Vector2 current,float radius,Vector2 pointer)
    {
        var old=PreviousCenter(current,radius);
        if(Vector2.DistanceSquared(old,pointer)<=MathF.Pow(PreviousRadius(radius)+3,2))return true;
        var delta=current-old;float t=Math.Clamp(Vector2.Dot(pointer-old,delta)/delta.LengthSquared(),0,1);
        return Vector2.DistanceSquared(pointer,old+delta*t)<=16;
    }
    public static void Draw(UiPainter ui,Vector2 current,float radius,Color before,Color after,Color ink)
    {
        var old=PreviousCenter(current,radius);float small=PreviousRadius(radius);
        var direction=Vector2.Normalize(current-old);var side=new Vector2(-direction.Y,direction.X);
        var start=old+direction*(small+2);var end=current-direction*(radius+3);
        void Line(Vector2 a,Vector2 b,float width,Color color){var d=b-a;ui.Tile((a+b)/2,new(d.Length()+1,width),MathF.Atan2(d.Y,d.X),color);}
        Line(start,end,5,ink);Line(start,end,2,Color.White);
        foreach(int sign in new[]{-1,1}){var tail=end-direction*5+side*sign*4;Line(tail,end,5,ink);Line(tail,end,2,Color.White);}
        void Disc(Vector2 center,float r,Color color){for(float y=-r+.5f;y<r;y++){float width=2*MathF.Sqrt(Math.Max(0,r*r-y*y));ui.Tile(center+new Vector2(0,y),new(width,1.1f),0,color);}}
        Disc(old,small+2,ink);Disc(old,small,before);
        Disc(current,radius+2,ink);Disc(current,radius,after);
    }
}
