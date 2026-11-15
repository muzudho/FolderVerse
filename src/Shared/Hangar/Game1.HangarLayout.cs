namespace FolderVerse;
using System;
using System.Linq;
using Microsoft.Xna.Framework;

public partial class Game1
{
    private static readonly Point[] HangarZoomSizes={new(2,1),new(3,2),new(4,3),new(6,4),new(8,5),new(10,6)};

    private static int HangarColumnsForCapacity(int capacity)=>HangarZoomSizes.FirstOrDefault(size=>size.X>=3 &&
        size.X*size.Y>=capacity,new Point(10,6)).X;

    private static Vector2 HangarCellPitch(Rectangle viewport,Point zoom,Vector2 gap)=>new(
        viewport.Width/(zoom.X-gap.X),viewport.Height/(zoom.Y-gap.Y));

    private static Rectangle HangarViewCell(Rectangle viewport,Vector2 pitch,Vector2 gap,int slot,int columns,int column,int row)
    {
        float width=pitch.X,height=pitch.Y;
        int x=slot%columns-column,y=slot/columns-row;
        return new(viewport.X+(int)(x*width),viewport.Y+(int)(y*height),
            (int)(width*(1-gap.X)+.001f),(int)(height*(1-gap.Y)+.001f));
    }

    private static Point HangarPanOffset(Point origin,Point delta,Vector2 pitch,Point limit)=>new(
        Math.Clamp(origin.X+(int)MathF.Round(delta.X/pitch.X),0,limit.X),
        Math.Clamp(origin.Y+(int)MathF.Round(delta.Y/pitch.Y),0,limit.Y));

    private static Rectangle HangarCellRegion(Rectangle cell,int x,int y,int width,int height)=>
        new(cell.X+x*cell.Width/252,cell.Y+y*cell.Height/260,
            Math.Max(1,width*cell.Width/252),Math.Max(1,height*cell.Height/260));

    private static Rectangle HangarRobotArea(Rectangle cell)
    {
        float scale=Math.Min(cell.Width/252f,cell.Height/260f);
        int width=Math.Max(1,(int)(100*scale));
        return new(cell.Center.X-width/2,cell.Y+(int)(12*scale),width,Math.Max(1,(int)(180*scale)));
    }
}
