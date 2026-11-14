namespace FolderVerse;
using System;
using System.Linq;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Graphics;

public partial class Game1
{
    private bool _directMap,_directPairOpen,_directRoutesBefore;
    private readonly int[] _directNodes=new int[2];
    private readonly long?[][] _directSlots={new long?[12],new long?[12]};
    private readonly List<(Rectangle Bounds,int A,int B)> _directTrucks=new();
    private (int Side,long Robot,RobotParts Part)? _directDrag;
    private string _directMessage="";
    private static readonly Rectangle DirectPairBack=new(932,110,56,48);
    private static Rectangle DirectCell(int side,int slot)=>new(40+side*1320+(slot%HangarSlotsPerPage)%3*174,300+(slot%HangarSlotsPerPage)/3*290,164,280);
    private static Rectangle DirectPart(int side,int slot,int row){var r=DirectCell(side,slot);return new(r.X+6,r.Y+8+row*60,90,58);}
    private void DrawDirectDeliveryTag(Rectangle part,string label,bool second=false){if(second)part.Offset(0,26);DrawHangarDeliveryTag(part,label,gap:2);}
    private RobotParts DirectHoverPart(int side,int slot)
    {
        int node=_directNodes[side];long? id=_directSlots[side][slot];var robot=PlanningStock.Nodes[node].Robots.FirstOrDefault(r=>r.Id==id);
        for(int row=0;row<3;row++)if(DirectViewPart(side,slot,row).Contains(_pointer)){
            var part=HangarPart(row);
            if(robot!=null && (robot.Parts&part)!=0 || _setup.Robots.DirectDelivery.Orders.Any(o=>o.Target==node && (o.Part&part)!=0 && (o.TargetRobotId.HasValue?o.TargetRobotId==id:o.TargetSlot==slot)))return part;
        }
        return 0;
    }
    private bool DirectNodeAllowed(int node)=>node>=0 && PlanningFunctional(node) && CanManageNodeFactory(_setup.Nodes.All[node].Owner);
    private bool DirectSeaRoute(int a,int b)
    {
        var node=_setup.Nodes.All[a];var next=_setup.Nodes.All[b];
        var route=_setup.Routes.Neighbors(node).FirstOrDefault(r=>_setup.Nodes.At(r.Target,r.Entry)?.Id==b);
        return route?.Portal?.Terrain==TravelTerrain.Sea || !_setup.Routes.Land(node.Cell,node.Center) || !_setup.Routes.Land(next.Cell,next.Center);
    }
    private void CloseDirectDelivery(){_longDeliveryMode=false;_longSource=-1;if(_directMap)_net.ShowRoutes=_directRoutesBefore;_directMap=_directPairOpen=_directPairClosing=false;_directDrag=null;_directMaterialDrag=null;_directPanSide=-1;_directTrucks.Clear();_directPairTrucks.Clear();}
    private void OpenDirectMap(){if(!DomesticOperationsAllowed)return;ResetInspector();_directRoutesBefore=_net.ShowRoutes;_net.ShowRoutes=true;_directMap=true;_statusMenuOpen=false;_directMessage="";}
    private void OpenDirectPair(int a,int b){
        if(!DomesticOperationsAllowed)return;
        if(!DirectNodeAllowed(a) || !DirectNodeAllowed(b) || _setup.Campaign.RobotBattles.LockedNode(a) || _setup.Campaign.RobotBattles.LockedNode(b) || _setup.Campaign.RobotBattles.LockedEdge(a,b))return;
        bool switching=_directPairOpen;BeginDirectPairTransition(switching);_directNodes[0]=a;_directNodes[1]=b;Array.Clear(_directPages);Array.Fill(_directZoom,1);Array.Clear(_directColumn);Array.Clear(_directRow);_directPanSide=-1;_directMaterialDrag=null;foreach(var slots in _directSlots)Array.Clear(slots);foreach(var positions in _directMaterialPositions)positions.Clear();_directPairOpen=true;_directPairClosing=false;_directPairSlide=0;if(!switching)PrepareDirectPairMap();FitDirectPairMap();SyncDirectSlots();_directDrag=null;_directMessage="表示は入荷・組替後の予定 / 操作は予約 / 上のボタンで直前の予約を取消";
    }
    private void SyncDirectSlots()
    {
        for(int side=0;side<2;side++){
            int node=_directNodes[side];
            var store=PlanningStock.Nodes[node];
            Array.Resize(ref _directSlots[side],store.TotalCapacity);
            ClampDirectScroll(side);
            SyncHangarOccupants(_directSlots[side],store,slot=>_setup.Robots.DirectDelivery.Orders.Any(o=>o.Target==node && o.TargetRobotId==null && o.TargetSlot==slot));
        }
    }
    private bool DirectMapClick(Point p){
        if(_longDeliveryMode)return LongMapClick(p);
        if(!_directMap || _directPairOpen || MapTransitionActive)return false;
        if(MapViewButton.Contains(p) || !MapViewBounds.Contains(p))return false;
        if(!_statusGlobe){
            if(_net.IsAnimating)return true;
            _net.HoverEdge(NetPanel,p);
            if(_net.ClickEdge()){_directTrucks.Clear();RecordTutorialMapEdge();return true;}
        }
        var hit=_directTrucks.Where(t=>t.Bounds.Contains(p)).OrderBy(t=>Vector2.DistanceSquared(t.Bounds.Center.ToVector2(),p.ToVector2())).FirstOrDefault();if(hit.Bounds!=Rectangle.Empty){OpenDirectPair(hit.A,hit.B);return true;}return _directMap;
    }
    private void DrawDirectTrucks(){
        if(_longDeliveryMode){DrawLongMarkers();return;}
        _directTrucks.Clear();if(!_directMap || _directPairOpen || MapTransitionActive || !_statusGlobe && _net.IsAnimating)return;
        foreach(var node in _setup.Nodes.All.Where(n=>DirectNodeAllowed(n.Id))){
            if(!OperationPosition(node.Id,out var a))continue;
            foreach(int destination in FunctionalDeliveryRoutes.Targets(_setup,node.Id)){var next=_setup.Nodes.All[destination];if(node.Id>=next.Id || !DirectNodeAllowed(next.Id) || !OperationPosition(next.Id,out var b))continue;
                var at=(a+b)/2;if(!MapViewBounds.Contains(at.ToPoint()))continue;
                var bounds=new Rectangle((int)at.X-22,(int)at.Y-17,44,34);bool enabled=!_setup.Campaign.RobotBattles.LockedNode(node.Id) && !_setup.Campaign.RobotBattles.LockedNode(next.Id) && !_setup.Campaign.RobotBattles.LockedEdge(node.Id,next.Id);
                DrawDirectTruck(node.Id,next.Id,a,b,bounds,enabled);
                if(enabled)_directTrucks.Add((bounds,node.Id,next.Id));
            }
        }
    }
    private void DrawDirectTruck(int first,int second,Vector2 a,Vector2 b,Rectangle bounds,bool enabled){
        var at=bounds.Center.ToVector2();
        var color=enabled?(bounds.Contains(_pointer)?Cream:Accent):Muted;var delta=b-a;_ui.Tile(at,new(delta.Length(),2),MathF.Atan2(delta.Y,delta.X),color*.4f);
        if(DirectSeaRoute(first,second)){
            _ui.Box(new(bounds.X+7,bounds.Y+7,29,14),color);
            _ui.Box(new(bounds.X+26,bounds.Y+2,9,7),Cream);
            for(int x=11;x<34;x+=8)_ui.Box(new(bounds.X+x,bounds.Y+10,5,5),new Color(125,196,222));
            for(int row=0;row<8;row++)_ui.Box(new(bounds.X+row,bounds.Y+21+row,44-row*2,1),color);
            _ui.Box(new(bounds.X+4,bounds.Y+31,15,2),new Color(125,196,222));_ui.Box(new(bounds.X+25,bounds.Y+31,15,2),new Color(125,196,222));
        }else{
            _ui.Box(new(bounds.X,bounds.Y+3,27,20),color);_ui.Box(new(bounds.X+29,bounds.Y+8,13,15),color);_ui.Box(new(bounds.X+31,bounds.Y+10,8,7),new Color(125,196,222));_ui.Box(new(bounds.X+5,bounds.Y+23,9,8),new Color(14,22,29));_ui.Box(new(bounds.X+30,bounds.Y+23,9,8),new Color(14,22,29));
        }
        int count=_setup.Robots.DirectDelivery.Orders.Count(o=>o.Source==first && o.Target==second || o.Source==second && o.Target==first)+_setup.Robots.PartPlanning.Orders.Count(o=>PlanningMoveSource(o)==first && o.Target==second || PlanningMoveSource(o)==second && o.Target==first)
            +_setup.Robots.MapleShipments.Count(o=>o.Source==first && o.Target==second || o.Source==second && o.Target==first);
        if(count>0)_ui.Text(count.ToString(),new(bounds.X+15,bounds.Y-13),.35f,Cream);
        DrawDirectTruckCheck(first,second,bounds);
    }
    private void DirectPairInput(MouseState mouse,bool click,bool escape){
        if(_longDeliveryMode){LongPairInput(mouse,click,escape);return;}
        if(!DomesticOperationsAllowed){_directDrag=null;CloseDirectDelivery();return;}
        if(escape || click && !_directDrag.HasValue && !_directMaterialDrag.HasValue && DirectPairBack.Contains(_pointer)){_directMaterialDrag=null;_directPanSide=-1;CloseDirectPair();return;}
        if(_directPairClosing || _directPairSlide<1)return;
        if(DirectHangarNavigation(mouse))return;
        if(DirectMaterialInput(mouse,click))return;
        if(click && new Rectangle(770,62,380,44).Contains(_pointer)){UndoHangarPlan();_directMessage=_hangarMessage;return;}
        if(click && !_directDrag.HasValue && DirectPageClick(_pointer))return;
        if(click && !_directDrag.HasValue && DirectPairCheckButton.Contains(_pointer)){ToggleDirectPairCheck();return;}
        if(click && !_directDrag.HasValue && DirectPairTruckClick(_pointer))return;
        SyncDirectSlots();
        if(click){for(int side=0;side<2;side++)foreach(int slot in DirectVisibleSlots(side))for(int row=0;row<3;row++)if(DirectHoverPart(side,slot)==HangarPart(row)){
            int node=_directNodes[side];var part=HangarPart(row);long? id=_directSlots[side][slot];
            var existing=_setup.Robots.DirectDelivery.Orders.LastOrDefault(o=>o.Source==node && o.RobotId==id && (o.Part&part)!=0 || o.Target==node && (o.Part&part)!=0 && (o.TargetRobotId.HasValue?o.TargetRobotId==id:o.TargetSlot==slot));
            if(existing!=null){_setup.Robots.DirectDelivery.Cancel(existing.Number);_directMessage="直送便の予約を取り消しました";return;}
            var robot=PlanningStock.Nodes[node].Robots.FirstOrDefault(r=>r.Id==id);if(robot!=null && (robot.Parts&part)!=0 && CanManageNodeFactory(robot.Owner) && !_setup.Campaign.RobotBattles.LockedNode(node) && !PlanningStock.PartLocked(node,robot,part))_directDrag=(side,robot.Id,robot.ConnectedParts(part));else _directMessage="そのパーツは操作できません";return;
        }
        for(int side=0;side<2;side++)foreach(int slot in DirectVisibleSlots(side))if(DirectViewCell(side,slot).Contains(_pointer)){
            int node=_directNodes[side];var robot=PlanningStock.Nodes[node].Robots.FirstOrDefault(r=>r.Id==_directSlots[side][slot]);
            if(robot!=null && CanManageNodeFactory(robot.Owner) && !_setup.Campaign.RobotBattles.LockedNode(node) && !PlanningStock.AssemblyLocked(node,robot))_directDrag=(side,robot.Id,(RobotParts)0);
            else _directMessage="その区画は操作できません / パーツ単位での操作を確認してください";
            return;
        }}
        if(!_directDrag.HasValue || mouse.LeftButton!=ButtonState.Released || _previousMouse.LeftButton!=ButtonState.Pressed)return;
        var drag=_directDrag.Value;_directDrag=null;
        for(int side=0;side<2;side++)foreach(int slot in DirectVisibleSlots(side))if(DirectViewCell(side,slot).Contains(_pointer)){
            try{int source=_directNodes[drag.Side],target=_directNodes[side];var destination=_directSlots[side][slot];
                var parts=drag.Part==0?PlanningStock.Nodes[source].Robots.Single(r=>r.Id==drag.Robot).Parts:drag.Part;
                var cargo=DirectStoredMaterials(side);
                if(Enumerable.Range(0,3).Any(row=>(parts&HangarPart(row))!=0 && MaterialOccupies(cargo,slot,row)))throw new InvalidOperationException("Material occupies this section.");
                if(side==drag.Side){if(destination==drag.Robot)return;if(!destination.HasValue && _setup.Robots.DirectDelivery.Orders.Any(o=>o.Target==target && o.TargetRobotId==null && o.TargetSlot==slot))throw new InvalidOperationException("Arrival slot reserved.");
                    if(drag.Part==0){if(destination.HasValue && PlanningStock.AssemblyLocked(source,PlanningStock.Nodes[source].Robots.Single(r=>r.Id==destination)))throw new InvalidOperationException("Robot is reserved.");int from=Array.IndexOf(_directSlots[side],(long?)drag.Robot);_directSlots[side][from]=destination;_directSlots[side][slot]=drag.Robot;}
                    else {long id=_setup.Robots.PartPlanning.Reserve(_setup,source,target,drag.Robot,drag.Part,destination,_setup.PlayerSlot);_directSlots[side][slot]=id;}_directMessage="格納庫内の組替を予約しました";}
                else {var robot=PlanningStock.Nodes[source].Robots.Single(r=>r.Id==drag.Robot);_directSlots[side][slot]=_setup.Robots.PartPlanning.Reserve(_setup,source,target,drag.Robot,drag.Part==0?robot.Parts:drag.Part,destination,_setup.PlayerSlot);BeginWorkReservationEffect(robot,robot.Owner);_directMessage="直送便を予約しました / 配送時に実施";}SyncDirectSlots();}
            catch(InvalidOperationException){_directMessage="所有国・予約・同じ部位・空き枠を確認してください";}return;
        }
    }
    private void DrawDirectPair(){
        if(_longDeliveryMode){DrawLongPair();return;}
        _deliveryTagHint="";
        SyncDirectSlots();_ui.Box(new(0,0,1920,1080),new Color(14,29,40));
        DrawDirectPairMap();
        DrawDepartingDirectControls();
        var canvas=CanvasBounds();var transform=Matrix.CreateScale(canvas.Width/1920f,canvas.Height/1080f,1)*Matrix.CreateTranslation(canvas.X,canvas.Y,0);
        for(int side=0;side<2;side++){
            int shift=(int)((1-DirectPairEase)*600)*(side==0?-1:1);
            _spriteBatch.End();_spriteBatch.Begin(transformMatrix:Matrix.CreateTranslation(shift,0,0)*transform);
            var color=DirectSideColor(side);int x=20+side*1320;
            _ui.Box(new(x,146,560,820),new Color(27,49,62));_ui.Box(new(x,146,560,6),color);
            int node=_directNodes[side];var store=PlanningStock.Nodes[node];string label=_setup.Nodes.DisplayName(_setup.Nodes.All[node]);
            _ui.Text(side==0?"直送便左インスペクター":"直送便右インスペクター",new(x+20,164),.5f,color);
            _ui.Text(label,new(x+20,208),Math.Min(.48f,520/Math.Max(1,_font.MeasureString(label).X)),Cream);
            string mark="目印："+NodeLandmarks.At(_setup,node).Description;
            _ui.Text(mark,new(x+20,241),Math.Min(.35f,520/Math.Max(1,_font.MeasureString(mark).X)),Cream);
            _ui.Text($"予定の格納庫 {store.Count}/{store.TotalCapacity}",new(x+20,274),.32f,Cream);
            if(store.MapleUnits>0)_ui.Text($"楓の木 {store.MapleUnits}",new(x+20,888),.32f,Cream);
            var incomingMaterials=DirectIncomingMaterials(side);
            var storedMaterials=DirectStoredMaterials(side);
            var (mapleLabel,mergedMaple)=MapleArrivalLabel(node,store);
            if(mapleLabel.Length>0)_ui.Text(mapleLabel,new(x+20,920),.32f,Accent);
            foreach(int slot in DirectVisibleSlots(side)){
                var cell=DirectViewCell(side,slot);var robot=store.Robots.FirstOrDefault(r=>r.Id==_directSlots[side][slot]);var hover=DirectHoverPart(side,slot);
                bool target=_directDrag.HasValue && cell.Contains(_pointer);bool cellHover=cell.Contains(_pointer) && hover==0;
                _ui.Box(new(cell.X-2,cell.Y-2,cell.Width+4,cell.Height+4),target?color:new Color(37,64,78));
                DrawHangarBay(cell,cellHover,target);
                for(int row=0;row<3;row++){
                    var part=HangarPart(row);var bounds=DirectViewPart(side,slot,row);_ui.Box(bounds,(hover!=0 && ((robot?.ConnectedParts(hover)??hover)&part)!=0)?new Color(88,125,141):new Color(40,63,76));
                }
                var head=DirectViewPart(side,slot,0);
                var joinedGlyph=new Rectangle(head.X,head.Y,head.Width,Math.Max(1,180*cell.Height/280));
                if(robot!=null)DrawRobotGlyph(joinedGlyph,robot,whiteOutline:true,partOpacity:part=>(_setup.Robots.DirectDelivery.LockedParts(node,robot.Id)&part)!=0?.35f:1);
                foreach(var order in _setup.Robots.DirectDelivery.Orders.Where(o=>o.Target==node && (o.TargetRobotId.HasValue?o.TargetRobotId==robot?.Id:o.TargetSlot==slot))){
                    var original=PlanningStock.Nodes[order.Source].Robots.FirstOrDefault(r=>r.Id==order.RobotId);
                    if(original!=null)DrawRobotGlyph(joinedGlyph,original with {Parts=order.Part,IntegratedParts=original.IntegratedParts&order.Part},whiteOutline:true,opacity:.5f);
                }
                for(int row=0;row<3;row++){
                    var part=HangarPart(row);var bounds=DirectViewPart(side,slot,row);
                    int key=slot*3+row;
                    if(storedMaterials.TryGetValue(key,out var package)){
                        DrawMaplePackage(Rectangle.Union(bounds,DirectViewPart(side,slot,row+package.Sections-1)),package,compact:true);
                        if(cell.Width>=130 && _setup.Robots.MapleShipments.Any(o=>o.PackageId==package.Id))DrawDirectDeliveryTag(bounds,"出荷予定");
                        else if(cell.Width>=130 && mergedMaple.TryGetValue(package.Id,out int units))DrawDirectDeliveryTag(bounds,$"合算 +{units}");
                        continue;
                    }
                    if(incomingMaterials.TryGetValue(key,out var incomingMaterial)){
                        DrawMaplePackage(Rectangle.Union(bounds,DirectViewPart(side,slot,row+incomingMaterial.Sections-1)),incomingMaterial,opacity:.55f,compact:true);
                        if(cell.Width>=130)DrawDirectDeliveryTag(bounds,"入荷予定");
                        continue;
                    }
                    bool incoming=_setup.Robots.DirectDelivery.Orders.Any(o=>o.Target==node && (o.Part&part)!=0 && (o.TargetRobotId.HasValue?o.TargetRobotId==robot?.Id:o.TargetSlot==slot));
                    bool outgoing=robot!=null && _setup.Robots.DirectDelivery.Orders.Any(o=>o.Source==node && o.RobotId==robot.Id && (o.Part&part)!=0);
                    if(cell.Width>=130){
                        if(incoming)DrawDirectDeliveryTag(bounds,"入荷予定");
                        if(outgoing)DrawDirectDeliveryTag(bounds,"出荷予定",incoming);
                        else if(robot!=null && (robot.Parts&part)!=0 && _setup.Robots.DirectDelivery.FailedPart(node,robot.Id,part))DrawDirectDeliveryTag(bounds,"出荷失敗",incoming);
                    }else if(incoming || outgoing)_ui.Box(new(bounds.Right+2,bounds.Y,4,bounds.Height),Accent);
                }
                if(robot!=null && cell.Height>=90){
                    var labelArea=new Rectangle(cell.X,cell.Y+cell.Height*208/280,cell.Width,Math.Max(1,cell.Height*24/280));
                    _ui.Center(robot.Id<0?"組替予定":"ID "+robot.Id,labelArea,Math.Min(.32f,cell.Width/510f),Cream);
                    labelArea.Y=cell.Y+cell.Height*242/280;
                    if(robot.Stationed || robot.Disposal)_ui.Center(robot.HarvestWorker?"採取員":robot.FactoryWorker?"製造作業員":robot.InterceptOnly?"迎撃専用":robot.DefenseOnly?"守備専用":"廃棄予約",labelArea,Math.Min(.24f,cell.Width/680f),color);
                    else if(robot.IntegratedParts!=0)_ui.Center(IntegratedLabel(robot.IntegratedParts),labelArea,Math.Min(.24f,cell.Width/680f),Cream);
                }
            }
            DrawDirectPages(side);
            _spriteBatch.End();_spriteBatch.Begin(transformMatrix:transform);
        }
        _spriteBatch.End();_spriteBatch.Begin(transformMatrix:Matrix.CreateTranslation(0,-300*(1-DirectPairEase),0)*transform);
        _ui.Text("ロボットパーツ直送便",new(36,26),.85f,Cream);
        _ui.Text("左右どちらからでも予約 / 配送フェーズで配送",new(36,76),.4f,Cream);
        _ui.Button(new(770,62,380,44),"直前の予約を取り消す",Muted,.42f);
        // Shared upper handle keeps closing controls away from the drag corridor.
        _ui.Box(new(20,114,940,34),DirectSideColor(0));_ui.Box(new(960,114,940,34),Color.White);
        _ui.Button(DirectPairBack,"×",Muted,.8f);
        DrawDirectPairCheckButton();
        _spriteBatch.End();_spriteBatch.Begin(transformMatrix:transform);
        _ui.Center("パーツ・素材をドラッグして予約 / ホイール：ズーム / 右ドラッグ：スクロール",new(20,978,1880,30),.38f,Cream);
        _ui.Center(_directMessage,new(20,1012,1880,28),.4f,Cream);
        var pending=_setup.Robots.DirectDelivery.Orders.Where(o=>_directNodes.Contains(o.Source) && _directNodes.Contains(o.Target)).ToArray();string status=pending.Length==0?"この２地点の配送予約はありません":string.Join(" / ",pending.Take(6).Select(o=>$"#{o.Number} {(o.Source==_directNodes[0]?"左→右":"右→左")} {PartsLabel(o.Part)} {o.Status}"));
        int planned=_setup.Robots.PartPlanning.Orders.Count(o=>_directNodes.Contains(PlanningMoveSource(o)) && _directNodes.Contains(o.Target));
        if(planned>0)status=$"組替・入荷予約 {planned}件 / 表示は予約後の予定です";
        int materials=_setup.Robots.MapleShipments.Count(o=>_directNodes.Contains(o.Source) && _directNodes.Contains(o.Target));
        if(materials>0)status=$"素材の直送予約 {materials}件"+(planned>0 || pending.Length>0?" / "+status:"");
        _ui.Center(status,new(20,1046,1880,25),Math.Min(.32f,1840/Math.Max(1,_font.MeasureString(status).X)),Accent);
        DrawNodeCheckHint();
        if(!_directDrag.HasValue)DrawDeliveryTagHint(new(0,0,1920,1080));
        if(_directDrag is {} drag){var robot=PlanningStock.Nodes[_directNodes[drag.Side]].Robots.FirstOrDefault(r=>r.Id==drag.Robot);if(robot!=null){var selected=drag.Part==0?robot.Parts:drag.Part;var bounds=new Rectangle(_pointer.X-34,_pointer.Y-34,68,102);DrawRobotGlyph(bounds,robot with {Parts=selected,IntegratedParts=robot.IntegratedParts&selected},whiteOutline:true);}}
        if(_directMaterialDrag is {} material && _setup.Robots.Nodes[_directNodes[material.Side]].MaplePackages.FirstOrDefault(p=>p.Id==material.Id) is {} cargo)
            DrawMaplePackage(new(_pointer.X-55,_pointer.Y-46,110,92),cargo);
    }
}
