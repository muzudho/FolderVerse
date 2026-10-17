namespace FolderVerse;
using System;
using System.Linq;
using System.Globalization;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
public partial class Game1
{
    private Texture2D _medallionRing;
    private Color _medallionColor;
    private const float MedallionNameRadius=132; // Lettering moved outward by 2 px; the metal ring stays in place.
    private const float MedallionBandWidth=22*1.2f;
    private const float MedallionBandInner=132-MedallionBandWidth/2;
    private const float MedallionBandOuter=132+MedallionBandWidth/2;
    private const float MedallionPortraitRadius=MedallionBandInner-3;
    private const float MedallionRadius=MedallionBandOuter+3;
    private const float MedallionFontScale=.36f*1.2f;
    private const float MedallionLetterGap=MathHelper.Pi/36; // Extra five degrees between glyph bounds.
    private static string MedallionName(string fullName)=>string.Join(" ",fullName.Replace('・',' ').Split(new[]{' ','　'},StringSplitOptions.RemoveEmptyEntries));
    private void DrawPortraitMedallion(Vector2 center,Color nation){
        if(_medallionRing==null || _medallionColor!=nation){
            _medallionRing?.Dispose();DisposeMedallionLetters();_medallionColor=nation;
            int size=(int)MathF.Ceiling(MedallionRadius)*2+2;var pixels=new Color[size*size];
            var surface=Color.Lerp(nation,new Color(246,241,226),.52f);
            for(int y=0;y<size;y++)for(int x=0;x<size;x++){
                float dx=x-(size-1)*.5f,dy=y-(size-1)*.5f,r=MathF.Sqrt(dx*dx+dy*dy);
                if(r<MedallionPortraitRadius || r>MedallionRadius+.5f)continue;
                uint hash=unchecked((uint)x*374761393u+(uint)y*668265263u);hash=unchecked((hash^(hash>>13))*1274126177u);hash^=hash>>16;
                float grain=(hash&65535)/65535f-.5f;
                Color ink=r<MedallionBandInner || r>MedallionBandOuter?nation:Color.Lerp(surface,grain<0?new Color(65,61,57):Color.White,Math.Abs(grain)*.22f);
                if(r>MedallionBandOuter-1.5f && r<MedallionBandOuter)ink=Color.Lerp(ink,Color.White,.22f);
                if(r>MedallionBandInner && r<MedallionBandInner+1.5f)ink=Color.Lerp(ink,Color.Black,.18f);
                float coverage=Math.Min(Math.Clamp(r-MedallionPortraitRadius+.5f,0,1),Math.Clamp(MedallionRadius+.5f-r,0,1));
                pixels[y*size+x]=ink*coverage;
            }
            _medallionRing=new Texture2D(GraphicsDevice,size,size);_medallionRing.SetData(pixels);
        }
        _spriteBatch.Draw(_medallionRing,center-new Vector2(_medallionRing.Width*.5f),Color.White);
    }
    private (string Glyph,Vector2 At,float Rotation,Vector2 Origin)[] MedallionNameLayout(Vector2 center,string fullName){
        var text=MedallionName(fullName);var glyphs=new List<string>();
        var enumerator=StringInfo.GetTextElementEnumerator(text);while(enumerator.MoveNext())glyphs.Add(enumerator.GetTextElement());
        if(glyphs.Count==0)return Array.Empty<(string,Vector2,float,Vector2)>();
        float arc=glyphs.Sum(g=>_font.MeasureString(g).X)*MedallionFontScale/MedallionNameRadius+MedallionLetterGap*(glyphs.Count-1),cursor=-arc/2;
        var result=new List<(string,Vector2,float,Vector2)>();
        foreach(string glyph in glyphs){
            var measure=_font.MeasureString(glyph);float advance=measure.X*MedallionFontScale/MedallionNameRadius;
            float angle=-MathHelper.PiOver2+cursor+advance/2;
            var position=center+new Vector2(MathF.Cos(angle),MathF.Sin(angle))*MedallionNameRadius;
            result.Add((glyph,position,angle+MathHelper.PiOver2,measure/2));cursor+=advance+MedallionLetterGap;
        }
        return result.ToArray();
    }
    private readonly Dictionary<(string Glyph,Color Nation),Texture2D> _medallionLetters=new();
    private void DisposeMedallionLetters(){foreach(var texture in _medallionLetters.Values)texture.Dispose();_medallionLetters.Clear();}
    private Texture2D MedallionLetterTexture(string glyph,Color nation){
        if(_medallionLetters.TryGetValue((glyph,nation),out var cached))return cached;
        var shape=_font.GetGlyphs()[glyph[0]];var bounds=shape.BoundsInTexture;
        var source=new Color[bounds.Width*bounds.Height];_font.Texture.GetData(0,bounds,source,0,source.Length);
        const int bold=3,padding=bold+1,bevel=3;
        int width=bounds.Width+padding*2,height=bounds.Height+padding*2;var mask=new byte[width*height];
        // Dilate the original glyph into an extra-bold silhouette while retaining antialiasing.
        for(int y=0;y<bounds.Height;y++)for(int x=0;x<bounds.Width;x++){
            byte alpha=source[y*bounds.Width+x].A;if(alpha==0)continue;
            for(int dy=-bold;dy<=bold;dy++)for(int dx=-bold;dx<=bold;dx++)if(dx*dx+dy*dy<=bold*bold){
                int at=(y+padding+dy)*width+x+padding+dx;if(mask[at]<alpha)mask[at]=alpha;
            }
        }
        var pixels=new Color[mask.Length];var surface=Color.Lerp(nation,new Color(246,241,226),.52f);
        var floor=Color.Lerp(surface,new Color(35,31,28),.42f);
        float Alpha(int x,int y)=>x<0 || y<0 || x>=width || y>=height?0:mask[y*width+x]/255f;
        for(int y=0;y<height;y++)for(int x=0;x<width;x++){
            float alpha=Alpha(x,y);if(alpha==0)continue;
            // Recessed lettering: shadow INSIDE the upper-left edge and light on the lower-right wall.
            float shadow=Math.Max(0,alpha-Alpha(x-bevel,y-bevel))/alpha;
            float light=Math.Max(0,alpha-Alpha(x+bevel,y+bevel))/alpha;
            var color=Color.Lerp(floor,new Color(18,15,13),shadow*.90f);
            color=Color.Lerp(color,new Color(255,249,229),light*.88f);
            pixels[y*width+x]=color*alpha;
        }
        var texture=new Texture2D(GraphicsDevice,width,height);texture.SetData(pixels);_medallionLetters.Add((glyph,nation),texture);return texture;
    }
    private void DrawPortraitMedallionName(Vector2 center,string fullName,Color nation){
        foreach(var letter in MedallionNameLayout(center,fullName)){
            if(letter.Glyph==" ")continue;
            var texture=MedallionLetterTexture(letter.Glyph,nation);
            _spriteBatch.Draw(texture,letter.At,null,Color.White,letter.Rotation,new Vector2(texture.Width/2f,texture.Height/2f),MedallionFontScale,SpriteEffects.None,0);
        }
    }
}
