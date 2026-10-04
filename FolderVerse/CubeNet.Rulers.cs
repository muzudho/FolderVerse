namespace FolderVerse;
using System;
using System.Linq;
using Microsoft.Xna.Framework;

public sealed partial class CubeNet
{
    public bool ShowRulers {get;set;}=true;
    public static Rectangle MapViewport(Rectangle panel)=>new(panel.Left-50,panel.Top-14,panel.Width+90,panel.Height+18);
    public static Rectangle TopRuler(Rectangle panel)=>new(panel.Left-50,panel.Top-14,panel.Width+90,32);
    public static Rectangle LeftRuler(Rectangle panel)=>new(panel.Left-50,panel.Top+18,40,panel.Height-18);
    public bool OnRuler(Rectangle panel,Point pointer)=>ShowRulers && (TopRuler(panel).Contains(pointer) || LeftRuler(panel).Contains(pointer));
    public void DrawRulers(UiPainter ui,WorldSetup world,Rectangle panel,int focused)
    {
        if(!ShowRulers)return;
        var top=TopRuler(panel);var left=LeftRuler(panel);var background=new Color(32,57,70);
        ui.Box(top,background);ui.Box(left,background);
        ui.Box(new(top.Left,top.Bottom-1,top.Width,1),new Color(72,100,111));
        ui.Box(new(left.Right-1,left.Top,1,left.Height),new Color(72,100,111));
        ui.Text("経番",new(top.X+5,top.Y+6),.33f,WorldCoordinates.LongitudeColor);
        ui.Center("緯番",new(left.X,left.Y,left.Width,26),.30f,WorldCoordinates.LatitudeColor);
        int face=focused>=0?world.Cells[focused].Face:CenterFace;
        ui.Text(FaceNames[face],new(top.Right-82,top.Y+7),.30f,new Color(180,209,219));
        if(IsAnimating)return;
        var fit=Fit(panel);var cells=world.Cells.Where(c=>c.Face==face)
            .Select(c=>(Cell:c,Bounds:CellBounds(c,panel),Coordinate:WorldCoordinates.At(world,c))).ToArray();
        var reference=focused>=0?cells.First(c=>c.Cell.Id==focused):cells.OrderBy(c=>Vector2.DistanceSquared(new Vector2(c.Bounds.Center.X,c.Bounds.Center.Y),Faces.First(f=>f.Face==face).Center*fit.Scale+fit.Origin)).ThenBy(c=>c.Cell.Id).First();
        float spacing=fit.Scale*.25f;float lastX=float.NegativeInfinity,lastY=float.NegativeInfinity;
        foreach(var cell in cells.Where(c=>Math.Abs(c.Bounds.Center.Y-reference.Bounds.Center.Y)<=spacing).OrderBy(c=>c.Bounds.Center.X))
        {
            int x=cell.Bounds.Center.X;if(x<top.Left+58 || x>top.Right-94)continue;
            string number=cell.Coordinate.X.ToString();var size=ui.Measure(number,WorldCoordinates.MapFontScale);
            if(x-size.X/2<lastX+8)continue;lastX=x+size.X/2;
            ui.Box(new(x,top.Bottom-7,1,7),WorldCoordinates.LongitudeColor);
            ui.Center(number,new(x-28,top.Y+1,56,22),WorldCoordinates.MapFontScale,WorldCoordinates.LongitudeColor);
        }
        foreach(var cell in cells.Where(c=>Math.Abs(c.Bounds.Center.X-reference.Bounds.Center.X)<=spacing).OrderBy(c=>c.Bounds.Center.Y))
        {
            int y=cell.Bounds.Center.Y;if(y<left.Top+40 || y>left.Bottom-12)continue;
            var size=ui.Measure(cell.Coordinate.Y.ToString(),WorldCoordinates.MapFontScale);
            if(y-size.Y/2<lastY+7)continue;lastY=y+size.Y/2;
            ui.Box(new(left.Right-7,y,7,1),WorldCoordinates.LatitudeColor);
            ui.Center(cell.Coordinate.Y.ToString(),new(left.X,y-12,left.Width-8,24),WorldCoordinates.MapFontScale,WorldCoordinates.LatitudeColor);
        }
    }
}
