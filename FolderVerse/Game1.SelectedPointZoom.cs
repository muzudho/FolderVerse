namespace FolderVerse;
using System;
using Microsoft.Xna.Framework;

public partial class Game1
{
    private void ZoomSelectedPoint(int wheel)
    {
        if(wheel==0)return;
        // A wheel gesture takes priority over the inspector's pending automatic slide.
        _inspectorCenterPending=false;_inspectorCenterAge=-1;
        int id=_movementOpen && _selectedMoveNode>=0?_selectedMoveNode:
            _populationNode>=0?_populationNode:_inspectorCenterNode>=0?_inspectorCenterNode:_globeSelectedNode;
        bool selected=id>=0 && id<_setup.Nodes.All.Length;
        Vector2 before=default;
        if(selected){
            var node=_setup.Nodes.All[id];
            if(_statusGlobe)selected=_world.ProjectVisible(_setup.Routes.Position(node.Cell,node.Center),_setup.Cells[node.Cell].Normal,_yaw,_pitch,StatusGlobeArea,out before);
            else before=_net.NodePosition(_setup,node,NetPanel);
        }
        if(_statusGlobe){
            _world.GlobeZoom=MathHelper.Clamp(_world.GlobeZoom*MathF.Pow(1.2f,wheel/120f),.5f,16);
            if(selected){
                var node=_setup.Nodes.All[id];
                _world.ProjectVisible(_setup.Routes.Position(node.Cell,node.Center),_setup.Cells[node.Cell].Normal,_yaw,_pitch,StatusGlobeArea,out var after);
                _world.GlobePan+=new Vector2((before.X-after.X)/StatusGlobeArea.Width,(before.Y-after.Y)/StatusGlobeArea.Height);
            }
        }else{
            _net.ZoomAt(NetPanel,_pointer,wheel);
            if(selected)_net.Drag(before-_net.NodePosition(_setup,_setup.Nodes.All[id],NetPanel));
        }
    }
}
