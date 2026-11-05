namespace FolderVerse;
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;

public partial class Game1
{
    private IReadOnlyList<March> _computerMarches;
    private March[] _visibleComputerMarches;
    private int _computerMarchIndex;
    private double _computerMarchAge;
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
        BeginComputerView(visible[0].Ruler);
        return true;
    }

    private bool UpdateComputerMarches(double elapsed,bool skip)
    {
        if(_computerMarches==null)return false;
        _computerMarchAge+=elapsed;
        if(!skip && _computerMarchAge<ComputerMarchSeconds)return true;
        _computerMarchAge=0;
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

    private void DrawComputerMarches(Matrix transform)
    {
        if(_computerMarches==null)return;
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
