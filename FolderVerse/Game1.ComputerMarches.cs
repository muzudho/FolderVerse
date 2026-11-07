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
    private Point _computerWarClick;
    private readonly Queue<Point> _computerWarCards=new();
    private int _computerViewRuler,_computerViewLeader;
    private const double ComputerViewLeadIn=.75,ComputerMarchSeconds=2.4+ComputerViewLeadIn;
    private static readonly Rectangle ComputerMarchPanel=new(54,170,750,280);
    private static readonly Rectangle ComputerMarchTarget=new(82,264,694,50);
    private static readonly Rectangle ComputerMarchOrder=new(82,331,694,50);

    private bool ShowComputerMarches(IReadOnlyList<March> orders)
    {
        if(_demoActive)return false;
        var visible=orders.Where(order=>ShowsComputerTurn(order.Ruler)).OrderBy(order=>order.Ruler).ToArray();
        if(visible.Length==0)return false;
        _computerMarches=orders;
        _visibleComputerMarches=visible;
        _computerMarchIndex=0;
        _computerMarchAge=0;
        _computerWarStage=0;
        BeginComputerView(visible[0].Ruler);
        return true;
    }

    private bool UpdateComputerMarches(double elapsed,bool skip)
    {
        if(_computerMarches==null)return false;
        _net.UpdateAnimation((float)elapsed);
        UpdateMapTransition((float)elapsed);
        UpdateDomesticSlide((float)elapsed);
        _computerMarchAge+=elapsed;
        var order=_visibleComputerMarches[_computerMarchIndex];
        if(!skip && _computerWarStage>=0 &&
            (order.Peaceful || order is {Queen:false, PocketOnly:false, SourceNode:>=0, Fighters:>0}))
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
                if(!OperationPosition(order.TargetNode,out var at))break;
                _computerWarClick=at.ToPoint();
                if(!MapViewBounds.Contains(_computerWarClick) ||
                    (_statusGlobe?HitGlobeNode(_computerWarClick,_net.MovementTargets):
                        _net.HitNode(_setup,NetPanel,_computerWarClick,_net.MovementTargets))!=order.TargetNode)break;
                MovementClick(_computerWarClick,new KeyboardState());
                if(_selectedMoveNode==order.TargetNode)return true;
                break;
            case 2:
                if(order.Peaceful)
                {
                    _computerWarClick=ConfirmMoveButton.Center;
                    MovementClick(_computerWarClick,new KeyboardState());
                    if(_movementOpen)break;
                    _computerWarStage=5;
                    return true;
                }
                int source=_invasionSources.FindIndex(row=>!row.Queen && row.Owner==order.Ruler && row.Node==order.SourceNode);
                if(source<0)break;
                var robots=InvasionRobots(_invasionSources[source]);
                var ids=order.RobotIds??robots.Take((int)order.Fighters).Select(robot=>robot.Id).ToArray();
                var cards=ids.Select(id=>Array.FindIndex(robots,robot=>robot.Id==id)).ToArray();
                if(ids.Length!=order.Fighters || cards.Distinct().Count()!=cards.Length || cards.Any(i=>i<0 || InvasionRobotCard(i).Center.Y>=InvasionPickerDone.Top))break;
                _invasionPage=InvasionPageForSource(source);
                var local=InvasionVisibleRows().Single(row=>row.Index==source).Bounds.Center;
                _computerWarClick=new Point(local.X+NodeInspectorOffset,local.Y);
                OpenInvasionPicker(source);
                if(_invasionPicker<0)break;
                _invasionDraft.Clear();
                _computerWarCards.Clear();
                foreach(int card in cards)_computerWarCards.Enqueue(InvasionRobotCard(card).Center);
                _computerWarClick=_computerWarCards.Peek();
                return true;
            case 3:
                if(_computerWarCards.TryDequeue(out var cardClick))
                {
                    _computerWarClick=_pointer=cardClick;
                    InvasionPickerInput(true,false);
                    _computerWarStage--;
                    return true;
                }
                _computerWarClick=InvasionPickerDone.Center;
                _pointer=_computerWarClick;
                InvasionPickerInput(true,false);
                return true;
            case 4:
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

    private void DrawComputerMarches(Matrix transform)
    {
        if(_computerMarches==null)return;
        if(_computerWarStage>0)
        {
            if(_computerWarStage==5 || (_computerWarStage==1 && _visibleComputerMarches[_computerMarchIndex].Peaceful))return;
            _spriteBatch.Begin(transformMatrix:transform);
            // Once the modal opens, the source button is behind it.
            if(_invasionPicker>=0)DrawDemoHand(_computerWarClick.ToVector2(),Math.Clamp(.4f-(float)_computerMarchAge,0,.4f));
            else if(_computerWarStage==4)DrawDemoHand(InspectorClose.Center.ToVector2(),0);
            else DrawDemoHand(_computerWarClick.ToVector2(),Math.Clamp(.4f-(float)_computerMarchAge,0,.4f));
            _spriteBatch.End();
            return;
        }
        var order=_visibleComputerMarches[_computerMarchIndex];
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
