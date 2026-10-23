namespace FolderVerse;
using System.Linq;
using Microsoft.Xna.Framework;

public partial class Game1
{
    private static readonly Rectangle PocketButton=new(354,1018,140,52);
    private long? _pocketRobot;
    private bool _pocketFromNode;
    private string _pocketMessage="";
    private static Rectangle PocketTransferButton(bool fromNode)=>new(fromNode?1000:54,615,300,48);
    private bool PocketRobotSelectable(Robot robot,bool fromNode)=>robot.Owner==_setup.PlayerSlot &&
        !_setup.Campaign.RobotBattles.LockedNode(_setup.Nodes.Current(_setup.PlayerSlot).Id) &&
        (!fromNode || !_setup.Robots.AssemblyLocked(_setup.Nodes.Current(_setup.PlayerSlot).Id,robot));
    private bool PocketTransferEnabled(bool fromNode)
    {
        if(!_pocketRobot.HasValue || _pocketFromNode!=fromNode)return false;
        int player=_setup.PlayerSlot,node=_setup.Nodes.Current(player).Id;
        var source=fromNode?_setup.Robots.Nodes[node]:_setup.Robots.Retinues[player];
        var destination=fromNode?_setup.Robots.Retinues[player]:_setup.Robots.Nodes[node];
        var robot=source.Robots.FirstOrDefault(r=>r.Id==_pocketRobot);
        return robot!=null && PocketRobotSelectable(robot,fromNode) && destination.HasSpace;
    }
    private void PocketOutline(Rectangle box,Color color,int width=3)
    {
        _ui.Box(new(box.X,box.Y,box.Width,width),color);_ui.Box(new(box.X,box.Bottom-width,box.Width,width),color);
        _ui.Box(new(box.X,box.Y,width,box.Height),color);_ui.Box(new(box.Right-width,box.Y,width,box.Height),color);
    }
    private static Rectangle PocketCard(int index,bool fromNode)
    {var card=RobotCard(index);if(fromNode)card.X+=946;return card;}
    private void OpenPocket()
    {CloseMovement();_populationCell=-1;_pocketRobot=null;_pocketMessage="";_screen=Screen.Pocket;}
    private void PocketInput(bool click,bool escape)
    {
        if(escape || click && InactiveBack.Contains(_pointer)){_screen=Screen.WorldStatus;return;}
        if(!click)return;
        int player=_setup.PlayerSlot,node=_setup.Nodes.Current(player).Id;
        var pocket=_setup.Robots.Retinues[player];var deployed=_setup.Robots.Nodes[node];
        foreach(bool fromNode in new[]{false,true})
        {
            var store=fromNode?deployed:pocket;
            for(int i=0;i<store.Count;i++)if(PocketCard(i,fromNode).Contains(_pointer))
            {
                var robot=store.Robots[i];
                if(PocketRobotSelectable(robot,fromNode)){_pocketRobot=robot.Id;_pocketFromNode=fromNode;_pocketMessage="";}
                else _pocketMessage=robot.Owner!=player?"他国のロボットはポケットに入れられません":"継戦中・作業予約中のロボットは移せません";
                return;
            }
        }
        if(!_pocketRobot.HasValue)return;
        if(!PocketTransferButton(_pocketFromNode).Contains(_pointer))return;
        var source=_pocketFromNode?deployed:pocket;var destination=_pocketFromNode?pocket:deployed;
        long id=_pocketRobot.Value;
        if(_setup.Campaign.RobotBattles.LockedNode(node)){_pocketMessage="継戦中の節点では配備を変更できません";return;}
        if(!destination.HasSpace){_pocketMessage="移動先が満杯です（出撃中の帰還予約を含む）";return;}
        if(_pocketFromNode && !PocketRobotSelectable(source.Robots.Single(r=>r.Id==id),true))
        {_pocketMessage="組立・分解・廃棄を予約したロボットは移せません";return;}
        if(!source.Robots.Any(r=>r.Id==id && r.Owner==player)){_pocketRobot=null;return;}
        destination.Add(source.Remove(id));_pocketRobot=null;
        if(_pocketFromNode && _setup.TutorialStep==2 && player==0)_setup.TutorialPocketed=true;
        _pocketMessage=_pocketFromNode?"自分のポケットに入れました":"現在地の節点へ配備しました";
    }
    private void DrawPocket()
    {
        int player=_setup.PlayerSlot;var node=_setup.Nodes.Current(player);
        _ui.Text("自分のポケット",new(54,30),1f,Cream);
        _ui.Text("ポケットのロボットは自分と一緒に移動します",new(54,112),.55f,Cream);
        _ui.Text("通常のロボット輸送計画の対象にはなりません",new(54,160),.5f,Cream);
        string label="現在地："+_setup.Nodes.Label(node);
        _ui.Text(label,new(1000,112),System.Math.Min(.5f,830/_font.MeasureString(label).X),Cream);
        foreach(bool fromNode in new[]{false,true})
        {
            var store=fromNode?_setup.Robots.Nodes[node.Id]:_setup.Robots.Retinues[player];int x=fromNode?1000:54;
            _ui.Text($"{(fromNode?"節点の配備":"自分のポケット")} {store.Count}/12",new(x,185),.5f,Cream);
            for(int i=0;i<12;i++)
            {
                var card=PocketCard(i,fromNode);_ui.Box(card,new(30,47,59));if(i>=store.Count)continue;var robot=store.Robots[i];
                bool selectable=PocketRobotSelectable(robot,fromNode),hover=selectable && card.Contains(_pointer),own=robot.Owner==player;
                _ui.Box(card,hover?new Color(70,103,118):own?Muted:new Color(37,44,53));
                var ownerColor=_setup.OwnerColor(robot.Owner);_ui.Box(new(card.X+3,card.Y+3,card.Width-6,23),ownerColor);
                _ui.Center((own?"自国":"他国")+$" #{robot.Owner+1}",new(card.X+3,card.Y+3,card.Width-6,23),.35f,UiPainter.ContrastInk(ownerColor));
                DrawRobotGlyph(new(card.X+46,card.Y+31,44,52),robot,whiteOutline:true);
                _ui.Center(PartsLabel(robot.Parts),new(card.X,card.Y+89,card.Width,28),.4f,Cream);
                _ui.Center("ID "+robot.Id,new(card.X,card.Y+115,card.Width,24),.32f,Cream);
                if(_pocketRobot==robot.Id && _pocketFromNode==fromNode)PocketOutline(card,new(255,220,114),4);
                else if(hover)PocketOutline(card,Cream);
            }
            var button=PocketTransferButton(fromNode);bool enabled=PocketTransferEnabled(fromNode),buttonHover=enabled && button.Contains(_pointer);
            string action=fromNode?"ポケットへ入れる":"節点へ配備";
            if(enabled)_ui.Button(button,action,buttonHover?new Color(81,173,172):Accent,.5f);
            else{_ui.Box(button,new(53,65,75));_ui.Center(action,button,.5f,new(163,174,180));}
            if(buttonHover)PocketOutline(button,Cream);
        }
        _ui.Text("自分のロボットを選んで、現在地の節点と出し入れできます",new(54,695),.5f,Cream);
        _ui.Text("自国・他国の帯は所属国 / ロボットの色はパーツの製造国です",new(54,741),.46f,Cream);
        foreach(bool fromNode in new[]{false,true}){
            var store=fromNode?_setup.Robots.Nodes[node.Id]:_setup.Robots.Retinues[player];
            for(int i=0;i<store.Count;i++)if(PocketCard(i,fromNode).Contains(_pointer)){
                var robot=store.Robots[i];string name=$"所属 #{robot.Owner+1}　"+_setup.ConquerorNames[robot.Owner]+(robot.Owner==player?" / 自国":" / 他国");
                _ui.Text(name,new(54,793),System.Math.Min(.55f,1770/_font.MeasureString(name).X),_setup.OwnerColor(robot.Owner));
            }
        }
        _ui.Text(_pocketMessage,new(54,936),.5f,Cream);
        _ui.Button(InactiveBack,"世界へ戻る",Accent,.5f);
        if(InactiveBack.Contains(_pointer))PocketOutline(InactiveBack,Cream);
    }
}
