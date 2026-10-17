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
    private const int MedallionRadius=148;
    private static string MedallionName(string fullName)=>string.Join(" ",fullName.Replace('・',' ').Split(new[]{' ','　'},StringSplitOptions.RemoveEmptyEntries));
    private void DrawPortraitMedallion(Vector2 center,Color nation){
        if(_medallionRing==null || _medallionColor!=nation){
            _medallionRing?.Dispose();_medallionColor=nation;
            const int size=MedallionRadius*2+2;var pixels=new Color[size*size];
            var surface=Color.Lerp(nation,new Color(246,241,226),.52f);
            for(int y=0;y<size;y++)for(int x=0;x<size;x++){
                float dx=x-(size-1)*.5f,dy=y-(size-1)*.5f,r=MathF.Sqrt(dx*dx+dy*dy);
                if(r<120 || r>MedallionRadius+.5f)continue;
                uint hash=unchecked((uint)x*374761393u+(uint)y*668265263u);hash=unchecked((hash^(hash>>13))*1274126177u);hash^=hash>>16;
                float grain=(hash&65535)/65535f-.5f;
                Color ink=r<123 || r>145?nation:Color.Lerp(surface,grain<0?new Color(65,61,57):Color.White,Math.Abs(grain)*.22f);
                if(r>143.5f && r<145)ink=Color.Lerp(ink,Color.White,.22f);
                if(r>123 && r<124.5f)ink=Color.Lerp(ink,Color.Black,.18f);
                float coverage=Math.Min(Math.Clamp(r-119.5f,0,1),Math.Clamp(MedallionRadius+.5f-r,0,1));
                pixels[y*size+x]=ink*coverage;
            }
            _medallionRing=new Texture2D(GraphicsDevice,size,size);_medallionRing.SetData(pixels);
        }
        _spriteBatch.Draw(_medallionRing,center-new Vector2(_medallionRing.Width*.5f),Color.White);
    }
    private void DrawPortraitMedallionName(Vector2 center,string fullName,Color nation){
        var text=MedallionName(fullName);if(text.Length==0)return;
        var glyphs=new List<string>();var enumerator=StringInfo.GetTextElementEnumerator(text);while(enumerator.MoveNext())glyphs.Add(enumerator.GetTextElement());
        const float radius=134,maximumArc=2.7f,tracking=1.0f;
        float natural=glyphs.Sum(g=>_font.MeasureString(g).X),scale=Math.Min(.36f,(radius*maximumArc-tracking*(glyphs.Count-1))/Math.Max(1,natural));
        float width=natural*scale+tracking*(glyphs.Count-1),cursor=-width*.5f;
        Color ink=Color.Lerp(nation,new Color(15,23,28),.82f),shine=Color.Lerp(nation,Color.White,.8f);
        foreach(string glyph in glyphs){
            var measure=_font.MeasureString(glyph);float advance=measure.X*scale;
            float angle=-MathHelper.PiOver2+(cursor+advance*.5f)/radius,rotation=angle+MathHelper.PiOver2;
            var position=center+new Vector2(MathF.Cos(angle),MathF.Sin(angle))*radius;
            if(glyph!=" "){
                var origin=measure*.5f;
                _spriteBatch.DrawString(_font,glyph,position+new Vector2(.6f,.8f),shine,rotation,origin,scale,SpriteEffects.None,0);
                _spriteBatch.DrawString(_font,glyph,position,ink,rotation,origin,scale,SpriteEffects.None,0);
            }
            cursor+=advance+tracking;
        }
    }
}
