namespace FolderVerse;
using System;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

public static class PortraitHudLayout
{
    public static readonly Rectangle Background=new(0,0,640,1080);
    public static readonly Rectangle Character=new(70,88,600,900);
    public static readonly Rectangle Nameplate=new(20,673,280,94); // Center (160,720).
    public static readonly Rectangle RoleText=new(35,699,58,42),NameText=new(102,699,178,42);
    public static void DrawBackground(SpriteBatch batch,Texture2D image)
        =>batch.Draw(image,Background,PortraitRenderer.FitSource(new(0,0,image.Width,image.Height),Background),Color.White);
    // Center actual glyph ink, rather than the font's line-spacing box.
    public static void CenterLines(UiPainter ui,SpriteFont font,string[] lines,Rectangle area,float scale,Color color)
    {
        var glyphs=font.GetGlyphs();
        var metrics=lines.Select(line=>{
            var gs=line.Where(c=>glyphs.ContainsKey(c)&&!char.IsWhiteSpace(c)).Select(c=>glyphs[c]).ToArray();
            float top=gs.Length==0?0:gs.Min(g=>g.Cropping.Y);
            float bottom=gs.Length==0?font.LineSpacing:gs.Max(g=>g.Cropping.Y+g.BoundsInTexture.Height);
            return (Top:top,Height:(bottom-top)*scale);
        }).ToArray();
        float y=area.Center.Y-(metrics.Sum(m=>m.Height)+Math.Max(0,lines.Length-1)*2)/2;
        for(int i=0;i<lines.Length;i++){
            float x=area.Center.X-font.MeasureString(lines[i]).X*scale/2;
            ui.Text(lines[i],new(x,y-metrics[i].Top*scale),scale,color);y+=metrics[i].Height+2;
        }
    }
}