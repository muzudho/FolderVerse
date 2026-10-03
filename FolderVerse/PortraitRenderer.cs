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
    private static readonly int[][] RowEdges={
        new[]{0,172,342,512,684,838,1024},new[]{0,171,342,513,684,854,1024},
        new[]{0,170,342,513,682,836,1024},new[]{0,171,341,512,682,853,1024},
        new[]{0,170,341,513,684,837,1024},new[]{0,166,336,513,682,852,1024},
        new[]{0,158,315,473,631,791,1024},new[]{0,171,342,512,683,853,1024},
        new[]{0,170,341,511,684,853,1024},new[]{0,170,342,511,683,839,1024}
    };
    public static Rectangle SourceBounds(ConquerorLook look,int width,int height)
    {
        int col=look.VariantId,row=look.BaseId%6;
        // Use measured row separators: generated atlases do not all have equal row heights.
        int[] rows=RowEdges[look.BaseId/6];
        int top=rows[row]*height/1024;
        int bottom=rows[row+1]*height/1024;
        int left=col*width/6,right=(col+1)*width/6;
        // Inset past separator pixels and avoid sampling adjacent portraits.
        return new(left+2,top+2,right-left-4,bottom-top-4);
    }
    public void Draw(SpriteBatch batch,ConquerorLook look,Rectangle destination,bool active=true)
    {
        if(look.BaseId<0 || look.BaseId>=60 || look.VariantId<0 || look.VariantId>=6)
            throw new ArgumentOutOfRangeException(nameof(look));
        var texture=_sheets[look.BaseId/6];
        var source=SourceBounds(look,texture.Width,texture.Height);
        batch.Draw(texture,destination,source,active?Color.White:new Color(115,115,115));
    }
}