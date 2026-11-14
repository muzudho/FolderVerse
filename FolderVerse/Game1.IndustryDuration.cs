namespace FolderVerse;
using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

public partial class Game1
{
    private SpriteFont _durationFont;
    private Dictionary<char,SpriteFont.Glyph> _durationGlyphs;
    private void PrepareDurationFont(){if(_durationFont!=_font){_durationFont=_font;_durationGlyphs=_font.GetGlyphs();}}
    private Vector2 IndustryDurationSize(int units,float scale,string prefix="",string suffix="")
    {
        PrepareDurationFont();
        int whole=units/IndustryTime.UnitsPerTurn,remainder=units%IndustryTime.UnitsPerTurn;
        string leading=prefix+(whole>0 || remainder==0?whole.ToString():"")+(whole>0 && remainder!=0?" ":"");
        float width=_font.MeasureString(leading+suffix).X+(remainder==0?0:_font.MeasureString("3").X*.9f+10);
        float height=_font.MeasureString("0").Y*scale;
        if(remainder!=0){float unit=scale/.32f;float top=6*unit+_durationGlyphs[(char)('0'+remainder)].BoundsInTexture.Height*scale*.9f,bottom=unit+_durationGlyphs['3'].BoundsInTexture.Height*scale*.9f;height=Math.Max(height,2*Math.Max(top,bottom));}
        return new(width*scale,height);
    }

    // Shared mixed-number label. Numerator/denominator placement uses visible glyph
    // bounds, rather than line spacing, so the fraction bar cannot touch either digit.
    private void DrawIndustryDuration(int units,Rectangle bounds,Color color,float maxScale=.32f,string prefix="",string suffix="")
        =>DrawMixedNumber(units,IndustryTime.UnitsPerTurn,bounds,color,maxScale,prefix,suffix);

    private void DrawMixedNumber(int units,int divisor,Rectangle bounds,Color color,float maxScale,string prefix="",string suffix="")
    {
        PrepareDurationFont();
        int whole=units/divisor,remainder=units%divisor;
        int gcd=divisor;
        for(int rest=remainder;rest!=0;)(gcd,rest)=(rest,gcd%rest);
        string numerator=(remainder/gcd).ToString(),bottom=(divisor/gcd).ToString();
        string leading=prefix+(whole>0 || remainder==0?whole.ToString():"")+(whole>0 && remainder!=0?" ":"");
        const float fractionRatio=.9f;
        float fractionWidth=remainder==0?0:Math.Max(_font.MeasureString(numerator).X,_font.MeasureString(bottom).X)*fractionRatio+10;
        float textWidth=_font.MeasureString(leading).X+fractionWidth+_font.MeasureString(suffix).X;
        float scale=Math.Min(maxScale,bounds.Width/textWidth);
        float x=bounds.Center.X-textWidth*scale/2;
        float textY=bounds.Center.Y-_font.MeasureString("0").Y*scale/2;
        _ui.Text(leading,new(x,textY),scale,color);x+=_font.MeasureString(leading).X*scale;
        if(remainder!=0){
            float unit=scale/.32f;
            int left=(int)Math.Round(x),width=(int)Math.Ceiling(fractionWidth*scale);
            float barY=bounds.Center.Y-3*unit;
            float digitScale=scale*fractionRatio;
            void Digit(string digit,float top){
                var glyph=_durationGlyphs[digit[0]];
                float digitX=x+(fractionWidth*scale-_font.MeasureString(digit).X*digitScale)/2;
                _ui.Text(digit,new(digitX,top-glyph.Cropping.Y*digitScale),digitScale,color);
            }
            Digit(numerator,barY-3*unit-_durationGlyphs[numerator[0]].BoundsInTexture.Height*digitScale);
            int inset=Math.Max(1,(int)Math.Round(unit));
            _ui.Box(new(left+inset,(int)Math.Round(barY),Math.Max(1,width-inset*2),Math.Max(1,(int)Math.Round(unit))),color);
            Digit(bottom,barY+4*unit);
            x+=fractionWidth*scale;
        }
        _ui.Text(suffix,new(x,textY),scale,color);
    }
}
