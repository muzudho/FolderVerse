namespace FolderVerse;

using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

// Scale the actual ink height, rather than font line spacing, for readable toy-style counters.
public sealed class ToyCounterRenderer
{
    public static string SectorUnit(int count)=>count==1?"sector":"sectors";
    private readonly SpriteFont _font;
    private readonly Texture2D _pixel;
    public ToyCounterRenderer(SpriteFont font,Texture2D pixel) { _font=font;_pixel=pixel; }
    public void DrawNumber(SpriteBatch batch,int number,Rectangle bounds,Color color)
    {
        string text=number.ToString(System.Globalization.CultureInfo.InvariantCulture);var metrics=Metrics(text);
        float scale=Math.Min((bounds.Height-6)/metrics.Height,(bounds.Width-4)/metrics.Width);
        Ink(batch,text,new Vector2(bounds.Center.X-metrics.Width*scale/2,bounds.Center.Y-metrics.Height*scale/2),scale,metrics.Top,color);
    }
    public void DrawText(SpriteBatch batch,string text,Rectangle bounds,Color color)
    {
        var metrics=Metrics(text);
        float scale=Math.Min(bounds.Height/metrics.Height,bounds.Width/metrics.Width);
        Ink(batch,text,new(bounds.Center.X-metrics.Width*scale/2,bounds.Center.Y-metrics.Height*scale/2),scale,metrics.Top,color);
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
            if(x*x+y*y<=5)batch.DrawString(_font,text,at+new Vector2(x,y),new Color(8,18,29)*(color.A/255f),0,Vector2.Zero,scale,SpriteEffects.None,0);
        batch.DrawString(_font,text,at,color,0,Vector2.Zero,scale,SpriteEffects.None,0);
    }
    public void Draw(SpriteBatch batch,int count,Rectangle portrait,Color color)
    {
        string number=count.ToString(System.Globalization.CultureInfo.InvariantCulture);
        string unitText=SectorUnit(count);
        var digits=Metrics(number);var unit=Metrics(unitText);
        // Fit the number independently; a plural unit must not shrink its digits or the portrait.
        float numberScale=Math.Min(portrait.Height*.26f/digits.Height,(portrait.Width-16)/digits.Width);
        float unitScale=Math.Min(portrait.Height*.07f/unit.Height,(portrait.Width-16)/unit.Width);
        float numberHeight=digits.Height*numberScale,unitHeight=unit.Height*unitScale;
        float height=numberHeight+4+unitHeight,width=Math.Max(digits.Width*numberScale,unit.Width*unitScale);
        float right=portrait.Right-6,bottom=portrait.Bottom-6;
        batch.Draw(_pixel,new Rectangle((int)(right-width-5),(int)(bottom-height-5),(int)Math.Ceiling(width+10),(int)Math.Ceiling(height+10)),new Color(10,25,35,195));
        Ink(batch,number,new Vector2(right-digits.Width*numberScale,bottom-height),numberScale,digits.Top,color);
        Ink(batch,unitText,new Vector2(right-unit.Width*unitScale,bottom-unitHeight),unitScale,unit.Top,color);
    }
}
