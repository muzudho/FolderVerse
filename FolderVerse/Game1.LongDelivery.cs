namespace FolderVerse;
using System;
using System.Linq;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

public partial class Game1
{
    private bool _longDeliveryMode;
    private int _longSource=-1;
    private int _longOrderPage;
    private (long Id,RobotParts Parts)? _longSelected;
    private readonly List<(Rectangle Bounds,int Node)> _longMarkers=new();
    private void OpenLongDeliveryMap(){OpenDirectMap();if(!_directMap)return;_longDeliveryMode=true;_longSource=-1;_longOrderPage=0;_longSelected=null;}
    private bool LongDestination(int node)=>DirectNodeAllowed(node) && !_setup.Campaign.RobotBattles.LockedNode(node) && (_longSource<0 || node==_longSource || RobotLongDelivery.Path(_setup,_longSource,node,_setup.Nodes.All[_longSource].Owner).Length>1);
    private void DrawLongMarkers()
    {
        _longMarkers.Clear();if(!_directMap || _directPairOpen || MapTransitionActive || !_statusGlobe && _net.IsAnimating)return;
        foreach(var node in _setup.Nodes.All.Where(n=>LongDestination(n.Id))){
            if(!OperationPosition(node.Id,out var at) || !MapViewBounds.Contains(at.ToPoint()))continue;
            var bounds=new Rectangle((int)at.X-18,(int)at.Y-18,36,36);
            _ui.Box(bounds,node.Id==_longSource?Cream:Accent);DrawPrimaryIcon(LongMapAction,new(bounds.X+3,bounds.Y+3,30,30),new Color(14,29,40));
            _longMarkers.Add((bounds,node.Id));
        }
        _ui.Center(_longSource<0?"パーツ長距離便：元地点を選んでください":"パーツ長距離便：つながる先地点を選んでください / 元地点を再クリックで選び直す",new(54,110,1300,40),.43f,Cream);
    }
    private bool LongMapClick(Point point)
    {
        if(!_directMap || _directPairOpen || MapTransitionActive || !MapViewBounds.Contains(point))return false;
        var hit=_longMarkers.Where(m=>m.Bounds.Contains(point)).OrderBy(m=>Vector2.DistanceSquared(m.Bounds.Center.ToVector2(),point.ToVector2())).FirstOrDefault();
        if(hit.Bounds!=Rectangle.Empty){
            if(_longSource<0)_longSource=hit.Node;
            else if(_longSource==hit.Node)_longSource=-1;
            else{OpenDirectPair(_longSource,hit.Node);_longSelected=null;_directMessage="左のパーツを選び、右の受取区画をクリック / １全体ターンに１地点分進みます";}
            return true;
        }
        if(!_statusGlobe && !_net.IsAnimating){_net.HoverEdge(NetPanel,point);if(_net.ClickEdge())_longMarkers.Clear();}
        return true;
    }
    private void SyncLongSlots()
    {
        for(int side=0;side<2;side++){
            var store=_setup.Robots.Nodes[_directNodes[side]];Array.Resize(ref _directSlots[side],store.TotalCapacity);
            for(int slot=0;slot<_directSlots[side].Length;slot++)if(_directSlots[side][slot].HasValue && !store.Robots.Any(r=>r.Id==_directSlots[side][slot]))_directSlots[side][slot]=null;
            foreach(var robot in store.Robots)if(!_directSlots[side].Contains(robot.Id)){int slot=Array.IndexOf(_directSlots[side],null);if(slot>=0)_directSlots[side][slot]=robot.Id;}
        }
    }
    private static Rectangle LongPageButton(int side,int direction)=>new(40+side*1320+(direction<0?0:402),244,120,42);
    private static Rectangle LongOrderButton(int index)=>new(610,270+index*65,700,58);
    private LongPartOrder[] AllLongOrders=>_setup.Robots.LongDelivery.Orders.Where(o=>o.Commander==_setup.PlayerSlot).ToArray();
    private LongPartOrder[] VisibleLongOrders{get{_longOrderPage=Math.Clamp(_longOrderPage,0,Math.Max(0,(AllLongOrders.Length-1)/8));return AllLongOrders.Skip(_longOrderPage*8).Take(8).ToArray();}}
    private static Rectangle LongOrderPageButton(int direction)=>new(direction<0?610:1160,940,150,42);
    private void LongPairInput(bool click,bool escape)
    {
        if(!DomesticOperationsAllowed){CloseDirectDelivery();return;}
        if(escape || click && DirectPairBack.Contains(_pointer)){_directPairOpen=false;_directPairClosing=false;_directDrag=null;_longSelected=null;return;}
        if(!click)return;SyncLongSlots();
        foreach(int direction in new[]{-1,1})if(LongOrderPageButton(direction).Contains(_pointer)){_longOrderPage=Math.Clamp(_longOrderPage+direction,0,Math.Max(0,(AllLongOrders.Length-1)/8));return;}
        for(int side=0;side<2;side++)foreach(int direction in new[]{-1,1})if(LongPageButton(side,direction).Contains(_pointer)){
            _directPages[side]=Math.Clamp(_directPages[side]+direction,0,Math.Max(0,(_directSlots[side].Length-1)/12));return;
        }
        var orders=VisibleLongOrders;for(int i=0;i<orders.Length;i++)if(LongOrderButton(i).Contains(_pointer)){
            _setup.Robots.LongDelivery.Cancel(_setup.Robots,orders[i].Number);_directMessage="長距離便を取り消しました / 荷物は現在地点に残ります";return;
        }
        for(int side=0;side<2;side++)for(int slot=_directPages[side]*12;slot<Math.Min(_directSlots[side].Length,(_directPages[side]+1)*12);slot++)if(DirectCell(side,slot).Contains(_pointer)){
            var robot=_setup.Robots.Nodes[_directNodes[side]].Robots.FirstOrDefault(r=>r.Id==_directSlots[side][slot]);
            if(side==0){
                if(robot==null)return;
                var parts=robot.Parts;for(int row=0;row<3;row++)if(DirectPart(0,slot,row).Contains(_pointer) && (robot.Parts&HangarPart(row))!=0)parts=robot.ConnectedParts(HangarPart(row));
                var existing=_setup.Robots.LongDelivery.Orders.FirstOrDefault(o=>o.CargoId==robot.Id && o.Node==_directNodes[0] && (o.Parts&parts)!=0);
                if(existing!=null){_setup.Robots.LongDelivery.Cancel(_setup.Robots,existing.Number);_directMessage="長距離便の予約を取り消しました";return;}
                if((!_setup.Robots.CanShip(_directNodes[0],robot) && !_setup.Robots.LongDelivery.HoldsSource(_directNodes[0],robot.Id)) || !CanManageNodeFactory(robot.Owner)){_directMessage="予約中・他国所有のパーツは出荷できません";return;}
                _longSelected=(robot.Id,parts);_directMessage="右の受取区画を選んでください / 区画の余白を選ぶと個体全体を出荷";return;
            }
            if(!_longSelected.HasValue){_directMessage="先に左の出荷するパーツを選んでください";return;}
            try{
                var selected=_longSelected.Value;var order=_setup.Robots.LongDelivery.Reserve(_setup,_directNodes[0],_directNodes[1],selected.Id,selected.Parts,robot?.Id,_setup.PlayerSlot);
                _directMessage=$"長距離便 #{order.Number} を予約しました / 配送ごとに１地点ずつ進みます";_longSelected=null;_longOrderPage=Math.Max(0,(AllLongOrders.Length-1)/8);
                _operations.Write("long_delivery_reserved",order);
            }catch(InvalidOperationException error){_directMessage=error.Message;}
            return;
        }
    }
    private void DrawLongPair()
    {
        SyncLongSlots();_ui.Box(new(0,0,1920,1080),new Color(14,29,40));
        _ui.Text("ロボットパーツ長距離便",new(36,26),.85f,Cream);_ui.Button(DirectPairBack,"×",Muted,.55f);
        _ui.Center(_directMessage,new(40,90,1840,42),.4f,Cream);
        for(int side=0;side<2;side++){
            int node=_directNodes[side],x=20+side*1320;var store=_setup.Robots.Nodes[node];
            _ui.Box(new(x,146,560,820),new Color(27,49,62));_ui.Text(side==0?"元地点 / 出荷するパーツを選択":"先地点 / 受取区画を選択",new(x+20,164),.48f,Cream);
            _ui.Center(_setup.Nodes.DisplayName(_setup.Nodes.All[node]),new(x+12,210,536,30),.32f,Cream);
            _ui.Button(LongPageButton(side,-1),"前",Muted,.4f);_ui.Button(LongPageButton(side,1),"次",Muted,.4f);
            _ui.Center($"{store.Count}/{store.TotalCapacity}枠　{_directPages[side]+1}/{Math.Max(1,(_directSlots[side].Length+11)/12)}ページ",new(x+140,250,270,30),.31f,Cream);
            for(int slot=_directPages[side]*12;slot<Math.Min(_directSlots[side].Length,(_directPages[side]+1)*12);slot++){
                var cell=DirectCell(side,slot);var robot=store.Robots.FirstOrDefault(r=>r.Id==_directSlots[side][slot]);
                _ui.Box(cell,new Color(10,23,33));if(robot==null){_ui.Center("空き",cell,.35f,Muted);continue;}
                DrawRobotGlyph(new(cell.X+48,cell.Y+8,68,102),robot,whiteOutline:true);
                _ui.Center(robot.LongDeliveryNumber.HasValue?$"長距離便 #{robot.LongDeliveryNumber}":$"ID {robot.Id}",new(cell.X,cell.Bottom-34,cell.Width,28),.3f,robot.LongDeliveryNumber.HasValue?Accent:Cream);
                if(side==0 && _longSelected?.Id==robot.Id)for(int row=0;row<3;row++)if((_longSelected.Value.Parts&HangarPart(row))!=0){var r=DirectPart(side,slot,row);_ui.Box(new(r.X-3,r.Y,r.Width+6,3),Accent);_ui.Box(new(r.X-3,r.Bottom,r.Width+6,3),Accent);}
            }
        }
        var path=RobotLongDelivery.Path(_setup,_directNodes[0],_directNodes[1],_setup.PlayerSlot);
        _ui.Center($"配送距離 {Math.Max(0,path.Length-1)} 地点 / 最短 {Math.Max(0,path.Length-1)} 全体ターン",new(600,182,720,40),.44f,Cream);
        _ui.Center("予約一覧：クリックで取り消す / 荷物は現在地点に残る",new(600,228,720,30),.32f,Cream);
        var orders=VisibleLongOrders;for(int i=0;i<orders.Length;i++){
            var o=orders[i];var r=LongOrderButton(i);_ui.Box(r,Muted);
            _ui.Text($"#{o.Number}　地点{o.Source+1} → {o.Target+1}　現在 {o.Node+1}",new(r.X+12,r.Y+5),.32f,Cream);
            _ui.Text(o.Status,new(r.X+12,r.Y+32),.29f,Cream);
        }
        int rowIndex=0;foreach(var result in _setup.Robots.LongDelivery.Results.Where(o=>o.Commander==_setup.PlayerSlot).TakeLast(2)){
            _ui.Center($"#{result.Number} {result.Status}",new(600,828+rowIndex++*40,720,36),.3f,Cream);
        }
        _ui.Button(LongOrderPageButton(-1),"前",Muted,.4f);_ui.Button(LongOrderPageButton(1),"次",Muted,.4f);
        _ui.Center($"予約 {_longOrderPage+1}/{Math.Max(1,(AllLongOrders.Length+7)/8)} ページ",new(780,946,360,32),.33f,Cream);
        _ui.Center("満杯なら現在地点で待機 / 経路がなくなれば荷物を残して予約解除",new(40,998,1840,40),.4f,Cream);
    }
}
