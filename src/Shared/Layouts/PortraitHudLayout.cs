namespace FolderVerse;
using System;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

public static class PortraitHudLayout
{
    public static readonly Rectangle Background=new(0,0,640,1080);
    public static readonly Rectangle MainPanel=new(640,0,1280,1080);
    public static readonly Rectangle Character=new(70,88,600,900);
    public static readonly Rectangle Nameplate=new(20,673,280,94); // Center (160,720).
    public static readonly Rectangle RoleText=new(35,699,58,42),NameText=new(102,699,178,42);
    public static void DrawBackground(SpriteBatch batch,Texture2D image)
        =>batch.Draw(image,Background,PortraitRenderer.FitSource(new(0,0,image.Width,image.Height),Background),Color.White);
    public static void DrawMainBackground(SpriteBatch batch,Texture2D image)
        =>batch.Draw(image,MainPanel,PortraitRenderer.FitSource(new(0,0,image.Width,image.Height),MainPanel),Color.White);
    public static void DrawSplitter(UiPainter ui,int x=640,int height=1080)
    {
        // A decorative stitched toy ribbon; it never intercepts input.
        ui.Box(new(x-7,0,26,height),new Color(5,18,24)*.32f);
        ui.Box(new(x-12,0,24,height),new Color(178,130,62));
        ui.Box(new(x-10,0,20,height),new Color(248,226,175));
        ui.Box(new(x-7,0,14,height),new Color(42,104,109));
        ui.Box(new(x-9,0,2,height),new Color(255,242,204));
        ui.Box(new(x+8,0,2,height),new Color(119,78,35));
        for(int y=8;y<height;y+=20){
            ui.Tile(new(x,y),new(12,3),-.42f,new Color(172,215,203));
            ui.Box(new(x-9,y,2,5),new Color(150,104,48));
            ui.Box(new(x+7,y+7,2,5),new Color(255,246,217));
        }
    }
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