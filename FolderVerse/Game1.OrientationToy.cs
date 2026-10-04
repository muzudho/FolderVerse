namespace FolderVerse;
using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

public partial class Game1
{
    private static (Rectangle Globe,Rectangle Toy,Rectangle Legend) OrientationAreas(Rectangle area,bool cast)
    {
        if(cast)area=new(area.X,area.Y+64,area.Width,area.Height-64);
        int width=Math.Min(310,(int)(area.Width*.28f));
        int legendHeight=width<180?66:90,titleHeight=width<180?22:32;
        int height=Math.Min((int)(width*1.2f),area.Height-legendHeight-titleHeight-8);
        int x=area.Right-width-8,y=area.Center.Y-(height+legendHeight+titleHeight)/2+titleHeight;
        return (new(area.X,area.Y,area.Width-width-20,area.Height),new(x,y,width,height),new(x,y+height,width,legendHeight));
    }
    private static Viewport OrientationViewport(Rectangle area,Rectangle canvas)=>new(
        canvas.X+(int)(area.X*canvas.Width/1920f),canvas.Y+(int)(area.Y*canvas.Height/1080f),
        Math.Max(1,(int)(area.Width*canvas.Width/1920f)),Math.Max(1,(int)(area.Height*canvas.Height/1080f)));
    private void DrawOrientationLegend(Rectangle toy,Rectangle legend)
    {
        bool compact=toy.Width<180;float scale=compact?.30f:.43f;
        _ui.Center("向きの人形",new(toy.X,toy.Y-(compact?22:32),toy.Width,compact?22:32),scale,Cream);
        string[] labels={"碗 +X","箸 -X","頭 +Y","足 -Y","腹 +Z","背 -Z"};
        Color[] colors={WorldCoordinates.LongitudeColor,WorldCoordinates.LatitudeColor,new(132,191,255)};
        for(int i=0;i<labels.Length;i++)
            _ui.Center(labels[i],new(legend.X+i%2*legend.Width/2,legend.Y+i/2*legend.Height/3,legend.Width/2,legend.Height/3),scale,colors[i/2]);
    }
}
