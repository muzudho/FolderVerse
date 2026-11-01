namespace FolderVerse;
using System;
using Microsoft.Xna.Framework;

public static class RobotDamageArt
{
    public static Color Gray(Color color)=>Color.Lerp(color,new Color(116,119,116),.8f);
    // Fixed local noise keeps the chipped edge stable while robots move or the camera pans.
    public static int SplitX(Rectangle part,int y){
        int row=(y-part.Top)/Math.Max(1,part.Height/17);
        uint noise=unchecked((uint)(row+31)*747796405u+2891336453u);noise=(noise^(noise>>16))*2246822519u;
        float chip=((noise>>24)/255f-.5f)*Math.Max(3,part.Width*.18f);
        return part.Left+(int)Math.Round(part.Width*(1-(y-part.Top+.5)/Math.Max(1,part.Height))+chip);
    }
    public static Color Tint(Rectangle part,int x,int y,Color color){
        float feather=Math.Max(1,part.Width*.035f);
        float amount=Math.Clamp((x-SplitX(part,y)+feather)/(feather*2),0,1);
        return Color.Lerp(color,Gray(color),amount);
    }
    public static void Draw(UiPainter ui,Rectangle area,Rectangle part,Color color,float opacity=1)
    {
        ui.Box(area,color*opacity);
        for(int y=area.Top;y<area.Bottom;y++){
            int feather=Math.Max(1,(int)Math.Ceiling(part.Width*.035f)),split=SplitX(part,y);
            int start=Math.Clamp(split-feather,area.Left,area.Right),end=Math.Clamp(split+feather+1,area.Left,area.Right);
            for(int x=start;x<end;x++)ui.Box(new(x,y,1,1),Tint(part,x,y,color)*opacity);
            if(end<area.Right)ui.Box(new(end,y,area.Right-end,1),Gray(color)*opacity);
        }
    }
}
