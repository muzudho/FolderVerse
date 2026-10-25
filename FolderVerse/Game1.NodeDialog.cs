namespace FolderVerse;
using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

public partial class Game1
{
    private int _populationCell=-1;
    private int _populationNode=-1;
    private Rectangle PopulationTurnButton=>new(310,974,242,96);
    private bool NodeDialogClick(Point pointer,KeyboardState keyboard)
    {
        if(!InspectorVisible && PopulationTurnButton.Contains(pointer)){EndPlannedTurn();return true;}
        if(_populationCell<0 || _populationNode<0 || _populationNode>=_setup.Nodes.All.Length || _populationNode>=_setup.Robots.Workshops.Length)return false;
        if(SupervisorNodePanel){pointer.X-=920;pointer.Y-=40;}
        pointer.X-=NodeInspectorOffset;
        if(_populationCell>=0 && !NodeInspectorBounds.Contains(new Point(pointer.X+NodeInspectorOffset,pointer.Y)))return false;
        if(_nodeFactoryOpen && (_factoryDropDown>=0 || _factoryPlanLine>=0))return NodeFactoryClick(pointer);
        if(NodeAutomationClick(pointer))return true;
        if(_populationCell<0)return false;
        if(NodeOfficeTile.Contains(pointer)){OpenNodeOffice();return true;}
        if(NodeAssemblyTile.Contains(pointer)){OpenNodeAssembly();return true;}
        if(NodeDisposalTile.Contains(pointer)){OpenNodeDisposal();return true;}
        if(NodeTransportAllowed && NodeTransportTile.Contains(pointer)){if(_nodeTransportOpen)_nodeTransportOpen=false;else OpenNodeTransport();return true;}

        if(NodeFactoryTile.Contains(pointer)){OpenNodeFactory();return true;}
        if(_nodeOfficeOpen)return true;
        if(_nodeAssemblyOpen)return new Rectangle(54,150,850,825).Contains(pointer);
        if(_nodeDisposalOpen)return NodeDisposalClick(pointer);
        if(_nodeTransportOpen)return NodeTransportClick(pointer);
        if(_nodeFactoryOpen)return NodeFactoryClick(pointer);
        return new Rectangle(54,150,850,825).Contains(pointer);
    }
    private void DrawNodeDialog()
    {
        if(!_nodeOfficeOpen && !_nodeAssemblyOpen && !_nodeFactoryOpen && !_nodeTransportOpen && !_nodeDisposalOpen)RestoreNodeTab();
        if(_nodeOfficeOpen){DrawNodeOffice();return;}
        if(_nodeAssemblyOpen){DrawNodeAssembly();return;}
        if(_nodeDisposalOpen){DrawNodeDisposal();return;}
        if(_nodeTransportOpen){DrawNodeTransport();return;}
        if(_nodeFactoryOpen){DrawNodeFactory();return;}
    }
}
