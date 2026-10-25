namespace FolderVerse;
using System;
using Microsoft.Xna.Framework;

internal static class SelectionGlow
{
    // A repeating broad wave keeps the interior recognizable even with every edge offscreen.
    public static float Strength(float u,float v,float time)=>.08f+.14f*(.5f+.5f*MathF.Sin((u+v)*MathF.Tau-time*1.8f));
    public static Color Brighten(Color color,float u,float v,float time)=>Color.Lerp(color,Color.White,Strength(u,v,time));
}
public sealed partial class CubeNet
{
    public int SelectedCell {get;set;}=-1;
}
