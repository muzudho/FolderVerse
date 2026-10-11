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
        if(!active || !DestinationOperation || _rateTarget<0 || mouse.LeftButton!=ButtonState.Pressed){_rateDragging=false;return;}
        if(click && new Rectangle(1150,480,540,60).Contains(_pointer))_rateDragging=true;
        if(_rateDragging)SetDestinationRate(_rateTarget,(int)Math.Round(Math.Clamp((_pointer.X-1170)/500f,0,1)*20));
    }
    private bool DestinationOperation=>_movementOpen || (_nodeTransportOpen && _populationCell>=0);
    private int OperationSource=>_movementOpen?(_setup.Nodes.Current(_setup.PlayerSlot)?.Id??-1):_populationNode;
    private int[] DestinationNodes()=>_movementOpen?_net.MovementTargets.ToArray():_setup.Routes.Neighbors(_setup.Nodes.All[_populationNode]).Select(r=>_setup.Nodes.At(r.Target,r.Entry).Id).Distinct().ToArray();
    private void PrepareDestinationMap()
    {
        _net.VisibleOperationNodes=DestinationOperation?new HashSet<int>(DestinationNodes().Append(OperationSource)):null;
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
    private int Rate(int id)=>_setup.Robots.Transport.Plan(_populationNode,_setup.PlayerSlot,_nodeTransportPart,_nodeTransportDisposal).FirstOrDefault(w=>w.Target==id)?.Twentieths??0;
    private int RateMaximum(int id)=>Math.Max(0,20-_setup.Robots.Transport.Plan(_populationNode,_setup.PlayerSlot,_nodeTransportPart,_nodeTransportDisposal).Where(w=>w.Target!=id).Sum(w=>w.Twentieths)-_setup.Robots.Transport.Plan(_populationNode,_setup.PlayerSlot,_nodeTransportPart,!_nodeTransportDisposal).Sum(w=>w.Twentieths));
    private void SetDestinationRate(int id,int value)
    {
        var weights=_setup.Robots.Transport.Plan(_populationNode,_setup.PlayerSlot,_nodeTransportPart,_nodeTransportDisposal).Where(w=>w.Target!=id).ToList();
        value=Math.Clamp(value,0,RateMaximum(id));if(value>0)weights.Add(new(id,value));
        _setup.Robots.Transport.SetPlan(_populationNode,_setup.PlayerSlot,_nodeTransportPart,weights,_nodeTransportDisposal);
    }
    private bool DestinationMapClick(Point pointer)
    {
        if(_rateTarget>=0)
        {
            if(new Rectangle(1120,440,600,150).Contains(pointer))
            {
                if(new Rectangle(1150,480,540,70).Contains(pointer))SetDestinationRate(_rateTarget,(int)Math.Round(Math.Clamp((pointer.X-1170)/500f,0,1)*20));
                if(new Rectangle(1400,550,160,32).Contains(pointer) && CanManageNodeFactory(_setup.Nodes.All[_populationNode].Owner))
                {var order=_setup.Robots.Transport.Priority(_populationNode).Concat(DestinationNodes()).Distinct().ToList();order.Remove(_rateTarget);order.Insert(0,_rateTarget);_setup.Robots.Transport.SetPriority(_populationNode,order);}
                if(new Rectangle(1580,550,110,32).Contains(pointer))_rateTarget=-1;
                return true;
            }
            _rateTarget=-1;return true;
        }
        foreach(int id in DestinationNodes())if(DestinationBadge(id).Contains(pointer)){_rateTarget=id;return true;}
        int hit=_statusGlobe?HitGlobeNode(pointer,DestinationNodes()):_net.HitNode(_setup,NetPanel,pointer,DestinationNodes());
        if(hit>=0){_rateTarget=hit;return true;}return false;
    }
    private void DrawDestinationMap()
    {
        if(!DestinationOperation){_rateTarget=-1;return;}
        foreach(int id in DestinationNodes().Append(OperationSource).Distinct())
        {
            if(id<0 || !OperationPosition(id,out var at))continue;
            bool source=id==OperationSource;
            var badge=DestinationBadge(id);
            string text=source?"現在": "→";
            if(_nodeTransportOpen && !_movementOpen)
            {
                int remain=20-_setup.Robots.Transport.Plan(_populationNode,_setup.PlayerSlot,_nodeTransportPart,false).Sum(w=>w.Twentieths)-_setup.Robots.Transport.Plan(_populationNode,_setup.PlayerSlot,_nodeTransportPart,true).Sum(w=>w.Twentieths);
                text=$"{(source?remain:Rate(id))*5}%";
            }
            if(source){_ui.Box(badge,new Color(25,42,52));_ui.Center(text,badge,.65f,Cream);}
            else _ui.Button(badge,text,id==_selectedMoveNode?Accent:new Color(190,140,56),.65f);
            _ui.Box(new((int)at.X-5,(int)at.Y-5,10,10),source?Cream:Accent);
        }
        if(!_nodeTransportOpen || _rateTarget<0)return;
        _ui.Box(new(1120,440,600,150),new Color(18,35,48));
        _ui.Text($"移送割合　{Rate(_rateTarget)*5}%",new(1150,453),.6f,Cream);
        _ui.Box(new(1170,510,500,8),Muted);
        _ui.Box(new(1170,510,RateMaximum(_rateTarget)*25,8),Accent);
        for(int i=0;i<=20;i++)_ui.Box(new(1170+i*25,506,2,i%5==0?22:13),i<=RateMaximum(_rateTarget)?Cream:Muted);
        _ui.Box(new(1163+Rate(_rateTarget)*25,496,16,30),Color.White);
        _ui.Text($"0% - {RateMaximum(_rateTarget)*5}%",new(1150,555),.4f,Cream);
        _ui.Button(new(1400,550,160,32),"受入１位",CanManageNodeFactory(_setup.Nodes.All[_populationNode].Owner)?Accent:Muted,.36f);
        _ui.Button(new(1580,550,110,32),"閉じる",Accent,.4f);
    }
}
