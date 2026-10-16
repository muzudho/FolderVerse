namespace FolderVerse;
using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

public partial class Game1
{
    private static (Rectangle Globe,Rectangle Toy,Rectangle Legend) OrientationAreas(Rectangle area,bool cast)
    {
        if(cast)area=new(area.X,area.Y+64,area.Width,area.Height-64);
        int width=Math.Min(190,Math.Min(area.Width/3,area.Height/3));
        var toy=new Rectangle(area.Right-width-12,area.Y+100,width,width);
        return (area,toy,new Rectangle(toy.X,toy.Bottom-26,toy.Width,26));
    }
    private static Viewport OrientationViewport(Rectangle area,Rectangle canvas)=>new(
        canvas.X+(int)(area.X*canvas.Width/1920f),canvas.Y+(int)(area.Y*canvas.Height/1080f),
        Math.Max(1,(int)(area.Width*canvas.Width/1920f)),Math.Max(1,(int)(area.Height*canvas.Height/1080f)));
    private void DrawOrientationLegend(Rectangle toy,Rectangle legend,string title="方向のロボット")
    {
        _ui.Center(title,legend,.32f,Cream);
    }
    private void DrawOrientationOverlay(Rectangle area,Rectangle canvas,Matrix transform)
    {
        var areas=OrientationAreas(area,false);var toy=areas.Toy;
        _spriteBatch.Begin(transformMatrix:transform);
        int radius=toy.Width/2;
        for(int y=-radius;y<=radius;y++){
            int half=(int)MathF.Sqrt(radius*radius-y*y);
            _ui.Box(new(toy.Center.X-half,toy.Center.Y+y,half*2+1,1),new Color(26,51,65,235));
        }
        _spriteBatch.End();
        if(_statusGlobe){
        var viewport=GraphicsDevice.Viewport;
        GraphicsDevice.Viewport=OrientationViewport(new(toy.X+16,toy.Y+4,toy.Width-32,toy.Height-30),canvas);
        _orientationToy.Draw(_yaw,_pitch,_world.ViewOrientation(_yaw,_pitch));
        GraphicsDevice.Viewport=viewport;
        }
        _spriteBatch.Begin(transformMatrix:transform);
        DrawOrientationControls(toy);
        _spriteBatch.End();
    }
}