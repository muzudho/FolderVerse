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
        if(!_nodeAssemblyOpen && !_nodeFactoryOpen && !_nodeTransportOpen && !_nodeDisposalOpen)OpenNodeAssembly();
        if(NodeAssemblyTile.Contains(pointer)){OpenNodeAssembly();return true;}
        if(NodeDisposalTile.Contains(pointer)){if(_nodeDisposalOpen)_nodeDisposalOpen=false;else OpenNodeDisposal();return true;}
        if(NodeTransportTile.Contains(pointer)){if(_nodeTransportOpen)_nodeTransportOpen=false;else OpenNodeTransport();return true;}

        if(NodeFactoriesAllowed && _setup.Robots.Workshops[_populationNode].ProductionLines.Length>0 && NodeFactoryTile.Contains(pointer)){if(_nodeFactoryOpen)_nodeFactoryOpen=false;else OpenNodeFactory();return true;}
        if(_nodeAssemblyOpen)return new Rectangle(54,150,850,825).Contains(pointer);
        if(_nodeDisposalOpen)return NodeDisposalClick(pointer);
        if(_nodeTransportOpen)return NodeTransportClick(pointer);
        if(_nodeFactoryOpen)return NodeFactoryClick(pointer);
        return new Rectangle(54,150,850,825).Contains(pointer);
    }
    private void DrawNodeDialog()
    {
        var heading="節点インスペクター / "+_setup.Nodes.Label(_setup.Nodes.All[_populationNode]);
        _ui.Text(heading,new(54,112),Math.Min(.5f,850/_font.MeasureString(heading).X),Cream);
        if(!_nodeAssemblyOpen && !_nodeFactoryOpen && !_nodeTransportOpen && !_nodeDisposalOpen)OpenNodeAssembly();
        if(_nodeAssemblyOpen){DrawNodeAssembly();return;}
        if(_nodeDisposalOpen){DrawNodeDisposal();return;}
        if(_nodeTransportOpen){DrawNodeTransport();return;}
        if(_nodeFactoryOpen){DrawNodeFactory();return;}
    }
}
