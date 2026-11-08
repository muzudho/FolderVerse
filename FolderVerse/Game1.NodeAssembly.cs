namespace FolderVerse;
using System;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

public partial class Game1
{
    private Texture2D _robotAssemblyIcon;
    private bool _nodeAssemblyOpen;
    private long?[] _hangarSlots=new long?[12];
    private long? _hangarDrag;
    private RobotParts _hangarPart;
    private int _hangarSource;
    private Point _hangarGrabOffset;
    private string _hangarMessage="";
    private string _deliveryTagHint="";
    private static readonly Rectangle NodeAssemblyTile=new(678,210,190,40),HangarAutoCheck=new(84,950,350,24);
    private HangarReservation _hangarReservation;
    private static readonly Rectangle HangarAssemblyButton=new(84,860,182,42);
    private static Rectangle HangarReservationButton(int i)=>new(280+i%3*196,860+i/3*48,182,42);
    private int HangarReservationCountry(Robot robot)=>_hangarReservation==HangarReservation.Defense || robot?.DefenseFlag!=null?_setup.PlayerSlot:robot?.Owner??_setup.PlayerSlot;
    private bool HangarReservationAccessible(Robot robot)=>robot!=null && !VisitingForeignHangar && CanManageNodeFactory(_setup.Nodes.All[_populationNode].Owner) && (_hangarReservation==HangarReservation.Defense || CanManageNodeFactory(robot.Owner));
    private bool HangarReservationEnabled(Robot robot)=>HangarReservationAccessible(robot) && PlanningStock.CanSetReservation(HangarPlanningNode,robot,_hangarReservation,HangarReservationCountry(robot));
    private static Rectangle HangarCell(int slot)=>new(84+(slot%12)%4*196,300+(slot%12)/4*182,182,174);
    private static Rectangle HangarRobotBounds(int slot)
    {var cell=HangarCell(slot);return new(cell.X+57,cell.Y+12,68,102);}
    private static RobotParts HangarPart(int row)=>row==0?RobotParts.Head:row==1?RobotParts.Body:RobotParts.Legs;
    private static Rectangle HangarPartBounds(int slot,int row)
    {var cell=HangarCell(slot);return new(cell.X+56,cell.Y+10+row*38,70,36);}
    private bool VisitingForeignHangar=>_setup.Nodes.All[_populationNode].Owner>=0 && !CanManageNodeFactory(_setup.Nodes.All[_populationNode].Owner) &&
        _setup.Relations.CanAct(_setup.PlayerSlot) && _setup.Nodes.Current(_setup.PlayerSlot)?.Id==_populationNode;
    private bool CanOpenHangar=>InspectableNode(_populationNode) && (_setup.Nodes.All[_populationNode].Owner<0 || CanManageNodeFactory(_setup.Nodes.All[_populationNode].Owner) || VisitingForeignHangar);
    private bool CanMoveHangarRobot(Robot robot)=>DomesticOperationsAllowed && CanOpenHangar && !_setup.Campaign.RobotBattles.LockedNode(_populationNode) && !PlanningStock.AssemblyLocked(HangarPlanningNode,robot);
    private bool CanMoveHangarPart(Robot robot,RobotParts part)=>DomesticOperationsAllowed && CanOpenHangar && PlanningStock.CanEditReservedPart(HangarPlanningNode,robot,_setup.PlayerSlot) &&
        (CanManageNodeFactory(_setup.Nodes.All[_populationNode].Owner) || new[]{RobotParts.Head,RobotParts.Body,RobotParts.Legs}.Where(p=>(robot.ConnectedParts(part)&robot.Parts&p)!=0).All(p=>robot.Origin(p)==_setup.PlayerSlot));
    private bool HangarPlaceEnabled(Robot target,RobotParts part)
    {
        var dragging=_hangarDrag.HasValue?PlanningStock.Nodes[HangarPlanningNode].Robots.FirstOrDefault(r=>r.Id==_hangarDrag):null;
        part=dragging?.ConnectedParts(part)??target?.ConnectedParts(part)??part;
        if(!DomesticOperationsAllowed || !CanOpenHangar || _setup.Campaign.RobotBattles.LockedNode(_populationNode) || target!=null && (!CanMoveHangarPart(target,part) || !target.CanSeparate(target.Parts&part) || (target.Parts&part)!=0 && (target.Parts&part)!=part))return false;
        if(target!=null && (target.Parts&part)!=0 && !CanMoveHangarPart(target,part))return false;
        if(_hangarDrag.HasValue)
        {
            var source=PlanningStock.Nodes[HangarPlanningNode].Robots.FirstOrDefault(r=>r.Id==_hangarDrag);
            if(source==null || !CanMoveHangarPart(source,_hangarPart) || part!=_hangarPart || target!=null && source.Disposal!=target.Disposal)return false;
            if(target==null && source.Parts!=part && !PlanningStock.Nodes[HangarPlanningNode].HasSpace)return false;
        }
        return true;
    }
    private void OpenNodeAssembly()
    {
        if(!InspectableNode(_populationNode))return;
        _hangarPage=0;_hangarTrainingCountry=-1;_hangarAbandonMode=false;_hangarReturnHomeMode=false;
        _nodeOfficeOpen=false;_lastNodeTab=3;
        _nodeAssemblyOpen=true;_nodeFactoryOpen=_nodeTransportOpen=_nodeDisposalOpen=false;
        _hangarDrag=null;_hangarReservation=HangarReservation.None;_hangarMessage="";Array.Clear(_hangarSlots);
        SyncHangarSlots();
    }
    private void NodeAssemblyInput(MouseState mouse,bool click,bool escape)
    {
        var original=_pointer;_pointer.X-=NodeInspectorOffset;
        try{NodeAssemblyLocalInput(mouse,click,escape);}finally{_pointer=original;}
    }
    private void NodeAssemblyLocalInput(MouseState mouse,bool click,bool escape)
    {
        if(ReturnHomeHangarInput(click))return;
        if(click && HangarUndoButton.Contains(_pointer)){UndoHangarPlan();return;}
        if(click && HangarPageClick(_pointer))return;
        if(!DomesticOperationsAllowed){
            _hangarDrag=null;_hangarReservation=HangarReservation.None;_hangarAbandonMode=false;
            if(escape)CloseNodeInspector();
            else if(click && (NodeOfficeTile.Contains(_pointer) || NodeFactoryTile.Contains(_pointer) || NodeDisposalTile.Contains(_pointer)))NodeDialogClick(new Point(_pointer.X+NodeInspectorOffset,_pointer.Y),new KeyboardState());
            return;
        }
        if(click || escape)_hangarTrainingCountry=-1;
        if(click && HangarSalvageButton.Contains(_pointer)){OpenSalvagePlan(_populationNode);return;}
        if(click && HangarAbandonButton.Contains(_pointer) && HangarPlanningNode==_populationNode && _setup.Nodes.All[_populationNode].Owner==_setup.PlayerSlot){_hangarAbandonMode=!_hangarAbandonMode;_hangarReservation=HangarReservation.None;_hangarDrag=null;return;}
        if(click && _hangarAbandonMode){
            var stock=PlanningStock.Nodes[HangarPlanningNode];SyncHangarSlots();
            for(int i=_hangarPage*12;i<Math.Min(_hangarSlots.Length,(_hangarPage+1)*12);i++)if(HangarCell(i).Contains(_pointer)){
                var robot=stock.Robots.FirstOrDefault(r=>r.Id==_hangarSlots[i]);if(robot==null)return;
                var parts=robot.Parts;for(int row=0;row<3;row++)if(HangarPartBounds(i,row).Contains(_pointer))parts=robot.ConnectedParts(HangarPart(row));
                if(robot.Owner!=_setup.PlayerSlot || (parts&robot.Parts)!=parts || new[]{RobotParts.Head,RobotParts.Body,RobotParts.Legs}.Any(p=>(parts&p)!=0 && _setup.Robots.PartLocked(_populationNode,robot,p)) || _setup.Campaign.RobotBattles.LockedNode(_populationNode))_hangarMessage="予約・所有国・戦闘状況を確認してください";
                else _salvageAbandonConfirmation=(_populationNode,robot.Id,parts);
                return;
            }
        }
        if(click && NodeAutomationClick(_pointer)){_hangarDrag=null;return;}
        if(escape){if(_hangarDrag.HasValue)_hangarDrag=null;else CloseNodeInspector();return;}
        if(click && HangarAssemblyButton.Contains(_pointer)){_hangarAbandonMode=false;_hangarReservation=HangarReservation.None;_hangarDrag=null;_hangarMessage="";return;}
        if(click && !VisitingForeignHangar && CanManageNodeFactory(_setup.Nodes.All[_populationNode].Owner))for(int i=0;i<4;i++)if(HangarReservationButton(i).Contains(_pointer)){_hangarAbandonMode=false;var kind=(HangarReservation)(i+1);_hangarReservation=_hangarReservation==kind?HangarReservation.None:kind;_hangarDrag=null;return;}
        if(click && (NodeOfficeTile.Contains(_pointer) || NodeFactoryTile.Contains(_pointer) || NodeTransportTile.Contains(_pointer) || NodeDisposalTile.Contains(_pointer) || NodeAssemblyTile.Contains(_pointer)))
        {_hangarAbandonMode=false;_hangarDrag=null;NodeDialogClick(new Point(_pointer.X+NodeInspectorOffset,_pointer.Y),new KeyboardState());return;}
        if(click && HangarAutoCheck.Contains(_pointer))
        {
            if(CanManageNodeFactory(_setup.Nodes.All[_populationNode].Owner) && !_setup.Campaign.RobotBattles.LockedNode(_populationNode))
            {var workshop=PlanningWorkshop(_populationNode);workshop.AutoAssemblyDisabled=!workshop.AutoAssemblyDisabled;}
            return;
        }
        var store=PlanningStock.Nodes[HangarPlanningNode];
        SyncHangarSlots();
        if(click)
        {
            for(int slot=_hangarPage*12;slot<Math.Min(_hangarSlots.Length,(_hangarPage+1)*12);slot++)
            {
                var robot=store.Robots.FirstOrDefault(r=>r.Id==_hangarSlots[slot]);if(robot==null)continue;
                var cell=HangarCell(slot);
                if(_hangarReservation!=HangarReservation.None && cell.Contains(_pointer)){
                    bool accepted=HangarReservationEnabled(robot) && _setup.Robots.PartPlanning.ReserveRole(_setup,_populationNode,robot.Id,_hangarReservation,HangarReservationCountry(robot));
                    if(accepted){
                        var reserved=PlanningStock.Nodes[HangarPlanningNode].Robots.Single(r=>r.Id==robot.Id);
                        _workReservationEffects.Remove(robot.Id);
                        if(_hangarReservation==HangarReservation.Defense && reserved.DefenseOnly){BeginDefenseReservationEffect(reserved);_hangarMessage="国旗で守備専用予約 / 靴を履いて仕事中";}
                        else{
                            _defenseReservationEffects.Remove(robot.Id);
                            if(reserved.InterceptOnly || reserved.FactoryWorker)BeginWorkReservationEffect(reserved,reserved.Owner);
                            _hangarMessage="予約を切り替えました / 続けて個体を選択できます";
                        }
                    }else if(_hangarReservation==HangarReservation.Defense && HangarReservationAccessible(robot) && _setup.Robots.ReservationBlockedByTraining(_populationNode,robot,_hangarReservation,HangarReservationCountry(robot))){
                        _hangarTrainingCountry=HangarReservationCountry(robot);_hangarTrainingRobot=robot.Id;
                        _hangarMessage="この国の調練により、自国パーツ 3/3 のロボットだけを戦闘に出します";
                    }else _hangarMessage="予約国・完全体・自国パーツ・出撃条件・処理中の状態を確認してください";
                    return;
                }
                if(!CanOpenHangar || _setup.Campaign.RobotBattles.LockedNode(_populationNode))continue;
                for(int row=0;row<3;row++)if(HangarPartBounds(slot,row).Contains(_pointer) && (robot.Parts&HangarPart(row))!=0 && CanMoveHangarPart(robot,HangarPart(row)))
                {_hangarDrag=robot.Id;_hangarPart=robot.ConnectedParts(HangarPart(row));_hangarSource=slot;_hangarGrabOffset=_pointer-HangarRobotBounds(slot).Location;_hangarMessage=robot.IntegratedParts!=0?"一体型は結合したまま移動します":"同じパーツがある区画へドロップすると交換";return;}
            }
        }
        if(!_hangarDrag.HasValue || mouse.LeftButton!=ButtonState.Released || _previousMouse.LeftButton!=ButtonState.Pressed)return;
        long source=_hangarDrag.Value;_hangarDrag=null;
        int destination=Enumerable.Range(_hangarPage*12,Math.Min(12,_hangarSlots.Length-_hangarPage*12)).FirstOrDefault(i=>HangarCell(i).Contains(_pointer),-1);
        if(destination<0){_hangarMessage="ドラッグを取り消しました";return;}
        try
        {
            var robot=store.Robots.Single(r=>r.Id==source);
            var target=store.Robots.FirstOrDefault(r=>r.Id==_hangarSlots[destination]);
            if(!CanMoveHangarPart(robot,_hangarPart) || target!=null && !CanMoveHangarPart(target,_hangarPart))throw new InvalidOperationException();
            if(target!=null && (target.Parts&_hangarPart)!=0 && !CanMoveHangarPart(target,_hangarPart))throw new InvalidOperationException();
            long id=_setup.Robots.PartPlanning.Reserve(_setup,_populationNode,_populationNode,source,_hangarPart,_hangarSlots[destination],_setup.PlayerSlot);
            if(destination!=_hangarSource && (id==source || !store.Robots.Any(r=>r.Id==source)))_hangarSlots[_hangarSource]=null;
            _hangarSlots[destination]=id;_hangarMessage="組替を予約しました / 表示は入荷・組替後の予定";
        }
        catch(InvalidOperationException){_hangarMessage="所有国・廃棄フラグ・容量・予約を確認してください";}
        catch(ArgumentException){_hangarMessage="そのパーツは移動できません";}
    }
    private void DrawNodeAssemblyTile()
    {
        bool enabled=CanOpenHangar;
        _ui.Box(NodeAssemblyTile,_nodeAssemblyOpen || enabled && NodeAssemblyTile.Contains(_pointer)?Accent:Muted);
        _ui.Center("ロボット格納庫",NodeAssemblyTile,.42f,enabled?(_nodeAssemblyOpen || NodeAssemblyTile.Contains(_pointer)?SelectionInk:Cream):Cream*.4f);
        if(_nodeAssemblyOpen)_ui.Box(new(NodeAssemblyTile.X,NodeAssemblyTile.Y,NodeAssemblyTile.Width,4),Cream);
    }
    private void DrawNodeAssembly()
    {
        _deliveryTagHint="";
        SyncHangarSlots();
        var workshop=PlanningWorkshop(_populationNode);var store=PlanningStock.Nodes[HangarPlanningNode];
        _ui.Box(new(54,150,850,825),new(27,49,62));
        _ui.Text($"格納庫 {store.Count}/{store.TotalCapacity}",new(84,285),.28f,Cream);
        _ui.Text(HangarPlanningNode==_populationNode?"入荷・機能移転・組替後の予定":"移転先での格納・組替予定",new(84,266),.28f,Cream);DrawHangarUndo();


        bool manage=DomesticOperationsAllowed && CanManageNodeFactory(_setup.Nodes.All[_populationNode].Owner);
        bool hoveringPart=false;
        bool reservationMode=_hangarReservation!=HangarReservation.None || _hangarReturnHomeMode;
        string hover=VisitingForeignHangar?"滞在中：自国パーツ同士の交換・空き部位への移動ができます":CanOpenHangar?"頭・胴・脚をつかんで、格納庫の区画へドラッグ＆ドロップ":"他国の地点：自分の征服者がいる地点では自国パーツを動かせます";
        if(reservationMode)hover="区画をクリック：予約 / 同じ国の同じ予約は解除 / 続けて選択できます";
        if(!DomesticOperationsAllowed)hover="軍事フェーズ：格納庫は閲覧のみ / 組立・配送は次の内政フェーズで操作できます";
        for(int slot=_hangarPage*12;slot<Math.Min(_hangarSlots.Length,(_hangarPage+1)*12);slot++)
        {
            var cell=HangarCell(slot);var occupant=store.Robots.FirstOrDefault(r=>r.Id==_hangarSlots[slot]);
            bool cellHover=reservationMode && cell.Contains(_pointer) && occupant!=null;
            bool canReserve=_hangarReturnHomeMode?CanSelectReturnHome(occupant):reservationMode && HangarReservationEnabled(occupant);
            _ui.Box(cell,cellHover?(canReserve?new Color(48,78,94):new Color(67,40,44)):new Color(18,35,46));
            for(int row=0;row<3;row++)
            {
                var bounds=HangarPartBounds(slot,row);var part=HangarPart(row);bool enabled=!reservationMode && HangarPlaceEnabled(occupant,part);
                _ui.Box(bounds,enabled?new Color(48,78,94):new Color(9,19,26));
                if(enabled)
                {
                    var edge=_hangarDrag.HasValue && cell.Contains(_pointer)?ControlEdge:ControlEdge*.4f;
                    _ui.Box(new(bounds.X,bounds.Y,bounds.Width,2),edge);_ui.Box(new(bounds.X,bounds.Bottom-2,bounds.Width,2),edge);
                    _ui.Box(new(bounds.X,bounds.Y,2,bounds.Height),edge);_ui.Box(new(bounds.Right-2,bounds.Y,2,bounds.Height),edge);
                    if(occupant==null || (occupant.Parts&part)==0)_ui.Center("＋",bounds,.35f,Cream*.65f);
                }
            }
            var robot=store.Robots.FirstOrDefault(r=>r.Id==_hangarSlots[slot]);var incoming=_setup.Robots.DirectDelivery.Orders.Where(o=>o.Target==_populationNode && (o.TargetRobotId.HasValue?o.TargetRobotId==robot?.Id:o.TargetSlot==slot)).ToArray();foreach(var order in incoming)
            {
                var original=_setup.Robots.Nodes[order.Source].Robots.FirstOrDefault(r=>r.Id==order.RobotId);
                if(original!=null)DrawRobotGlyph(HangarRobotBounds(slot),original.Slice(order.Part),whiteOutline:true,opacity:.5f);
                _ui.Center("到着予定："+PartsLabel(order.Part),new(cell.X,cell.Y+148,cell.Width,22),.27f,Cream);
            }
            // Draw opaque labels after every glyph, including disabled/occupied slots.
            if(robot==null){DrawHangarDeliveryTags(slot,incoming,occupant);continue;}
            robot=HomePersuasionRobot(robot);
            DrawRobotGlyph(HangarRobotBounds(slot),robot,whiteOutline:true,partOpacity:part=>reservationMode?(canReserve?1:.45f):HangarPlaceEnabled(occupant,part)?1:.22f);
            DrawHangarDeliveryTags(slot,incoming,occupant);
            for(int row=0;!reservationMode && row<3;row++)
            {
                var bounds=HangarPartBounds(slot,row);var part=HangarPart(row);
                if(!CanMoveHangarPart(robot,part) || !bounds.Contains(_pointer) || (robot.Parts&part)==0)continue;
                hoveringPart=true;
                bool movable=CanMoveHangarPart(robot,part);
                var color=movable?ControlEdge:new Color(145,156,166);
                _ui.Box(new(bounds.X,bounds.Y,bounds.Width,2),color);_ui.Box(new(bounds.X,bounds.Bottom-2,bounds.Width,2),color);
                _ui.Box(new(bounds.X,bounds.Y,2,bounds.Height),color);_ui.Box(new(bounds.Right-2,bounds.Y,2,bounds.Height),color);
                hover=movable?$"{PartsLabel(part)}{(robot.IsPremium(part)?"（高級・盾／短剣／小銃＋１）":robot.IsCheap(part)?"（安い）":"")}{(robot.IsDamaged(part)?"（損傷・未所属扱い）":"")}をドラッグ＆ドロップして再配置":"この個体は処理中・予約中、または操作できない所有国です";
            }
            DrawDefenseReservationEffect(cell,robot);
            string partsName=PartsLabel(robot)+$" / #{robot.Owner+1}";
            string reservation=HangarReservationLabel(robot);
            if(reservation.Length>0){
                if(Math.Min(DefenseShoeProgress(robot),WorkShoeProgress(robot))>.6f)DrawHangarReservationLabel(new(cell.X,cell.Y+126,cell.Width,24),robot,reservation);
            }else _ui.Center(partsName,new(cell.X,cell.Y+126,cell.Width,24),Math.Min(.4f,(cell.Width-8)/Math.Max(1,_font.MeasureString(partsName).X)),Cream);
            _ui.Center((robot.Id<0?"組替予定":"ID "+robot.Id)+(robot.DamagedParts!=0?" / 損傷":""),new(cell.X,cell.Y+148,90,22),.27f,Cream);
            if(robot.ReturnHomeCommander.HasValue){
                _ui.Box(new(cell.X+2,cell.Y+124,cell.Width-4,24),new Color(18,35,46));
                _ui.Center(robot.ReturnHomeStatus??"国へ帰る予約",new(cell.X+3,cell.Y+124,cell.Width-6,26),.25f,Accent);
                if(cell.Contains(_pointer))hover=robot.ReturnHomeStatus??"国へ帰る予約";
            }
            if(robot.FunctionDisplayNode is int origin)_ui.Text("元地点 "+origin,new(cell.X+92,cell.Y+151),.23f,Cream);
            if(cellHover){var edge=canReserve?ControlEdge:new Color(184,102,104);_ui.Box(new(cell.X,cell.Y,cell.Width,3),edge);_ui.Box(new(cell.X,cell.Bottom-3,cell.Width,3),edge);_ui.Box(new(cell.X,cell.Y,3,cell.Height),edge);_ui.Box(new(cell.Right-3,cell.Y,3,cell.Height),edge);}
        }
        if(!hoveringPart && !_hangarDrag.HasValue){
            var arrivals=_setup.Robots.DirectDelivery.Orders.Where(o=>o.Target==_populationNode).ToArray();
            int planned=Enumerable.Range(0,_setup.Robots.Nodes.Length).Where(n=>n!=_populationNode).Sum(n=>Enumerable.Range(0,_setup.ActiveCount).Sum(owner=>new[]{RobotParts.Head,RobotParts.Body,RobotParts.Legs}.Sum(part=>new[]{false,true}.Sum(disposal=>_setup.Robots.Transport.Plan(n,owner,part,disposal).Count(w=>w.Target==_populationNode && w.Degrees>0)))));
            if(arrivals.Length>0 || planned>0)hover=$"到着予定：直送便 {arrivals.Length}件（半透明） / 長期輸送設定 {planned}件（実輸送量はターン終了時に決定）";
        }
        bool hoveringManagement=HangarAutoCheck.Contains(_pointer);
        if(hoveringManagement)hover=manage?"自動合体と輸送時の自動分解を止めます / 手動操作はできます":"他国のパーツ管理設定は変更できません";
        _ui.Text(hoveringManagement?hover:_hangarAbandonMode?"部位を選択：パーツを放棄 / 区画の余白：個体を放棄":HangarPlanningFailure.Length>0?HangarPlanningFailure:_hangarMessage.Length>0 && !hoveringPart?_hangarMessage:hover,new(84,838),.32f,Cream);
        _ui.Button(HangarAssemblyButton,"組み立て・分解",DomesticOperationsAllowed && !reservationMode?Accent:Muted,.42f);
        if(!VisitingForeignHangar){
            string[] labels={"廃棄予約","守備専用予約","工場作業員予約","迎撃専用予約"};
            for(int i=0;i<4;i++){
                var button=HangarReservationButton(i);var color=_hangarReservation==(HangarReservation)(i+1)?Accent:Muted;
                _ui.Button(button,"",color,.36f);
                if(i==0)DrawDirectoryRoleIcon(5,new(button.X+6,button.Y+9,24,24),UiPainter.ContrastInk(color));
                else RobotWorkArt.DrawFlag(_ui,new(button.X+6,button.Y+6,24,30),_setup.OwnerColor(_setup.PlayerSlot),DefenseFlagWave());
                _ui.Center(labels[i],new(button.X+32,button.Y,button.Width-36,button.Height),.32f,UiPainter.ContrastInk(color));
            }
        }
        if(HangarPlanningNode==_populationNode && _setup.Nodes.All[_populationNode].Owner==_setup.PlayerSlot)_ui.Button(HangarAbandonButton,"放棄",_hangarAbandonMode?Accent:Muted,.42f);
        _ui.Button(HangarSalvageButton,"放棄品置き場",Muted,.38f);
        var check=new Rectangle(HangarAutoCheck.X,HangarAutoCheck.Y+3,18,18);
        var checkColor=manage?(hoveringManagement?Accent:Cream):Cream*.45f;
        _ui.Box(check,checkColor);_ui.Box(new(check.X+2,check.Y+2,14,14),new Color(18,35,46));
        if(workshop.AutoAssemblyDisabled)_ui.Center("✓",check,.35f,checkColor);
        _ui.Text("パーツ構成を自分で管理する",new(check.Right+8,HangarAutoCheck.Y+4),.36f,checkColor);
        DrawReturnHomeHangar();
        DrawNodeTabs();DrawHangarPages();
        if(_hangarDrag.HasValue)
        {
            var robot=store.Robots.FirstOrDefault(r=>r.Id==_hangarDrag);if(robot!=null)
            {DrawHangarDrag(robot);}
        }
        else DrawDeliveryTagHint(new(54,150,850,825));
    }
    private void DrawHangarDeliveryTags(int slot,DirectPartOrder[] orders,Robot robot)
    {
        for(int row=0;row<3;row++){
            bool incoming=orders.Any(o=>(o.Part&HangarPart(row))!=0);
            bool outgoing=robot!=null && _setup.Robots.DirectDelivery.Orders.Any(o=>o.Source==_populationNode && o.RobotId==robot.Id && (o.Part&HangarPart(row))!=0);
            var part=HangarPartBounds(slot,row);
            if(incoming)DrawHangarDeliveryTag(part,"入荷予定");
            if(outgoing)DrawHangarDeliveryTag(part,"出荷予定",incoming);
            else if(robot!=null && (robot.Parts&HangarPart(row))!=0 && _setup.Robots.DirectDelivery.FailedPart(_populationNode,robot.Id,HangarPart(row)))DrawHangarDeliveryTag(part,"出荷失敗",incoming);
        }
    }
    private void DrawHangarDeliveryTag(Rectangle part,string label,bool left=false,int gap=8)
    {
            const int tagWidth=60;
            var tag=new Rectangle(left?part.Left-gap-tagWidth:part.Right+gap,part.Y+7,tagWidth,23);
            var ink=new Color(54,38,20);var paper=new Color(255,221,139);
            if(label=="出荷失敗")paper=new Color(255,177,163);
            _ui.Box(new(left?tag.Right:part.Right-4,tag.Y+10,gap+5,2),paper);
            _ui.Box(new(tag.X-1,tag.Y-1,tag.Width+2,tag.Height+2),ink);
            _ui.Box(tag,paper);
            _ui.Box(new(tag.X+3,tag.Y+9,3,3),ink);
            _ui.Center(label,new(tag.X+7,tag.Y,tag.Width-7,tag.Height),.3f,ink);
            if(tag.Contains(_pointer))_deliveryTagHint=label=="出荷失敗"?"配送先が敵国になったため出荷に失敗しました（表示のみ・予約解除済み）":label=="出荷予定"?"内政終了後の配送フェーズで出荷予定です":"この部位へ入荷予定です（配送フェーズで配送）";
    }
    private void DrawDeliveryTagHint(Rectangle area)
    {
        if(_deliveryTagHint.Length==0)return;
        const float scale=.38f;
        int width=(int)Math.Ceiling(_font.MeasureString(_deliveryTagHint).X*scale)+24;
        var box=new Rectangle(Math.Clamp(_pointer.X+18,area.Left+8,area.Right-width-8),Math.Clamp(_pointer.Y-48,area.Top+8,area.Bottom-46),width,38);
        _ui.Box(box,Cream);_ui.Box(new(box.X+2,box.Y+2,box.Width-4,box.Height-4),new Color(16,32,43));
        _ui.Text(_deliveryTagHint,new(box.X+12,box.Y+9),scale,Cream);
    }
    private void DrawHangarDrag(Robot robot)
    {
        var origin=_pointer-_hangarGrabOffset;
        var bounds=new Rectangle(origin.X,origin.Y,68,102);
        DrawRobotGlyph(bounds,robot with {Parts=_hangarPart,IntegratedParts=robot.IntegratedParts&_hangarPart},whiteOutline:true);
    }
    private void SyncHangarSlots()
    {
        Array.Resize(ref _hangarSlots,PlanningStock.Nodes[HangarPlanningNode].TotalCapacity);
        _hangarPage=Math.Min(_hangarPage,FunctionPageCount(HangarPlanningNode)-1);
        var robots=PlanningStock.Nodes[HangarPlanningNode].Robots;
        for(int i=0;i<_hangarSlots.Length;i++)if(_hangarSlots[i].HasValue && !robots.Any(r=>r.Id==_hangarSlots[i]))_hangarSlots[i]=null;
        foreach(var robot in robots)if(!_hangarSlots.Contains(robot.Id)){int slot=Array.FindIndex(_hangarSlots,id=>!id.HasValue);if(slot>=0)_hangarSlots[slot]=robot.Id;}
    }
}
