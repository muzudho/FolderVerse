namespace FolderVerse;
using System;
using System.Linq;
using Microsoft.Xna.Framework;

public partial class Game1
{
    private int _factoryStartLane=-1;
    private Rectangle FactoryStartButton(int row)=>new(478,FactoryRowY(row),350,28);
    private static Rectangle FactoryStartOption(int i)=>new(378+i/14*250,300+i%14*34,245,30);
    private bool FactoryStartClick(Point pointer,RobotWorkshop factory,RobotWorkshop[] lines)
    {
        if(_factoryStartLane<0)return false;
        int target=_factoryStartLane;_factoryStartLane=-1;
        if(target>=lines.Length)return true;
        int actual=Array.IndexOf(factory.ProductionLines,lines[target]);
        if(actual<0){_factoryMessage="追加予定のレーンは任命確定後に設定できます";return true;}
        for(int i=0;i<=factory.ProductionLines.Length;i++)if(FactoryStartOption(i).Contains(pointer)){
            if(i==0)factory.SetStartAfter(actual,null);
            else if(!factory.ProductionLines[i-1].HarvestOnly && factory.ProductionLines[i-1].Manufacturing && factory.CanStartAfter(actual,i-1))factory.SetStartAfter(actual,i-1);
            else _factoryMessage="このレーンは開始条件に指定できません";
            return true;
        }
        return true;
    }
    private void DrawFactoryStartMenu(RobotWorkshop factory,RobotWorkshop[] lines)
    {
        if(_factoryStartLane<0 || _factoryStartLane>=lines.Length)return;
        _ui.Box(new(350,260,540,530),new Color(8,24,36));
        _ui.Text("生産の開始条件",new(378,269),.45f,Cream);
        int actual=Array.IndexOf(factory.ProductionLines,lines[_factoryStartLane]);
        for(int i=0;i<=factory.ProductionLines.Length;i++){
            bool valid=actual>=0 && (i==0 || factory.ProductionLines[i-1].Manufacturing && !factory.ProductionLines[i-1].HarvestOnly && factory.CanStartAfter(actual,i-1));
            _ui.Button(FactoryStartOption(i),i==0?"今すぐ開始":$"レーン {i} 生産完了後",valid?Accent:Muted,.34f);
        }
    }
    private void DrawFactoryRawProgress(RobotWorkshop line,int y,NodePurpose purpose)
    {
        if(!line.Manufacturing)return;
        int row=0;
        foreach(var material in RobotWorld.RawMaterials.Where(_setup.Robots.RawRequired)){
            int spent=line.RawConsumed(material),left=line.RawRemaining(material),cost=spent+left;
            if(line.CycleFinished){spent=cost;left=0;}
            if(cost==0)continue;
            int yy=y+35+row++*25;
            _ui.Box(new(478,yy+19,350,4),Muted);
            _ui.Box(new(478,yy+19,350*spent/Math.Max(1,cost),4),new Color(91,205,143));
            DrawMixedNumber(spent,16,new(478,yy,215,22),Cream,.29f,prefix:RobotWorld.RawName(material)+" 消費 ");
            DrawMixedNumber(left,16,new(700,yy,135,22),Cream,.29f,prefix:"残り ");
        }
        var done=line.WorkDone(purpose);
        DrawMixedNumber((int)done.Numerator,(int)done.Denominator,new(168,y+15,250,26),Cream,.31f,prefix:"仕掛具合 ");
        DrawIndustryDuration(line.WorkRemaining(purpose),new(168,y+42,250,26),Cream,.31f,prefix:"残り工数 ");
    }
}
