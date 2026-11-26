namespace FolderVerse;
using System;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

/// <summary>
///     <pre>
///         シーンのネームプレート
///     </pre>
/// </summary>
public partial class Game1
{
    private Texture2D _phasePlaqueFrame,_phasePlaqueIcons,_phasePlaqueSubtitle;
    private Rectangle _phasePlaqueFrameSource,_phasePlaqueSubtitleSource;
    // Logical 1920x1080 coordinates. Shared screen center; allow the square illustration to protrude above the frame.
    private static readonly Rectangle PhasePlaqueBounds=new(680,36,560,112);
    private static readonly Rectangle PhasePlaqueIconBounds=new(690,-4,164,164);
    private static readonly Rectangle PhasePlaqueTitleBounds=new(834,54,292,48),PhasePlaqueSubtitleBounds=new(860,108,240,34);
    private Texture2D LoadUiTexture(string name){
        using var stream=File.OpenRead(Path.Combine(AppContext.BaseDirectory,"Content","Images","Ui",name));
        var texture=Texture2D.FromStream(GraphicsDevice,stream);
        // PNGs store straight alpha; SpriteBatch's AlphaBlend expects premultiplied RGB.
        var pixels=new Color[texture.Width*texture.Height];texture.GetData(pixels);
        for(int i=0;i<pixels.Length;i++){var c=pixels[i];pixels[i]=new Color((byte)(c.R*c.A/255),(byte)(c.G*c.A/255),(byte)(c.B*c.A/255),c.A);}
        texture.SetData(pixels);return texture;
    }
    private void DrawPhasePlaqueIcon(int icon,Rectangle bounds,Color tint){
        _phasePlaqueIcons??=LoadUiTexture("phase-plaque-icons-v001.png");
        int width=_phasePlaqueIcons.Width/3,height=_phasePlaqueIcons.Height/3;
        _spriteBatch.Draw(_phasePlaqueIcons,bounds,new Rectangle(icon%3*width,icon/3*height,width,height),tint);
    }
    private void DrawPhasePlaqueLabel(string main,string sub,int icon){
        if(_phasePlaqueFrame==null){
            _phasePlaqueFrame=LoadUiTexture("phase-plaque-frame-v003-cloud.png");
            _phasePlaqueFrameSource=UiArtInkBounds(_phasePlaqueFrame,_phasePlaqueFrame.Bounds);
        }
        if(_phasePlaqueSubtitle==null){
            _phasePlaqueSubtitle=LoadUiTexture("phase-plaque-subtitle-v001.png");
            _phasePlaqueSubtitleSource=UiArtInkBounds(_phasePlaqueSubtitle,_phasePlaqueSubtitle.Bounds);
        }
        var plate=PhasePlaqueBounds;
        var accent=DomesticTheme?_setup.OwnerColor(_setup.PlayerSlot):DefaultAccent;
        // A national-color backing rim; the title itself stays ivory and readable.
        DrawPhasePlaqueFrame(new Rectangle(plate.X-1,plate.Y,plate.Width+2,plate.Height),accent);
        DrawPhasePlaqueFrame(plate,Color.White);
        foreach(var segment in PhasePlaqueCapsuleSegments(_phasePlaqueSubtitleSource,PhasePlaqueSubtitleBounds))
            if(segment.Target.Width>0)_spriteBatch.Draw(_phasePlaqueSubtitle,segment.Target,segment.Source,Color.White);
        var iconBounds=PhasePlaqueIconBounds;if(icon==1)iconBounds.Y-=10;
        DrawPhasePlaqueIcon(icon,iconBounds,Color.White);
        var ink=new Color(57,43,30);
        var title=PhasePlaqueTitleBounds;var subtitle=PhasePlaqueSubtitleBounds;
        float Fit(string text,Rectangle area,float maximum)=>Math.Min(maximum,(area.Width-8)/Math.Max(1,_font.MeasureString(text).X));
        // One scalar for both axes: Japanese full-width glyphs are never stretched to fit the plate.
        PortraitHudLayout.CenterLines(_ui,_font,new[]{main},title,Fit(main,title,.80f),ink);
        PortraitHudLayout.CenterLines(_ui,_font,new[]{sub},subtitle,Fit(sub,subtitle,.46f),ink);
    }
    private static (Rectangle Source,Rectangle Target)[] PhasePlaqueSegments(Rectangle source,Rectangle target){
        // Preserve the cloud lobes, rivets and rounded subtitle ends at a uniform scale.
        // Only the flat central 20% changes width.
        int cap=source.Width*2/5;
        int width=Math.Min(target.Width/2,(int)MathF.Round(cap*target.Height/(float)source.Height));
        return new[]{
            (new Rectangle(source.X,source.Y,cap,source.Height),new Rectangle(target.X,target.Y,width,target.Height)),
            (new Rectangle(source.X+cap,source.Y,source.Width-2*cap,source.Height),new Rectangle(target.X+width,target.Y,target.Width-2*width,target.Height)),
            (new Rectangle(source.Right-cap,source.Y,cap,source.Height),new Rectangle(target.Right-width,target.Y,width,target.Height))};
    }
    private static Rectangle UiArtInkBounds(Texture2D texture,Rectangle area){
        var pixels=new Color[area.Width*area.Height];texture.GetData(0,area,pixels,0,pixels.Length);
        int left=area.Width,top=area.Height,right=-1,bottom=-1;
        for(int y=0;y<area.Height;y++)for(int x=0;x<area.Width;x++)if(pixels[y*area.Width+x].A>=32){left=Math.Min(left,x);right=Math.Max(right,x);top=Math.Min(top,y);bottom=Math.Max(bottom,y);}
        return right>=left?new Rectangle(area.X+left,area.Y+top,right-left+1,bottom-top+1):area;
    }
    private static (Rectangle Source,Rectangle Target)[] PhasePlaqueCapsuleSegments(Rectangle source,Rectangle target){
        int cap=Math.Min(source.Width/2,source.Height/2),width=Math.Min(target.Width/2,target.Height/2);
        return new[]{
            (new Rectangle(source.X,source.Y,cap,source.Height),new Rectangle(target.X,target.Y,width,target.Height)),
            (new Rectangle(source.X+cap,source.Y,source.Width-2*cap,source.Height),new Rectangle(target.X+width,target.Y,target.Width-2*width,target.Height)),
            (new Rectangle(source.Right-cap,source.Y,cap,source.Height),new Rectangle(target.Right-width,target.Y,width,target.Height))};
    }
    private void DrawPhasePlaqueFrame(Rectangle target,Color tint){
        foreach(var segment in PhasePlaqueSegments(_phasePlaqueFrameSource,target))
            if(segment.Target.Width>0)_spriteBatch.Draw(_phasePlaqueFrame,segment.Target,segment.Source,tint);
    }
    private void DisposePhasePlaques(){_phasePlaqueFrame?.Dispose();_phasePlaqueIcons?.Dispose();_phasePlaqueSubtitle?.Dispose();_phasePlaqueFrame=_phasePlaqueIcons=_phasePlaqueSubtitle=null;}
}
