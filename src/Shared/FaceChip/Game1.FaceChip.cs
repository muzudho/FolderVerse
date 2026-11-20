namespace FolderVerse;
using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

public partial class Game1
{
    // Pause face-on, then turn once around the vertical axis. Each ruler has a different phase.
    private static float FaceChipWidth(float seconds,int ruler)
    {
        float phase=(seconds+ruler*1.37f)%11;
        if(phase<8)return 1;
        float t=MathHelper.SmoothStep(0,1,(phase-8)/3);
        return Math.Max(.055f,Math.Abs(MathF.Cos(t*MathHelper.TwoPi)));
    }
    private void DrawFaceChip(Vector2 center,int ruler,int radius=36,bool animate=false,float opacity=1)
    {
        if(ruler<0 || ruler>=_setup.ActiveCount || opacity<=0)return;
        float width=animate?FaceChipWidth(_animationTime,ruler):1;
        var gold=new Color(255,231,133)*opacity;
        void Disc(Vector2 point,int r,Color color){for(int y=-r;y<=r;y++){int half=(int)(MathF.Sqrt(Math.Max(0,r*r-y*y))*width);_ui.Box(new((int)point.X-half,(int)point.Y+y,Math.Max(1,half*2),1),color);}}
        Disc(center+new Vector2(2,3),radius+1,new Color(6,18,27)*opacity);
        Disc(center,radius,gold);Disc(center,radius-3,_setup.OwnerColor(ruler)*opacity);Disc(center,radius-5,Cream*opacity);
        DrawFaceChipPortrait(center,radius-7,ruler,width,opacity);
        if(width>.45f){
            var number=new Rectangle((int)center.X-(int)(radius*.58f*width),(int)center.Y+radius*3/5,Math.Max(1,(int)(radius*1.16f*width)),Math.Max(14,radius/2));
            _ui.Box(number,gold);_ui.Center("#"+(ruler+1),number,Math.Min(.27f,radius*.009f)*width,new Color(52,45,33)*opacity);
        }
    }
    private void DrawFaceChipPortrait(Vector2 center,int radius,int ruler,float width,float opacity)
    {
        var canvas=CanvasBounds();var transform=Matrix.CreateScale(canvas.Width/1920f,canvas.Height/1080f,1)*Matrix.CreateTranslation(canvas.X,canvas.Y,0);
        var image=new Rectangle((int)(center.X-radius*width),(int)center.Y-radius,Math.Max(1,(int)(radius*2*width)),radius*2);
        var old=GraphicsDevice.ScissorRectangle;_spriteBatch.End();
        using(var clip=new RasterizerState{ScissorTestEnable=true}){
            for(int y=-radius;y<radius;y+=2){
                float half=MathF.Sqrt(Math.Max(0,radius*radius-y*y))*width;
                var strip=new Rectangle(canvas.X+(int)((center.X-half)*canvas.Width/1920f),canvas.Y+(int)((center.Y+y)*canvas.Height/1080f),Math.Max(1,(int)(half*2*canvas.Width/1920f)),Math.Max(1,(int)(2*canvas.Height/1080f)));
                GraphicsDevice.ScissorRectangle=Rectangle.Intersect(strip,canvas);_spriteBatch.Begin(transformMatrix:transform,rasterizerState:clip);
                _portraitRenderer.Draw(_spriteBatch,_setup.Looks[ruler],image,faceOnly:true,brightness:opacity);_spriteBatch.End();
            }
        }
        GraphicsDevice.ScissorRectangle=old;_spriteBatch.Begin(transformMatrix:transform);
    }
}
