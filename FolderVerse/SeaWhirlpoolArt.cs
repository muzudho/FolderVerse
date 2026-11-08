namespace FolderVerse;
using System;
using Microsoft.Xna.Framework;

public static class SeaWhirlpoolArt
{
    public const int Detail=40;
    public static Color ColorAt(Vector2 p)
    {
        float radius=p.Length();
        if(radius<.11f)return new(9,31,68);
        float spiral=MathF.Atan2(p.Y,p.X)+radius*11;
        float foam=(MathF.Sin(spiral*3)+1)/2;
        var water=Color.Lerp(new Color(12,52,104),new Color(40,169,205),radius);
        return foam>.88f?Color.Lerp(new Color(121,226,239),new Color(244,255,246),radius):water;
    }
    public static void Draw(UiPainter ui,Func<Vector2,Vector2> project)
    {
        var unitX=project(new(2f/Detail,0))-project(Vector2.Zero);
        var unitY=project(new(0,2f/Detail))-project(Vector2.Zero);
        float angle=MathF.Atan2(unitX.Y,unitX.X);
        for(int y=0;y<Detail;y++)for(int x=0;x<Detail;x++){
            var p=new Vector2((x+.5f)*2/Detail-1,(y+.5f)*2/Detail-1);if(p.LengthSquared()>1)continue;
            ui.Tile(project(p),new Vector2(unitX.Length()+.7f,unitY.Length()+.7f),angle,ColorAt(p));
        }
    }
}
