namespace FolderVerse;
using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;



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
    public static Color Dim(Color color,float brightness=.65f)=>new Color(color.R*brightness/255,color.G*brightness/255,color.B*brightness/255,color.A/255f);
}
public sealed partial class CubeNet
{
    public int NodeFocus {get;set;}=-1;
    public bool FolderSearchFocus {get;set;}
    public float FocusBrightness {get;set;}=.65f;
    public HashSet<int> EnabledNodes {get;}=new();
    public int CountryFocus {get;set;}=-1;
    public HashSet<int> FocusCells {get;}=new();
    public HashSet<int> SelectedNodes {get;}=new();
    public HashSet<int> AttentionNodes {get;}=new();
    public HashSet<MapTerrainTile> HighlightTiles {get;}=new();
    public float AppearanceTime {get;set;}
    public bool DimNode(int id)=>RestrictNodeActions?!ActionTargetNodes.Contains(id):
        (CountryFocus>=0 || NodeFocus>=0 || FolderSearchFocus) && id!=NodeFocus && !EnabledNodes.Contains(id);
    public bool NodeSelected(int id)=>!DimNode(id) && (SelectedNodes.Contains(id) || SelectedTarget==id || NodeFocus==id || InspectedNode==id);
    public bool NodeNeedsRing(int id)=>!DimNode(id) && id!=HoverSuppressedNode && (HoveredNode==id || AttentionNodes.Contains(id));
    public Color NodeTint(int id,Color color){
        var colors=MapNodeColors.FromEnabled(color);
        if(DimNode(id))return colors.DisabledColor;
        if(NodeSelected(id))return colors.HighlightColor;
        return AttentionNodes.Contains(id)?Color.Lerp(colors.EnabledColor,colors.HighlightColor,MapHud.Pulse(AppearanceTime)):colors.EnabledColor;
    }
    // Terrain and routes are never dimmed by interaction state.
    public Color FocusTerrain(Color color,Vector2 point,int cell=-1)=>color;
    public void PrepareTerrainHighlights(WorldSetup setup){
        SelectedNodes.RemoveWhere(DimNode);
        if(SelectedTarget>=0 && DimNode(SelectedTarget))SelectedTarget=-1;
        HighlightTiles.Clear();
        foreach(var node in setup.Nodes.All)if(NodeSelected(node.Id)){
            var position=SiteDisplayPosition(setup,node,ResourceClearance(setup,node.Cell));
            HighlightTiles.Add(MapTerrainTile.At(setup.Cells[node.Cell],position));
        }
    }
    private void DrawNodeHud(UiPainter ui,int id,Vector2 at,float radius){
        if(NodeNeedsRing(id))MapHud.DrawRing(ui,at,radius,AppearanceTime,AttentionNodes.Contains(id));
    }
    public int RegionFocusCell {get;set;}=-1;
    public int SelectedCell {get;set;}=-1;
}
