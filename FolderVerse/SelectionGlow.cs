namespace FolderVerse;
using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

public readonly record struct MapFocusCircle(Vector2 Center,float Radius);

// Selection colors are UI palettes, independent of daylight and scene lighting.
public readonly record struct MapNodeColors(Color EnabledColor,Color DisabledColor,Color HighlightColor)
{
    public static MapNodeColors FromEnabled(Color normal)
    {
        float gray=normal.R*.2126f+normal.G*.7152f+normal.B*.0722f;
        // Keep a quarter of the country hue, with only a modest reduction in brightness.
        byte Channel(byte value)=>(byte)MathF.Round((gray+(value-gray)*.25f)*.75f);
        float min=Math.Min(normal.R,Math.Min(normal.G,normal.B));
        float max=Math.Max(normal.R,Math.Max(normal.G,normal.B));
        float saturation=max==0?0:(max-min)/max;
        float bright=Math.Min(255,max+20);
        float chroma=bright*Math.Min(1,saturation*1.15f);
        byte HighlightChannel(byte value)=>(byte)Math.Clamp(MathF.Round(bright-chroma+(max==min?0:(value-min)/(max-min)*chroma)),0,255);
        var highlight=new Color(HighlightChannel(normal.R),HighlightChannel(normal.G),HighlightChannel(normal.B),normal.A);
        return new(normal,new Color(Channel(normal.R),Channel(normal.G),Channel(normal.B),normal.A),highlight);
    }
    public Color ForSelectable(bool selectable,bool highlighted=false)=>!selectable?DisabledColor:highlighted?HighlightColor:EnabledColor;
}

internal static class SelectionGlow
{
    public static float OutsideCircle(Vector2 point,Vector2 center,float radius)=>radius<=0?1:MathHelper.SmoothStep(0,1,MathHelper.Clamp((Vector2.Distance(point,center)-radius)/4,0,1));
    public static Color Dim(Color color,float brightness=.65f)=>new Color(color.R*brightness/255,color.G*brightness/255,color.B*brightness/255,color.A/255f);
    // A repeating broad wave keeps the interior recognizable even with every edge offscreen.
    public static float Strength(float u,float v,float time)=>.08f+.14f*(.5f+.5f*MathF.Sin((u+v)*MathF.Tau-time*1.8f));
    public static Color Brighten(Color color,float u,float v,float time)=>Color.Lerp(color,Color.White,Strength(u,v,time));
}
public sealed partial class CubeNet
{
    public int NodeFocus {get;set;}=-1;
    public bool FolderSearchFocus {get;set;}
    public float FocusBrightness {get;set;}=.65f;
    public Vector2 FocusCircleCenter {get;set;}
    public float FocusCircleRadius {get;set;}
    public HashSet<int> FocusCircleNodes {get;}=new();
    public int CountryFocus {get;set;}=-1;
    public HashSet<int> FocusCells {get;}=new();
    public List<MapFocusCircle> FocusCircles {get;}=new();
    public bool DimNode(int id)=>AttackTargetSpotlight?!AttackHighlightTargets.Contains(id): (CountryFocus>=0 || NodeFocus>=0 || FolderSearchFocus) && id!=NodeFocus && !FocusCircleNodes.Contains(id);
    public Color NodeTint(int id,Color color)=>MapNodeColors.FromEnabled(color).ForSelectable(!DimNode(id),EmphasizedNode(id) || SelectedTarget==id);
    public Color FocusTerrain(Color color,Vector2 point,int cell=-1){
        if(CountryFocus>=0 && FocusCells.Contains(cell))return color;
        if(NodeFocus<0 && CountryFocus<0 && !AttackTargetSpotlight)return color;
        float outside=1;
        foreach(var circle in FocusCircles)outside=Math.Min(outside,SelectionGlow.OutsideCircle(point,circle.Center,circle.Radius));
        if(NodeFocus>=0 && FocusCircles.Count==0)outside=SelectionGlow.OutsideCircle(point,FocusCircleCenter,FocusCircleRadius);
        return Color.Lerp(color,SelectionGlow.Dim(color,FocusBrightness),outside);
    }
    public int RegionFocusCell {get;set;}=-1;
    public int SelectedCell {get;set;}=-1;
}
