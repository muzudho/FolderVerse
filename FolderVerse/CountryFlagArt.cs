namespace FolderVerse;
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;

// One shared design for settings, geographic markers and combat objectives.
public static class CountryFlagArt
{
    private static readonly Dictionary<(int,int,int,int,int),List<(Rectangle Box,Color Ink)>> Cache=new();
    public static Rectangle Fit(Rectangle area,int shape)
    {
        float ratio=shape==1?2:shape==2?1:1.5f;
        int width=Math.Min(area.Width,(int)(area.Height*ratio)),height=Math.Min(area.Height,(int)(area.Width/ratio));
        return new(area.X,area.Y+(area.Height-height)/2,Math.Max(1,width),Math.Max(1,height));
    }
    public static bool Contains(int shape,int x,int y)=>x>=0 && x<36 && y>=0 && y<24 && (shape!=3 || Math.Abs((y+.5f)/24-.5f)<=.5f*(1-(x+.5f)/36));
    public static Color Pixel(WorldSetup world,int owner,int x,int y)
    {
        int shape=world.CountryFlagShape(owner);
        if(!Contains(shape,x-1,y) || !Contains(shape,x+1,y) || !Contains(shape,x,y-1) || !Contains(shape,x,y+1))return new(12,23,31);
        int emblem=world.CountryEmblem(owner);bool mark=false,outline=false;
        // A dark outline separates the emblem from a two-colour patterned field.
        float center=world.CountryFlagShape(owner)==3?12:18;
        float ratio=shape==1?2:shape==2?1:1.5f;
        int ex=(int)((x-center)*32/(24/ratio)+16),ey=(int)((y-12)*32/16+16);
        foreach(var tile in CountryEmblemArt.Tiles(emblem)){
            if(tile.Contains(ex,ey))mark=true;
            if(ex>=tile.Left-2 && ex<tile.Right+2 && ey>=tile.Top-2 && ey<tile.Bottom+2)outline=true;
        }
        if(mark)return WorldSetup.Colors[world.CountrySecondaryColor(owner)];
        if(outline)return new(12,23,31);
        return world.CountryMapColor(owner,x*10/36,y*10/24);
    }
    public static void Draw(UiPainter ui,WorldSetup world,int owner,Rectangle area,float opacity=1,Func<Color,Color> tint=null)
    {
        if(owner<0 || owner>=world.ActiveCount || area.Width<1 || area.Height<1)return;
        var flag=Fit(area,world.CountryFlagShape(owner));
        // Merge identical vertical spans as well as horizontal runs to keep small flags inexpensive.
        var design=(world.CountryFlagShape(owner),world.CountryPattern(owner),world.ColorIndices[owner]%WorldSetup.Colors.Length,world.CountrySecondaryColor(owner),world.CountryEmblem(owner));
        if(!Cache.TryGetValue(design,out var patches)){
        var runs=new Dictionary<(int X,int Width,Color Ink),Rectangle>();patches=new List<(Rectangle Box,Color Ink)>();
        for(int y=0;y<24;y++){
            var row=new HashSet<(int,int,Color)>();
            for(int x=0;x<36;){
                if(!Contains(world.CountryFlagShape(owner),x,y)){x++;continue;}
                int start=x;var ink=Pixel(world,owner,x,y);x++;
                while(x<36 && Contains(world.CountryFlagShape(owner),x,y) && Pixel(world,owner,x,y)==ink)x++;
                var key=(start,x-start,ink);row.Add(key);
                if(runs.TryGetValue(key,out var old) && old.Bottom==y)runs[key]=new(old.X,old.Y,old.Width,old.Height+1);
                else runs[key]=new(start,y,x-start,1);
            }
            foreach(var key in runs.Keys.Where(k=>!row.Contains(k)).ToArray()){patches.Add((runs[key],key.Ink));runs.Remove(key);}
        }
        foreach(var run in runs)patches.Add((run.Value,run.Key.Ink));
        if(Cache.Count>=256)Cache.Clear();Cache[design]=patches;
        }
        foreach(var patch in patches){var b=patch.Box;var ink=tint==null?patch.Ink:tint(patch.Ink);
            int left=flag.X+b.X*flag.Width/36,top=flag.Y+b.Y*flag.Height/24;
            int right=flag.X+b.Right*flag.Width/36,bottom=flag.Y+b.Bottom*flag.Height/24;
            if(right>left && bottom>top)ui.Box(new(left,top,right-left,bottom-top),ink*opacity);
        }
    }
    public static void Marker(UiPainter ui,WorldSetup world,int owner,Vector2 foot,int width=30,Func<Color,Color> tint=null)
    {
        Color Ink(Color c)=>tint==null?c:tint(c);
        int height=(int)(width*.9f),x=(int)foot.X,y=(int)foot.Y;
        bool friendly=world.Relations.Controls(owner);
        ui.Box(new(x-3,y-2,7,4),Ink(owner<0?new Color(142,153,162):friendly?new Color(247,232,173):new Color(30,42,57)));
        if(owner<0)return;
        ui.Box(new(x,y-height,2,height),Ink(new Color(229,218,185)));
        var area=new Rectangle(x+2,y-height,width,Math.Max(8,height*2/3));
        Draw(ui,world,owner,area,1,tint);
    }
}
