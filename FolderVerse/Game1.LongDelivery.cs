namespace FolderVerse;
using System;
using System.Linq;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

public partial class Game1
{
    private bool _longDeliveryMode,_longChoosingSource,_longOrdersOpen;
    private int _longSource=-1,_longTarget=-1,_longOrderPage;
    private readonly List<(Rectangle Bounds,int Node)> _longMarkers=new();
    private readonly HashSet<int> _longSourceCandidates=new();
    private static readonly Rectangle LongOrdersToggle=new(610,968,700,36);
    private static Rectangle LongCancelButton(int side)=>new(40+side*1320,242,150,32);
    private static Rectangle LongPageButton(int side,int direction)=>new(220+side*1320+(direction<0?0:222),242,120,32);
    private static Rectangle LongOrderButton(int index)=>new(610,270+index*65,700,58);
    private static Rectangle LongOrderPageButton(int direction)=>new(direction<0?610:1160,840,150,42);
    private LongPartOrder[] AllLongOrders=>_setup.Robots.LongDelivery.Orders.Where(o=>o.Commander==_setup.PlayerSlot).ToArray();
    private LongPartOrder[] VisibleLongOrders{get{_longOrderPage=Math.Clamp(_longOrderPage,0,Math.Max(0,(AllLongOrders.Length-1)/8));return AllLongOrders.Skip(_longOrderPage*8).Take(8).ToArray();}}
    private void OpenLongDeliveryMap(){OpenDirectMap();if(!_directMap)return;_longDeliveryMode=true;_longSource=_longTarget=-1;_longChoosingSource=_longOrdersOpen=false;_longOrderPage=0;}
    private bool LongDestination(int node)=>DirectNodeAllowed(node) && !_setup.Campaign.RobotBattles.LockedNode(node) && (_longTarget<0 || _longSourceCandidates.Contains(node));
    private void FindLongSources(int target)
    {
        _longSourceCandidates.Clear();_longSourceCandidates.Add(target);var queue=new Queue<int>();queue.Enqueue(target);
        while(queue.TryDequeue(out int node))foreach(int next in RobotLongDelivery.Neighbors(_setup,node,_setup.Nodes.All[target].Owner))if(_longSourceCandidates.Add(next))queue.Enqueue(next);
    }
    private void DrawLongMarkers()
    {
        _longMarkers.Clear();if(!_directMap || _directPairOpen || MapTransitionActive || !_statusGlobe && _net.IsAnimating)return;
        foreach(var node in _setup.Nodes.All.Where(n=>LongDestination(n.Id))){
            if(!OperationPosition(node.Id,out var at) || !MapViewBounds.Contains(at.ToPoint()))continue;
            var bounds=new Rectangle((int)at.X-18,(int)at.Y-18,36,36);_ui.Box(bounds,Accent);
            DrawPrimaryIcon(LongMapAction,new(bounds.X+3,bounds.Y+3,30,30),new Color(14,29,40));_longMarkers.Add((bounds,node.Id));
        }
        _ui.Center("パーツ長距離便：まず右インスペクターの移動先を選んでください",new(54,110,1300,40),.43f,Cream);
    }
    private bool LongMapClick(Point point)
    {
        if(!_directMap || _directPairOpen || MapTransitionActive || !MapViewBounds.Contains(point))return false;
        var hit=_longMarkers.Where(m=>m.Bounds.Contains(point)).OrderBy(m=>Vector2.DistanceSquared(m.Bounds.Center.ToVector2(),point.ToVector2())).FirstOrDefault();
        if(hit.Bounds!=Rectangle.Empty){
            _longTarget=hit.Node;_longSource=-1;_longChoosingSource=true;
            FindLongSources(hit.Node);
            OpenDirectPair(hit.Node,hit.Node);_directPages[0]=_directPages[1]=0;
            if(!_directPairGlobe)_directPairNet.FitAll(DirectPairMapBounds);
            _directMessage="移動先を選びました / 中央の地図から左の元地点を選んでください";return true;
        }
        if(!_statusGlobe && !_net.IsAnimating){_net.HoverEdge(NetPanel,point);if(_net.ClickEdge())_longMarkers.Clear();}return true;
    }
    private void DrawLongPairMarkers(Rectangle panel)
    {
        _longMarkers.Clear();if(!_longChoosingSource || _longOrdersOpen || _directPairSlide<1 || _directPairNet.IsAnimating)return;
        foreach(var node in _setup.Nodes.All.Where(n=>n.Id!=_longTarget && LongDestination(n.Id))){
            if(!DirectPairVisible(node.Id,out var at) || !panel.Contains(at.ToPoint()))continue;
            var r=new Rectangle((int)at.X-12,(int)at.Y-12,24,24);_ui.Box(r,Accent);DrawPrimaryIcon(LongMapAction,new(r.X+2,r.Y+2,20,20),new Color(14,29,40));_longMarkers.Add((r,node.Id));
        }
    }
    private void LongBack()
    {
        _directDrag=null;
        if(_longOrdersOpen){_longOrdersOpen=false;return;}
        if(!_longChoosingSource){_longChoosingSource=true;_longSource=-1;_directNodes[0]=_longTarget;Array.Clear(_directSlots[0]);_directMessage="移動先を残しました / 元地点を選び直してください";}
        else{_directPairOpen=_directPairClosing=false;_longTarget=_longSource=-1;_longChoosingSource=false;_directMessage="移動先を選び直してください";}
    }
    private bool LongPageClick(Point point)
    {
        for(int side=_longChoosingSource?1:0;side<2;side++)foreach(int direction in new[]{-1,1})if(LongPageButton(side,direction).Contains(point)){
            _directDrag=null;
            _directPages[side]=Math.Clamp(_directPages[side]+direction,0,Math.Max(0,(_directSlots[side].Length-1)/HangarSlotsPerPage));return true;
        }
        return false;
    }
    private void SyncLongSlots()
    {
        for(int side=0;side<2;side++){
            if(side==0 && _longChoosingSource)continue;int node=_directNodes[side];var store=_setup.Robots.Nodes[node];Array.Resize(ref _directSlots[side],store.TotalCapacity);_directPages[side]=Math.Clamp(_directPages[side],0,Math.Max(0,(store.TotalCapacity-1)/HangarSlotsPerPage));
            for(int slot=0;slot<_directSlots[side].Length;slot++)if(_directSlots[side][slot].HasValue && !store.Robots.Any(r=>r.Id==_directSlots[side][slot]))_directSlots[side][slot]=null;
            foreach(var robot in store.Robots)if(!_directSlots[side].Contains(robot.Id)){
                int slot=Enumerable.Range(0,_directSlots[side].Length).FirstOrDefault(i=>!_directSlots[side][i].HasValue && _setup.Robots.LongDelivery.SlotArrival(node,i,robot.Owner)==robot.Id,-1);
                if(slot<0)slot=Enumerable.Range(0,_directSlots[side].Length).FirstOrDefault(i=>!_directSlots[side][i].HasValue && !_setup.Robots.LongDelivery.Orders.Any(o=>o.Target==node && !o.Receiver.HasValue && o.TargetSlot==i),-1);
                if(slot<0)slot=Array.IndexOf(_directSlots[side],null);if(slot>=0)_directSlots[side][slot]=robot.Id;
            }
        }
    }
    private void LongPairInput(MouseState mouse,bool click,bool escape)
    {
        if(!DomesticOperationsAllowed){CloseDirectDelivery();return;}
        if(escape || click && (DirectPairBack.Contains(_pointer) || LongCancelButton(1).Contains(_pointer) || !_longChoosingSource && LongCancelButton(0).Contains(_pointer))){LongBack();return;}
        if(click && LongOrdersToggle.Contains(_pointer)){_longOrdersOpen=!_longOrdersOpen;_directDrag=null;return;}
        if(_longOrdersOpen){
            if(!click)return;foreach(int direction in new[]{-1,1})if(LongOrderPageButton(direction).Contains(_pointer)){_longOrderPage=Math.Clamp(_longOrderPage+direction,0,Math.Max(0,(AllLongOrders.Length-1)/8));return;}
            var orders=VisibleLongOrders;for(int i=0;i<orders.Length;i++)if(LongOrderButton(i).Contains(_pointer)){_setup.Robots.LongDelivery.Cancel(_setup.Robots,orders[i].Number);_directMessage="長距離便を取り消しました / 荷物は現在地点に残ります";return;}return;
        }
        if(click && LongPageClick(_pointer))return;
        if(_longChoosingSource){
            if(click){var hit=_longMarkers.Where(m=>m.Bounds.Contains(_pointer)).OrderBy(m=>Vector2.DistanceSquared(m.Bounds.Center.ToVector2(),_pointer.ToVector2())).FirstOrDefault();
                if(hit.Bounds!=Rectangle.Empty){_longSource=hit.Node;_directNodes[0]=hit.Node;_longChoosingSource=false;Array.Clear(_directSlots[0]);FitDirectPairMap();_directMessage="左のパーツを右の格納庫へドラッグ＆ドロップしてください";}
            }
            return;
        }
        SyncLongSlots();

        if(click && !_directDrag.HasValue)for(int side=0;side<2;side++)foreach(int slot in VisibleHangarSlots(_directSlots[side].Length,_directPages[side]))if(DirectCell(side,slot).Contains(_pointer)){
            int node=_directNodes[side];var robot=_setup.Robots.Nodes[node].Robots.FirstOrDefault(r=>r.Id==_directSlots[side][slot]);
            RobotParts part=0;for(int row=0;row<3;row++)if(DirectPart(side,slot,row).Contains(_pointer))part=HangarPart(row);
            var pending=_setup.Robots.LongDelivery.Orders.FirstOrDefault(o=>(part==0 || (o.Parts&part)!=0) && (side==0?o.Node==node && o.CargoId==robot?.Id:o.Target==node && (o.Receiver.HasValue?o.Receiver==robot?.Id:o.TargetSlot==slot)));
            if(pending!=null){_setup.Robots.LongDelivery.Cancel(_setup.Robots,pending.Number);_directMessage="長距離便の予約を取り消しました";return;}
            if(side!=0 || robot==null)return;
            if((!_setup.Robots.CanShip(node,robot) && !_setup.Robots.LongDelivery.HoldsSource(node,robot.Id)) || !CanManageNodeFactory(robot.Owner)){_directMessage="予約中・他国所有のパーツは出荷できません";return;}
            if(part==0)part=robot.Parts;if((robot.Parts&part)==0)return;_directDrag=(0,robot.Id,robot.ConnectedParts(part));return;
        }
        if(!_directDrag.HasValue || mouse.LeftButton!=ButtonState.Released || _previousMouse.LeftButton!=ButtonState.Pressed)return;
        var drag=_directDrag.Value;_directDrag=null;
        foreach(int slot in VisibleHangarSlots(_directSlots[1].Length,_directPages[1]))if(DirectCell(1,slot).Contains(_pointer)){
            try{var order=_setup.Robots.LongDelivery.Reserve(_setup,_directNodes[0],_directNodes[1],drag.Robot,drag.Part,_directSlots[1][slot],_setup.PlayerSlot,slot);_directMessage=$"長距離便 #{order.Number} を予約しました / 配送ごとに１地点ずつ進みます";_operations.Write("long_delivery_reserved",order);}
            catch(InvalidOperationException error){_directMessage=error.Message;}return;
        }
        _directMessage="ドラッグを取り消しました";
    }
    private LongPartOrder[] LongIncoming(int node,int slot,Robot robot)=>_setup.Robots.LongDelivery.Orders.Where(o=>o.Target==node && (o.Receiver.HasValue?o.Receiver==robot?.Id:o.TargetSlot==slot)).ToArray();
    private void DrawLongPane(int side)
    {
        int node=_directNodes[side],x=20+side*1320;var store=_setup.Robots.Nodes[node];var color=DirectSideColor(side);
        _ui.Box(new(x,146,560,820),new Color(27,49,62));_ui.Box(new(x,146,560,6),color);
        _ui.Text(side==0?"長距離便 左インスペクター / 元地点":"長距離便 右インスペクター / 移動先",new(x+20,164),.46f,color);
        _ui.Center(_setup.Nodes.DisplayName(_setup.Nodes.All[node]),new(x+12,206,536,34),.34f,Cream);
        _ui.Button(LongCancelButton(side),"キャンセル",Muted,.38f);_ui.Button(LongPageButton(side,-1),"前",Muted,.4f);_ui.Button(LongPageButton(side,1),"次",Muted,.4f);
        _ui.Text($"格納庫 {store.Count}/{store.TotalCapacity}　{_directPages[side]+1}/{Math.Max(1,(_directSlots[side].Length+HangarSlotsPerPage-1)/HangarSlotsPerPage)}ページ",new(x+20,278),.28f,Cream);
        foreach(int slot in VisibleHangarSlots(_directSlots[side].Length,_directPages[side])){
            var cell=DirectCell(side,slot);var robot=store.Robots.FirstOrDefault(r=>r.Id==_directSlots[side][slot]);var incoming=LongIncoming(node,slot,robot);
            _ui.Box(cell,_directDrag.HasValue && side==1 && cell.Contains(_pointer)?new Color(65,100,115):new Color(18,35,46));
            for(int row=0;row<3;row++)_ui.Box(DirectPart(side,slot,row),new Color(40,63,76));
            var glyph=new Rectangle(cell.X+6,cell.Y+8,90,180);
            if(robot!=null)DrawLongDeliveryRobot(glyph,robot,partOpacity:p=>_setup.Robots.LongDelivery.Orders.Any(o=>o.CargoId==robot.Id && o.Node==node && (o.Parts&p)!=0)?.35f:1);
            foreach(var order in incoming){var cargo=_setup.Robots.Nodes[order.Node].Robots.FirstOrDefault(r=>r.Id==order.CargoId);if(cargo!=null && (cargo.Parts&order.Parts)==order.Parts)DrawLongDeliveryRobot(glyph,cargo.Slice(order.Parts),opacity:.5f);}
            for(int row=0;row<3;row++){
                var part=HangarPart(row);bool arrival=incoming.Any(o=>(o.Parts&part)!=0);
                bool outgoing=robot!=null && _setup.Robots.LongDelivery.Orders.Any(o=>o.Node==node && o.CargoId==robot.Id && (o.Parts&part)!=0);
                if(arrival)DrawDirectDeliveryTag(DirectPart(side,slot,row),"入荷予定");
                if(outgoing)DrawDirectDeliveryTag(DirectPart(side,slot,row),"出荷予定",arrival);
            }
            if(robot!=null)_ui.Center($"ID {robot.Id}",new(cell.X,cell.Bottom-32,cell.Width,26),.3f,Cream);
        }
    }
    private void DrawLongPair()
    {
        SyncLongSlots();_deliveryTagHint="";_ui.Box(new(0,0,1920,1080),new Color(14,29,40));DrawDirectPairMap();
        _ui.Text("ロボットパーツ長距離便",new(36,26),.85f,Cream);_ui.Button(DirectPairBack,"×",Muted,.55f);_ui.Center(_directMessage,new(40,76,1840,32),.4f,Cream);
        if(!_longChoosingSource)DrawLongPane(0);DrawLongPane(1);
        if(_longOrdersOpen){_ui.Box(new(600,220,720,670),new Color(20,42,54));var orders=VisibleLongOrders;
            for(int i=0;i<orders.Length;i++){var order=orders[i];var r=LongOrderButton(i);_ui.Box(r,Muted);_ui.Text($"#{order.Number} 地点{order.Source+1} → {order.Target+1} / 現在 {order.Node+1}",new(r.X+12,r.Y+5),.32f,Cream);_ui.Text(order.Status,new(r.X+12,r.Y+32),.29f,Cream);}
            _ui.Center("予約をクリックして取消 / 荷物は現在地点に残ります",new(610,224,700,34),.3f,Cream);_ui.Button(LongOrderPageButton(-1),"前",Muted,.4f);_ui.Button(LongOrderPageButton(1),"次",Muted,.4f);
        }
        _ui.Button(LongOrdersToggle,$"配送予約 {AllLongOrders.Length}件 / {(_longOrdersOpen?"地図に戻る":"一覧と取消")}",Muted,.4f);
        if(_directDrag.HasValue){var drag=_directDrag.Value;var robot=_setup.Robots.Nodes[_directNodes[0]].Robots.FirstOrDefault(r=>r.Id==drag.Robot);if(robot!=null)DrawLongDeliveryRobot(new(_pointer.X-34,_pointer.Y-51,68,102),robot.Slice(drag.Part),opacity:.8f);}
        else DrawDeliveryTagHint(new(20,146,1880,820));
        _ui.Center("１全体ターンに１地点 / 満杯なら待機 / 経路が切れたら荷物を残して解除",new(40,1012,1840,40),.37f,Cream);
    }
    private void DrawLongDeliveryRobot(Rectangle bounds,Robot robot,float opacity=1,Func<RobotParts,float> partOpacity=null)
    {
        bool materials=_setup.EquipmentMode==RobotEquipmentMode.Materials;
        DrawRobotGlyphCore(bounds,robot,true,opacity,partOpacity,showIntegratedMaterial:materials,allowCaptain:materials);
    }
}
