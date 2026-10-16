namespace FolderVerse;
using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
public partial class Game1
{
    private Texture2D _robotFactoryIcon;
    private bool _nodeFactoryOpen;
    private int _factoryDropDown=-1,_factoryLine,_factoryPlanLine=-1;
    private string _factoryMessage="";
    private static readonly Rectangle NodeFactoryTile=new(84,169,230,60);
    private bool CanManageNodeFactory(int owner){for(int r=owner;r>=0;r=_setup.Relations.Superiors[r])if(r==_setup.PlayerSlot)return true;return false;}
    private void OpenNodeFactory(){if(_setup.Robots.Workshops[_populationNode].ProductionLines.Length==0 || !CanManageNodeFactory(_setup.Nodes.All[_populationNode].Owner))return;_factoryDropDown=_factoryPlanLine=-1;_factoryLine=0;_factoryMessage="";_nodeAssemblyOpen=_nodeDisposalOpen=_nodeTransportOpen=false;_nodeFactoryOpen=true;}
    private static Rectangle FactoryRobotButton(int line,int period)=>new(160+(Math.Min(5,Math.Max(1,period))-1)*72,254+line*220,72,88);
    private static Rectangle FactoryRobotArrow(int line,int period){var r=FactoryRobotButton(line,period);return new(r.Right+2,r.Y+30,24,28);}
    private static Rectangle FactoryPlanButton(int i)=>new(94+(i<4?i:i-4)*194,510+(i<4?0:155),180,144);
    private Rectangle FactoryDropButton(int field)
    {
        if(field==2)return new(420,926,110,44);
        var line=_setup.Robots.Workshops[_populationNode].ProductionLines[_factoryLine];
        int period=Math.Min(5,line.Manufacturing?line.ProductionPeriod:1),rest=Math.Min(4,line.RestTurns),y=250+_factoryLine*220;
        return field==0?new(160+period*72/2-55,y+155,110,44):new(160+period*72+rest*72/2-55,y+155,110,44);
    }
    private Rectangle FactoryDropOption(int field,int index){var r=FactoryDropButton(field);int count=field==2?13:9;return new(r.X,r.Y-count*30+index*30,r.Width,30);}
    private bool NodeFactoryClick(Point pointer)
    {
        var factory=_setup.Robots.Workshops[_populationNode];
        if(!CanManageNodeFactory(_setup.Nodes.All[_populationNode].Owner))return true;
        if(_setup.Campaign.RobotBattles.LockedNode(_populationNode)){_factoryMessage="継戦中は製造計画を変更できません";return true;}
        if(_factoryDropDown>=0){int field=_factoryDropDown;_factoryDropDown=-1;for(int i=0;i<(field==2?13:9);i++)if(FactoryDropOption(field,i).Contains(pointer)){
            if(field==2)factory.SetReservedSlots(i);
            else {var line=factory.ProductionLines[_factoryLine];if(field==0)line.SetRest(i+1,line.RestTurns);else line.SetRest(line.RestEvery,i+1);}return true;
        }return true;}
        if(_factoryPlanLine>=0){int line=_factoryPlanLine;for(int i=0;i<7;i++)if(FactoryPlanButton(i).Contains(pointer)){factory.ProductionLines[line].SelectPlan(i);_factoryPlanLine=-1;return true;}_factoryPlanLine=-1;return true;}
        for(int l=0;l<factory.ProductionLines.Length;l++){
            var line=factory.ProductionLines[l];if(FactoryRobotButton(l,line.Manufacturing?line.ProductionPeriod:1).Contains(pointer) || FactoryRobotArrow(l,line.Manufacturing?line.ProductionPeriod:1).Contains(pointer)){_factoryPlanLine=l;_factoryLine=l;return true;}
            _factoryLine=l;for(int field=0;field<2;field++)if(FactoryDropButton(field).Contains(pointer)){_factoryDropDown=field;return true;}
        }
        if(FactoryDropButton(2).Contains(pointer))_factoryDropDown=2;
        if(NodeFactoryTile.Contains(pointer))_nodeFactoryOpen=false;
        return new Rectangle(54,150,850,825).Contains(pointer);
    }
    private void FactoryDrop(int field,int value)
    {var r=FactoryDropButton(field);_ui.Button(r,(field==0?"× ":field==1?"休 ":"")+value,Accent,.48f);for(int row=0;row<8;row++)_ui.Box(new(r.Right-23+row,r.Center.Y-3+row,16-row*2,1),Cream);}
    private void DrawNodeFactory()
    {
        var node=_setup.Nodes.All[_populationNode];var factory=_setup.Robots.Workshops[node.Id];
        _ui.Box(new(54,150,850,825),new(27,49,62));_ui.Box(new(54,150,850,6),_setup.OwnerColor(node.Owner));


        int savedLine=_factoryLine;
        if(factory.ProductionLines.Length==0)_ui.Center("製造しない",new(84,330,770,250),.65f,Cream);
        for(int l=0;l<factory.ProductionLines.Length;l++){
            var line=factory.ProductionLines[l];int y=250+l*220,period=line.Manufacturing?line.ProductionPeriod:1;_factoryLine=l;
            _ui.Box(new(78,y-24,782,216),l%2==0?new Color(37,63,77):new Color(46,67,79));
            _ui.Center((l+1).ToString(),new(84,y+22,62,96),1.35f,new Color(167,190,194));
            int shownPeriod=Math.Min(5,period),shownRest=Math.Min(4,line.RestTurns);
            for(int i=0;i<shownPeriod+shownRest;i++){int x=160+i*72;_ui.Box(new(x,y+96,72,8),i<shownPeriod?Accent:new Color(137,151,157));_ui.Box(new(x+70,y+96,2,8),new(27,49,62));}
            _ui.Center("1",new(160,y+105,72,28),.43f,Cream);
            if(shownPeriod>1)_ui.Center(period.ToString(),new(160+(shownPeriod-1)*72,y+105,72,28),.43f,Cream);
            _ui.Center((period*line.RestEvery+line.RestTurns).ToString(),new(160+(shownPeriod+shownRest-1)*72,y+105,72,28),.43f,Cream);
            if(period>5)DrawFactoryOmission(160+2*72,y);
            if(line.RestTurns>4)DrawFactoryOmission(160+(shownPeriod+1)*72,y);
            var robot=FactoryRobotButton(l,period);_ui.Box(robot,_factoryPlanLine==l || robot.Contains(_pointer)?Accent:Muted);
            var arrow=FactoryRobotArrow(l,period);for(int row=0;row<10;row++)_ui.Box(new(arrow.X+2+row,arrow.Y+6+row,20-row*2,1),Cream);
            if(line.Manufacturing)DrawRobotGlyph(robot,node.Owner,line.Product,whiteOutline:true,cheap:line.CheapProduct);else _ui.Center("×",robot,.7f,Cream);
            int width=shownPeriod*72,mid=160+width/2;_ui.Box(new(160,y+126,width,2),Cream);_ui.Box(new(160,y+121,2,7),Cream);_ui.Box(new(158+width,y+121,2,7),Cream);for(int i=0;i<5;i++){_ui.Box(new(mid-5+i,y+128+i,1,1),Cream);_ui.Box(new(mid+5-i,y+128+i,1,1),Cream);}FactoryDrop(0,line.RestEvery);FactoryDrop(1,line.RestTurns);
            string status=!line.Manufacturing?"製造しない":line.RestRemaining>0?$"休業 あと{line.RestRemaining}産業ターン":line.Waiting?"空き枠待ち":$"{(line.CheapProduct?"安い":"正規")} {line.ProductionAge}/{line.ProductionPeriod}産業ターン";
            _ui.Text(status,new(560,y-19),.3f,Cream);
        }
        _factoryLine=savedLine;
        _ui.Text("格納庫は必ず",new(180,937),.43f,Cream);FactoryDrop(2,factory.ReservedSlots);_ui.Text("個空ける",new(552,937),.43f,Cream);
        DrawNodeFactoryTile(true);DrawNodeTransportTile();DrawNodeDisposalTile();DrawNodeAssemblyTile();
        if(_factoryPlanLine>=0){var line=factory.ProductionLines[_factoryPlanLine];_ui.Box(new(84,455,790,370),new(22,40,54));_ui.Text($"生産ライン {_factoryPlanLine+1} / 正規・安いパーツ",new(160,469),.45f,Cream);for(int i=0;i<7;i++){var r=FactoryPlanButton(i);_ui.Box(r,line.SelectedPlan==i?Accent:Muted);if(i==3)_ui.Center("製造しない",r,.42f,Cream);else{var plan=line.PlanAt(i);DrawRobotGlyph(new(r.X+54,r.Y+8,72,88),node.Owner,plan.Product,cheap:plan.Cheap);_ui.Center($"{(plan.Cheap?"安い":"正規")} / {plan.Turns}産業ターン",new(r.X,r.Bottom-36,r.Width,28),.32f,Cream);}}}
        if(_factoryDropDown>=0)for(int i=0;i<(_factoryDropDown==2?13:9);i++)_ui.Button(FactoryDropOption(_factoryDropDown,i),(_factoryDropDown==2?i:i+1).ToString(),Muted,.42f);
    }
    private void DrawFactoryOmission(int x,int y)
    {
        for(int offset=0;offset<=8;offset+=8)for(int row=-8;row<132;row++){
            int bend=(int)Math.Round(Math.Sin(row*.16)*3);
            _ui.Box(new(x+offset+bend-2,y+row,7,1),new(27,49,62));
            _ui.Box(new(x+offset+bend,y+row,2,1),Cream);
        }
    }
    private void DrawNodeFactoryTile(bool active)
    {
        if(_setup.Robots.Workshops[_populationNode].ProductionLines.Length==0)return;
        bool enabled=CanManageNodeFactory(_setup.Nodes.All[_populationNode].Owner);
        _ui.Button(NodeFactoryTile,"ロボット製造工場",active || enabled && NodeFactoryTile.Contains(_pointer)?Accent:Muted,.38f);
        if(active)_ui.Box(new(NodeFactoryTile.X,NodeFactoryTile.Y,NodeFactoryTile.Width,4),Cream);
    }
}
