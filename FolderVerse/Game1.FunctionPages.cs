namespace FolderVerse;
using System;
using Microsoft.Xna.Framework;

public partial class Game1
{
    private int _hangarPage,_factoryPage;
    private readonly int[] _directPages=new int[2];
    private static Rectangle FunctionPagePrevious=>new(660,262,62,30);
    private static Rectangle FunctionPageNext=>new(802,262,62,30);
    private int FunctionPageCount(int node)=>Math.Max(1,(PlanningStock.Nodes[node].TotalCapacity+11)/12);
    private bool HangarPageClick(Point p)
    {
        if(!FunctionPagePrevious.Contains(p) && !FunctionPageNext.Contains(p))return false;
        _hangarPage=Math.Clamp(_hangarPage+(FunctionPagePrevious.Contains(p)?-1:1),0,FunctionPageCount(HangarPlanningNode)-1);return true;
    }
    private void DrawHangarPages()
    {
        if(FunctionPageCount(HangarPlanningNode)<=1)return;
        _ui.Button(FunctionPagePrevious,"←",Muted,.4f);_ui.Button(FunctionPageNext,"→",Muted,.4f);
        _ui.Center($"{_hangarPage+1}/{FunctionPageCount(HangarPlanningNode)}",new(722,262,80,30),.35f,Cream);
    }
    private static Rectangle FactoryPagePrevious=>new(660,926,62,30);
    private static Rectangle FactoryPageNext=>new(802,926,62,30);
    private bool FactoryPageClick(Point p)
    {
        if(!FactoryPagePrevious.Contains(p) && !FactoryPageNext.Contains(p))return false;
        int pages=Math.Max(1,(PlanningWorkshop(_populationNode).ProductionLines.Length+2)/3);
        _factoryPage=Math.Clamp(_factoryPage+(FactoryPagePrevious.Contains(p)?-1:1),0,pages-1);_factoryDropDown=_factoryPlanLine=-1;return true;
    }
    private void DrawFactoryPages()
    {
        int pages=Math.Max(1,(PlanningWorkshop(_populationNode).ProductionLines.Length+2)/3);if(pages<=1)return;
        _ui.Button(FactoryPagePrevious,"←",Muted,.4f);_ui.Button(FactoryPageNext,"→",Muted,.4f);
        _ui.Center($"{_factoryPage+1}/{pages}",new(722,926,80,30),.35f,Cream);
    }
    private static Rectangle DirectPageButton(int side,bool next)=>new(420+side*1320+(next?74:0),264,64,30);
    private bool DirectPageClick(Point p)
    {
        for(int side=0;side<2;side++)foreach(bool next in new[]{false,true})if(DirectPageButton(side,next).Contains(p)){
            _directPages[side]=Math.Clamp(_directPages[side]+(next?1:-1),0,FunctionPageCount(_directNodes[side])-1);return true;
        }
        return false;
    }
    private void DrawDirectPages(int side)
    {
        if(FunctionPageCount(_directNodes[side])<=1)return;
        _ui.Button(DirectPageButton(side,false),"←",Muted,.4f);_ui.Button(DirectPageButton(side,true),"→",Muted,.4f);
        _ui.Text($"{_directPages[side]+1}/{FunctionPageCount(_directNodes[side])}",new(340+side*1320,270),.32f,Cream);
    }
}
