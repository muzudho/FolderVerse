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
    private const float MedallionNameRadius=132; // Previous 134 px, inset by 2 px.
    private const float MedallionBandWidth=22*1.2f;
    private const float MedallionBandInner=MedallionNameRadius-MedallionBandWidth/2;
    private const float MedallionBandOuter=MedallionNameRadius+MedallionBandWidth/2;
    private const float MedallionPortraitRadius=MedallionBandInner-3;
    private const float MedallionRadius=MedallionBandOuter+3;
    private const float MedallionFontScale=.36f*1.2f;
    private const float MedallionLetterGap=MathHelper.Pi/36; // Extra five degrees between glyph bounds.
    private static string MedallionName(string fullName)=>string.Join(" ",fullName.Replace('・',' ').Split(new[]{' ','　'},StringSplitOptions.RemoveEmptyEntries));
    private void DrawPortraitMedallion(Vector2 center,Color nation){
        if(_medallionRing==null || _medallionColor!=nation){
            _medallionRing?.Dispose();_medallionColor=nation;
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
    private void DrawPortraitMedallionName(Vector2 center,string fullName,Color nation){
        Color ink=Color.Lerp(nation,new Color(15,23,28),.82f),shine=Color.Lerp(nation,Color.White,.8f);
        foreach(var letter in MedallionNameLayout(center,fullName)){
            if(letter.Glyph==" ")continue;
            _spriteBatch.DrawString(_font,letter.Glyph,letter.At+new Vector2(.6f,.8f),shine,letter.Rotation,letter.Origin,MedallionFontScale,SpriteEffects.None,0);
            _spriteBatch.DrawString(_font,letter.Glyph,letter.At,ink,letter.Rotation,letter.Origin,MedallionFontScale,SpriteEffects.None,0);
        }
    }
}
