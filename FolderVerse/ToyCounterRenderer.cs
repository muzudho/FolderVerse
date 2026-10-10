namespace FolderVerse;

using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

// Scale the actual ink height, rather than font line spacing, for readable toy-style counters.
public sealed class ToyCounterRenderer
{
    private readonly SpriteFont _font;
    private readonly Texture2D _pixel;
    public ToyCounterRenderer(SpriteFont font,Texture2D pixel) { _font=font;_pixel=pixel; }
    public void DrawNumber(SpriteBatch batch,int number,Rectangle bounds,Color color)
    {
        string text=number.ToString(System.Globalization.CultureInfo.InvariantCulture);var metrics=Metrics(text);
        float scale=Math.Min((bounds.Height-6)/metrics.Height,(bounds.Width-4)/metrics.Width);
        Ink(batch,text,new Vector2(bounds.Center.X-metrics.Width*scale/2,bounds.Center.Y-metrics.Height*scale/2),scale,metrics.Top,color);
    }
    private (float Top,float Height,float Width) Metrics(string text)
    {
        var glyphs=_font.GetGlyphs();float top=float.MaxValue,bottom=0;
        foreach(char c in text)
        {
            var glyph=glyphs[c];
            top=Math.Min(top,glyph.Cropping.Y);
            bottom=Math.Max(bottom,glyph.Cropping.Y+glyph.BoundsInTexture.Height);
        }
        return (top,bottom-top,_font.MeasureString(text).X);
    }
    private void Ink(SpriteBatch batch,string text,Vector2 topLeft,float scale,float top,Color color)
    {
        var at=topLeft-new Vector2(0,top*scale);
        for(int x=-2;x<=2;x++)for(int y=-2;y<=2;y++)
            if(x*x+y*y<=5)batch.DrawString(_font,text,at+new Vector2(x,y),new Color(8,18,29),0,Vector2.Zero,scale,SpriteEffects.None,0);
        batch.DrawString(_font,text,at,color,0,Vector2.Zero,scale,SpriteEffects.None,0);
    }
    public void Draw(SpriteBatch batch,int count,Rectangle portrait,Color color)
    {
        string number=count.ToString(System.Globalization.CultureInfo.InvariantCulture);
        var digits=Metrics(number);var unit=Metrics("cell");
        float height=portrait.Height/3f,numberScale=height/digits.Height,unitScale=height*0.5f/unit.Height;
        float width=digits.Width*numberScale+7+unit.Width*unitScale;
        // The entire supported range (0..150) fits the card; retain a margin if dimensions change.
        float fit=Math.Min(1,(portrait.Width-16)/width);
        height*=fit;numberScale*=fit;unitScale*=fit;width*=fit;
        float right=portrait.Right-6,bottom=portrait.Bottom-6;
        batch.Draw(_pixel,new Rectangle((int)(right-width-5),(int)(bottom-height-5),(int)Math.Ceiling(width+10),(int)Math.Ceiling(height+10)),new Color(10,25,35,195));
        Ink(batch,number,new Vector2(right-width,bottom-height),numberScale,digits.Top,color);
        Ink(batch,"cell",new Vector2(right-unit.Width*unitScale,bottom-height*0.5f),unitScale,unit.Top,color);
    }
}
