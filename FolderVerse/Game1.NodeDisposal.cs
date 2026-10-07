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
    private static readonly Rectangle NodeDisposalTile=new(476,875,182,84);
    private Robot[] DisposalCandidates()
    {
        var workshop=_setup.Robots.Workshops[_populationNode];
        int Rank(long id){int index=workshop.DisposalOrder.ToList().IndexOf(id);return index<0?int.MaxValue:index;}
        return _setup.Robots.Nodes[_populationNode].Robots.Where(r=>r.Disposal && r.Id!=workshop.DisposalTarget).OrderBy(r=>Rank(r.Id)).ThenBy(r=>r.Id).ToArray();
    }
    private void OpenNodeDisposal()
    {
        if(!CanManageNodeFactory(_setup.Nodes.All[_populationNode].Owner))return;
        _nodeAssemblyOpen=false;_nodeDisposalOpen=true;_nodeFactoryOpen=_nodeTransportOpen=false;_nodeDisposalMessage="";
    }
    private bool NodeDisposalClick(Point pointer)
    {
        if(new Rectangle(803,169,58,44).Contains(pointer) || NodeDisposalTile.Contains(pointer)){_nodeDisposalOpen=false;return true;}
        if(!CanManageNodeFactory(_setup.Nodes.All[_populationNode].Owner))return true;
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
        bool enabled=CanManageNodeFactory(_setup.Nodes.All[_populationNode].Owner);
        _ui.Box(NodeDisposalTile,_nodeDisposalOpen || enabled && NodeDisposalTile.Contains(_pointer)?Accent:Muted);
        _spriteBatch.Draw(_robotDisposalIcon,new Rectangle(486,891,52,52),enabled?Color.White:Color.White*.35f);
        _ui.Center("ロボット廃棄計画",new(540,885,112,60),.24f,enabled?Cream:Cream*.4f);
        if(_nodeDisposalOpen)_ui.Box(new(NodeDisposalTile.X,NodeDisposalTile.Y,NodeDisposalTile.Width,4),Cream);
    }
    private void DrawNodeDisposal()
    {
        var node=_setup.Nodes.All[_populationNode];var workshop=_setup.Robots.Workshops[node.Id];var robots=DisposalCandidates();
        _ui.Box(new(54,150,850,825),new(27,49,62));_ui.Box(new(54,150,850,6),_setup.OwnerColor(node.Owner));
        _ui.Text("ロボット廃棄計画",new(84,178),.8f,Cream);_ui.Button(new(803,169,58,44),"×",Muted,.7f);
        string status=!workshop.HasDisposalFactory?"この節点に廃棄炉はありません":workshop.DisposalTarget is long id?$"レーン使用中 ID {id} / {workshop.DisposalAge}/{workshop.DisposalPeriod} 全体ターン":"レーンは空いています / 廃棄フラグ付きの個体が自動で入ります";
        _ui.Text(status,new(84,244),.43f,Cream);
        _ui.Text("頭１・胴２・脚２ターン / 組み合わせは合計 / ↑↓で順番を変更",new(84,288),.42f,Cream);
        for(int i=0;i<robots.Length;i++)
        {
            int x=84+i/6*388,y=340+i%6*78;var robot=robots[i];_ui.Box(new(x,y,374,68),Muted);
            DrawRobotGlyph(new(x+12,y+7,32,48),robot.Owner,robot.Parts);
            _ui.Text($"{i+1}. ID {robot.Id} / {PartsLabel(robot.Parts)}",new(x+58,y+9),.38f,Cream);
            _ui.Text($"{RobotWorkshop.DisposalTurns(robot.Parts)} 全体ターン",new(x+58,y+36),.34f,Cream);
            _ui.Button(new(x+274,y+8,42,42),"↑",Muted,.45f);_ui.Button(new(x+324,y+8,42,42),"↓",Muted,.45f);
        }
        if(robots.Length==0)_ui.Text("廃棄待ちのロボットはいません",new(84,355),.5f,Cream);
        _ui.Text(_nodeDisposalMessage,new(84,819),.4f,Cream);
        DrawNodeFactoryTile(false);DrawNodeTransportTile();DrawNodeDisposalTile();DrawNodeAssemblyTile();
    }
    private void ToggleRobotDisposal(int node,long id)
    {
        var store=_setup.Robots.Nodes[node];var robot=store.Robots.Single(r=>r.Id==id);
        if(robot.Owner!=_setup.PlayerSlot || _setup.Campaign.RobotBattles.LockedNode(node) || _setup.Robots.Workshops[node].DisposalTarget==id || _setup.Robots.PendingEdits.Any(e=>e.Ids.Contains(id)))return;
        store.Replace(store.Robots.Select(r=>r.Id==id?r with {Disposal=!r.Disposal}:r).ToArray());
    }
    private void DrawDisposalCheck(Rectangle card,Robot robot)
    {
        var box=new Rectangle(card.Right-36,card.Bottom-25,18,18);
        _ui.Box(box,robot.Disposal?Accent:new Color(25,40,51));
        if(robot.Disposal)_ui.Center("✓",box,.35f,Cream);
        _ui.Text("廃棄",new(card.Right-84,card.Bottom-25),.3f,Cream);
    }
}
