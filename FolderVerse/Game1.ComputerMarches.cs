namespace FolderVerse;
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

public partial class Game1
{
    private IReadOnlyList<March> _computerMarches;
    private March[] _visibleComputerMarches;
    private int _computerMarchIndex;
    private double _computerMarchAge;
    private int _computerWarStage;
    private int _computerWarSource;
    private Point _computerWarClick;
    private readonly Queue<int> _computerWarCards=new();
    private int _computerViewRuler,_computerViewLeader;
    private const double ComputerViewLeadIn=.75,ComputerMarchSeconds=2.4+ComputerViewLeadIn;
    private static readonly Rectangle ComputerMarchPanel=new(54,170,750,280);
    private static readonly Rectangle ComputerMarchTarget=new(82,264,694,50);
    private static readonly Rectangle ComputerMarchOrder=new(82,331,694,50);

    private static bool CanPreviewComputerMarch(March order)=>order.Peaceful ||
        order is {Queen:true, Fighters:>=0} or {PocketOnly:true, Fighters:>0} or
            {Queen:false, PocketOnly:false, SourceNode:>=0, Fighters:>0};

    private bool ShowComputerMarches(IReadOnlyList<March> orders)
    {
        if(_demoActive)return false;
        var visible=orders.Where(order=>ShowsComputerTurn(order.Ruler)).OrderBy(order=>WorldDayCycle.DawnPhase(_setup,order.Ruler)).ThenBy(order=>order.Ruler).ToArray();
        if(visible.Length==0)return false;
        _computerMarches=orders;
        _visibleComputerMarches=visible;
        _computerMarchIndex=0;
        _computerMarchAge=0;
        _computerWarStage=0;
        BeginComputerView(visible[0].Ruler);
        return true;
    }

    private bool HandleComputerMarches(GameTime time,bool skip)
    {
        if(!skip && _computerMarches!=null && _computerMarchIndex==0 && _computerWarStage==0 &&
            _screen==Screen.WorldStatus && !_conquestOverview &&
            (_lastCutInPhase!=PlanningPhaseLabel || time.TotalGameTime.TotalSeconds-_cutInStarted<PhaseCutInSeconds))
            return true;
        return UpdateComputerMarches(time.ElapsedGameTime.TotalSeconds,skip);
    }

    private bool UpdateComputerMarches(double elapsed,bool skip)
    {
        if(_computerMarches==null)return false;
        _net.UpdateAnimation((float)elapsed);
        UpdateMapTransition((float)elapsed);
        UpdateDomesticSlide((float)elapsed);
        _computerMarchAge+=elapsed;
        var order=_visibleComputerMarches[_computerMarchIndex];
        if(!skip && _computerWarStage>=0 && CanPreviewComputerMarch(order))
        {
            if(_computerMarchAge<1)return true;
            if(AdvanceComputerWarPreview(order)){_computerMarchAge=0;return true;}
        }
        if(!skip && _computerMarchAge<ComputerMarchSeconds)return true;
        CloseMovement();
        _computerMarchAge=0;
        _computerWarStage=0;
        if(!skip && ++_computerMarchIndex<_visibleComputerMarches.Length)
        {
            int ruler=_visibleComputerMarches[_computerMarchIndex].Ruler;
            if(ruler!=_setup.PlayerSlot)ShowComputerView(ruler);
            return true;
        }
        foreach(int ruler in _visibleComputerMarches.Select(order=>order.Ruler).Distinct())_warChecksByPlayer.Remove(ruler);
        var orders=_computerMarches;
        _computerMarches=null;
        _visibleComputerMarches=null;
        EndComputerView();
        _setup.Campaign.Resolve(_setup,orders);
        FinishMilitaryAdvance();
        return true;
    }

    private bool AdvanceComputerWarPreview(March order)
    {
        switch(_computerWarStage++)
        {
            case 0:
                if(order.Peaceful)
                {
                    OpenMovementWithFit(false);
                    if(!_net.MovementTargets.Contains(order.TargetNode))break;
                    FocusDemoNode(_setup.Nodes.All[order.TargetNode]);
                    return true;
                }
                _computerWarClick=WarButton.Center;
                MapOverlayClick(_computerWarClick,false);
                if(!_warPlanning)break;
                FocusDemoNode(_setup.Nodes.All[order.TargetNode]);
                return true;
            case 1:
                var target=_setup.Nodes.All[order.TargetNode];
                Vector2 at;
                if(_statusGlobe)
                {
                    var position=GlobeNodeDisplayPosition(target,_net.ResourceClearance(_setup,target.Cell));
                    if(!_world.ProjectVisible(position,_setup.Cells[target.Cell].Normal,_yaw,_pitch,StatusGlobeArea,out at))break;
                }
                else if(!OperationPosition(target.Id,out at))
                {
                    // The inspector slide can move the unfolded map after the initial focus.
                    FocusDemoNode(target);
                    if(!OperationPosition(target.Id,out at))break;
                }
                if(!TryComputerDestinationClick(target.Id,at.ToPoint(),out _computerWarClick))break;
                MovementClick(_computerWarClick,new KeyboardState());
                if(_selectedMoveNode==order.TargetNode)return true;
                break;
            case 2:
                if(order.Peaceful)
                {
                    _computerWarClick=InspectorScreenBounds(ConfirmMoveButton).Center;
                    MovementClick(_computerWarClick,new KeyboardState());
                    if(_movementOpen)break;
                    _computerWarStage=6;
                    return true;
                }
                bool pocket=order.Queen || order.PocketOnly;
                int source=_invasionSources.FindIndex(row=>row.Queen==pocket && row.Owner==order.Ruler && row.Node==(pocket?_setup.Nodes.Current(order.Ruler)?.Id:order.SourceNode));
                if(source<0)break;
                var robots=InvasionRobots(_invasionSources[source]);
                var ids=order.RobotIds??robots.Take((int)order.Fighters).Select(robot=>robot.Id).ToArray();
                var previews=InvasionPreviewRobots(_invasionSources[source]);
                var cards=ids.Select(id=>Array.FindIndex(previews,robot=>robot.Id==id)).ToArray();
                if(ids.Length!=order.Fighters || cards.Distinct().Count()!=cards.Length || cards.Any(i=>i<0) || ids.Any(id=>!robots.Any(robot=>robot.Id==id)))break;
                _computerWarSource=source;
                _invasionPage=InvasionPageForSource(source);
                // Selection is a visual replay; the original batch owns reservation/execution.
                _invasionSources[source].Robots.Clear();_invasionSources[source].Count=0;_invasionSources[source].Selected=false;
                _computerWarCards.Clear();
                if(order.Queen)_computerWarCards.Enqueue(-1);
                foreach(int index in cards.OrderBy(index=>index))_computerWarCards.Enqueue(index);
                _computerWarClick=ComputerWarSelectionPoint();
                return true;
            case 3:
                if(_computerWarCards.Count==0)return true;
                _computerWarClick=_pointer=ComputerWarSelectionPoint();
                if(!InvasionClick(new Point(_pointer.X-NodeInspectorOffset,_pointer.Y)))break;
                _computerWarCards.Dequeue();
                _computerWarStage--;
                return true;
            case 4:
                _computerWarClick=NodeCheckButton.Center;
                if(!_warChecksByPlayer.TryGetValue(order.Ruler,out var checks) || !checks.Contains(order.TargetNode))
                    NodeCheckClick(_computerWarClick);
                return true;
            case 5:
                _computerWarClick=InspectorClose.Center;
                DomesticViewClick(_computerWarClick);
                return true;
            default:
                _computerMarchAge=ComputerMarchSeconds;
                return false;
        }
        CloseMovement();
        _computerWarStage=-1;
        _computerMarchAge=0;
        return false;
    }

    private bool TryComputerDestinationClick(int target,Point center,out Point click)
    {
        bool HitsTarget(Point point)=>MapViewBounds.Contains(point) &&
            (_statusGlobe?HitGlobeNode(point,_net.MovementTargets):
                _net.HitNode(_setup,NetPanel,point,_net.MovementTargets))==target;

        click=center;
        if(HitsTarget(center))return true;
        if(!_statusGlobe)return false;
        // A neighboring harbor can overlap the center of a visible site.
        for(int radius=4;radius<=24;radius+=4)
            for(int y=-1;y<=1;y++)
                for(int x=-1;x<=1;x++)
                {
                    if(x==0 && y==0)continue;
                    var point=center+new Point(x*radius,y*radius);
                    if(!HitsTarget(point))continue;
                    click=point;
                    return true;
                }
        return false;
    }

    private Point ComputerWarSelectionPoint()
    {
        if(!_computerWarCards.TryPeek(out int card))return NodeCheckButton.Center;
        var bounds=InvasionVisibleRows().Single(row=>row.Index==_computerWarSource).Bounds;
        return InspectorScreenBounds(card<0?InvasionQueenAt(bounds):InvasionCardAt(bounds,card)).Center;
    }

    private void DrawComputerMarches(Matrix transform)
    {
        if(_computerMarches==null)return;
        var order=_visibleComputerMarches[_computerMarchIndex];
        if(_computerWarStage==0 && CanPreviewComputerMarch(order))
        {
            if(!order.Peaceful && _computerMarchAge>=ComputerViewLeadIn)
            {
                _spriteBatch.Begin(transformMatrix:transform);
                DrawDemoHand(WarButton.Center.ToVector2(),0);
                _spriteBatch.End();
            }
            return;
        }
        if(_computerWarStage>0)
        {
            if(_computerWarStage==6 || (_computerWarStage==1 && order.Peaceful))return;
            _spriteBatch.Begin(transformMatrix:transform);
            // Cursor follows the same inline cards and handle buttons as normal input.
            if(_invasionPicker>=0)DrawDemoHand(_computerWarClick.ToVector2(),Math.Clamp(.4f-(float)_computerMarchAge,0,.4f));
            else if(_computerWarStage==4)DrawDemoHand(NodeCheckButton.Center.ToVector2(),0);
            else if(_computerWarStage==5)DrawDemoHand(InspectorClose.Center.ToVector2(),0);
            else DrawDemoHand(_computerWarClick.ToVector2(),Math.Clamp(.4f-(float)_computerMarchAge,0,.4f));
            _spriteBatch.End();
            return;
        }
        string target=_setup.Nodes.DisplayName(_setup.Nodes.All[order.TargetNode]);
        int sourceId=order.SourceNode>=0?order.SourceNode:_setup.Nodes.Current(order.Ruler)?.Id??-1;
        string source=sourceId>=0?_setup.Nodes.DisplayName(_setup.Nodes.All[sourceId]):"現在地";
        string operation=order.Peaceful?"移動":order.Fighters>0?$"出撃 {order.Fighters}体":order.Queen?"征服者が移動":"出撃";
        string targetLabel="目標 / "+target,orderLabel=source+" → "+operation;
        float age=(float)(_computerMarchAge-ComputerViewLeadIn);
        var destination=ComputerMarchTarget.Center.ToVector2();
        if(OperationPosition(order.TargetNode,out var mapPoint) && MapViewBounds.Contains(mapPoint.ToPoint()) &&
            !ComputerMarchPanel.Contains(mapPoint.ToPoint()))destination=mapPoint;
        var confirmation=ComputerMarchOrder.Center.ToVector2();
        Vector2 cursor=age<1
            ?Vector2.SmoothStep(new Vector2(135,235),destination,Math.Clamp(age/.55f,0,1))
            :Vector2.SmoothStep(destination,confirmation,Math.Clamp((age-1)/.55f,0,1));
        float swing=age<1?Math.Clamp(.95f-age,0,.4f):Math.Clamp(1.95f-age,0,.4f);
        _spriteBatch.Begin(transformMatrix:transform);
        _ui.Box(ComputerMarchPanel,new Color(18,36,48));
        _ui.Text("コンピューターの操作 / "+_setup.ConquerorNames[order.Ruler],new(82,199),.58f,Cream);
        _ui.Button(ComputerMarchTarget,targetLabel,age>=.55f?Accent:Muted,Math.Min(.45f,670/_font.MeasureString(targetLabel).X));
        _ui.Button(ComputerMarchOrder,orderLabel,age>=1.55f?Accent:Muted,Math.Min(.43f,670/_font.MeasureString(orderLabel).X));
        _ui.Text(age>=1.55f?"指示確定 / Escでスキップ":"開戦予約の実行 / Escでスキップ",new(82,403),.39f,Cream);
        if(age>=0)DrawDemoHand(cursor,swing);
        _spriteBatch.End();
    }
}
