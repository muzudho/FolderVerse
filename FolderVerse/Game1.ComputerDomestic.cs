namespace FolderVerse;
using System;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

public partial class Game1
{
    private Node[] _computerDomesticNodes;
    private int _computerDomesticIndex;
    private double _computerDomesticAge;
    private int _computerDomesticStage;
    private Point _computerDomesticPoint;
    private double _computerDomesticClickAge;
    private RobotWorld _computerAssemblyStock;
    private PlannedPartMove[] _computerAssemblySteps;
    private PlannedPartMove _computerAssemblyDrop;
    private int _computerAssemblyIndex;
    private double _computerAssemblyAge;
    private Point _computerAssemblyStart;
    private double _computerAssemblyPageAge;
    private Point _computerAssemblyEnd;

    private void ClearComputerAssembly()
    {
        _computerAssemblyStock=null;
        _computerAssemblySteps=null;
        _computerAssemblyDrop=null;
        _hangarDrag=null;
        _hangarScrollDirection=0;
    }

    private bool AdvanceComputerAssembly(double elapsed)
    {
        if(_computerAssemblySteps==null)return false;
        if(_computerAssemblyIndex==_computerAssemblySteps.Length)
        {
            ClearComputerAssembly();
            return false;
        }
        var step=_computerAssemblySteps[_computerAssemblyIndex];
        int target=Array.IndexOf(_hangarSlots,step.TargetRobotId);
        var offset=new Point(NodeInspectorOffset,0);
        if(_computerAssemblyDrop==null)
        {
            int source=Array.IndexOf(_hangarSlots,(long?)step.RobotId);
            if(source<0 || target<0)
            {
                ClearComputerAssembly();
                return false;
            }
            if(!VisibleAssemblySlots().Contains(source))
            {
                var scroll=source<HangarStartSlot?HangarUp:HangarDown;
                _pointer=scroll.Center+offset;
                _computerAssemblyPageAge+=elapsed;
                if(_computerAssemblyPageAge>=.35)
                {
                    HangarPageClick(scroll.Center);
                    _computerAssemblyPageAge=0;
                }
                return true;
            }
            int row=(step.Parts&RobotParts.Head)!=0?0:(step.Parts&RobotParts.Body)!=0?1:2;
            _computerAssemblyStart=AssemblyPartBounds(source,row).Center+offset;
            _computerAssemblyPageAge=0;
            _pointer=_computerAssemblyStart;
            DemoAssemblyPress();
            if(_hangarDrag!=step.RobotId)
            {
                ClearComputerAssembly();
                return false;
            }
            _computerAssemblyDrop=step;
            _computerAssemblyAge=0;
            return true;
        }
        if(!VisibleAssemblySlots().Contains(target))
        {
            var scroll=target<HangarStartSlot?HangarUp:HangarDown;
            _pointer=scroll.Center+offset;
            _computerAssemblyPageAge+=elapsed;
            UpdateHangarDragScroll(scroll.Center,true,(float)_computerAssemblyPageAge);
            _computerAssemblyStart=_pointer;
            return true;
        }
        UpdateHangarDragScroll(Point.Zero,false,0);
        _computerAssemblyEnd=AssemblyCell(target).Center+offset;
        _computerAssemblyAge+=elapsed;
        _pointer=Vector2.Lerp(_computerAssemblyStart.ToVector2(),_computerAssemblyEnd.ToVector2(),
            Math.Clamp((float)(_computerAssemblyAge/.65),0,1)).ToPoint();
        if(_computerAssemblyAge<.65)return true;
        var previous=_previousMouse;
        _previousMouse=new MouseState(0,0,0,ButtonState.Pressed,ButtonState.Released,ButtonState.Released,ButtonState.Released,ButtonState.Released);
        try{NodeAssemblyInput(new MouseState(),false,false);}finally{_previousMouse=previous;}
        if(_computerAssemblyStock.Nodes[step.Target].Robots.FirstOrDefault(robot=>robot.Id==step.TargetRobotId)?.Parts
            !=(step.ExpectedTarget|step.Parts))
        {
            ClearComputerAssembly();
            return false;
        }
        RecordComputerDomesticClick(_pointer);
        _computerAssemblyDrop=null;
        _computerAssemblyIndex++;
        return true;
    }
    private const double ComputerDomesticSeconds=5;
    private const double ComputerHangarClickTime=1.55,ComputerCheckClickTime=3.2,ComputerCloseClickTime=4.2;
    private Point ComputerHangarPoint=>NodeAssemblyTile.Center+new Point(NodeInspectorOffset,0);
    private bool CanMarkComputerDomesticNode=>NodeCheckButtonVisible && !NodeChecked(_populationNode);

    private void RecordComputerDomesticClick(Point point)
    {
        _computerDomesticPoint=point;
        _computerDomesticClickAge=_computerDomesticAge;
    }

    private bool ShowsComputerTurn(int ruler)=>!_setup.IsHumanPlayer(ruler) &&
        _setup.ControllerOf(ruler)<0 && _setup.ShowComputerOperations(ruler);

    private void BeginComputerView(int ruler)
    {
        _computerViewRuler=_setup.PlayerSlot;
        _computerViewLeader=_setup.Relations.Leader;
        ShowComputerView(ruler);
    }

    private void ShowComputerView(int ruler)
    {
        ClearComputerAssembly();
        ResetInspector();
        _setup.SelectTurnRuler(ruler);
        RestorePlayerView();
    }

    private void EndComputerView()
    {
        ClearComputerAssembly();
        ResetInspector();
        _setup.SelectPlayer(_computerViewRuler);
        _setup.Relations.Leader=_computerViewLeader;
        RestorePlayerView();
    }

    private bool ShowComputerDomestic()
    {
        if(_demoActive)return false;
        var nodes=_setup.Nodes.All.Where(node=>node.Owner>=0 && ShowsComputerTurn(node.Owner) &&
            _setup.Relations.CanAct(node.Owner) && !_setup.Campaign.RobotBattles.LockedNode(node.Id))
            .GroupBy(node=>node.Owner).OrderBy(group=>group.Key)
            .Select(group=>group.OrderByDescending(node=>_setup.Campaign.FunctionTransfers.Orders.Any(o=>o.Source==node.Id))
                .ThenByDescending(node=>_setup.Robots.AssemblySelection(node.Id,node.Owner,true)!=null).First())
            .ToArray();
        if(nodes.Length==0)return false;
        _computerDomesticNodes=nodes;
        _computerDomesticIndex=0;
        _computerDomesticAge=0;
        BeginComputerView(nodes[0].Owner);
        FocusComputerDomestic(nodes[0]);
        return true;
    }

    private void FocusComputerDomestic(Node node)
    {
        _computerDomesticStage=0;
        _computerDomesticClickAge=double.NegativeInfinity;
        FocusDemoNode(node);
    }

    private bool UpdateComputerDomestic(double elapsed,bool skip)
    {
        if(_computerDomesticNodes==null)return false;
        UpdateDomesticSlide((float)elapsed);
        _computerDomesticAge+=elapsed;
        do
        {
            var node=_computerDomesticNodes[_computerDomesticIndex];
            if(!skip && _computerDomesticStage==0 && _computerDomesticAge>=ComputerViewLeadIn)
            {
                _computerDomesticStage=-1;
                if(OperationPosition(node.Id,out var position) && MapViewBounds.Contains(position.ToPoint()))
                {
                    var point=position.ToPoint();
                    int hit=_statusGlobe?HitGlobeNode(point):_net.HitNode(_setup,NetPanel,point);
                    if(hit==node.Id)
                    {
                        OpenNodeInspector(hit);
                        OpenNodeOffice();
                        RecordComputerDomesticClick(point);
                        _computerDomesticStage=1;
                    }
                }
            }
            if(!skip && _computerDomesticStage==1 && _computerDomesticAge>=ComputerHangarClickTime)
            {
                RecordComputerDomesticClick(ComputerHangarPoint);
                NodeDialogClick(_computerDomesticPoint,new KeyboardState());
                _computerDomesticStage=_nodeAssemblyOpen?2:-1;
                if(_nodeAssemblyOpen)
                {
                    (_computerAssemblyStock,_computerAssemblySteps)=_setup.Robots.PartPlanning.AssemblyPlayback(_setup,HangarPlanningNode);
                    _computerAssemblyIndex=0;
                    _computerAssemblyPageAge=0;
                    _pointer=_computerDomesticPoint;
                    if(_computerAssemblyStock!=null)
                    {
                        Array.Clear(_hangarSlots);
                        SyncHangarSlots();
                        return true;
                    }
                }
            }
            if(!skip && _computerDomesticStage==2 && AdvanceComputerAssembly(elapsed))
            {
                _computerDomesticAge=Math.Min(_computerDomesticAge,ComputerCheckClickTime-.1);
                return true;
            }
            if(!skip && _computerDomesticStage==2 && _computerDomesticAge>=ComputerCheckClickTime)
            {
                if(CanMarkComputerDomesticNode){
                    RecordComputerDomesticClick(NodeCheckButton.Center);
                    DomesticViewClick(_computerDomesticPoint);
                }
                _computerDomesticStage=3;
            }
            if(!skip && _computerDomesticStage==3 && _computerDomesticAge>=ComputerCloseClickTime)
            {
                RecordComputerDomesticClick(InspectorClose.Center);
                DomesticViewClick(_computerDomesticPoint);
                _computerDomesticStage=4;
            }
            if(!skip && _computerDomesticAge<ComputerDomesticSeconds)return true;
            _computerDomesticAge=0;
            if(++_computerDomesticIndex==_computerDomesticNodes.Length)break;
            if(!skip)
            {
                var next=_computerDomesticNodes[_computerDomesticIndex];
                ShowComputerView(next.Owner);
                FocusComputerDomestic(next);
            }
        }while(skip);
        if(!skip && _computerDomesticIndex<_computerDomesticNodes.Length)return true;
        foreach(int ruler in _computerDomesticNodes.Select(node=>node.Owner).Distinct())_nodeChecksByPlayer.Remove(ruler);
        _computerDomesticNodes=null;
        EndComputerView();
        // Apply delivery and automatic assembly in the same order as when playback is off.
        _setup.Campaign.BeginDelivery(_setup);
        FinishDomesticDelivery();
        return true;
    }

    private void DrawComputerDomestic(Matrix transform)
    {
        if(_computerDomesticNodes==null)return;
        if(_computerDomesticStage>=0)
        {
            const float swingDuration=.4f;
            float swing=Math.Clamp(swingDuration-(float)(_computerDomesticAge-_computerDomesticClickAge),0,swingDuration);
            if(_computerDomesticStage==0 || _computerDomesticStage==4 && swing==0)return;
            var target=_computerAssemblySteps!=null?_pointer:swing>0?_computerDomesticPoint:_computerDomesticStage switch
            {
                1=>ComputerHangarPoint,
                2 when _computerDomesticAge>=ComputerCheckClickTime-.6 && CanMarkComputerDomesticNode=>NodeCheckButton.Center,
                3=>InspectorClose.Center,
                _=>_computerDomesticPoint
            };
            _spriteBatch.Begin(transformMatrix:transform);
            DrawDemoHand(target.ToVector2(),swing);
            _spriteBatch.End();
            return;
        }
        var node=_computerDomesticNodes[_computerDomesticIndex];
        float age=(float)(_computerDomesticAge-ComputerViewLeadIn);
        string location=_setup.Nodes.DisplayName(node);
        bool reviewed=_computerDomesticAge>=ComputerCheckClickTime;
        string action=reviewed?"格納庫の確認完了":"格納庫を確認";
        var transfer=_setup.Campaign.FunctionTransfers.Orders.FirstOrDefault(o=>o.Source==node.Id);
        if(transfer!=null){
            location+=" → "+_setup.Nodes.DisplayName(_setup.Nodes.All[transfer.Target]);
            action="領地の入口へ機能移転を予約";
        }
        _spriteBatch.Begin(transformMatrix:transform);
        _ui.Box(ComputerMarchPanel,new Color(18,36,48));
        _ui.Text("コンピューターの操作 / "+_setup.ConquerorNames[node.Owner],new(82,199),.58f,Cream);
        _ui.Button(ComputerMarchTarget,location,Muted,Math.Min(.45f,670/_font.MeasureString(location).X));
        _ui.Button(ComputerMarchOrder,action,reviewed?Accent:Muted,.45f);
        _ui.Text("内政フェーズ / Escでスキップ",new(82,403),.39f,Cream);
        if(age>=0)DrawDemoHand(Vector2.SmoothStep(ComputerMarchTarget.Center.ToVector2(),ComputerMarchOrder.Center.ToVector2(),
            Math.Clamp(age/.8f,0,1)),Math.Clamp(1.2f-age,0,.4f));
        _spriteBatch.End();
    }
}
