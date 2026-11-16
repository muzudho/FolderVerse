namespace FolderVerse;
using System;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

public partial class Game1
{
    private Texture2D _robotDisposalIcon;
    private bool _nodeDisposalOpen;
    private string _nodeDisposalMessage="";
    private static readonly Rectangle NodeDisposalTile=new(480,210,190,40);
    private static readonly Rectangle NodeSalvageButton=new(790,252,HangarActionSize,HangarActionSize);
    private static readonly Rectangle NodeRecoveryPlanButton=new(586,300,184,34);
    private const string NodeDisposalName="ロボット回収・廃棄計画";
    private Robot[] DisposalCandidates()
    {
        var workshop=_setup.Robots.Workshops[_populationNode];
        int Rank(long id){int index=workshop.DisposalOrder.ToList().IndexOf(id);return index<0?int.MaxValue:index;}
        return _setup.Robots.Nodes[_populationNode].Robots.Where(r=>r.Disposal && !workshop.IsDisposing(r.Id)).OrderBy(r=>Rank(r.Id)).ThenBy(r=>r.Id).ToArray();
    }
    private void OpenNodeDisposal()
    {
        if(!InspectableNode(_populationNode))return;
        _nodeOfficeOpen=false;_lastNodeTab=2;
        _nodeAssemblyOpen=false;_nodeDisposalOpen=true;_nodeFactoryOpen=_nodeTransportOpen=false;_nodeDisposalMessage="";
    }
    private bool NodeDisposalClick(Point pointer)
    {
        if(NodeDisposalTile.Contains(pointer)){_nodeDisposalOpen=false;return true;}
        if(NodeSalvageButton.Contains(pointer)){if(DomesticOperationsAllowed)OpenSalvageCatalog(_populationNode);return true;}
        if(NodeRecoveryPlanButton.Contains(pointer) && DomesticOperationsAllowed){OpenSalvagePlan(_populationNode);return true;}
        if(!DomesticOperationsAllowed || !CanManageNodeFactory(_setup.Nodes.All[_populationNode].Owner))return true;
        var robots=DisposalCandidates();
        for(int i=0;i<robots.Length;i++)
        {
            int x=84+i/6*388,y=340+i%6*78;
            bool up=new Rectangle(x+274,y+8,42,42).Contains(pointer),down=new Rectangle(x+324,y+8,42,42).Contains(pointer);
            if(!up && !down)continue;int other=i+(up?-1:1);
            if(other<0 || other>=robots.Length)return true;
            (robots[i],robots[other])=(robots[other],robots[i]);
            _setup.Robots.Workshops[_populationNode].SetDisposalOrder(robots.Select(r=>r.Id));_nodeDisposalMessage="廃棄の順番を更新しました";return true;
        }
        return new Rectangle(54,150,850,825).Contains(pointer);
    }
    private void DrawNodeDisposalTile()
    {
        bool enabled=InspectableNode(_populationNode);
        _ui.Box(NodeDisposalTile,_nodeDisposalOpen || enabled && NodeDisposalTile.Contains(_pointer)?Accent:Muted);
        _ui.Center(NodeDisposalName,NodeDisposalTile,.32f,enabled?(_nodeDisposalOpen || NodeDisposalTile.Contains(_pointer)?SelectionInk:Cream):Cream*.4f);
        if(_nodeDisposalOpen)_ui.Box(new(NodeDisposalTile.X,NodeDisposalTile.Y,NodeDisposalTile.Width,4),Cream);
    }
    private void DrawNodeDisposal()
    {
        var node=_setup.Nodes.All[_populationNode];var workshop=_setup.Robots.Workshops[node.Id];
        bool manage=CanManageNodeFactory(node.Owner);
        var robots=manage?DisposalCandidates():Array.Empty<Robot>();
        _ui.Box(new(54,150,850,825),new(27,49,62));_ui.Box(new(54,150,850,6),_setup.OwnerColor(node.Owner));
        DrawHangarAction(NodeSalvageButton,10,DomesticOperationsAllowed);
        _ui.Button(NodeRecoveryPlanButton,"回収隊の計画",DomesticOperationsAllowed?Accent:Muted,.34f);
        int abandoned=_setup.Salvage.Items.Count(item=>item.Node==node.Id);
        _ui.Text($"放棄品 {abandoned} 件",new(84,318),.34f,Cream);

        string status=!manage?"この地点の廃棄順は管理者が変更できます":!workshop.HasDisposalFactory?"この地点に廃棄炉はありません":workshop.DisposalTarget is long id?$"レーン使用中 ID {id} / {IndustryTime.Format(workshop.DisposalAge)}/{IndustryTime.Format(workshop.DisposalPeriod)} 産業ターン":"レーンは空いています / 廃棄フラグ付きの個体が自動で入ります";
        DrawDisposalInfo(status,260,.43f);
        if(manage)DrawDisposalInfo("頭１・胴２・脚２産業ターン / 組み合わせは合計 / ↑↓で順番を変更",288,.42f);
        for(int i=0;i<robots.Length;i++)
        {
            int x=84+i/6*388,y=340+i%6*78;var robot=robots[i];_ui.Box(new(x,y,374,68),Muted);
            DrawRobotGlyph(new(x+12,y+7,32,48),robot);
            _ui.Text($"{i+1}. ID {robot.Id} / {PartsLabel(robot.Parts)}",new(x+58,y+9),.38f,Cream);
            _ui.Text($"{IndustryTime.Format(RobotWorkshop.DisposalTurns(robot.Parts))} 産業ターン",new(x+58,y+36),.34f,Cream);
            _ui.Button(new(x+274,y+8,42,42),"↑",Muted,.45f);_ui.Button(new(x+324,y+8,42,42),"↓",Muted,.45f);
        }
        if(manage && robots.Length==0)_ui.Text("廃棄待ちのロボットはいません",new(84,355),.5f,Cream);
        _ui.Text(_nodeDisposalMessage,new(84,819),.4f,Cream);
        DrawNodeTabs();

        void DrawDisposalInfo(string text,int y,float scale)=>
            _ui.Text(text,new(84,y),Math.Min(scale,(NodeRecoveryPlanButton.Left-96)/Math.Max(1,_font.MeasureString(text).X)),Cream);
    }
    private void ToggleRobotDisposal(int node,long id)
    {
        var store=_setup.Robots.Nodes[node];var robot=store.Robots.Single(r=>r.Id==id);
        if(!CanManageNodeFactory(robot.Owner) || _setup.Campaign.RobotBattles.LockedNode(node))return;
        _setup.Robots.ToggleReservation(node,id,HangarReservation.Disposal);
    }
    private void DrawDisposalCheck(Rectangle card,Robot robot)
    {
        var box=new Rectangle(card.Right-36,card.Bottom-25,18,18);
        _ui.Box(box,robot.Disposal?Accent:new Color(25,40,51));
        if(robot.Disposal)_ui.Center("✓",box,.35f,SelectionInk);
        _ui.Text("廃棄",new(card.Right-84,card.Bottom-25),.3f,Cream);
    }
}
