namespace FolderVerse;
using System;
using Microsoft.Xna.Framework;

// Empty sites leave the road junction visible; ownership is shown beside the road.
public static class JunctionArt
{
    public static Vector2 SymbolCenter(Vector2 junction,float size)=>junction+new Vector2(size*1.9f,-size*1.4f);
    public static float Size(float nodeRadius)=>Math.Clamp(nodeRadius,7,10);
    public static bool Hit(Vector2 junction,float size,Vector2 pointer)=>
        Vector2.DistanceSquared(junction,pointer)<=36 ||
        new Rectangle((int)(SymbolCenter(junction,size).X-size-4),(int)(SymbolCenter(junction,size).Y-size-4),(int)(size*2+8),(int)(size*2+8)).Contains(pointer.ToPoint());
    public static void Draw(UiPainter ui,Vector2 at,float size,bool sea,Color color,Color ink)
    {
        Vector2 P(float x,float y)=>at+new Vector2(x,y)*size;
        void Bar(float x,float y,float width,float height,Color c)=>ui.Tile(P(x,y),new Vector2(width,height)*size,0,c);
        void Line(float x,float y,float xx,float yy,float width,Color c){var a=P(x,y);var b=P(xx,yy);var d=b-a;ui.Tile((a+b)/2,new(d.Length()+width*size,width*size),MathF.Atan2(d.Y,d.X),c);}
        if(sea){
            // A mast and a broad hull distinguish a ship from the functional harbor anchor.
            Bar(0,-.2f,.18f,1.25f,ink);Bar(.3f,-.35f,.65f,.6f,ink);
            Bar(.3f,-.35f,.45f,.4f,color);
            Line(-.95f,.15f,-.6f,.65f,.3f,ink);Line(-.6f,.65f,.6f,.65f,.3f,ink);Line(.6f,.65f,.95f,.15f,.3f,ink);
            Bar(0,.28f,1.55f,.3f,color);Bar(0,.5f,1.15f,.24f,color);
        }else{
            Bar(0,.25f,1.45f,1.1f,ink);Bar(0,.25f,1.1f,.8f,color);
            Line(-.85f,-.3f,0,-.9f,.32f,ink);Line(0,-.9f,.85f,-.3f,.32f,ink);
            Line(-.85f,-.3f,0,-.9f,.13f,color);Line(0,-.9f,.85f,-.3f,.13f,color);
            Bar(0,.46f,.28f,.52f,ink);
        }
    }
}
