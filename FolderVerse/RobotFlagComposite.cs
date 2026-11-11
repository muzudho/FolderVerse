namespace FolderVerse;
using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

// Runtime artwork supplies these maps explicitly. Never infer paintable regions
// from the colour of the neutral artwork: faces and wood grain may be grey too.
public static class RobotFlagComposite
{
    public readonly record struct Surface(Vector2 Uv,byte Coverage,byte Diffuse,byte Reflection);

    // Tiles come directly from CountryEmblemArt.Tiles, shared with the flag.
    // A die-cut white edge follows the mark rather than adding a beige plaque.
    public static void Sticker(Color[] pixels,int width,int height,Rectangle area,IReadOnlyList<Rectangle> tiles,Color ink,
        bool mirror = false)
    {
        if(width<1 || height<1 || pixels==null || (long)width*height!=pixels.Length || tiles==null)
            throw new ArgumentException("Invalid sticker image.");
        if(area.Width<=0 || area.Height<=0 || tiles.Count==0)return;
        bool Mark(int x,int y){foreach(var tile in tiles)if(tile.Contains(x,y))return true;return false;}
        for(int y=Math.Max(0,area.Top);y<Math.Min(height,area.Bottom);y++)
        for(int x=Math.Max(0,area.Left);x<Math.Min(width,area.Right);x++){
            int i=y*width+x;if(pixels[i].A==0)continue;
            int stickerX = mirror ? area.Right - 1 - x : x - area.Left;
            int u=stickerX*36/area.Width-2,v=(y-area.Y)*36/area.Height-2;
            bool mark=Mark(u,v),edge=false;
            if(!mark)for(int dy=-1;dy<=1 && !edge;dy++)for(int dx=-1;dx<=1;dx++)if(Mark(u+dx,v+dy)){edge=true;break;}
            if(mark || edge){var colour=mark?ink:Color.White;pixels[i]=new Color(colour.R,colour.G,colour.B,pixels[i].A);}
        }
    }

    public static Color[] Compose(WorldSetup world,int owner,Color[] neutral,Surface[] surface,bool fieldOnly=false)
    {
        if(world==null)throw new ArgumentNullException(nameof(world));
        if(owner<0 || owner>=world.ActiveCount)throw new ArgumentOutOfRangeException(nameof(owner));
        if(neutral==null || surface==null || neutral.Length!=surface.Length)throw new ArgumentException("Artwork and surface maps must have equal pixel counts.");
        var result=(Color[])neutral.Clone();
        int shape=world.CountryFlagShape(owner);
        // Sample the shared design once. Large sprites otherwise rebuild the
        // emblem's tile lookup for every painted artwork pixel.
        var flag=new Color[36*24];
        for(int y=0;y<24;y++)for(int x=0;x<36;x++)
            if(CountryFlagArt.Contains(shape,x,y))flag[y*36+x]=CountryFlagArt.Pixel(world,owner,x,y);
        for(int i=0;i<result.Length;i++){
            var map=surface[i];var original=neutral[i];
            if(map.Coverage==0 || original.A==0 || !float.IsFinite(map.Uv.X) || !float.IsFinite(map.Uv.Y))continue;
            int x=(int)MathF.Floor(map.Uv.X*36),y=(int)MathF.Floor(map.Uv.Y*24);
            if(!CountryFlagArt.Contains(shape,x,y))continue;
            var ink=flag[y*36+x];
            // A shell wears the flag's field, while its emblem is a separate
            // sticker. Do not magnify the small flag's border/emblem over a face.
            if(fieldOnly)ink=world.CountryPattern(owner)==1
                ?WorldSetup.CountryStripeSecondary(map.Uv.X*10,map.Uv.Y*10)?WorldSetup.Colors[world.CountrySecondaryColor(owner)]:world.OwnerColor(owner)
                :world.CountryMapColor(owner,(int)MathF.Floor(map.Uv.X*10),(int)MathF.Floor(map.Uv.Y*10));
            float diffuse=map.Diffuse/255f,reflection=map.Reflection/255f,coverage=map.Coverage/255f;
            // The neutral image keeps its silhouette/alpha; reflection is a
            // separate authored layer, so a flag change cannot tint the shine.
            var shaded=Color.Lerp(new Color((int)(ink.R*diffuse),(int)(ink.G*diffuse),(int)(ink.B*diffuse)),Color.White,reflection);
            var painted=Color.Lerp(original,shaded,coverage);
            result[i]=new Color(painted.R,painted.G,painted.B,original.A);
        }
        return result;
    }
}
