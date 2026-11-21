namespace FolderVerse;
using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

// A code-drawn, cached white speech balloon. All bounds are in the 1920x1080 canvas.
public sealed class BalloonRenderer:IDisposable
{
    private readonly Texture2D _texture;
    public BalloonRenderer(GraphicsDevice device)
    {
        const int width=1380,height=420;var pixels=new Color[width*height];
        static float Cross(Vector2 a,Vector2 b)=>a.X*b.Y-a.Y*b.X;
        var a=new Vector2(15,270);var b=new Vector2(220,185);var c=new Vector2(240,300);
        for(int y=0;y<height;y++)for(int x=0;x<width;x++){
            float dx=(x-750)/620f,dy=(y-200)/190f;
            var p=new Vector2(x,y);bool tail=Cross(b-a,p-a)>=0&&Cross(c-b,p-b)>=0&&Cross(a-c,p-c)>=0;
            float alpha=tail?1:Math.Clamp((1-dx*dx-dy*dy)*150,0,1);
            pixels[y*width+x]=new Color(255,253,247)*alpha;
        }
        _texture=new Texture2D(device,width,height);_texture.SetData(pixels);
    }
    public void Draw(SpriteBatch batch,Rectangle bounds)
    {
        var shadow=bounds;shadow.Offset(14,18);batch.Draw(_texture,shadow,Color.Black*.18f);
        shadow.Offset(5,5);batch.Draw(_texture,shadow,Color.Black*.10f);
        batch.Draw(_texture,bounds,Color.White);
    }
    // Inset safe area for text: stays clear of the ellipse's curved ends and tail.
    public static Rectangle TextArea(Rectangle bounds)=>new(bounds.X+bounds.Width*24/100,bounds.Y+bounds.Height*17/100,bounds.Width*61/100,bounds.Height*48/100);
    public void Dispose()=>_texture.Dispose();
}