namespace FolderVerse;
using System;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

public partial class Game1
{
    private Texture2D _newspaperToys,_newspaperExtraToys;
    private static readonly Rectangle SummaryNewspaperSwitch=new(54,1020,650,50);
    private Texture2D LoadNewspaperToyAtlas(string name)
    {
        using var stream=File.OpenRead(Path.Combine(AppContext.BaseDirectory,"Content","Images","Newspapers",name));
        var texture=Texture2D.FromStream(GraphicsDevice,stream);
        var pixels=new Color[texture.Width*texture.Height];texture.GetData(pixels);
        for(int i=0;i<pixels.Length;i++){var c=pixels[i];pixels[i]=new Color(c.R*c.A/255,c.G*c.A/255,c.B*c.A/255,(int)c.A);}
        texture.SetData(pixels);return texture;
    }
    private Vector2 NewspaperToyPosition(int owner,Rectangle column,Rectangle photo,bool textOnly)
    {
        var random=new Random(unchecked(owner*3571+_newspaperEdition*7919+_setup.Population.Turn*104729));
        if(textOnly)return new(column.Right-25,column.Bottom-25);
        // Rest beside an arbitrary edge of the photo, leaving the face and the text readable.
        float x=random.Next(2)==0?photo.Left+8:photo.Right-8;
        float y=photo.Top+photo.Height*(.58f+(float)random.NextDouble()*.40f);
        return new(x,y);
    }
    private void DrawNewspaperToy(int owner,Vector2 center,int size)
    {
        if(owner<0)return;
        _newspaperToys??=LoadNewspaperToyAtlas("toys.png");
        _newspaperExtraToys??=LoadNewspaperToyAtlas("toys-extra-v001.png");
        // Repeatable within a paper, with a different arrangement in each edition and turn.
        int seed=unchecked(owner*7919+_newspaperEdition*104729+_setup.Population.Turn*1543+(int)center.X*17+(int)center.Y*31);
        var random=new Random(seed);
        int index=(_setup.ColorIndices[owner]+_newspaperEdition*7+_setup.Population.Turn)%28;
        var texture=index<20?_newspaperToys:_newspaperExtraToys;
        int cell=index<20?index:index-20,columns=index<20?5:4,rows=index<20?4:2;
        int x=cell%columns,y=cell/columns;
        var source=new Rectangle(x*texture.Width/columns,y*texture.Height/rows,(x+1)*texture.Width/columns-x*texture.Width/columns,(y+1)*texture.Height/rows-y*texture.Height/rows);
        bool multiple=index is 5 or 6 or 10 or 17 or 20 or 21 or 22 or 23 or 26;
        int count=multiple && size>=48?random.Next(2,4):1;
        float scale=size/(float)Math.Max(source.Width,source.Height)*(count>1?.8f:1);
        var offset=new Vector2((float)(random.NextDouble()-.5)*size*.4f,(float)(random.NextDouble()-.5)*size*.35f);
        for(int i=count-1;i>=0;i--)
        {
            var at=center+offset+new Vector2(i*size*.24f,i*size*.12f);
            float rotation=(float)(random.NextDouble()-.5)*1.5f;
            _spriteBatch.Draw(texture,at,source,i==0?_setup.OwnerColor(owner):Color.White,rotation,new Vector2(source.Width,source.Height)/2,scale,SpriteEffects.None,0);
        }
    }
}
