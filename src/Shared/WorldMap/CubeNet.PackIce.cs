namespace FolderVerse;
using System;
using System.Linq;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

public sealed partial class CubeNet
{
    private readonly Dictionary<int,Texture2D> _iceTextures=new();
    private SurfaceCell[] _iceCells;
    private SeaIceWall[] _iceLayout;
    private void DisposeIceTextures(){foreach(var texture in _iceTextures.Values)texture.Dispose();_iceTextures.Clear();_iceCells=null;_iceLayout=null;}
    private Texture2D IceTexture(UiPainter ui,WorldSetup world,int cell)
    {
        if(_iceTextures.TryGetValue(cell,out var found))return found;
        const int size=160;var pixels=new Color[size*size];
        foreach(var floe in SeaIceArt.Floes(world,cell)){
            var points=floe.Points;int minX=Math.Max(0,(int)(points.Min(p=>p.X)*size/10)),maxX=Math.Min(size-1,(int)(points.Max(p=>p.X)*size/10));
            int minY=Math.Max(0,(int)(points.Min(p=>p.Y)*size/10)),maxY=Math.Min(size-1,(int)(points.Max(p=>p.Y)*size/10));
            for(int y=minY;y<=maxY;y++)for(int x=minX;x<=maxX;x++){
                var at=new Vector2((x+.5f)*10/size,(y+.5f)*10/size);bool inside=false;
                for(int i=0,j=points.Length-1;i<points.Length;j=i++)if((points[i].Y>at.Y)!=(points[j].Y>at.Y) && at.X<(points[j].X-points[i].X)*(at.Y-points[i].Y)/(points[j].Y-points[i].Y)+points[i].X)inside=!inside;
                if(inside)pixels[y*size+x]=floe.Color;
            }
        }
        var texture=new Texture2D(ui.Batch.GraphicsDevice,size,size);texture.SetData(pixels);_iceTextures[cell]=texture;return texture;
    }
    private void DrawPackIce(UiPainter ui,WorldSetup world,Rectangle panel)
    {
        if(!ReferenceEquals(_iceCells,world.Cells) || !ReferenceEquals(_iceLayout,world.Routes.IceWalls)){DisposeIceTextures();_iceCells=world.Cells;_iceLayout=world.Routes.IceWalls;}
        foreach(var cell in world.Cells.Where(c=>world.IsPolarFace(c.Face))){
            if(!Enumerable.Range(0,100).Any(i=>world.Routes.PackIceAt(cell.Id,new(i%10,i/10))))continue;
            var at=AnimatedSurfacePosition(cell.Id,cell.Center,world,panel);
            var u=AnimatedSurfacePosition(cell.Id,cell.Center+cell.U,world,panel)-at;
            var v=AnimatedSurfacePosition(cell.Id,cell.Center+cell.V,world,panel)-at;
            var texture=IceTexture(ui,world,cell.Id);
            ui.Batch.Draw(texture,at+new Vector2(0,ui.OffsetY),null,Color.White,MathF.Atan2(u.Y,u.X),new Vector2(texture.Width/2f),new Vector2(u.Length()/texture.Width,v.Length()/texture.Height),u.X*v.Y-u.Y*v.X<0?SpriteEffects.FlipVertically:SpriteEffects.None,0);
        }
        foreach(var wall in world.Routes.IceWalls){
            var a=AnimatedSurfacePosition(wall.Cell,wall.A,world,panel);var b=AnimatedSurfacePosition(wall.Cell,wall.B,world,panel);var delta=b-a;
            ui.Tile((a+b)/2,new(delta.Length(),Math.Max(2,Fit(panel).Scale*.032f)),MathF.Atan2(delta.Y,delta.X),new Color(230,249,253));
        }
    }
}
