namespace FolderVerse;
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;

public partial class Game1
{
    private static readonly Rectangle FunctionTransferMapBanner=new(54,878,810,36);
    private bool _functionTransferMap, _functionTransferRoutesBefore;
    private string _functionTransferMessage="";
    private readonly List<FunctionTransferMarker> _functionTransferMarkers=new();
    private sealed record FunctionTransferMarker(int Source,int Target,Vector2 From,Vector2 To,Rectangle Bounds,bool Reserved);

    private void OpenFunctionTransferMap()
    {
        if(!DomesticOperationsAllowed)return;
        ResetInspector();
        _functionTransferRoutesBefore=_net.ShowRoutes;
        _statusMenuOpen=false;
        _net.ShowRoutes=true;
        _functionTransferMap=true;
    }

    private void CloseFunctionTransferMap()
    {
        if(!_functionTransferMap)return;
        _net.ShowRoutes=_functionTransferRoutesBefore;
        _functionTransferMap=false;
        _functionTransferMarkers.Clear();
        _functionTransferMessage="";
    }

    private FunctionTransferMarker HitFunctionTransferMarker(Point pointer)=>
        _functionTransferMarkers.Where(m=>m.Bounds.Contains(pointer))
            .OrderBy(m=>Vector2.DistanceSquared(m.Bounds.Center.ToVector2(),pointer.ToVector2())).FirstOrDefault();

    private string FunctionTransferDirection(int source,int target)=>
        $"{_setup.Nodes.DisplayName(_setup.Nodes.All[source])} → {_setup.Nodes.DisplayName(_setup.Nodes.All[target])}";

    private bool FunctionTransferMenusOpen=>_statusMenuOpen || _mapLayerListOpen || _trafficLayerListOpen || _mapDragListOpen;

    private bool FunctionTransferMapClick(Point pointer)
    {
        if(!_functionTransferMap || !DomesticOperationsAllowed || FunctionTransferMenusOpen || MapTransitionActive || !_statusGlobe && _net.IsAnimating)return false;
        var marker=HitFunctionTransferMarker(pointer);
        if(marker==null)return false;
        var transfers=_setup.Campaign.FunctionTransfers;
        string direction=FunctionTransferDirection(marker.Source,marker.Target);
        if(marker.Reserved)
        {
            if(transfers.Orders.Any(o=>o.Source==marker.Source && o.Target==marker.Target &&
                _setup.Relations.Controls(o.Owner) && _setup.Nodes.All[o.Source].Owner==o.Owner))
            {
                transfers.Cancel(marker.Source);
                _functionTransferMessage=$"{direction} の予約を取り消しました";
                _setup.Robots.PartPlanning.Preview(_setup);
                int failed=_setup.Robots.PartPlanning.PreviewResults.Count(r=>r.Status!="実施予定");
                _functionTransferMessage+=failed>0?$" / 関連予約 {failed}件は実施できません（格納庫で確認）":" / 関連操作は同地点・隣接地点の予約として継続";
            }
        }
        else if(!transfers.Orders.Any(o=>o.Source==marker.Source) && transfers.CanReserve(_setup,marker.Source,marker.Target))
        {
            transfers.Reserve(_setup,marker.Source,marker.Target);
            _functionTransferMessage=$"{direction} を予約しました（配送時に実施）";
        }
        _functionTransferMarkers.Clear();
        return true;
    }

    private void BuildFunctionTransferMarkers()
    {
        var transfers=_setup.Campaign.FunctionTransfers;
        var reservedSources=transfers.Orders.Select(o=>o.Source).ToHashSet();
        var positions=new Dictionary<int,Vector2>();
        foreach(var node in _setup.Nodes.All)
            if(_setup.Relations.Controls(node.Owner) && _net.NodeVisible(_setup,node) &&
                OperationPosition(node.Id,out var at) && MapViewBounds.Contains(at.ToPoint()))
                positions.Add(node.Id,at);

        foreach(var order in transfers.Orders)
            if(_setup.Nodes.All[order.Source].Owner==order.Owner)
                AddMarker(order.Source,order.Target,true);

        foreach(int source in positions.Keys)
        {
            var node=_setup.Nodes.All[source];
            if(!node.Functional || reservedSources.Contains(source))continue;
            foreach(int target in _setup.Routes.Neighbors(node).Select(r=>_setup.Nodes.At(r.Target,r.Entry).Id).Distinct())
                if(positions.ContainsKey(target) && transfers.CanReserve(_setup,source,target))
                    AddMarker(source,target,false);
        }

        void AddMarker(int source,int target,bool reserved)
        {
            if(!positions.TryGetValue(source,out var from) || !positions.TryGetValue(target,out var to))return;
            var delta=to-from;
            if(delta.LengthSquared()<1)return;
            var unit=Vector2.Normalize(delta);
            var center=((from+to)/2+new Vector2(-unit.Y,unit.X)*22).ToPoint();
            int halfWidth=reserved?23:16;
            var bounds=new Rectangle(center.X-halfWidth,center.Y-13,halfWidth*2,26);
            if(!MapViewBounds.Contains(bounds) || FunctionTransferMapBanner.Intersects(bounds) || MapViewButton.Intersects(bounds) ||
                NetButton(2).Intersects(bounds) || NetButton(3).Intersects(bounds) ||
                !_statusGlobe && NetButton(4).Intersects(bounds))return;
            _functionTransferMarkers.Add(new(source,target,from,to,bounds,reserved));
        }
    }

    private void DrawFunctionTransferMap()
    {
        _functionTransferMarkers.Clear();
        if(!_functionTransferMap || !DomesticOperationsAllowed || InspectorVisible || !MapMarkerMenuAvailable)return;
        if(!FunctionTransferMenusOpen && !MapTransitionActive && (_statusGlobe || !_net.IsAnimating))BuildFunctionTransferMarkers();
        var hovered=HitFunctionTransferMarker(_pointer);
        var background=new Color(7,23,35);
        var reservedColor=new Color(255,210,125);

        foreach(var marker in _functionTransferMarkers)
            DrawFunctionTransferLine(marker.From,marker.To,marker.Reserved?reservedColor:Accent*.6f,2);
        if(hovered!=null)
        {
            DrawFunctionTransferLine(hovered.From,hovered.To,Cream,4);
            foreach(var at in new[]{hovered.From,hovered.To})
            {
                _ui.Tile(at+new Vector2(0,-13),new(28,3),0,Cream);
                _ui.Tile(at+new Vector2(0,13),new(28,3),0,Cream);
            }
        }
        foreach(var marker in _functionTransferMarkers)
            if(marker!=hovered)DrawMarker(marker);
        if(hovered!=null)DrawMarker(hovered);

        string hint=hovered==null
            ? _functionTransferMessage.Length>0?_functionTransferMessage:"機能移転の予約　/　矢印で予約　/　書類で取消　/　Esc で閉じる"
            : FunctionTransferDirection(hovered.Source,hovered.Target)+(hovered.Reserved?" の予約を取り消す"
                :_setup.Nodes.All[hovered.Target].Functional?" に機能を集約する":" に機能を移す");
        _ui.Box(FunctionTransferMapBanner,background);
        _ui.Center(hint,FunctionTransferMapBanner,Math.Min(.4f,790/Math.Max(1,_font.MeasureString(hint).X)),Cream);

        void DrawMarker(FunctionTransferMarker marker)
        {
            var ink=marker==hovered?Cream:marker.Reserved?reservedColor:Accent;
            _ui.Box(marker.Bounds,marker==hovered || marker.Reserved?background:background*.85f);
            var at=marker.Bounds.Center.ToVector2();
            var unit=Vector2.Normalize(marker.To-marker.From);
            if(marker.Reserved)
            {
                DrawPrimaryIcon(FunctionTransferAction,new(marker.Bounds.X+2,marker.Bounds.Y+1,24,24),ink);
                at.X+=12;
            }
            var tip=at+unit*(marker.Reserved?6:11);
            var side=new Vector2(-unit.Y,unit.X)*5;
            DrawFunctionTransferLine(at-unit*(marker.Reserved?6:11),tip,ink,3);
            DrawFunctionTransferLine(tip-unit*7+side,tip,ink,3);
            DrawFunctionTransferLine(tip-unit*7-side,tip,ink,3);
        }
    }

    private void DrawFunctionTransferLine(Vector2 from,Vector2 to,Color color,float width)
    {
        var delta=to-from;
        _ui.Tile((from+to)/2,new(delta.Length(),width),MathF.Atan2(delta.Y,delta.X),color);
    }
}
