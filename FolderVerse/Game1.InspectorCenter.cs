namespace FolderVerse;
using Microsoft.Xna.Framework;

public partial class Game1
{
    private int _inspectorCenterNode=-1;
    private bool _inspectorCenterPending,_inspectorCenterGlobe;
    private float _inspectorCenterAge=-1,_inspectorCenterDistance,_inspectorCenterApplied;
    private int InspectorLocationNode=>_nodeInspectorClosing?-1:_salvagePlan?(_setup.Salvage.Items.Find(i=>i.Id==_salvageSelected)?.Node??CurrentSalvageAgent?.Node??-1):_regionCell>=0?_inspectedNode:_populationNode;
    private void FocusInspectorLocation()
    {
        int id=InspectorLocationNode;
        if(id<0 || id>=_setup.Nodes.All.Length || MapTransitionActive || _net.IsAnimating)return;
        _inspectorCenterPending=false;_inspectorCenterAge=-1;
        var node=_setup.Nodes.All[id];
        var position=_setup.Routes.Position(node.Cell,node.Center);
        _domesticMapLeft=55+480*_domesticSlide;
        Vector2 at;
        if(_statusGlobe){
            FocusGlobePosition(position);
            if(!_world.ProjectVisible(position,_setup.Cells[node.Cell].Normal,_yaw,_pitch,StatusGlobeArea,out at))return;
            var delta=MapViewBounds.Center.ToVector2()-at;
            _world.GlobePan+=new Vector2(delta.X/StatusGlobeArea.Width,delta.Y/StatusGlobeArea.Height);
        }else{
            _net.SetCenter(_setup,_setup.Cells[node.Cell].Face);
            at=_net.NodePosition(_setup,node,NetPanel);
            _net.Drag(MapViewBounds.Center.ToVector2()-at);
        }
        _dragging=_mapDragging=false;
    }
    private void QueueInspectorCenter(int node)
    {
        _inspectorCenterNode=node;_inspectorCenterPending=node>=0;_inspectorCenterAge=-1;
    }
    private void UpdateInspectorCenter(float elapsed)
    {
        if(_inspectorCenterPending){
            _inspectorCenterPending=false;
            if(_inspectorCenterNode<0 || _inspectorCenterNode>=_setup.Nodes.All.Length)return;
            var node=_setup.Nodes.All[_inspectorCenterNode];Vector2 at;
            if(_statusGlobe){
                if(!_world.ProjectVisible(_setup.Routes.Position(node.Cell,node.Center),_setup.Cells[node.Cell].Normal,_yaw,_pitch,StatusGlobeArea,out at))return;
            }else at=_net.NodePosition(_setup,node,NetPanel);
            _inspectorCenterDistance=MapViewBounds.Center.X-at.X;
            _inspectorCenterApplied=0;_inspectorCenterAge=0;_inspectorCenterGlobe=_statusGlobe;
        }
        if(_inspectorCenterAge<0)return;
        if(_statusGlobe!=_inspectorCenterGlobe || MapTransitionActive){_inspectorCenterAge=-1;return;}
        _inspectorCenterAge+=elapsed;
        float t=MathHelper.Clamp(_inspectorCenterAge/.45f,0,1);
        float shift=_inspectorCenterDistance*t*t*(3-2*t);
        float delta=shift-_inspectorCenterApplied;_inspectorCenterApplied=shift;
        if(_statusGlobe)_world.GlobePan+=new Vector2(delta/StatusGlobeArea.Width,0);
        else _net.Drag(new Vector2(delta,0));
        if(t>=1)_inspectorCenterAge=-1;
    }
}
