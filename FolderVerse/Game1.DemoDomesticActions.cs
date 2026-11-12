namespace FolderVerse;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

public partial class Game1
{
    private RobotParts _demoAssemblyPart;
    private bool DemoNodePoint(out Point point)
    {
        point=Point.Zero;
        var nodes=_setup.Nodes.All.Where(n=>n.Owner==_setup.PlayerSlot && !_setup.Campaign.RobotBattles.LockedNode(n.Id))
            .OrderByDescending(n=>PlanningStock.AssemblySelection(n.Id,_setup.PlayerSlot,true)!=null).ToArray();
        if(nodes.Length==0){_demoDomesticStage=12;return false;}
        _net.ShowRoutes=true;
        foreach(var node in nodes){
            if(!_net.NodeVisible(_setup,node) || !OperationPosition(node.Id,out var at) || !MapViewBounds.Contains(at.ToPoint()))continue;
            var p=at.ToPoint();
            int hit=_statusGlobe?HitGlobeNode(p):_net.HitNode(_setup,NetPanel,p);
            if(hit==node.Id){point=p;return true;}
        }
        // Bring an off-screen country into view before aiming; never click an unrelated fixed point.
        var first=nodes[0];
        FocusDemoNode(first);
        _demoDomesticAge=0;return false;
    }
    private void FocusDemoNode(Node node)
    {
        Vector2 at;
        if(_statusGlobe){
            _world.ManualOrientation=null;_world.GlobeZoom=1;
            var position=_setup.Routes.Position(node.Cell,node.Center);FocusGlobePosition(position);
            if(!_world.ProjectVisible(position,_setup.Cells[node.Cell].Normal,_yaw,_pitch,StatusGlobeArea,out at))return;
            var delta=MapViewBounds.Center.ToVector2()-at;
            _world.GlobePan+=new Vector2(delta.X/StatusGlobeArea.Width,delta.Y/StatusGlobeArea.Height);
        }else{
            _net.FitNodesAtSource(_setup,node,NetPanel,4);
            at=_net.NodePosition(_setup,node,NetPanel);
            _net.Drag(MapViewBounds.Center.ToVector2()-at);
        }
    }
    private bool PrepareDemoAssemblyDrag()
    {
        if(_demoAssemblySelection==null || _demoEmptySlot<0)return false;
        var store=PlanningStock.Nodes[_populationNode];
        var target=store.Robots.FirstOrDefault(r=>r.Id==_hangarSlots[_demoEmptySlot]);
        if(target==null || target.Parts==RobotParts.Complete)return false;
        foreach(long id in _demoAssemblySelection){
            var robot=store.Robots.FirstOrDefault(r=>r.Id==id);
            if(robot==null || robot.Id==target.Id)continue;
            foreach(var part in new[]{RobotParts.Head,RobotParts.Body,RobotParts.Legs}){
                if((robot.Parts&part)==0 || (target.Parts&robot.ConnectedParts(part))!=0 || !CanMoveHangarPart(robot,part))continue;
                _demoSourceSlot=System.Array.IndexOf(_hangarSlots,(long?)id);
                _demoAssemblyPart=part;return _demoSourceSlot>=0;
            }
        }
        return false;
    }
    private Point DemoAssemblySourcePoint()=>_demoSourceSlot<0?Point.Zero:AssemblyPartBounds(_demoSourceSlot,_demoAssemblyPart==RobotParts.Head?0:_demoAssemblyPart==RobotParts.Body?1:2).Center;
    private void DemoAssemblyPress()
    {
        var pressed=new MouseState(0,0,0,ButtonState.Pressed,ButtonState.Released,ButtonState.Released,ButtonState.Released,ButtonState.Released);
        NodeAssemblyInput(pressed,true,false);
    }
    private bool DemoAssemblyRelease()
    {
        // Use the same press/release contract as a physical drag, without changing the real input history.
        var previous=_previousMouse;
        var store=PlanningStock.Nodes[_populationNode];
        var before=store.Robots.FirstOrDefault(r=>r.Id==_hangarSlots[_demoEmptySlot])?.Parts??0;
        _previousMouse=new MouseState(0,0,0,ButtonState.Pressed,ButtonState.Released,ButtonState.Released,ButtonState.Released,ButtonState.Released);
        try{NodeAssemblyInput(new MouseState(),false,false);}finally{_previousMouse=previous;}
        return before!=(PlanningStock.Nodes[_populationNode].Robots.FirstOrDefault(r=>r.Id==_hangarSlots[_demoEmptySlot])?.Parts??0);
    }
}
