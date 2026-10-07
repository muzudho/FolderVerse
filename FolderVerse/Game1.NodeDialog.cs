namespace FolderVerse;
using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

public partial class Game1
{
    private int _populationCell=-1;
    private int _populationNode=-1;
    private static readonly Rectangle PopulationTurnButton=new(690,1018,208,52);
    private bool NodeDialogClick(Point pointer,KeyboardState keyboard)
    {
        if(PopulationTurnButton.Contains(pointer)){_setup.Campaign.Advance(_setup,-1,0);_world.ShowSetup(_setup,true);OpenBattle();return true;}
        if(_populationCell<0)return false;
        if(_nodeTransportOpen)return NodeTransportClick(pointer);
        if(NodeTransportTile.Contains(pointer)){OpenNodeTransport();return true;}
        if(_nodeFactoryOpen)return NodeFactoryClick(pointer);
        if(NodeFactoryTile.Contains(pointer)){OpenNodeFactory();return true;}
        if(new Rectangle(803,169,58,44).Contains(pointer)){_populationCell=-1;return true;}
        return new Rectangle(54,150,850,825).Contains(pointer);
    }
    private void DrawNodeDialog()
    {
        if(_nodeTransportOpen){DrawNodeTransport();return;}
        if(_nodeFactoryOpen){DrawNodeFactory();return;}
        var post=_setup.Nodes.All[_populationNode];var store=_setup.Robots.Nodes[post.Id];
        _ui.Box(new(64,162,850,825),new Color(3,13,20)*.8f);
        _ui.Box(new(54,150,850,825),new(27,49,62));
        _ui.Box(new(54,150,850,6),_setup.OwnerColor(post.Owner));
        string city=_setup.Nodes.Label(post)+" / "+_setup.Nodes.Pattern(post);
        _ui.Text(city,new(84,178),Math.Min(.7f,690/_font.MeasureString(city).X),Cream);
        _ui.Button(new(803,169,58,44),"×",Muted,.7f);
        _ui.Text($"節点のロボット {store.Count}/12",new(84,235),.65f,Cream);
        for(int slot=0;slot<12;slot++)
        {
            var card=new Rectangle(84+slot%4*196,282+slot/4*186,182,176);
            _ui.Box(card,Muted);if(slot>=store.Count)continue;var robot=store.Robots[slot];
            DrawRobotGlyph(new(card.X+57,card.Y+12,68,90),robot.Owner,robot.Parts,robot.Role==RobotRole.Captain);
            _ui.Center(PartsLabel(robot.Parts)+$" / #{robot.Owner+1}",new(card.X,card.Y+112,card.Width,30),.48f,Cream);
            _ui.Center("ID "+robot.Id,new(card.X,card.Y+148,card.Width,24),.35f,Cream);
        }
        DrawNodeFactoryTile(false);DrawNodeTransportTile();
        if(store.Count==0)_ui.Text("この節点にロボットはいません",new(592,908),.30f,Cream);
    }
}
