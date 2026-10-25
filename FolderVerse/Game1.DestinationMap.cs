namespace FolderVerse;
using System;
using System.Linq;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
public partial class Game1
{
    private int _rateTarget=-1;
    private bool _rateDragging;
    private void UpdateRateDrag(MouseState mouse,bool active,bool click)
    {
        if(!active || !(DestinationOperation || _transportOverview) || _rateTarget<0 || mouse.LeftButton!=ButtonState.Pressed){_rateDragging=false;return;}
        if(click && Vector2.Distance(new Vector2(_pointer.X,_pointer.Y),RatePieCenter)<=110)_rateDragging=true;
        if(_rateDragging)SetDestinationRate(_rateTarget,PointerDegrees(_pointer));
    }
    private bool DestinationOperation=>_movementOpen || (_nodeTransportOpen && _populationCell>=0);
    private int OperationSource=>_movementOpen?(_setup.Nodes.Current(_setup.PlayerSlot)?.Id??-1):_populationNode;
    private int[] DestinationNodes()=>_movementOpen?_net.MovementTargets.ToArray():_setup.Routes.Neighbors(_setup.Nodes.All[_populationNode]).Select(r=>_setup.Nodes.At(r.Target,r.Entry).Id).Distinct().ToArray();
    private void FitDestinationView()
    {
        if(!DestinationOperation || OperationSource<0)return;
        var ids=VisibleDestinationNodes().ToArray();
        var source=_setup.Nodes.All[OperationSource];
        var anchor=_setup.Routes.Position(source.Cell,source.Center);
        _dragging=_mapDragging=false;
        if(_statusGlobe)
        {
            _world.GlobePan=Vector2.Zero;
            _pitch=MathF.Atan2(anchor.Y,anchor.Z);
            if(_pitch>MathHelper.PiOver2)_pitch-=MathHelper.Pi;
            if(_pitch< -MathHelper.PiOver2)_pitch+=MathHelper.Pi;
            var turned=Vector3.Transform(anchor,Matrix.CreateRotationX(_pitch));
            _yaw=-MathF.Atan2(turned.X,turned.Z);
            _world.GlobeAnchor=anchor;
            _world.GlobeZoom=1;
            bool Fits(float zoom)
            {
                _world.GlobeZoom=zoom;
                var area=StatusGlobeArea;area.Inflate(-48,-60);
                return ids.All(id=>OperationPosition(id,out var at) && area.Contains(at.ToPoint()));
            }
            // Fit visible destinations when possible; peaceful movement keeps the chosen globe view.
            if(ids.All(id=>OperationPosition(id,out _)))
            {
                float low=.05f,high=16;
                for(int i=0;i<24;i++){float mid=(low+high)/2;if(Fits(mid))low=mid;else high=mid;}
                _world.GlobeZoom=low;return;
            }
            if(_movementOpen && !_warPlanning)return;
            _statusGlobe=false;
        }
        _net.SetCenter(_setup,_setup.Cells[source.Cell].Face);
        _net.ResetView();
        var origin=_net.NodePosition(_setup,source,NetPanel);
        float x=1,y=1;
        foreach(int id in ids){var delta=_net.NodePosition(_setup,_setup.Nodes.All[id],NetPanel)-origin;x=Math.Max(x,Math.Abs(delta.X));y=Math.Max(y,Math.Abs(delta.Y));}
        float zoom=Math.Min((NetPanel.Width/2f-48)/x,(NetPanel.Height/2f-60)/y);
        _net.FitNodesAtSource(_setup,source,NetPanel,Math.Min(zoom,64));
    }
    private void PrepareDestinationMap()
    {
        _net.VisibleOperationNodes=!_transportOverview && DestinationOperation?VisibleDestinationNodes():null;
        _net.VisibleOperationBatteries=_movementOpen && _warPlanning?Enumerable.Range(0,_setup.ActiveCount).Where(CanPlanRuler).Where(r=>_net.MovementTargets.Any(t=>(_selectedMoveNode<0 || t==_selectedMoveNode) && _setup.Campaign.RobotBattles.CanMarch(_setup,r,t))).ToHashSet():null;
    }
    private HashSet<int> VisibleDestinationNodes()
    {
        if(_movementOpen && _warPlanning)
        {
            var choices=_selectedMoveNode>=0?new[]{_selectedMoveNode}:DestinationNodes();
            var participants=new HashSet<int>(choices);
            foreach(var n in _setup.Nodes.All.Where(n=>CanPlanRuler(n.Owner)))
                if(choices.Any(t=>_setup.Campaign.RobotBattles.CanAttackFrom(_setup,n.Owner,n.Id,t)))participants.Add(n.Id);
            return participants;
        }
        var targets=new HashSet<int>(DestinationNodes());
        var visible=new HashSet<int>(targets);
        if(OperationSource>=0)visible.Add(OperationSource);
        return visible;
    }
    private bool OperationPosition(int id,out Vector2 at)
    {
        var n=_setup.Nodes.All[id];
        if(!_statusGlobe){at=_net.NodePosition(_setup,n,NetPanel);return MapViewBounds.Contains(at.ToPoint());}
        return _world.ProjectVisible(_setup.Routes.Position(n.Cell,n.Center),_setup.Cells[n.Cell].Normal,_yaw,_pitch,StatusGlobeArea,out at);
    }
    private Rectangle DestinationBadge(int id)
    {
        if(!_net.NodeVisible(_setup,_setup.Nodes.All[id]) || !OperationPosition(id,out var at))return Rectangle.Empty;
        return new((int)at.X-32,(int)at.Y-42,64,34);
    }
    private int Rate(int id)=>TransportRules.FirstOrDefault(r=>r.Number==id)?.Degrees??0;
    private int RateMaximum(int id)=>360;
    private void SetDestinationRate(int id,int value)
    {
        if(CanEditTransport && TransportRules.Any(r=>r.Number==id))_setup.Robots.Transport.SetInstructionRate(_populationNode,TransportOwner,id,value);
    }
    private bool DestinationMapClick(Point pointer)
    {
        if(_rateTarget>=0)
        {
            if(RateDialogArea.Contains(pointer)){RatePieClick(pointer);return true;}
            _rateTarget=-1;return true;
        }
        return false;
    }
    private void DrawTransportDestinationChoice()
    {
        if(!_transportDestinationPicking)return;
        _ui.Text("隣接する地点を地図で選択 / Escで取消",new(991,900),.45f,Cream);
        foreach(int id in TransportAdjacent())if(_net.NodeVisible(_setup,_setup.Nodes.All[id]) && OperationPosition(id,out var at) && MapViewBounds.Contains(at.ToPoint())){
            var r=new Rectangle((int)at.X-23,(int)at.Y-23,46,46);var c=new Color(255,239,130);
            _ui.Box(new(r.X,r.Y,r.Width,2),c);_ui.Box(new(r.X,r.Bottom-2,r.Width,2),c);_ui.Box(new(r.X,r.Y,2,r.Height),c);_ui.Box(new(r.Right-2,r.Y,2,r.Height),c);
        }
    }
    private void DrawDestinationMap()
    {
        if(_transportOverview || _nodeTransportOpen){DrawTransportOverviewMap();DrawTransportDestinationChoice();if(_nodeTransportOpen && _rateTarget>=0)DrawRatePieDialog();return;}
        _rateTarget=-1;
    }
}
