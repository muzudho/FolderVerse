namespace FolderVerse;
using System;
using System.Linq;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
public partial class Game1
{
    private const int InvasionRowsTop=300,InvasionRowsBottom=568,InvasionRobotsPerRowPage=48;
    private static readonly Rectangle InvasionPrevious=new(84,584,110,36),InvasionNext=new(210,584,110,36),InvasionChangeTarget=new(570,584,274,36);
    private readonly record struct InvasionRowLayout(int Index,int Row,Rectangle Bounds,int FirstSlot,int SlotCount);
    private List<List<InvasionRowLayout>> InvasionPages(){
        var pages=new List<List<InvasionRowLayout>>{new()};int y=InvasionRowsTop;
        for(int index=0;index<_invasionSources.Count;index++){
            int total=InvasionPreviewRobots(_invasionSources[index]).Length;
            for(int first=0;first<Math.Max(1,total);first+=InvasionRobotsPerRowPage){
                int count=Math.Min(InvasionRobotsPerRowPage,Math.Max(0,total-first));
                int height=Math.Max(76,34+Math.Max(1,(count+11)/12)*42);
                if(y+height>InvasionRowsBottom){pages.Add(new());y=InvasionRowsTop;}
                var page=pages[^1];page.Add(new(index,page.Count,new(84,y,760,height),first,count));y+=height+4;
            }
        }
        return pages;
    }
    private List<int> InvasionPageStarts()=>InvasionPages().Select(p=>p.Count==0?0:p[0].Index).ToList();
    private int InvasionSourceHeight(int index)=>Math.Max(76,34+Math.Max(1,(Math.Min(InvasionRobotsPerRowPage,InvasionPreviewRobots(_invasionSources[index]).Length)+11)/12)*42);
    private int InvasionPageForSource(int index)=>InvasionPageForRobot(index,0);
    private int InvasionPageForRobot(int index,int slot)=>Math.Max(0,InvasionPages().FindIndex(page=>page.Any(row=>row.Index==index && slot>=row.FirstSlot && slot<row.FirstSlot+Math.Max(1,row.SlotCount))));
    private IEnumerable<InvasionRowLayout> InvasionVisibleRows(){var pages=InvasionPages();_invasionPage=Math.Clamp(_invasionPage,0,pages.Count-1);return pages[_invasionPage];}
    private static Rectangle InvasionCardAt(Rectangle bounds,int slot)=>new(bounds.X+10+slot%12*61,bounds.Y+34+slot/12*42,57,37);
    private static Rectangle InvasionQueenAt(Rectangle bounds)=>new(474,bounds.Y+2,350,30);
}
