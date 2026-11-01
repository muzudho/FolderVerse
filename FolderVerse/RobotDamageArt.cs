namespace FolderVerse;
using System;
using Microsoft.Xna.Framework;

public static class RobotDamageArt
{
    public static Color Gray(Color color)=>Color.Lerp(color,new Color(116,119,116),.8f);
    // Pixel centers split the part from top-right to bottom-left, retaining half its country color.
    public static int SplitX(Rectangle part,int y)=>part.Left+(int)Math.Ceiling(part.Width*(1-(y-part.Top+.5)/Math.Max(1,part.Height))-.5);
    public static void Draw(UiPainter ui,Rectangle area,Rectangle part,Color color,float opacity=1)
    {
        ui.Box(area,color*opacity);
        for(int y=area.Top;y<area.Bottom;y++){
            int x=Math.Clamp(SplitX(part,y),area.Left,area.Right);
            if(x<area.Right)ui.Box(new(x,y,area.Right-x,1),Gray(color)*opacity);
        }
    }
}
