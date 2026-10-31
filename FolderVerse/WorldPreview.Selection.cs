namespace FolderVerse;
using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

public sealed partial class WorldPreview
{
    public Vector2 FocusCircleCenter {get;set;}
    public float FocusCircleRadius {get;set;}
    public Rectangle FocusCircleArea {get;set;}
    public float FocusCircleYaw {get;set;}
    public float FocusCirclePitch {get;set;}
    public int CountryFocus {get;set;}=-1;
    public float FocusBrightness {get;set;}=.65f;
    public HashSet<int> FocusCells {get;}=new();
    public List<MapFocusCircle> FocusCircles {get;}=new();
    public int NodeFocus {get;set;}=-1;
    public int RegionFocusCell {get;set;}=-1;
    public int SelectedCell {get;set;}=-1;
    public WorldSetup SelectionSetup {get;set;}
    private readonly VertexPositionColorNormal[] _selectionVertices=new VertexPositionColorNormal[24*24*6];
    private void DrawRegionDim()
    {
        _device.BlendState=BlendState.AlphaBlend;_device.DepthStencilState=DepthStencilState.DepthRead;_effect.LightingEnabled=false;
        foreach(var cell in SelectionSetup.Cells){
            if(CountryFocus>=0?FocusCells.Contains(cell.Id):NodeFocus<0 && cell.Id==RegionFocusCell)continue;
            var lift=cell.Normal*.003f;
            int detail=1;
            if(FocusCircles.Count>0 && ProjectVisible(cell.Center,cell.Normal,FocusCircleYaw,FocusCirclePitch,FocusCircleArea,out _)){
                var min=new Vector2(float.PositiveInfinity);var max=new Vector2(float.NegativeInfinity);
                foreach(var corner in cell.Corners){ProjectVisible(corner,cell.Normal,FocusCircleYaw,FocusCirclePitch,FocusCircleArea,out var at);min=Vector2.Min(min,at);max=Vector2.Max(max,at);}
                foreach(var circle in FocusCircles){var closest=Vector2.Clamp(circle.Center,min,max);if(Vector2.Distance(closest,circle.Center)<=circle.Radius+8){detail=24;break;}}
            }
            Color Mask(Vector3 p){
                float outside=1;
                if(detail>1){ProjectVisible(p,cell.Normal,FocusCircleYaw,FocusCirclePitch,FocusCircleArea,out var at);foreach(var circle in FocusCircles)outside=Math.Min(outside,SelectionGlow.OutsideCircle(at,circle.Center,circle.Radius));}
                return new Color(0f,0f,0f,(1-FocusBrightness)*outside);
            }
            int index=0;
            for(int y=0;y<detail;y++)for(int x=0;x<detail;x++){
                var a=cell.Origin+cell.U*(x/(float)detail)+cell.V*(y/(float)detail);
                var b=a+cell.U/detail;var d=a+cell.V/detail;var c=b+cell.V/detail;
                var ca=Mask(a);var cb=Mask(b);var cc=Mask(c);var cd=Mask(d);
                _selectionVertices[index++]=new(a+lift,ca,cell.Normal);_selectionVertices[index++]=new(b+lift,cb,cell.Normal);_selectionVertices[index++]=new(c+lift,cc,cell.Normal);
                _selectionVertices[index++]=new(a+lift,ca,cell.Normal);_selectionVertices[index++]=new(c+lift,cc,cell.Normal);_selectionVertices[index++]=new(d+lift,cd,cell.Normal);
            }
            foreach(var pass in _effect.CurrentTechnique.Passes){pass.Apply();_device.DrawUserPrimitives(PrimitiveType.TriangleList,_selectionVertices,0,index/3);}
        }
        _effect.LightingEnabled=true;_device.BlendState=BlendState.Opaque;_device.DepthStencilState=DepthStencilState.Default;
    }
    private void DrawCellSelection(float time)
    {
        if(SelectionSetup!=null && (CountryFocus>=0 || NodeFocus>=0 || RegionFocusCell>=0 && RegionFocusCell<SelectionSetup.Cells.Length)){DrawRegionDim();return;}
        if(SelectionSetup==null || SelectedCell<0 || SelectedCell>=SelectionSetup.Cells.Length)return;
        var cell=SelectionSetup.Cells[SelectedCell];int index=0;
        const int detail=24;
        var lift=cell.Normal*.003f;
        for(int y=0;y<detail;y++)for(int x=0;x<detail;x++){
            var a=cell.Origin+cell.U*(x/(float)detail)+cell.V*(y/(float)detail)+lift;
            var b=a+cell.U/detail;var d=a+cell.V/detail;var c=b+cell.V/detail;
            var color=Color.White*SelectionGlow.Strength((x+.5f)/detail,(y+.5f)/detail,time);
            _selectionVertices[index++]=new(a,color,cell.Normal);_selectionVertices[index++]=new(b,color,cell.Normal);_selectionVertices[index++]=new(c,color,cell.Normal);
            _selectionVertices[index++]=new(a,color,cell.Normal);_selectionVertices[index++]=new(c,color,cell.Normal);_selectionVertices[index++]=new(d,color,cell.Normal);
        }
        _device.BlendState=BlendState.AlphaBlend;_device.DepthStencilState=DepthStencilState.DepthRead;_effect.LightingEnabled=false;
        foreach(var pass in _effect.CurrentTechnique.Passes){pass.Apply();_device.DrawUserPrimitives(PrimitiveType.TriangleList,_selectionVertices,0,index/3);}
        _effect.LightingEnabled=true;_device.BlendState=BlendState.Opaque;_device.DepthStencilState=DepthStencilState.Default;
    }
}
