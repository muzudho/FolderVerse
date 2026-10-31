namespace FolderVerse;
using System.Linq;
using Microsoft.Xna.Framework;

public partial class Game1
{
    private int _hangarTrainingCountry=-1;
    private long _hangarTrainingRobot;
    private Rectangle HangarTrainingNoticeBounds=>new(990,450,870,240);
    private bool HangarTrainingNoticeVisible{
        get{
            if(!_nodeAssemblyOpen || _nodeInspectorClosing || _populationNode<0 || _populationNode>=_setup.Robots.Nodes.Length || _hangarTrainingCountry!=_setup.PlayerSlot || _hangarReservation!=HangarReservation.Defense)return false;
            var robot=_setup.Robots.Nodes[_populationNode].Robots.FirstOrDefault(r=>r.Id==_hangarTrainingRobot);
            return HangarReservationAccessible(robot) && _setup.Robots.ReservationBlockedByTraining(_populationNode,robot,HangarReservation.Defense,_hangarTrainingCountry);
        }
    }
    private bool HangarTrainingNoticeClick(Point point)
    {
        if(!HangarTrainingNoticeVisible)return false;
        bool consume=HangarTrainingNoticeBounds.Contains(point);_hangarTrainingCountry=-1;return consume;
    }
    private void DrawHangarTrainingNotice()
    {
        if(!HangarTrainingNoticeVisible)return;
        int country=_hangarTrainingCountry;var color=_setup.OwnerColor(country);
        var panel=HangarTrainingNoticeBounds;_ui.Box(panel,new Color(14,29,40));
        var portrait=new Rectangle(panel.X+14,panel.Y+35,160,160);
        _ui.Box(new(portrait.X-3,portrait.Y-3,166,166),color);
        _portraitRenderer.Draw(_spriteBatch,_setup.Looks[country],portrait,faceOnly:true);
        var bubble=new Rectangle(panel.X+205,panel.Y+10,650,174);
        _ui.Tile(new(bubble.X,bubble.Y+88),new(30,30),MathHelper.PiOver4,Cream);_ui.Box(bubble,Cream);
        _ui.Text("調練による制限",new(bubble.X+22,bubble.Y+14),.40f,Muted);
        _ui.Text("私の国では 3/3 が自国のパーツで",new(bubble.X+22,bubble.Y+57),.50f,Muted);
        _ui.Text("できているロボットしか戦闘に出しません",new(bubble.X+22,bubble.Y+105),.50f,Muted);
        _ui.Text("損傷したパーツは自国パーツに数えません",new(bubble.X+22,bubble.Y+148),.30f,Muted);
        _ui.Text("征服者　"+_setup.ConquerorFullNames[country],new(panel.X+14,panel.Y+204),.42f,color);
        _ui.Text("クリックで閉じる",new(panel.Right-205,panel.Y+207),.30f,Cream);
    }
}
