namespace FolderVerse;
using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

internal sealed class SectorChangeArt : IDisposable
{
    private const int Size=32;
    private readonly Texture2D _up,_down,_digits;
    private static readonly string[] Glyphs={
        "111/101/101/101/111","010/110/010/010/111","111/001/111/100/111",
        "111/001/111/001/111","101/101/111/001/001","111/100/111/001/111",
        "111/100/111/101/111","111/001/010/010/010","111/101/111/101/111",
        "111/101/111/001/111","000/010/111/010/000"
    };

    public SectorChangeArt(GraphicsDevice device)
    {
        _up=Arrow(device,true,new Color(11,91,55),new Color(36,175,99));
        _down=Arrow(device,false,new Color(130,28,45),new Color(231,74,83));
        _digits=new Texture2D(device,Glyphs.Length*3,5);
        var pixels=new Color[_digits.Width*_digits.Height];
        for(int glyph=0;glyph<Glyphs.Length;glyph++)
        {
            var rows=Glyphs[glyph].Split('/');
            for(int y=0;y<5;y++)
                for(int x=0;x<3;x++)
                    if(rows[y][x]=='1')pixels[y*_digits.Width+glyph*3+x]=Color.White;
        }
        _digits.SetData(pixels);
    }

    private static Texture2D Arrow(GraphicsDevice device,bool up,Color dark,Color light)
    {
        var texture=new Texture2D(device,Size,Size);
        var pixels=new Color[Size*Size];
        static bool Inside(int x,int y)
        {
            int half=y<13?y: y<30?10:0;
            return y>=1 && y<30 && x>=16-half && x<16+half;
        }
        for(int y=0;y<Size;y++)
            for(int x=0;x<Size;x++)
            {
                int row=up?y:Size-1-y;
                if(!Inside(x,row))continue;
                bool border=!Inside(x-1,row)||!Inside(x+1,row)||!Inside(x,row-1)||!Inside(x,row+1);
                bool dot=y%4==2 && x%4==((y/4)%2)*2;
                var fill=Color.Lerp(light,dark,y/(float)(Size-1));
                pixels[y*Size+x]=border?new Color(9,37,32):dot?Color.Lerp(fill,Color.White,.25f):fill;
            }
        texture.SetData(pixels);
        return texture;
    }

    public void Draw(SpriteBatch batch,Rectangle badge,int change)
    {
        bool up=change>0;
        batch.Draw(up?_up:_down,badge,Color.White);
        int amount=Math.Abs(change);
        string text=amount>99?"99+":amount.ToString(System.Globalization.CultureInfo.InvariantCulture);
        int scale=Math.Max(1,badge.Width/(text.Length==3?16:12));
        int width=(text.Length*4-1)*scale;
        int left=badge.Center.X-width/2;
        int top=badge.Y+badge.Height*(up?22:10)/Size-5*scale/2;
        for(int i=0;i<text.Length;i++)
        {
            int glyph=text[i]=='+'?10:text[i]-'0';
            var source=new Rectangle(glyph*3,0,3,5);
            var destination=new Rectangle(left+i*4*scale,top,3*scale,5*scale);
            batch.Draw(_digits,new Rectangle(destination.X,destination.Y+1,destination.Width,destination.Height),
                source,new Color(16,27,31));
            batch.Draw(_digits,destination,source,Color.White);
        }
    }

    public void Dispose(){_up.Dispose();_down.Dispose();_digits.Dispose();}
}
