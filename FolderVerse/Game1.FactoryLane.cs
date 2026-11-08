namespace FolderVerse;
using System;
using Microsoft.Xna.Framework;

public partial class Game1
{
    // Shared by the inspector and the industry demonstration, in canvas coordinates.
    private int DrawFactoryLaneTrack(RobotWorkshop line,int x,int y,double? visualProgress=null)
    {
        int period=line.Manufacturing?line.ProductionPeriod:3;
        int width=FactoryProductionWidth(period),rest=line.RestEnabled?Math.Min(4,line.RestTurns):0;
        _ui.Box(new(x,y+96,width,8),Accent);
        _ui.Box(new(x+width,y+96,rest*72,8),new Color(137,151,157));
        for(int unit=0;unit<=Math.Min(15,period);unit++)_ui.Box(new(x+unit*24-1,y+96,unit%3==0?2:1,unit%3==0?8:4),unit%3==0?new Color(27,49,62):Color.White);
        for(int unit=0;unit<=rest*3;unit++)_ui.Box(new(x+width+unit*24-1,y+96,unit%3==0?2:1,unit%3==0?8:4),unit%3==0?new Color(27,49,62):Color.White);
        DrawIndustryDuration(0,new(x-14,y+109,28,30),Cream,.3f);
        DrawIndustryDuration(period,new(x+width-40,y+109,80,30),Cream,.3f);
        if(rest>0)DrawIndustryDuration(line.CycleTurns,new(x+width+rest*72-40,y+109,80,30),Cream,.3f);
        if(!line.Manufacturing)return x;
        _ui.Box(new(x+width-1,y+93,2,14),Cream);
        double progress=visualProgress??line.LaneProgress;
        return x+(int)Math.Round(progress<=period?width*progress/period:width+rest*72*(progress-period)/IndustryTime.Whole(line.RestTurns));
    }
    private void DrawFactoryLaneOmissions(RobotWorkshop line,int x,int y)
    {
        int period=line.Manufacturing?line.ProductionPeriod:3,rest=line.RestEnabled?Math.Min(4,line.RestTurns):0;
        if(period>15)DrawFactoryOmission(x+2*72,y);
        if(line.RestEnabled && line.RestTurns>rest)DrawFactoryOmission(x+FactoryProductionWidth(period)+72,y);
    }
}
