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
public static class MapHud
{
    public static float Pulse(float time)=>.5f+.5f*MathF.Sin(time*MathHelper.TwoPi/1.6f);
    // Screen-space strokes: no scene lighting, no opaque disk, no terrain mask.
    public static void DrawRing(UiPainter ui,Vector2 center,float radius,float time,bool attention=false){
        const int segments=64;
        for(int i=0;i<segments;i++){
            float a=i*MathHelper.TwoPi/segments,b=(i+1)*MathHelper.TwoPi/segments;
            var from=center+new Vector2(MathF.Cos(a),MathF.Sin(a)*.62f)*radius;
            var to=center+new Vector2(MathF.Cos(b),MathF.Sin(b)*.62f)*radius;
            var delta=to-from;
            float light=attention?.35f+.65f*Pulse(time):.55f+.45f*MathF.Cos(a-time*1.6f);
            ui.Tile((from+to)/2,new(delta.Length()+1,5),MathF.Atan2(delta.Y,delta.X),new Color(92,217,255)*(.12f*light));
            ui.Tile((from+to)/2,new(delta.Length()+1,2),MathF.Atan2(delta.Y,delta.X),new Color(210,249,255)*light);
        }
    }
}
