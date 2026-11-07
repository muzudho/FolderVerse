namespace FolderVerse;
using System;
using Microsoft.Xna.Framework;

public partial class Game1
{
    private void DrawCountryEmblem(Rectangle area,int owner,float opacity=1)
    {
        if(owner<0 || owner>=_setup.ActiveCount)return;
        CountryEmblemArt.Draw(_ui,area,_setup.CountryEmblem(owner),WorldSetup.Colors[_setup.CountrySecondaryColor(owner)],opacity);
    }
    private void DrawCountryMark(Rectangle area,int owner,float opacity=1)
    {
        if(owner<0 || owner>=_setup.ActiveCount)return;
        int pattern=_setup.CountryPattern(owner);
        // A solid country uses just its primary color; patterned countries show a
        // tiny secondary-color suit without painting a background or an outline.
        var ink=pattern==0?_setup.OwnerColor(owner):WorldSetup.Colors[_setup.CountrySecondaryColor(owner)];
        for(int y=0;y<10;y++)for(int x=0;x<10;x++){
            if(pattern!=0 && !WorldSetup.CountryPatternSecondary(pattern,x,y))continue;
            int left=area.X+x*area.Width/10,top=area.Y+y*area.Height/10;
            _ui.Box(new(left,top,area.X+(x+1)*area.Width/10-left,area.Y+(y+1)*area.Height/10-top),ink*opacity);
        }
        DrawCountryEmblem(area,owner,opacity);
    }
    private void DrawCountryCornerMarks(Rectangle r,int owner,float opacity=1)
    {
        if(owner<0 || _setup.CountryPattern(owner)==0 && _setup.CountryEmblem(owner)==0)return;
        int size=Math.Clamp(Math.Min(r.Width,r.Height)/6,10,16),inset=5;
        DrawCountryMark(new(r.Right-inset-size,r.Top+inset,size,size),owner,opacity);
        DrawCountryMark(new(r.Left+inset,r.Bottom-inset-size,size,size),owner,opacity);
    }
    private void DrawCountryBorder(Rectangle r,int owner,int radius=0,int width=4,float opacity=1,bool selected=false,Func<int,int,Color,Color> tint=null)
    {
        if(owner<0 || owner>=_setup.ActiveCount || r.Width<=0 || r.Height<=0)return;
        bool Inside(int x,int y,int inset){
            int w=r.Width-inset*2,h=r.Height-inset*2,rad=Math.Max(0,radius-inset);
            x-=inset;y-=inset;if(x<0 || y<0 || x>=w || y>=h)return false;
            int dx=Math.Max(rad-x-1,Math.Max(0,x-(w-rad))),dy=Math.Max(rad-y-1,Math.Max(0,y-(h-rad)));
            return dx*dx+dy*dy<=rad*rad;
        }
        for(int y=0;y<r.Height;y++){
            int start=-1;Color prior=default;
            for(int x=0;x<=r.Width;x++){
                bool edge=x<r.Width && Inside(x,y,0) && !Inside(x,y,width);
                Color color=default;
                if(edge){color=selected && !Inside(x,y,1)?Color.White:_setup.CountryMapColor(owner,x/3,y/3);if(tint!=null)color=tint(r.X+x,r.Y+y,color);color*=opacity;}
                if(start>=0 && (!edge || color!=prior)){_ui.Box(new(r.X+start,r.Y+y,x-start,1),prior);start=-1;}
                if(edge && start<0){start=x;prior=color;}
            }
        }
    }
}
