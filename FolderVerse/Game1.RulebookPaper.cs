namespace FolderVerse;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

public partial class Game1
{
    private static readonly Color RulebookPaper=new(230,230,216),RulebookInk=new(67,57,43);
    private static readonly Color RulebookRose=new(238,162,159); // #EEA29F
    private Texture2D _rulebookTexture;
    private void EnsureRulebookTexture()
    {
        if(_rulebookTexture!=null)return;
        const int size=512;var pixels=new Color[size*size];var random=new System.Random(7319);
        var fibers=new float[size];for(int y=0;y<size;y++)fibers[y]=(float)random.NextDouble();
        for(int y=0;y<size;y++)for(int x=0;x<size;x++){
            float grain=(float)random.NextDouble()-.5f;
            float cloud=System.MathF.Sin(x*.031f+y*.012f)*System.MathF.Sin(y*.024f-x*.009f);
            float light=grain*9+cloud*3+(fibers[y]-.5f)*2;
            float edge=System.MathF.Exp(-System.Math.Min(System.Math.Min(x,size-1-x),System.Math.Min(y,size-1-y))/12f)*5;
            pixels[y*size+x]=new Color((int)(RulebookPaper.R+light-edge),(int)(RulebookPaper.G+light-edge),(int)(RulebookPaper.B+light-edge));
        }
        _rulebookTexture=new Texture2D(GraphicsDevice,size,size);_rulebookTexture.SetData(pixels);
    }
    private void RulebookBorder(Rectangle r,int inset,int width,Color color)
    {
        r.Inflate(-inset,-inset);
        _ui.Box(new(r.X,r.Y,r.Width,width),color);_ui.Box(new(r.X,r.Bottom-width,r.Width,width),color);
        _ui.Box(new(r.X,r.Y,width,r.Height),color);_ui.Box(new(r.Right-width,r.Y,width,r.Height),color);
    }
    private void DrawRulebookStock(Rectangle r,Color color)
    {
        _ui.Box(r,color);
        // Four-pixel extrusion with diagonal joins at the upper right and lower left.
        for(int i=0;i<4;i++){
            _ui.Box(new(r.Right+i,r.Y+i+1,1,r.Height-i-1),color);
            _ui.Box(new(r.X+i+1,r.Bottom+i,r.Width+3-i,1),color);
        }
    }
    private void DrawRulebookPaper(Rectangle r)
    {
        EnsureRulebookTexture();
        // The shadow follows the cut silhouette and extends 32 pixels beyond the stock.
        for(int i=32;i>=1;i--)DrawRulebookStock(new(r.X+i,r.Y+i,r.Width,r.Height),Color.Black*.02f);
        DrawRulebookStock(r,new Color(255,253,235));
        _spriteBatch.Draw(_rulebookTexture,r,Color.White);
        _ui.Box(new(r.X,r.Y,r.Width,1),new Color(255,249,226));
        _ui.Box(new(r.X,r.Y,1,r.Height),new Color(255,249,226));
        RulebookBorder(r,0,17,RulebookRose);
        // The remaining pink is 12px outside and 3px inside (8:2).
        RulebookBorder(r,12,2,RulebookPaper);
    }
    private static Rectangle RulebookHintArea=>new(500,4,1040,128);
    private void DrawRulebookHint(string title,string description)
    {
        var r=RulebookHintArea;DrawRulebookPaper(r);
        _ui.Text(title,new(r.X+24,r.Y+20),.46f,RulebookInk);
        _ui.Box(new(r.X+24,r.Y+51,r.Width-48,1),RulebookInk*.5f);
        _ui.Text(description,new(r.X+24,r.Y+65),System.Math.Min(.38f,(r.Width-48)/System.Math.Max(1,_font.MeasureString(description).X)),RulebookInk);
    }
}
