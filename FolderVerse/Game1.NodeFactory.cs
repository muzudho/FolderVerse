namespace FolderVerse;
using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

public partial class Game1
{
    private Texture2D _robotFactoryIcon;
    private bool _nodeFactoryOpen;
    private RobotParts _factoryPart;
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
        _factoryPart=_setup.Robots.Workshops[_populationNode].Product;
        _factoryMessage="";_nodeAssemblyOpen=false;_nodeDisposalOpen=false;_nodeTransportOpen=false;_nodeFactoryOpen=true;
    }
    private bool NodeFactoryClick(Point pointer)
    {
        if(new Rectangle(803,169,58,44).Contains(pointer)){_nodeFactoryOpen=false;return true;}
        if(!CanManageNodeFactory(_setup.Nodes.All[_populationNode].Owner))return true;
        if(_setup.Campaign.RobotBattles.LockedNode(_populationNode)){_factoryMessage="継戦中は製造計画を変更できません";return true;}
        var workshop=_setup.Robots.Workshops[_populationNode];
        for(int i=0;i<4;i++)if(FactoryPlanButton(i).Contains(pointer)){workshop.SelectPlan(i);_factoryMessage="製造プランを選択しました";return true;}
        for(int i=1;i<=9;i++){
            if(FactoryNumberButton(i-1,550).Contains(pointer)){workshop.SetRest(i,workshop.RestTurns);return true;}
            if(FactoryNumberButton(i-1,640).Contains(pointer)){workshop.SetRest(workshop.RestEvery,i);return true;}
        }
        for(int i=0;i<=12;i++)if(FactoryNumberButton(i,750).Contains(pointer)){workshop.SetReservedSlots(i);return true;}
        if(NodeFactoryTile.Contains(pointer))_nodeFactoryOpen=false;        return new Rectangle(54,150,850,825).Contains(pointer);
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
    private static Rectangle FactoryPlanButton(int i)=>new(84+i%2*386,340+i/2*68,372,58);
    private static Rectangle FactoryNumberButton(int i,int y)=>new(84+i*58,y,52,48);
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
            string plan=i==3?"製造しない":$"{workshop.Plans[i].Turns}ターン毎に {PartsLabel(workshop.Plans[i].Product)}";
            _ui.Button(FactoryPlanButton(i),plan,workshop.SelectedPlan==i?Accent:Muted,.43f);
        }
        _ui.Text($"休業計画 / {workshop.RestEvery}回製造毎に {workshop.RestTurns}ターン休業",new(84,495),.5f,Cream);
        _ui.Text("製造回数",new(84,525),.34f,Cream);
        _ui.Text("休業ターン",new(84,615),.34f,Cream);
        for(int i=1;i<=9;i++){
            _ui.Button(FactoryNumberButton(i-1,550),i.ToString(),workshop.RestEvery==i?Accent:Muted,.5f);
            _ui.Button(FactoryNumberButton(i-1,640),i.ToString(),workshop.RestTurns==i?Accent:Muted,.5f);
        }
        _ui.Text($"製造数 / 格納庫は必ず {workshop.ReservedSlots}個空ける",new(84,705),.5f,Cream);
        for(int i=0;i<=12;i++)_ui.Button(FactoryNumberButton(i,750),i.ToString(),workshop.ReservedSlots==i?Accent:Muted,.46f);
        string status=!workshop.Manufacturing?"製造しない":workshop.RestRemaining>0?$"休業 あと{workshop.RestRemaining}ターン":workshop.Waiting?"空き枠待ち":$"製造 {workshop.ProductionAge}/{workshop.ProductionPeriod}ターン";
        _ui.Text(status,new(84,814),.42f,Cream);        DrawNodeFactoryTile(true);DrawNodeTransportTile();DrawNodeDisposalTile();DrawNodeAssemblyTile();

    }
}

