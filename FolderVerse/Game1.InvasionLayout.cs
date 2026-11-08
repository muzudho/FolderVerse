namespace FolderVerse;
using System;
using System.Linq;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

public partial class Game1
{
    private List<int> InvasionPageStarts()
    {
        var starts=new List<int>{0};int y=300;
        for(int i=0;i<_invasionSources.Count;i++){
            int height=InvasionSourceHeight(i);
            if(y>300 && y+height>839){starts.Add(i);y=300;}
            y+=height+4;
        }
        return starts;
    }
    private int InvasionSourceHeight(int index)=>Math.Max(73,34+Math.Max(1,(InvasionPreviewRobots(_invasionSources[index]).Length+11)/12)*42);
    private int InvasionPageForSource(int index)=>InvasionPageStarts().FindLastIndex(start=>start<=index);
    private IEnumerable<(int Index,int Row,Rectangle Bounds)> InvasionVisibleRows()
    {
        var starts=InvasionPageStarts();_invasionPage=Math.Clamp(_invasionPage,0,starts.Count-1);
        int end=_invasionPage+1<starts.Count?starts[_invasionPage+1]:_invasionSources.Count,y=300,row=0;
        for(int index=starts[_invasionPage];index<end;index++){
            var bounds=new Rectangle(84,y,760,InvasionSourceHeight(index));
            yield return(index,row++,bounds);y=bounds.Bottom+4;
        }
    }
    private static Rectangle InvasionCardAt(Rectangle bounds,int slot)=>new(bounds.X+10+slot%12*61,bounds.Y+34+slot/12*42,57,37);
    private static Rectangle InvasionQueenAt(Rectangle bounds)=>new(474,bounds.Y+2,350,30);
}
