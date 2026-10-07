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
    private static readonly Rectangle NodeFactoryTile=new(84,875,240,84);
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
        _factoryMessage="";_nodeFactoryOpen=true;
    }
    private bool NodeFactoryClick(Point pointer)
    {
        if(new Rectangle(803,169,58,44).Contains(pointer)){_nodeFactoryOpen=false;return true;}
        if(!CanManageNodeFactory(_setup.Nodes.All[_populationNode].Owner))return true;
        if(_setup.Campaign.RobotBattles.LockedNode(_populationNode)){_factoryMessage="継戦中は製造計画を変更できません";return true;}
        var workshop=_setup.Robots.Workshops[_populationNode];
        for(int mask=1;mask<=7;mask++)if(new Rectangle(84+(mask-1)%4*196,350+(mask-1)/4*64,182,52).Contains(pointer))
        {_factoryPart=(RobotParts)mask;return true;}
        if(new Rectangle(84,510,360,58).Contains(pointer))
        {workshop.Configure(_factoryPart,workshop.ProductionPeriod);_factoryMessage="製造計画を更新し、工期を開始しました";}
        else if(new Rectangle(462,510,360,58).Contains(pointer))workshop.Paused=!workshop.Paused;
        else if(new Rectangle(84,655,180,58).Contains(pointer))workshop.Configure(workshop.Product,Math.Max(1,workshop.ProductionPeriod-1));
        else if(new Rectangle(282,655,180,58).Contains(pointer))workshop.Configure(workshop.Product,workshop.ProductionPeriod+1);
        else if(NodeFactoryTile.Contains(pointer))_nodeFactoryOpen=false;
        return new Rectangle(54,150,850,825).Contains(pointer);
    }
    private void DrawNodeFactoryTile(bool active)
    {
        bool enabled=CanManageNodeFactory(_setup.Nodes.All[_populationNode].Owner);
        _ui.Box(new(84,852,770,2),Muted);
        _ui.Box(NodeFactoryTile,active || enabled && NodeFactoryTile.Contains(_pointer)?Accent:Muted);
        _spriteBatch.Draw(_robotFactoryIcon,new Rectangle(94,883,68,68),enabled?Color.White:Color.White*.35f);
        _ui.Center("ロボット製造工場",new(166,885,150,60),.32f,enabled?Cream:Cream*.4f);
        if(active)_ui.Box(new(NodeFactoryTile.X,NodeFactoryTile.Y,NodeFactoryTile.Width,4),Cream);
    }
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
        _ui.Text("製造計画 / 製造するパーツを選択",new(84,302),.6f,Cream);
        for(int mask=1;mask<=7;mask++)_ui.Button(new(84+(mask-1)%4*196,350+(mask-1)/4*64,182,52),PartsLabel((RobotParts)mask),_factoryPart==(RobotParts)mask?Accent:Muted,.5f);
        _ui.Button(new(84,510,360,58),"製造計画を適用",Accent,.55f);
        _ui.Button(new(462,510,360,58),workshop.Paused?"製造再開":"製造停止",Muted,.55f);
        _ui.Text($"工期 {workshop.ProductionPeriod} ターン / 進捗 {workshop.ProductionAge}/{workshop.ProductionPeriod}",new(84,602),.65f,Cream);
        _ui.Button(new(84,655,180,58),"工期−",Muted,.6f);_ui.Button(new(282,655,180,58),"工期＋",Muted,.6f);
        _ui.Text($"製造中：{PartsLabel(workshop.Product)} / {(!workshop.Manufacturing?"未設定":workshop.Waiting?"満杯待ち":workshop.Paused?"停止":"進行中")}",new(84,745),.55f,Cream);
        _ui.Text(_factoryMessage,new(84,813),.43f,Cream);
        DrawNodeFactoryTile(true);
        _ui.Text("× / Esc で節点へ戻る",new(360,908),.43f,Cream);
    }
}
