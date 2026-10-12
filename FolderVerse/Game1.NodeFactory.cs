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
    private static readonly Rectangle NodeFactoryTile=new(84,875,182,84);
    private bool CanManageNodeFactory(int owner){for(int r=owner;r>=0;r=_setup.Relations.Superiors[r])if(r==_setup.PlayerSlot)return true;return false;}
    private void OpenNodeFactory(){if(!CanManageNodeFactory(_setup.Nodes.All[_populationNode].Owner))return;_factoryDropDown=_factoryPlanLine=-1;_factoryLine=0;_factoryMessage="";_nodeAssemblyOpen=_nodeDisposalOpen=_nodeTransportOpen=false;_nodeFactoryOpen=true;}
    private static Rectangle FactoryRobotButton(int line,int period)=>new(100+(Math.Max(1,period)-1)*72,304+line*164,72,88);
    private static Rectangle FactoryPlanButton(int i)=>new(94+i*194,510,180,144);
    private Rectangle FactoryDropButton(int field)=>field switch{0=>new(560,357+_factoryLine*164,110,44),1=>new(740,357+_factoryLine*164,110,44),_=>new(420,806,110,44)};
    private Rectangle FactoryDropOption(int field,int index){var r=FactoryDropButton(field);int count=field==2?13:9;return new(r.X,r.Y-count*30+index*30,r.Width,30);}
    private bool NodeFactoryClick(Point pointer)
    {
        if(new Rectangle(803,169,58,44).Contains(pointer)){_nodeFactoryOpen=false;return true;}
        var factory=_setup.Robots.Workshops[_populationNode];
        if(!CanManageNodeFactory(_setup.Nodes.All[_populationNode].Owner))return true;
        if(_setup.Campaign.RobotBattles.LockedNode(_populationNode)){_factoryMessage="継戦中は製造計画を変更できません";return true;}
        if(_factoryDropDown>=0){int field=_factoryDropDown;_factoryDropDown=-1;for(int i=0;i<(field==2?13:9);i++)if(FactoryDropOption(field,i).Contains(pointer)){
            if(field==2)factory.SetReservedSlots(i);
            else {var line=factory.ProductionLines[_factoryLine];if(field==0)line.SetRest(i+1,line.RestTurns);else line.SetRest(line.RestEvery,i+1);}return true;
        }return true;}
        if(_factoryPlanLine>=0){int line=_factoryPlanLine;for(int i=0;i<4;i++)if(FactoryPlanButton(i).Contains(pointer)){factory.ProductionLines[line].SelectPlan(i);_factoryPlanLine=-1;return true;}_factoryPlanLine=-1;return true;}
        for(int l=0;l<factory.ProductionLines.Length;l++){
            var line=factory.ProductionLines[l];if(FactoryRobotButton(l,line.Manufacturing?line.ProductionPeriod:1).Contains(pointer)){_factoryPlanLine=l;_factoryLine=l;return true;}
            _factoryLine=l;for(int field=0;field<2;field++)if(FactoryDropButton(field).Contains(pointer)){_factoryDropDown=field;return true;}
        }
        if(FactoryDropButton(2).Contains(pointer))_factoryDropDown=2;
        if(NodeFactoryTile.Contains(pointer))_nodeFactoryOpen=false;
        return new Rectangle(54,150,850,825).Contains(pointer);
    }
    private void FactoryDrop(int field,int value)
    {var r=FactoryDropButton(field);_ui.Button(r,value.ToString(),Accent,.48f);for(int row=0;row<8;row++)_ui.Box(new(r.Right-23+row,r.Center.Y-3+row,16-row*2,1),Cream);}
    private void DrawNodeFactory()
    {
        var node=_setup.Nodes.All[_populationNode];var factory=_setup.Robots.Workshops[node.Id];
        _ui.Box(new(54,150,850,825),new(27,49,62));_ui.Box(new(54,150,850,6),_setup.OwnerColor(node.Owner));
        _ui.Text("ロボット製造工場",new(84,178),.8f,Cream);_ui.Button(new(803,169,58,44),"×",Muted,.7f);
        string label=_setup.Nodes.Label(node);_ui.Text(label,new(84,245),Math.Min(.5f,770/_font.MeasureString(label).X),Cream);
        int savedLine=_factoryLine;
        if(factory.ProductionLines.Length==0)_ui.Center("製造しない",new(84,330,770,250),.65f,Cream);
        for(int l=0;l<factory.ProductionLines.Length;l++){
            var line=factory.ProductionLines[l];int y=300+l*164,period=line.Manufacturing?line.ProductionPeriod:1;_factoryLine=l;
            _ui.Text($"生産ライン {l+1}",new(84,y-20),.35f,Cream);
            for(int i=0;i<period;i++){int x=100+i*72;_ui.Box(new(x,y+96,72,8),Accent);_ui.Box(new(x+70,y+96,2,8),new(27,49,62));_ui.Center((i+1).ToString(),new(x,y+107,72,23),.29f,Cream);}
            var robot=FactoryRobotButton(l,period);_ui.Box(robot,_factoryPlanLine==l || robot.Contains(_pointer)?Accent:Muted);
            if(line.Manufacturing)DrawRobotGlyph(robot,node.Owner,line.Product,whiteOutline:true);else _ui.Center("×",robot,.7f,Cream);
            _ui.Text("繰り返す",new(560,y+29),.32f,Cream);FactoryDrop(0,line.RestEvery);_ui.Text("回",new(680,y+68),.35f,Cream);
            _ui.Text("休業",new(740,y+29),.32f,Cream);FactoryDrop(1,line.RestTurns);_ui.Text("ターン",new(740,y+111),.32f,Cream);
            string status=!line.Manufacturing?"製造しない":line.RestRemaining>0?$"休業 あと{line.RestRemaining}ターン":line.Waiting?"空き枠待ち":$"製造 {line.ProductionAge}/{line.ProductionPeriod}ターン";
            _ui.Text(status,new(100,y+135),.3f,Cream);
        }
        _factoryLine=savedLine;
        _ui.Text("格納庫は必ず",new(180,817),.43f,Cream);FactoryDrop(2,factory.ReservedSlots);_ui.Text("個空ける",new(552,817),.43f,Cream);
        DrawNodeFactoryTile(true);DrawNodeTransportTile();DrawNodeDisposalTile();DrawNodeAssemblyTile();
        if(_factoryPlanLine>=0){var line=factory.ProductionLines[_factoryPlanLine];_ui.Box(new(84,455,790,216),new(22,40,54));_ui.Text($"生産ライン {_factoryPlanLine+1} / 製造プラン",new(100,469),.45f,Cream);for(int i=0;i<4;i++){var r=FactoryPlanButton(i);_ui.Box(r,line.SelectedPlan==i?Accent:Muted);if(i==3)_ui.Center("製造しない",r,.42f,Cream);else{DrawRobotGlyph(new(r.X+54,r.Y+8,72,88),node.Owner,line.Plans[i].Product);_ui.Center($"{line.Plans[i].Turns}ターン毎に製造",new(r.X,r.Bottom-36,r.Width,28),.36f,Cream);}}}
        if(_factoryDropDown>=0)for(int i=0;i<(_factoryDropDown==2?13:9);i++)_ui.Button(FactoryDropOption(_factoryDropDown,i),(_factoryDropDown==2?i:i+1).ToString(),Muted,.42f);
    }
    private void DrawNodeFactoryTile(bool active)
    {
        bool enabled=CanManageNodeFactory(_setup.Nodes.All[_populationNode].Owner);
        _ui.Box(new(84,852,770,2),Muted);
        _ui.Box(NodeFactoryTile,active || enabled && NodeFactoryTile.Contains(_pointer)?Accent:Muted);
        _spriteBatch.Draw(_robotFactoryIcon,new Rectangle(94,891,52,52),enabled?Color.White:Color.White*.35f);
        _ui.Center("ロボット製造工場",new(148,885,112,60),.24f,enabled?Cream:Cream*.4f);
        if(active)_ui.Box(new(NodeFactoryTile.X,NodeFactoryTile.Y,NodeFactoryTile.Width,4),Cream);
    }
}
