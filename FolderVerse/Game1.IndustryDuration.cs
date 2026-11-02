namespace FolderVerse;
using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

public partial class Game1
{
    private SpriteFont _durationFont;
    private Dictionary<char,SpriteFont.Glyph> _durationGlyphs;

    // Shared mixed-number label. Numerator/denominator placement uses visible glyph
    // bounds, rather than line spacing, so the fraction bar cannot touch either digit.
    private void DrawIndustryDuration(int units,Rectangle bounds,Color color,float maxScale=.32f,string prefix="",string suffix="")
    {
        if(_durationFont!=_font){_durationFont=_font;_durationGlyphs=_font.GetGlyphs();}
        int whole=units/IndustryTime.UnitsPerTurn,remainder=units%IndustryTime.UnitsPerTurn;
        string leading=prefix+(whole>0 || remainder==0?whole.ToString():"");
        const float fractionRatio=.9f;
        float fractionWidth=remainder==0?0:_font.MeasureString("3").X*fractionRatio+10;
        float textWidth=_font.MeasureString(leading).X+fractionWidth+_font.MeasureString(suffix).X;
        float scale=Math.Min(maxScale,bounds.Width/textWidth);
        float x=bounds.Center.X-textWidth*scale/2;
        float textY=bounds.Center.Y-_font.MeasureString("0").Y*scale/2;
        _ui.Text(leading,new(x,textY),scale,color);x+=_font.MeasureString(leading).X*scale;
        if(remainder!=0){
            int left=(int)Math.Round(x),width=(int)Math.Ceiling(fractionWidth*scale),barY=bounds.Center.Y-3;
            float digitScale=scale*fractionRatio;
            void Digit(string digit,float top){
                var glyph=_durationGlyphs[digit[0]];
                float digitX=x+(fractionWidth*scale-_font.MeasureString(digit).X*digitScale)/2;
                _ui.Text(digit,new(digitX,top-glyph.Cropping.Y*digitScale),digitScale,color);
            }
            string numerator=remainder.ToString();
            Digit(numerator,barY-3-_durationGlyphs[numerator[0]].BoundsInTexture.Height*digitScale);
            _ui.Box(new(left+1,barY,Math.Max(1,width-2),1),color);
            Digit("3",barY+4);
            x+=fractionWidth*scale;
        }
        _ui.Text(suffix,new(x,textY),scale,color);
    }
}
