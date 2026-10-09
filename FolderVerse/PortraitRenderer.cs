namespace FolderVerse;
using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

// Each tile is complete imagegen art: no facial layers or runtime transformations.
public sealed class PortraitRenderer
{
    private readonly Texture2D[] _sheets=new Texture2D[10];
    private readonly Texture2D[] _individual=new Texture2D[360];
    private readonly Texture2D[] _creation=new Texture2D[360];
    private readonly ContentManager _content;
    private readonly Dictionary<int,Texture2D> _normalized=new();
    public PortraitRenderer(ContentManager content)
    {
        _content=content;
        if(PortraitAssets.Version=="v3-standardized")
        {
            for(int id=0;id<60;id++)for(int variant=0;variant<6;variant++)
                _individual[id*6+variant]=content.Load<Texture2D>(PortraitAssets.ContentPath+"/tiles/character-"+(id+1).ToString("00")+"-"+(variant+1));
            return;
        }
        for(int i=0;i<10;i++)_sheets[i]=content.Load<Texture2D>(PortraitAssets.ContentPath+"/sheet-"+(i+1).ToString("00"));
        foreach(int id in new[]{5,17,29,41,59})_normalized[id]=content.Load<Texture2D>(PortraitAssets.ContentPath+"/base-"+(id+1).ToString("00")+"-normalized");
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
        if((look.BaseId/6==6 || look.BaseId/6==9) && PortraitAssets.Version=="v2-toy-style")
        {
            top=row*height/6;bottom=(row+1)*height/6;
        }
        int left=col*width/6,right=(col+1)*width/6;
        // Inset past separator pixels and avoid sampling adjacent portraits.
        return new(left+2,top+2,right-left-4,bottom-top-4);
    }
    public void Draw(SpriteBatch batch,ConquerorLook look,Rectangle destination,bool active=true,bool forCreation=false)
    {
        if(look.BaseId<0 || look.BaseId>=60 || look.VariantId<0 || look.VariantId>=6)
            throw new ArgumentOutOfRangeException(nameof(look));
        if(PortraitAssets.Version=="v3-standardized")
        {
            int index=look.BaseId*6+look.VariantId;
            var individual=forCreation
                ? _creation[index] ??= _content.Load<Texture2D>("Images/Portraits/conqueror-creation/tiles/character-"+(look.BaseId+1).ToString("00")+"-"+(look.VariantId+1))
                : _individual[index];
            var crop=FitSource(new Rectangle(0,0,individual.Width,individual.Height),destination);
            batch.Draw(individual,destination,crop,active?Color.White:new Color(115,115,115));
            return;
        }
        var texture=_sheets[look.BaseId/6];
        var source=SourceBounds(look,texture.Width,texture.Height);
        if(_normalized.TryGetValue(look.BaseId,out var replacement))
        {
            texture=replacement;source=NormalizedSourceBounds(look.VariantId,texture.Width,texture.Height);
        }
        source=FitSource(source,destination);
        batch.Draw(texture,destination,source,active?Color.White:new Color(115,115,115));
    }
    public static Rectangle NormalizedSourceBounds(int variant,int width,int height)
    {
        int col=variant%3,row=variant/3;
        int left=col*width/3,right=(col+1)*width/3,top=row*height/2,bottom=(row+1)*height/2;
        return new(left+3,top+3,right-left-6,bottom-top-6);
    }
    public static Rectangle FitSource(Rectangle source,Rectangle destination)
    {
        // Crop to fill the frame with uniform scaling; never flatten a face to fit a different aspect ratio.
        if(destination.Width<=0 || destination.Height<=0)return source;
        float ratio=destination.Width/(float)destination.Height;
        if(source.Width/(float)source.Height>ratio)
        {
            int width=Math.Max(1,(int)MathF.Round(source.Height*ratio));source.X+=(source.Width-width)/2;source.Width=width;
        }
        else
        {
            int height=Math.Max(1,(int)MathF.Round(source.Width/ratio));source.Y+=(source.Height-height)/3;source.Height=height;
        }
        return source;
    }
}
