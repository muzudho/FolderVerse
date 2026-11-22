namespace FolderVerse;
using System;
using Microsoft.Xna.Framework;

public readonly record struct MapTerrainTile(int Cell,int X,int Y)
{
    public static MapTerrainTile At(SurfaceCell cell,Vector3 point)=>new(cell.Id,
        Math.Clamp((int)MathF.Floor(Vector3.Dot(point-cell.Origin,cell.U)*10),0,9),
        Math.Clamp((int)MathF.Floor(Vector3.Dot(point-cell.Origin,cell.V)*10),0,9));
}
public static class MapTerrainColors
{
    // Leave headroom for snow and other near-white surfaces.
    public static Color Enabled(Color color)=>new((byte)(color.R*.9f),(byte)(color.G*.9f),(byte)(color.B*.9f),color.A);
    public static Color ForTile(Color color,bool selected){var enabled=Enabled(color);return selected?MapNodeColors.FromEnabled(enabled).HighlightColor:enabled;}
}
public readonly record struct MapHudRing(Vector2 Center,Vector2 Radii);

public static class MapHud
{
    public static float Pulse(float time)=>.5f+.5f*MathF.Sin(time*MathHelper.TwoPi/1.6f);
    public static MapHudRing Enclose(Vector2 min,Vector2 max)=>new((min+max)*.5f,
        Vector2.Max(new(34,26),(max-min)*.5f+new Vector2(12)));
    // Screen-space strokes retain a minimum readable width; bounds follow the projected object.
    public static void DrawRing(UiPainter ui,Vector2 center,float radius,float time,bool attention=false)
        =>DrawRing(ui,new(center,new(radius,radius*.62f)),time,attention);
    public static void DrawRing(UiPainter ui,MapHudRing ring,float time,bool attention=false){
        int segments=Math.Clamp((int)(Math.Max(ring.Radii.X,ring.Radii.Y)*2),64,256);
        float width=Math.Clamp(Math.Min(ring.Radii.X,ring.Radii.Y)*.045f,4,7);
        for(int i=0;i<segments;i++){
            float a=i*MathHelper.TwoPi/segments,b=(i+1)*MathHelper.TwoPi/segments;
            var from=ring.Center+new Vector2(MathF.Cos(a),MathF.Sin(a))*ring.Radii;
            var to=ring.Center+new Vector2(MathF.Cos(b),MathF.Sin(b))*ring.Radii;
            var delta=to-from;float angle=MathF.Atan2(delta.Y,delta.X);
            float wave=attention?Pulse(time):.5f+.5f*MathF.Cos(a-time*1.6f);
            float light=attention?.45f+.55f*wave:.8f+.2f*wave;
            var gold=Color.Lerp(new Color(255,176,48),new Color(255,252,219),wave);
            ui.Tile((from+to)/2,new(delta.Length()+1,width+10),angle,new Color(255,193,63)*(.16f*light));
            ui.Tile((from+to)/2,new(delta.Length()+1,width+4),angle,new Color(31,20,42)*.92f);
            ui.Tile((from+to)/2,new(delta.Length()+1,width),angle,gold*light);
        }
    }
}
