namespace FolderVerse;
using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

public partial class Game1
{
    private Texture2D _robotFactoryIcon;
    private bool _nodeFactoryOpen;
    private int _factoryDropDown=-1;
    private string _factoryMessage="";
    private static readonly Rectangle NodeFactoryTile=new(84,875,182,84);
    private bool CanManageNodeFactory(int owner)
    {
        for(int ruler=owner;ruler>=0;ruler=_setup.Relations.Superiors[ruler])
            if(ruler==_setup.PlayerSlot)return true;
        return false;
    }
    private void OpenNodeFactory()
    {
        if(!CanManageNodeFactory(_setup.Nodes.All[_populationNode].Owner))return;
        _factoryDropDown=-1;
        _factoryMessage="";_nodeAssemblyOpen=false;_nodeDisposalOpen=false;_nodeTransportOpen=false;_nodeFactoryOpen=true;
    }
    private bool NodeFactoryClick(Point pointer)
    {
        if(new Rectangle(803,169,58,44).Contains(pointer)){_nodeFactoryOpen=false;return true;}
        if(!CanManageNodeFactory(_setup.Nodes.All[_populationNode].Owner))return true;
        if(_setup.Campaign.RobotBattles.LockedNode(_populationNode)){_factoryMessage="継戦中は製造計画を変更できません";return true;}
        var workshop=_setup.Robots.Workshops[_populationNode];

        if(_factoryDropDown>=0){int field=_factoryDropDown;_factoryDropDown=-1;int count=field==2?13:9;
            for(int i=0;i<count;i++)if(FactoryDropOption(field,i).Contains(pointer)){
                if(field==0)workshop.SetRest(i+1,workshop.RestTurns);
                if(field==1)workshop.SetRest(workshop.RestEvery,i+1);
                if(field==2)workshop.SetReservedSlots(i);
                return true;
            }return true;
        }
        for(int i=0;i<4;i++)if(FactoryPlanButton(i).Contains(pointer)){workshop.SelectPlan(i);_factoryMessage="製造プランを選択しました";return true;}
        for(int i=0;i<3;i++)if(FactoryDropButton(i).Contains(pointer)){_factoryDropDown=i;return true;}        if(NodeFactoryTile.Contains(pointer))_nodeFactoryOpen=false;        return new Rectangle(54,150,850,825).Contains(pointer);
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
    private static Rectangle FactoryPlanButton(int i)=>new(84+i*196,340,182,146);
    private static Rectangle FactoryDropButton(int field)=>field switch{0=>new(250,550,120,48),1=>new(460,550,120,48),_=>new(420,650,120,48)};
    private static Rectangle FactoryDropOption(int field,int index){var button=FactoryDropButton(field);int count=field==2?13:9;return new(button.X,button.Y-count*30+index*30,button.Width,30);}
    private void DrawNodeFactory()
    {
        var node=_setup.Nodes.All[_populationNode];var workshop=_setup.Robots.Workshops[node.Id];
        _ui.Box(new(64,162,850,825),new Color(3,13,20)*.8f);
        _ui.Box(new(54,150,850,825),new(27,49,62));
        _ui.Box(new(54,150,850,6),_setup.OwnerColor(node.Owner));
        _ui.Text("ロボット製造工場",new(84,178),.8f,Cream);
        _ui.Button(new(803,169,58,44),"×",Muted,.7f);
        string label=_setup.Nodes.Label(node);
        _ui.Text(label,new(84,245),Math.Min(.5f,770/_font.MeasureString(label).X),Cream);
        _ui.Text("工期 / この節点の製造プラン",new(84,300),.55f,Cream);
        for(int i=0;i<4;i++){
            var rect=FactoryPlanButton(i);_ui.Box(rect,workshop.SelectedPlan==i?Accent:Muted);
            if(i==3)_ui.Center("製造しない",rect,.48f,Cream);
            else {
                DrawRobotGlyph(new(rect.X+55,rect.Y+10,72,88),node.Owner,workshop.Plans[i].Product);
                _ui.Center($"{workshop.Plans[i].Turns}ターン毎に製造",new(rect.X,rect.Bottom-40,rect.Width,30),.38f,Cream);
            }
        }
        _ui.Text("休業計画",new(84,515),.55f,Cream);
        _ui.Text("製造",new(180,561),.43f,Cream);_ui.Text("回毎に",new(378,561),.43f,Cream);_ui.Text("ターン休業",new(590,561),.43f,Cream);
        _ui.Button(FactoryDropButton(0),workshop.RestEvery+"  v",Accent,.55f);
        _ui.Button(FactoryDropButton(1),workshop.RestTurns+"  v",Accent,.55f);
        _ui.Text("製造数",new(84,615),.55f,Cream);
        _ui.Text("格納庫は必ず",new(180,661),.43f,Cream);_ui.Text("個空ける",new(552,661),.43f,Cream);
        _ui.Button(FactoryDropButton(2),workshop.ReservedSlots+"  v",Accent,.55f);        string status=!workshop.Manufacturing?"製造しない":workshop.RestRemaining>0?$"休業 あと{workshop.RestRemaining}ターン":workshop.Waiting?"空き枠待ち":$"製造 {workshop.ProductionAge}/{workshop.ProductionPeriod}ターン";
        _ui.Text(status,new(84,814),.42f,Cream);        DrawNodeFactoryTile(true);DrawNodeTransportTile();DrawNodeDisposalTile();DrawNodeAssemblyTile();
        if(_factoryDropDown>=0){int field=_factoryDropDown,count=field==2?13:9;for(int i=0;i<count;i++)_ui.Button(FactoryDropOption(field,i),(field==2?i:i+1).ToString(),Muted,.42f);}

    }
}



