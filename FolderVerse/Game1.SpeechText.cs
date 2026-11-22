namespace FolderVerse;
using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

public partial class Game1
{
    private const float SpeechTextScale=.48f;
    private const int SpeechLineHeight=36;
    private string[] WrapSpeechLines(string[] lines,int width){
        var result=new List<string>();
        foreach(string line in lines){
            string row="";
            foreach(char c in line){
                if(row.Length>0 && _font.MeasureString(row+c).X*SpeechTextScale>width){result.Add(row);row="";}
                row+=c;
            }
            result.Add(row);
        }
        return result.ToArray();
    }
    private Rectangle SizeSpeechBalloon(Rectangle bounds,string[] lines){
        int count=WrapSpeechLines(lines,BalloonRenderer.TextArea(bounds).Width).Length;
        bounds.Height=Math.Max(bounds.Height,(int)Math.Ceiling((count*SpeechLineHeight+12)/.48f));return bounds;
    }
    private void DrawSpeechParagraph(string[] lines,Rectangle area,bool centered=true){
        var wrapped=WrapSpeechLines(lines,area.Width);
        int y=centered?area.Center.Y-wrapped.Length*SpeechLineHeight/2:area.Y;
        foreach(string line in wrapped){
            int width=centered?area.Width:(int)Math.Ceiling(_font.MeasureString(line).X*SpeechTextScale);
            PortraitHudLayout.CenterLines(_ui,_font,new[]{line},new Rectangle(area.X,y,width,SpeechLineHeight),SpeechTextScale,new Color(57,52,48));
            y+=SpeechLineHeight;
        }
    }
}
