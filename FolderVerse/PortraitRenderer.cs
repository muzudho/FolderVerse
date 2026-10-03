namespace FolderVerse;
using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

// Each tile is complete imagegen art: no facial layers or runtime transformations.
public sealed class PortraitRenderer
{
    private readonly Texture2D[] _sheets=new Texture2D[10];
    public PortraitRenderer(ContentManager content)
    {
        for(int i=0;i<10;i++)_sheets[i]=content.Load<Texture2D>("Images/Portraits/sheet-"+(i+1).ToString("00"));
    }
    public void Draw(SpriteBatch batch,ConquerorLook look,Rectangle destination,bool active=true)
    {
        if(look.BaseId<0 || look.BaseId>=60 || look.VariantId<0 || look.VariantId>=6)
            throw new ArgumentOutOfRangeException(nameof(look));
        var texture=_sheets[look.BaseId/6];
        int col=look.VariantId,row=look.BaseId%6;
        int left=col*texture.Width/6,top=row*texture.Height/6;
        var source=new Rectangle(left,top,(col+1)*texture.Width/6-left,(row+1)*texture.Height/6-top);
        batch.Draw(texture,destination,source,active?Color.White:new Color(115,115,115));
    }
}