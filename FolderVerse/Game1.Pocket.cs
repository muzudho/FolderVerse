namespace FolderVerse;
using System.Linq;
using Microsoft.Xna.Framework;

public partial class Game1
{
    private static readonly Rectangle PocketButton=new(54,1018,260,52);
    private long? _pocketRobot;
    private bool _pocketFromNode;
    private string _pocketMessage="";
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
                if(robot.Owner==player){_pocketRobot=robot.Id;_pocketFromNode=fromNode;_pocketMessage="";}
                else _pocketMessage="他国のロボットはポケットに入れられません";
                return;
            }
        }
        if(!_pocketRobot.HasValue)return;
        if(!new Rectangle(_pocketFromNode?1000:54,615,300,48).Contains(_pointer))return;
        var source=_pocketFromNode?deployed:pocket;var destination=_pocketFromNode?pocket:deployed;
        long id=_pocketRobot.Value;
        if(_setup.Campaign.RobotBattles.LockedNode(node)){_pocketMessage="継戦中の節点では配備を変更できません";return;}
        if(destination.Count>=12){_pocketMessage="移動先の12枠が満杯です";return;}
        if(_pocketFromNode && (_setup.Robots.Workshops[node].DisposalTarget==id || _setup.Robots.PendingEdits.Any(e=>e.Ids.Contains(id))))
        {_pocketMessage="組立・分解・廃棄を予約したロボットは移せません";return;}
        if(!source.Robots.Any(r=>r.Id==id && r.Owner==player)){_pocketRobot=null;return;}
        destination.Add(source.Remove(id));_pocketRobot=null;
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
                var card=PocketCard(i,fromNode);_ui.Box(card,Muted);if(i>=store.Count)continue;var robot=store.Robots[i];
                if(_pocketRobot==robot.Id && _pocketFromNode==fromNode)_ui.Box(new(card.X,card.Y,card.Width,4),Cream);
                DrawRobotGlyph(new(card.X+42,card.Y+12,52,70),robot.Owner,robot.Parts,robot.Role==RobotRole.Captain);
                _ui.Center(PartsLabel(robot.Parts)+$" / #{robot.Owner+1}",new(card.X,card.Y+89,card.Width,28),.4f,Cream);
                _ui.Center("ID "+robot.Id,new(card.X,card.Y+115,card.Width,24),.32f,Cream);
            }
            _ui.Button(new(x,615,300,48),fromNode?"ポケットへ入れる":"節点へ配備",Accent,.5f);
        }
        _ui.Text("自分のロボットを選んで、現在地の節点と出し入れできます",new(54,695),.5f,Cream);
        _ui.Text(_pocketMessage,new(54,936),.5f,Cream);
        _ui.Button(InactiveBack,"世界へ戻る",Accent,.5f);
    }
}
