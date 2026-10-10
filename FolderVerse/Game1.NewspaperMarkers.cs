namespace FolderVerse;
using System;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

public partial class Game1
{
    private Texture2D _newspaperToys;
    private static readonly Rectangle SummaryNewspaperSwitch=new(54,1020,650,50);
    private void DrawNewspaperToy(int owner,Vector2 center,int size)
    {
        if(owner<0)return;
        if(_newspaperToys==null){using var stream=File.OpenRead(Path.Combine(AppContext.BaseDirectory,"Content","Images","Newspapers","toys.png"));_newspaperToys=Texture2D.FromStream(GraphicsDevice,stream);var pixels=new Color[_newspaperToys.Width*_newspaperToys.Height];_newspaperToys.GetData(pixels);for(int i=0;i<pixels.Length;i++){var c=pixels[i];pixels[i]=new Color(c.R*c.A/255,c.G*c.A/255,c.B*c.A/255,(int)c.A);}_newspaperToys.SetData(pixels);}
        int index=_setup.ColorIndices[owner],x=index%5,y=index/5;
        var source=new Rectangle(x*_newspaperToys.Width/5,y*_newspaperToys.Height/4,(x+1)*_newspaperToys.Width/5-x*_newspaperToys.Width/5,(y+1)*_newspaperToys.Height/4-y*_newspaperToys.Height/4);
        float scale=size/(float)Math.Max(source.Width,source.Height);
        _spriteBatch.Draw(_newspaperToys,center,source,_setup.OwnerColor(owner),(index%3-1)*.08f,new Vector2(source.Width,source.Height)/2,scale,SpriteEffects.None,0);
    }
}
