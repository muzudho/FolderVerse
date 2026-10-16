namespace FolderVerse;
using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

public partial class Game1
{
    private int _populationCell=-1;
    private int _populationNode=-1;
    private static readonly Rectangle PopulationTurnButton=new(310,1006,242,64);
    private bool NodeDialogClick(Point pointer,KeyboardState keyboard)
    {
        if(!_reignView && PopulationTurnButton.Contains(pointer)){EndPlannedTurn();return true;}
        if(SupervisorNodePanel){pointer.X-=920;pointer.Y-=40;}
        if(NodeAutomationClick(pointer))return true;
        if(_populationCell<0)return false;
        if(!_nodeAssemblyOpen && !_nodeFactoryOpen && !_nodeTransportOpen && !_nodeDisposalOpen)OpenNodeAssembly();
        if(NodeAssemblyTile.Contains(pointer)){OpenNodeAssembly();return true;}
        if(NodeDisposalTile.Contains(pointer)){if(_nodeDisposalOpen)_nodeDisposalOpen=false;else OpenNodeDisposal();return true;}
        if(NodeTransportTile.Contains(pointer)){if(_nodeTransportOpen)_nodeTransportOpen=false;else OpenNodeTransport();return true;}

        if(_setup.Robots.Workshops[_populationNode].ProductionLines.Length>0 && NodeFactoryTile.Contains(pointer)){if(_nodeFactoryOpen)_nodeFactoryOpen=false;else OpenNodeFactory();return true;}
        if(_nodeAssemblyOpen){if(new Rectangle(803,169,58,44).Contains(pointer)){_nodeAssemblyOpen=false;_populationCell=-1;}return new Rectangle(54,150,850,825).Contains(pointer);}
        if(_nodeDisposalOpen)return NodeDisposalClick(pointer);
        if(_nodeTransportOpen)return NodeTransportClick(pointer);
        if(_nodeFactoryOpen)return NodeFactoryClick(pointer);
        if(new Rectangle(803,169,58,44).Contains(pointer)){_populationCell=-1;return true;}
        return new Rectangle(54,150,850,825).Contains(pointer);
    }
    private void DrawNodeDialog()
    {
        if(!_nodeAssemblyOpen && !_nodeFactoryOpen && !_nodeTransportOpen && !_nodeDisposalOpen)OpenNodeAssembly();
        if(_nodeAssemblyOpen){DrawNodeAssembly();return;}
        if(_nodeDisposalOpen){DrawNodeDisposal();return;}
        if(_nodeTransportOpen){DrawNodeTransport();return;}
        if(_nodeFactoryOpen){DrawNodeFactory();return;}
    }
}
