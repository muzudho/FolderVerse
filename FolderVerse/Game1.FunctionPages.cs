namespace FolderVerse;
using System;
using Microsoft.Xna.Framework;

public partial class Game1
{
    private int _hangarPage,_factoryPage;
    private readonly int[] _directPages=new int[2];
    private static Rectangle HangarUp=>new(HangarDown.X,276,HangarDown.Width,HangarDown.Height);
    private static Rectangle HangarDown=>new(84,833,780,24);
    private static readonly Rectangle HangarLeft=new(56,300,24,530),HangarRight=new(868,300,24,530);
    private int _hangarScrollDirection;
    private float _hangarScrollAt;
    private int FunctionPageCount(int node)=>Math.Max(1,(PlanningStock.Nodes[node].TotalCapacity+11)/12);
    private int HangarPageCount(int node)=>Math.Max(1,(PlanningStock.Nodes[node].TotalCapacity+HangarSlotsPerPage-1)/HangarSlotsPerPage);
    private bool HangarPageClick(Point p)
    {
        int direction=HangarScrollDirection(p);
        if(direction==0)return false;
        _hangarDrag=null;
        ScrollHangar(direction);
        return true;
    }
    private static int HangarScrollDirection(Point p)=>HangarUp.Contains(p)?-1:HangarDown.Contains(p)?1:
        HangarLeft.Contains(p)?-2:HangarRight.Contains(p)?2:0;
    private int HangarLastRow=>Math.Max(0,HangarGridRows-HangarZoom.Y);
    private void ScrollHangar(int direction)
    {
        if(Math.Abs(direction)==1)_hangarPage=Math.Clamp(_hangarPage+direction,0,HangarLastRow);
        else _hangarColumn=Math.Clamp(_hangarColumn+Math.Sign(direction),0,HangarLastColumn);
    }
    private void UpdateHangarDragScroll(bool held)
    {
        int direction=held && _hangarDrag.HasValue?HangarScrollDirection(_pointer):0;
        if(direction!=_hangarScrollDirection){
            _hangarScrollDirection=direction;
            _hangarScrollAt=_animationTime+.55f;
        }
        if(direction==0 || _animationTime<_hangarScrollAt)return;
        ScrollHangar(direction);
        _hangarScrollAt=_animationTime+.75f;
    }
    private void DrawHangarPages()
    {
        DrawArrow(HangarUp,-1,_hangarPage>0);
        DrawArrow(HangarDown,1,_hangarPage<HangarLastRow);
        DrawArrow(HangarLeft,-2,_hangarColumn>0);
        DrawArrow(HangarRight,2,_hangarColumn<HangarLastColumn);
        _ui.Center($"{HangarZoom.X}×{HangarZoom.Y}   {_hangarColumn+1}/{HangarLastColumn+1}  {_hangarPage+1}/{HangarLastRow+1}",
            new(HangarUp.Right-240,HangarUp.Y,236,HangarUp.Height),.32f,Cream);

        void DrawArrow(Rectangle button,int direction,bool enabled)
        {
            var fill=enabled && button.Contains(_pointer)?Accent:Muted;
            var ink=UiPainter.ContrastInk(fill)*(enabled?1f:.4f);
            _ui.Box(button,fill);
            int sign=Math.Sign(direction);
            if(Math.Abs(direction)==1){
                _ui.Tile(new(button.Center.X-6,button.Center.Y),new(15,3),sign*MathHelper.PiOver4,ink);
                _ui.Tile(new(button.Center.X+6,button.Center.Y),new(15,3),-sign*MathHelper.PiOver4,ink);
            }else{
                _ui.Tile(new(button.Center.X,button.Center.Y-6),new(15,3),sign*MathHelper.PiOver4,ink);
                _ui.Tile(new(button.Center.X,button.Center.Y+6),new(15,3),-sign*MathHelper.PiOver4,ink);
            }
        }
    }
    private static Rectangle FactoryPagePrevious=>new(660,926,62,30);
    private static Rectangle FactoryPageNext=>new(802,926,62,30);
    private int FactoryPageCount()
    {
        var factory=PlanningWorkshop(_populationNode);
        int displayed=Math.Min(factory.LineLimit,factory.ProductionLines.Length+PendingFactoryWorkers(factory).Length);
        return Math.Max(1,(displayed+2)/3);
    }
    private bool FactoryPageClick(Point p)
    {
        if(!FactoryPagePrevious.Contains(p) && !FactoryPageNext.Contains(p))return false;
        int pages=FactoryPageCount();
        _factoryPage=Math.Clamp(_factoryPage+(FactoryPagePrevious.Contains(p)?-1:1),0,pages-1);_factoryDropDown=_factoryPlanLine=-1;return true;
    }
    private void DrawFactoryPages()
    {
        int pages=FactoryPageCount();if(pages<=1)return;
        _ui.Button(FactoryPagePrevious,"←",Muted,.4f);_ui.Button(FactoryPageNext,"→",Muted,.4f);
        _ui.Center($"{_factoryPage+1}/{pages}",new(722,926,80,30),.35f,Cream);
    }
    private static Rectangle DirectPageButton(int side,bool next)=>new(420+side*1320+(next?74:0),264,64,30);
    private bool DirectPageClick(Point p)
    {
        for(int side=0;side<2;side++)foreach(bool next in new[]{false,true})if(DirectPageButton(side,next).Contains(p)){
            _directDrag=null;_directPages[side]=Math.Clamp(_directPages[side]+(next?1:-1),0,HangarPageCount(_directNodes[side])-1);return true;
        }
        return false;
    }
    private void DrawDirectPages(int side)
    {
        if(HangarPageCount(_directNodes[side])<=1)return;
        _ui.Button(DirectPageButton(side,false),"←",Muted,.4f);_ui.Button(DirectPageButton(side,true),"→",Muted,.4f);
        _ui.Text($"{_directPages[side]+1}/{HangarPageCount(_directNodes[side])}",new(340+side*1320,270),.32f,Cream);
    }
}
