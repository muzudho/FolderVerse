namespace FolderVerse;
using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

internal static class SelectionGlow
{
    public static float OutsideCircle(Vector2 point,Vector2 center,float radius)=>radius<=0?1:MathHelper.SmoothStep(0,1,MathHelper.Clamp((Vector2.Distance(point,center)-radius)/4,0,1));
    public static Color Dim(Color color)=>new Color(color.R*.65f/255,color.G*.65f/255,color.B*.65f/255,color.A/255f);
    // A repeating broad wave keeps the interior recognizable even with every edge offscreen.
    public static float Strength(float u,float v,float time)=>.08f+.14f*(.5f+.5f*MathF.Sin((u+v)*MathF.Tau-time*1.8f));
    public static Color Brighten(Color color,float u,float v,float time)=>Color.Lerp(color,Color.White,Strength(u,v,time));
}
public sealed partial class CubeNet
{
    public int NodeFocus {get;set;}=-1;
    public Vector2 FocusCircleCenter {get;set;}
    public float FocusCircleRadius {get;set;}
    public HashSet<int> FocusCircleNodes {get;}=new();
    public Color NodeTint(int id,Color color)=>NodeFocus>=0 && id!=NodeFocus && !FocusCircleNodes.Contains(id)?SelectionGlow.Dim(color):color;
    public Color FocusTerrain(Color color,Vector2 point)=>NodeFocus>=0?Color.Lerp(color,SelectionGlow.Dim(color),SelectionGlow.OutsideCircle(point,FocusCircleCenter,FocusCircleRadius)):color;
    public int RegionFocusCell {get;set;}=-1;
    public int SelectedCell {get;set;}=-1;
}
