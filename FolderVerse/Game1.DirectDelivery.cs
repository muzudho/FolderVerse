namespace FolderVerse;
using System;
using System.Linq;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

public partial class Game1
{
    private bool _directMap,_directPairOpen,_directRoutesBefore;
    private readonly int[] _directNodes=new int[2];
    private readonly long?[][] _directSlots={new long?[12],new long?[12]};
    private readonly List<(Rectangle Bounds,int A,int B)> _directTrucks=new();
    private (int Side,long Robot,RobotParts Part)? _directDrag;
    private string _directMessage="";
    private static readonly Rectangle DirectDeliveryButton=new(1004,1006,242,64),DirectPairBack=new(40,24,250,52);
    private static Rectangle DirectCell(int side,int slot)=>new(65+side*960+slot%4*196,250+slot/4*196,182,180);
    private static Rectangle DirectPart(int side,int slot,int row){var r=DirectCell(side,slot);return new(r.X+56,r.Y+10+row*39,70,36);}
    private RobotParts DirectHoverPart(int side,int slot)
    {
        int node=_directNodes[side];long? id=_directSlots[side][slot];var robot=_setup.Robots.Nodes[node].Robots.FirstOrDefault(r=>r.Id==id);
        for(int row=0;row<3;row++)if(DirectPart(side,slot,row).Contains(_pointer)){
            var part=HangarPart(row);
            if(robot!=null && (robot.Parts&part)!=0 || _setup.Robots.DirectDelivery.Orders.Any(o=>o.Target==node && o.Part==part && (o.TargetRobotId.HasValue?o.TargetRobotId==id:o.TargetSlot==slot)))return part;
        }
        return 0;
    }
    private bool DirectNodeAllowed(int node)=>node>=0 && CanManageNodeFactory(_setup.Nodes.All[node].Owner);
    private bool InspectorDirectTrucks=>_populationCell>=0 && _populationNode>=0 && !_nodeInspectorClosing;
    private void CloseDirectDelivery(){if(_directMap)_net.ShowRoutes=_directRoutesBefore;_directMap=_directPairOpen=false;_directDrag=null;_directTrucks.Clear();}
    private void OpenDirectMap(){ResetInspector();_directRoutesBefore=_net.ShowRoutes;_net.ShowRoutes=true;_directMap=true;_directMessage="";}
    private void OpenDirectPair(int a,int b){
        if(!DirectNodeAllowed(a) || !DirectNodeAllowed(b) || _setup.Campaign.RobotBattles.LockedNode(a) || _setup.Campaign.RobotBattles.LockedNode(b) || _setup.Campaign.RobotBattles.LockedEdge(a,b))return;
        _directNodes[0]=a;_directNodes[1]=b;foreach(var slots in _directSlots)Array.Clear(slots);SyncDirectSlots();_directPairOpen=true;_directDrag=null;_directMessage="同じ側：組立・分解 / 反対側：直送便を予約 / 予約パーツをクリック：取消";
    }
    private void SyncDirectSlots(){for(int side=0;side<2;side++){var robots=_setup.Robots.Nodes[_directNodes[side]].Robots;for(int i=0;i<12;i++)if(_directSlots[side][i].HasValue && !robots.Any(r=>r.Id==_directSlots[side][i]))_directSlots[side][i]=null;
        foreach(var robot in robots)if(!_directSlots[side].Contains(robot.Id)){int slot=Enumerable.Range(0,12).FirstOrDefault(i=>!_directSlots[side][i].HasValue && !_setup.Robots.DirectDelivery.Orders.Any(o=>o.Target==_directNodes[side] && o.TargetRobotId==null && o.TargetSlot==i),-1);if(slot<0)slot=Array.FindIndex(_directSlots[side],id=>!id.HasValue);if(slot>=0)_directSlots[side][slot]=robot.Id;}
    }}
    private bool DirectMapClick(Point p){
        if((!_directMap && !InspectorDirectTrucks) || _directPairOpen || MapTransitionActive)return false;
        if(_directMap && DirectDeliveryButton.Contains(p)){CloseDirectDelivery();return true;}
        if(MapViewButton.Contains(p) || !MapViewBounds.Contains(p))return false;
        var hit=_directTrucks.Where(t=>t.Bounds.Contains(p)).OrderBy(t=>Vector2.DistanceSquared(t.Bounds.Center.ToVector2(),p.ToVector2())).FirstOrDefault();if(hit.Bounds!=Rectangle.Empty){OpenDirectPair(hit.A,hit.B);return true;}return _directMap;
    }
    private void DrawDirectTrucks(){
        _directTrucks.Clear();if((!_directMap && !InspectorDirectTrucks) || _directPairOpen || MapTransitionActive)return;
        foreach(var node in _setup.Nodes.All.Where(n=>DirectNodeAllowed(n.Id))){
            if(!OperationPosition(node.Id,out var a))continue;
            foreach(var route in _setup.Routes.Neighbors(node)){var next=_setup.Nodes.At(route.Target,route.Entry);if(node.Id>=next.Id || !DirectNodeAllowed(next.Id) || !OperationPosition(next.Id,out var b))continue;
                if(!_directMap && node.Id!=_populationNode && next.Id!=_populationNode)continue;
                var at=(a+b)/2;if(!MapViewBounds.Contains(at.ToPoint()))continue;
                var bounds=new Rectangle((int)at.X-22,(int)at.Y-17,44,34);bool enabled=!_setup.Campaign.RobotBattles.LockedNode(node.Id) && !_setup.Campaign.RobotBattles.LockedNode(next.Id) && !_setup.Campaign.RobotBattles.LockedEdge(node.Id,next.Id);
                var color=enabled?(bounds.Contains(_pointer)?Cream:Accent):Muted;var delta=b-a;_ui.Tile(at,new(delta.Length(),2),MathF.Atan2(delta.Y,delta.X),color*.4f);
                _ui.Box(new(bounds.X,bounds.Y+3,27,20),color);_ui.Box(new(bounds.X+29,bounds.Y+8,13,15),color);_ui.Box(new(bounds.X+31,bounds.Y+10,8,7),new Color(125,196,222));_ui.Box(new(bounds.X+5,bounds.Y+23,9,8),new Color(14,22,29));_ui.Box(new(bounds.X+30,bounds.Y+23,9,8),new Color(14,22,29));
                int count=_setup.Robots.DirectDelivery.Orders.Count(o=>o.Source==node.Id && o.Target==next.Id || o.Source==next.Id && o.Target==node.Id);if(count>0)_ui.Text(count.ToString(),new(bounds.X+15,bounds.Y-13),.35f,Cream);
                if(enabled)_directTrucks.Add((bounds,node.Id,next.Id));
            }
        }
    }
    private void DirectPairInput(MouseState mouse,bool click,bool escape){
        if(escape || click && DirectPairBack.Contains(_pointer)){_directPairOpen=false;_directDrag=null;return;}
        SyncDirectSlots();
        if(click){for(int side=0;side<2;side++)for(int slot=0;slot<12;slot++)for(int row=0;row<3;row++)if(DirectHoverPart(side,slot)==HangarPart(row)){
            int node=_directNodes[side];var part=HangarPart(row);long? id=_directSlots[side][slot];
            var existing=_setup.Robots.DirectDelivery.Orders.LastOrDefault(o=>o.Source==node && o.RobotId==id && o.Part==part || o.Target==node && o.Part==part && (o.TargetRobotId.HasValue?o.TargetRobotId==id:o.TargetSlot==slot));
            if(existing!=null){_setup.Robots.DirectDelivery.Cancel(existing.Number);_directMessage="直送便の予約を取り消しました";return;}
            var robot=_setup.Robots.Nodes[node].Robots.FirstOrDefault(r=>r.Id==id);if(robot!=null && (robot.Parts&part)!=0 && CanManageNodeFactory(robot.Owner) && !_setup.Campaign.RobotBattles.LockedNode(node) && !_setup.Robots.PartLocked(node,robot,part))_directDrag=(side,robot.Id,part);else _directMessage="そのパーツは操作できません";return;
        }
        for(int side=0;side<2;side++)for(int slot=0;slot<12;slot++)if(DirectCell(side,slot).Contains(_pointer)){
            int node=_directNodes[side];var robot=_setup.Robots.Nodes[node].Robots.FirstOrDefault(r=>r.Id==_directSlots[side][slot]);
            if(robot!=null && CanManageNodeFactory(robot.Owner) && !_setup.Campaign.RobotBattles.LockedNode(node) && !_setup.Robots.AssemblyLocked(node,robot))_directDrag=(side,robot.Id,(RobotParts)0);
            else _directMessage="その区画は操作できません / パーツ単位での操作を確認してください";
            return;
        }}
        if(!_directDrag.HasValue || mouse.LeftButton!=ButtonState.Released || _previousMouse.LeftButton!=ButtonState.Pressed)return;
        var drag=_directDrag.Value;_directDrag=null;
        for(int side=0;side<2;side++)for(int slot=0;slot<12;slot++)if(DirectCell(side,slot).Contains(_pointer)){
            try{int source=_directNodes[drag.Side],target=_directNodes[side];var destination=_directSlots[side][slot];if(side==drag.Side){if(destination==drag.Robot)return;if(!destination.HasValue && _setup.Robots.DirectDelivery.Orders.Any(o=>o.Target==target && o.TargetRobotId==null && o.TargetSlot==slot))throw new InvalidOperationException("Arrival slot reserved.");
                    if(drag.Part==0){if(destination.HasValue && _setup.Robots.AssemblyLocked(source,_setup.Robots.Nodes[source].Robots.Single(r=>r.Id==destination)))throw new InvalidOperationException("Robot is reserved.");int from=Array.IndexOf(_directSlots[side],(long?)drag.Robot);_directSlots[side][from]=destination;_directSlots[side][slot]=drag.Robot;}
                    else {long id=_setup.Robots.MovePart(source,drag.Robot,drag.Part,destination,_setup.Nodes.All[source].Owner);_directSlots[side][slot]=id;}_directMessage="格納庫内でパーツを組み替えました";}
                else {var robot=_setup.Robots.Nodes[source].Robots.Single(r=>r.Id==drag.Robot);_setup.Robots.DirectDelivery.ReserveParts(_setup,source,target,drag.Robot,drag.Part==0?robot.Parts:drag.Part,destination,slot);_directMessage="直送便を予約しました / ターン終了時に輸送";}SyncDirectSlots();}
            catch(InvalidOperationException){_directMessage="所有国・予約・同じ部位・空き枠を確認してください";}return;
        }
    }
    private void DrawDirectPair(){
        SyncDirectSlots();_ui.Box(new(0,0,1920,1080),new Color(14,29,40));_ui.Button(DirectPairBack,"← トラックの地図へ",Muted,.5f);_ui.Center("ロボットパーツ直送便 / ターン終了時に輸送",new(340,20,1490,70),.8f,Cream);
        _ui.Center("パーツをドラッグ：部位ごと / 区画をドラッグ：まとめて / 予約パーツをクリック：取消",new(340,84,1490,24),.35f,Cream);
        for(int side=0;side<2;side++){
            int node=_directNodes[side];var store=_setup.Robots.Nodes[node];_ui.Box(new(40+side*960,110,860,805),new Color(27,49,62));string label=_setup.Nodes.Label(_setup.Nodes.All[node]);
            DrawDirectNodePreview(side,node);
            _ui.Text("ロボット格納庫",new(197+side*960,120),.4f,Cream);
            _ui.Box(new(187+side*960,153,698,46),new Color(39,70,86));
            _ui.Text(label,new(197+side*960,159),Math.Min(.65f,676/Math.Max(1,_font.MeasureString(label).X)),Accent);
            _ui.Text("目印："+NodeLandmarks.At(_setup,node).Description,new(197+side*960,203),.4f,Cream);
            _ui.Text($"実物 {store.Count}/12 / 到着予定は半透明",new(197+side*960,229),.3f,Cream);
            for(int slot=0;slot<12;slot++){
                var cell=DirectCell(side,slot);var robot=store.Robots.FirstOrDefault(r=>r.Id==_directSlots[side][slot]);var hover=DirectHoverPart(side,slot);bool cellHover=cell.Contains(_pointer) && hover==0;_ui.Box(cell,cellHover?new Color(46,73,87):new Color(18,35,46));
                for(int row=0;row<3;row++){var part=HangarPart(row);var bounds=DirectPart(side,slot,row);_ui.Box(bounds,hover==part?new Color(88,125,141):new Color(40,63,76));var glyph=new Rectangle(cell.X+57,cell.Y+12,68,106);
                    if(robot!=null && (robot.Parts&part)!=0){bool reserved=(_setup.Robots.DirectDelivery.LockedParts(node,robot.Id)&part)!=0;DrawRobotGlyph(glyph,robot.Origin(part),part,robot.Role==RobotRole.Captain,opacity:reserved?.35f:1,whiteOutline:true,damaged:robot.IsDamaged(part),cheap:robot.IsCheap(part));if(reserved)_ui.Text("予約",new(cell.X+137,bounds.Y+8),.28f,Accent);}
                    foreach(var order in _setup.Robots.DirectDelivery.Orders.Where(o=>o.Target==node && o.Part==part && (o.TargetRobotId.HasValue?o.TargetRobotId==robot?.Id:o.TargetSlot==slot))){var original=_setup.Robots.Nodes[order.Source].Robots.FirstOrDefault(r=>r.Id==order.RobotId);if(original!=null)DrawRobotGlyph(glyph,original.Origin(part),part,opacity:.5f,whiteOutline:true,damaged:original.IsDamaged(part),cheap:original.IsCheap(part));_ui.Text("予定",new(cell.X+137,bounds.Y+8),.28f,Cream);}
                }
                if(robot!=null){_ui.Center("ID "+robot.Id,new(cell.X,cell.Y+137,cell.Width,27),.35f,Cream);if(robot.Stationed || robot.Disposal)_ui.Center(robot.FactoryWorker?"工場作業員":robot.DefenseOnly?"守備専用":"廃棄予約",new(cell.X,cell.Y+160,cell.Width,19),.27f,Accent);}
            }
        }
        _ui.Center(_directMessage,new(40,932,1840,60),.48f,Cream);
        var pending=_setup.Robots.DirectDelivery.Orders.Where(o=>_directNodes.Contains(o.Source) && _directNodes.Contains(o.Target)).ToArray();string status=string.Join(" / ",pending.Take(6).Select(o=>$"#{o.Number} {PartsLabel(o.Part)} {o.Status}"));_ui.Center(status,new(40,992,1840,30),Math.Min(.35f,1800/Math.Max(1,_font.MeasureString(status).X)),Accent);
        _ui.Center("予約は自動保存の対象 / 満杯・継戦中は待機 / 既存の役割予約は上書きしません",new(40,1030,1840,40),.38f,Cream);
        if(_directDrag is {} drag){var robot=_setup.Robots.Nodes[_directNodes[drag.Side]].Robots.FirstOrDefault(r=>r.Id==drag.Robot);if(robot!=null)foreach(var part in new[]{RobotParts.Head,RobotParts.Body,RobotParts.Legs})if(((drag.Part==0?robot.Parts:drag.Part)&part)!=0)DrawRobotGlyph(new(_pointer.X-34,_pointer.Y-34,68,106),robot.Origin(part),part,whiteOutline:true);}
    }
}
