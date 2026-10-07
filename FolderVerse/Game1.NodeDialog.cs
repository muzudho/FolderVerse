namespace FolderVerse;
using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

public partial class Game1
{
    private int _populationCell=-1;
    private int _populationNode=-1;
    private static readonly Rectangle PopulationTurnButton=new(548,1018,350,52);
    private bool NodeDialogClick(Point pointer,KeyboardState keyboard)
    {
        if(PopulationTurnButton.Contains(pointer)){_setup.Campaign.Advance(_setup,-1,0);_world.ShowSetup(_setup,true);OpenBattle();return true;}
        if(_populationCell<0)return false;
        if(NodeAssemblyTile.Contains(pointer)){if(_nodeAssemblyOpen)_nodeAssemblyOpen=false;else OpenNodeAssembly();return true;}
        if(NodeDisposalTile.Contains(pointer)){if(_nodeDisposalOpen)_nodeDisposalOpen=false;else OpenNodeDisposal();return true;}
        if(NodeTransportTile.Contains(pointer)){if(_nodeTransportOpen)_nodeTransportOpen=false;else OpenNodeTransport();return true;}
        if(NodeFactoryTile.Contains(pointer)){if(_nodeFactoryOpen)_nodeFactoryOpen=false;else OpenNodeFactory();return true;}
        if(_nodeAssemblyOpen){if(new Rectangle(803,169,58,44).Contains(pointer))_nodeAssemblyOpen=false;return new Rectangle(54,150,850,825).Contains(pointer);}
        if(_nodeDisposalOpen)return NodeDisposalClick(pointer);
        if(_nodeTransportOpen)return NodeTransportClick(pointer);
        if(_nodeFactoryOpen)return NodeFactoryClick(pointer);
        if(new Rectangle(803,169,58,44).Contains(pointer)){_populationCell=-1;return true;}
        var robots=_setup.Robots.Nodes[_populationNode].Robots;
        for(int i=0;i<robots.Count;i++)if(new Rectangle(84+i%4*196+146,282+i/4*186+151,18,18).Contains(pointer))
        {ToggleRobotDisposal(_populationNode,robots[i].Id);return true;}
        return new Rectangle(54,150,850,825).Contains(pointer);
    }
    private void DrawNodeDialog()
    {
        if(_nodeAssemblyOpen){DrawNodeAssembly();return;}
        if(_nodeDisposalOpen){DrawNodeDisposal();return;}
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
            _ui.Center("ID "+robot.Id,new(card.X,card.Y+148,90,24),.35f,Cream);
            DrawDisposalCheck(card,robot);
        }
        DrawNodeFactoryTile(false);DrawNodeTransportTile();DrawNodeDisposalTile();DrawNodeAssemblyTile();
        if(store.Count==0)_ui.Text("この節点にロボットはいません",new(84,842),.25f,Cream);
    }
}
