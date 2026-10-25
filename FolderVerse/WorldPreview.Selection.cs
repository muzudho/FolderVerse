namespace FolderVerse;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

public sealed partial class WorldPreview
{
    public int SelectedCell {get;set;}=-1;
    public WorldSetup SelectionSetup {get;set;}
    private readonly VertexPositionColorNormal[] _selectionVertices=new VertexPositionColorNormal[24*24*6];
    private void DrawCellSelection(float time)
    {
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
