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
            // A globe cannot show nodes on its hidden surface: use the net in that case.
            if(ids.All(id=>OperationPosition(id,out _)))
            {
                float low=.05f,high=16;
                for(int i=0;i<24;i++){float mid=(low+high)/2;if(Fits(mid))low=mid;else high=mid;}
                _world.GlobeZoom=low;return;
            }
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
    }
    private HashSet<int> VisibleDestinationNodes()
    {
        var targets=new HashSet<int>(DestinationNodes());
        var visible=new HashSet<int>(targets);
        if(OperationSource>=0)visible.Add(OperationSource);
        if(_movementOpen && _warPlanning)
            foreach(var node in _setup.Nodes.All.Where(n=>n.Owner>=0 && _setup.Relations.Controls(n.Owner)))
                if(_setup.Routes.Neighbors(node).Any(route=>targets.Contains(_setup.Nodes.At(route.Target,route.Entry).Id)))
                    visible.Add(node.Id);
        return visible;
    }
    private bool OperationPosition(int id,out Vector2 at)
    {
        var n=_setup.Nodes.All[id];
        if(!_statusGlobe){at=_net.NodePosition(_setup,n,NetPanel);return NetPanel.Contains(at.ToPoint());}
        return _world.ProjectVisible(_setup.Routes.Position(n.Cell,n.Center),_setup.Cells[n.Cell].Normal,_yaw,_pitch,StatusGlobeArea,out at);
    }
    private Rectangle DestinationBadge(int id)
    {
        if(!OperationPosition(id,out var at))return Rectangle.Empty;
        return new((int)at.X-32,(int)at.Y-42,64,34);
    }
    private int Rate(int id)=>_setup.Robots.Transport.Plan(_populationNode,_setup.PlayerSlot,_nodeTransportPart,_nodeTransportDisposal).FirstOrDefault(w=>w.Target==id)?.Degrees??0;
    private int RateMaximum(int id)=>360;
    private void SetDestinationRate(int id,int value)
    {
        _setup.Robots.Transport.SetBalancedRate(_populationNode,_setup.PlayerSlot,_nodeTransportPart,id,value,_nodeTransportDisposal);
    }
    private bool DestinationMapClick(Point pointer)
    {
        if(_rateTarget>=0)
        {
            if(RateDialogArea.Contains(pointer)){RatePieClick(pointer);return true;}
            _rateTarget=-1;return true;
        }
        foreach(int id in DestinationNodes())if(DestinationBadge(id).Contains(pointer)){_rateTarget=id;return true;}
        int hit=_statusGlobe?HitGlobeNode(pointer,DestinationNodes()):_net.HitNode(_setup,NetPanel,pointer,DestinationNodes());
        if(hit>=0){_rateTarget=hit;return true;}return false;
    }
    private void DrawDestinationMap()
    {
        if(_transportOverview){DrawTransportOverviewMap();return;}
        if(!DestinationOperation){_rateTarget=-1;return;}
        if(_movementOpen)return; // Nodes already provide destination highlighting and hit testing.
        foreach(int id in DestinationNodes().Append(OperationSource).Distinct())
        {
            if(id<0 || !OperationPosition(id,out var at))continue;
            bool source=id==OperationSource;
            var badge=DestinationBadge(id);
            string text=source?"現在": "→";
            if(_nodeTransportOpen && !_movementOpen)
            {
                int remain=360-_setup.Robots.Transport.Plan(_populationNode,_setup.PlayerSlot,_nodeTransportPart,false).Sum(w=>w.Degrees)-_setup.Robots.Transport.Plan(_populationNode,_setup.PlayerSlot,_nodeTransportPart,true).Sum(w=>w.Degrees);
                text=$"{(source?remain:Rate(id))*100/360f:0.#}%";
            }
            if(source){_ui.Box(badge,new Color(25,42,52));_ui.Center(text,badge,.65f,Cream);}
            else _ui.Button(badge,text,id==_selectedMoveNode?Accent:new Color(190,140,56),.65f);
            _ui.Box(new((int)at.X-5,(int)at.Y-5,10,10),source?Cream:Accent);
        }
        if(!_nodeTransportOpen || _rateTarget<0)return;
        DrawRatePieDialog();
    }
}
