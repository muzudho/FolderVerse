namespace FolderVerse;
using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

public readonly record struct MapFocusCircle(Vector2 Center,float Radius);

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
    public int CountryFocus {get;set;}=-1;
    public HashSet<int> FocusCells {get;}=new();
    public List<MapFocusCircle> FocusCircles {get;}=new();
    public bool DimNode(int id)=> (CountryFocus>=0 || NodeFocus>=0) && id!=NodeFocus && !FocusCircleNodes.Contains(id);
    public Color NodeTint(int id,Color color)=>DimNode(id)?SelectionGlow.Dim(color):color;
    public Color FocusTerrain(Color color,Vector2 point,int cell=-1){
        if(CountryFocus>=0 && FocusCells.Contains(cell))return color;
        if(NodeFocus<0 && CountryFocus<0)return color;
        float outside=1;
        foreach(var circle in FocusCircles)outside=Math.Min(outside,SelectionGlow.OutsideCircle(point,circle.Center,circle.Radius));
        if(NodeFocus>=0 && FocusCircles.Count==0)outside=SelectionGlow.OutsideCircle(point,FocusCircleCenter,FocusCircleRadius);
        return Color.Lerp(color,SelectionGlow.Dim(color),outside);
    }
    public int RegionFocusCell {get;set;}=-1;
    public int SelectedCell {get;set;}=-1;
}
