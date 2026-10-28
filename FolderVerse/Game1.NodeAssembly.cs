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
    private readonly long?[] _hangarSlots=new long?[12];
    private long? _hangarDrag;
    private RobotParts _hangarPart;
    private int _hangarSource;
    private Point _hangarGrabOffset;
    private string _hangarMessage="";
    private static readonly Rectangle NodeAssemblyTile=new(678,210,190,40),HangarAutoCheck=new(84,258,770,34);
    private HangarReservation _hangarReservation;
    private static readonly Rectangle HangarAssemblyButton=new(84,860,182,42);
    private static Rectangle HangarReservationButton(int i)=>new(280+i%3*196,860+i/3*48,182,42);
    private int HangarReservationCountry(Robot robot)=>_hangarReservation==HangarReservation.Defense || robot?.DefenseFlag!=null?_setup.PlayerSlot:robot?.Owner??_setup.PlayerSlot;
    private bool HangarReservationEnabled(Robot robot)=>robot!=null && !VisitingForeignHangar && CanManageNodeFactory(_setup.Nodes.All[_populationNode].Owner) && (_hangarReservation==HangarReservation.Defense || CanManageNodeFactory(robot.Owner)) && _setup.Robots.CanSetReservation(_populationNode,robot,_hangarReservation,HangarReservationCountry(robot));
    private static Rectangle HangarCell(int slot)=>new(84+slot%4*196,300+slot/4*182,182,174);
    private static Rectangle HangarRobotBounds(int slot)
    {var cell=HangarCell(slot);return new(cell.X+57,cell.Y+12,68,102);}
    private static RobotParts HangarPart(int row)=>row==0?RobotParts.Head:row==1?RobotParts.Body:RobotParts.Legs;
    private static Rectangle HangarPartBounds(int slot,int row)
    {var cell=HangarCell(slot);return new(cell.X+56,cell.Y+10+row*38,70,36);}
    private bool VisitingForeignHangar=>_setup.Nodes.All[_populationNode].Owner>=0 && !CanManageNodeFactory(_setup.Nodes.All[_populationNode].Owner) &&
        _setup.Relations.CanAct(_setup.PlayerSlot) && _setup.Nodes.Current(_setup.PlayerSlot)?.Id==_populationNode;
    private bool CanOpenHangar=>_setup.Nodes.All[_populationNode].Owner<0 || CanManageNodeFactory(_setup.Nodes.All[_populationNode].Owner) || VisitingForeignHangar;
    private bool CanMoveHangarRobot(Robot robot)=>CanOpenHangar && !_setup.Campaign.RobotBattles.LockedNode(_populationNode) && !_setup.Robots.AssemblyLocked(_populationNode,robot);
    private bool CanMoveHangarPart(Robot robot,RobotParts part)=>CanOpenHangar && !_setup.Campaign.RobotBattles.LockedNode(_populationNode) && !_setup.Robots.PartLocked(_populationNode,robot,part) &&
        (CanManageNodeFactory(_setup.Nodes.All[_populationNode].Owner) || new[]{RobotParts.Head,RobotParts.Body,RobotParts.Legs}.Where(p=>(robot.ConnectedParts(part)&p)!=0).All(p=>robot.Origin(p)==_setup.PlayerSlot));
    private bool HangarPlaceEnabled(Robot target,RobotParts part)
    {
        var dragging=_hangarDrag.HasValue?_setup.Robots.Nodes[_populationNode].Robots.FirstOrDefault(r=>r.Id==_hangarDrag):null;
        part=dragging?.ConnectedParts(part)??target?.ConnectedParts(part)??part;
        if(!CanOpenHangar || _setup.Campaign.RobotBattles.LockedNode(_populationNode) || target!=null && (!CanMoveHangarPart(target,part) || !target.CanSeparate(target.Parts&part) || (target.Parts&part)!=0 && (target.Parts&part)!=part))return false;
        if(target!=null && (target.Parts&part)!=0 && !CanMoveHangarPart(target,part))return false;
        if(_hangarDrag.HasValue)
        {
            var source=_setup.Robots.Nodes[_populationNode].Robots.FirstOrDefault(r=>r.Id==_hangarDrag);
            if(source==null || !CanMoveHangarPart(source,_hangarPart) || part!=_hangarPart || target!=null && source.Disposal!=target.Disposal)return false;
            if(target==null && source.Parts!=part && !_setup.Robots.Nodes[_populationNode].HasSpace)return false;
        }
        return true;
    }
    private void OpenNodeAssembly()
    {
        _hangarAbandonMode=false;
        _nodeOfficeOpen=false;_lastNodeTab=3;
        _nodeAssemblyOpen=true;_nodeFactoryOpen=_nodeTransportOpen=_nodeDisposalOpen=false;
        _hangarDrag=null;_hangarReservation=HangarReservation.None;_hangarMessage="";Array.Clear(_hangarSlots);
        var robots=_setup.Robots.Nodes[_populationNode].Robots;
        for(int i=0;i<robots.Count;i++)_hangarSlots[i]=robots[i].Id;
    }
    private void NodeAssemblyInput(MouseState mouse,bool click,bool escape)
    {
        var original=_pointer;_pointer.X-=NodeInspectorOffset;
        try{NodeAssemblyLocalInput(mouse,click,escape);}finally{_pointer=original;}
    }
    private void NodeAssemblyLocalInput(MouseState mouse,bool click,bool escape)
    {
        if(click && HangarSalvageButton.Contains(_pointer)){OpenSalvagePlan(_populationNode);return;}
        if(click && HangarAbandonButton.Contains(_pointer) && _setup.Nodes.All[_populationNode].Owner==_setup.PlayerSlot){_hangarAbandonMode=!_hangarAbandonMode;_hangarReservation=HangarReservation.None;_hangarDrag=null;return;}
        if(click && _hangarAbandonMode){
            var stock=_setup.Robots.Nodes[_populationNode];SyncHangarSlots();
            for(int i=0;i<12;i++)if(HangarCell(i).Contains(_pointer)){
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
            {var workshop=_setup.Robots.Workshops[_populationNode];workshop.AutoAssemblyDisabled=!workshop.AutoAssemblyDisabled;}
            return;
        }
        var store=_setup.Robots.Nodes[_populationNode];
        SyncHangarSlots();
        if(click)
        {
            for(int slot=0;slot<12;slot++)
            {
                var robot=store.Robots.FirstOrDefault(r=>r.Id==_hangarSlots[slot]);if(robot==null)continue;
                var cell=HangarCell(slot);
                if(_hangarReservation!=HangarReservation.None && cell.Contains(_pointer)){
                    bool accepted=HangarReservationEnabled(robot) && _setup.Robots.ToggleReservation(_populationNode,robot.Id,_hangarReservation,HangarReservationCountry(robot));
                    if(accepted){
                        var reserved=store.Robots.Single(r=>r.Id==robot.Id);
                        if(_hangarReservation==HangarReservation.Defense && reserved.DefenseOnly){BeginDefenseReservationEffect(reserved);_hangarMessage="旗で守備専用予約 / 靴を履いて仕事中";}
                        else{_defenseReservationEffects.Remove(robot.Id);_hangarMessage="予約を切り替えました / 続けて個体を選択できます";}
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
        int destination=Enumerable.Range(0,12).FirstOrDefault(i=>HangarCell(i).Contains(_pointer),-1);
        if(destination<0){_hangarMessage="ドラッグを取り消しました";return;}
        try
        {
            var robot=store.Robots.Single(r=>r.Id==source);
            var target=store.Robots.FirstOrDefault(r=>r.Id==_hangarSlots[destination]);
            if(!CanMoveHangarPart(robot,_hangarPart) || target!=null && !CanMoveHangarPart(target,_hangarPart))throw new InvalidOperationException();
            if(target!=null && (target.Parts&_hangarPart)!=0 && !CanMoveHangarPart(target,_hangarPart))throw new InvalidOperationException();
            long id=_setup.Robots.MovePart(_populationNode,source,_hangarPart,_hangarSlots[destination],_setup.PlayerSlot);
            if(destination!=_hangarSource && (id==source || !store.Robots.Any(r=>r.Id==source)))_hangarSlots[_hangarSource]=null;
            _hangarSlots[destination]=id;_hangarMessage="手動でパーツの配置を調整しました";
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
        SyncHangarSlots();
        var workshop=_setup.Robots.Workshops[_populationNode];var store=_setup.Robots.Nodes[_populationNode];
        _ui.Box(new(54,150,850,825),new(27,49,62));


        bool manage=CanManageNodeFactory(_setup.Nodes.All[_populationNode].Owner);
        _ui.Button(HangarAutoCheck,manage?(workshop.AutoAssemblyDisabled?"✓ ":"□ ")+"自動での組み立て・分解を行わない":"他国の自動組立設定は変更できません",manage && workshop.AutoAssemblyDisabled?Accent:Muted,.48f);
        bool hoveringPart=false;
        bool reservationMode=_hangarReservation!=HangarReservation.None;
        string hover=VisitingForeignHangar?"滞在中：自国パーツ同士の交換・空き部位への移動ができます":CanOpenHangar?"頭・胴・脚をつかんで、格納庫の区画へドラッグ＆ドロップ":"他国の地点：自分の征服者がいる地点では自国パーツを動かせます";
        if(reservationMode)hover="区画をクリック：予約 / 同じ国の同じ予約は解除 / 続けて選択できます";
        for(int slot=0;slot<12;slot++)
        {
            var cell=HangarCell(slot);var occupant=store.Robots.FirstOrDefault(r=>r.Id==_hangarSlots[slot]);
            bool cellHover=reservationMode && cell.Contains(_pointer) && occupant!=null;
            bool canReserve=reservationMode && HangarReservationEnabled(occupant);
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
            var robot=store.Robots.FirstOrDefault(r=>r.Id==_hangarSlots[slot]);if(robot==null)continue;robot=HomePersuasionRobot(robot);
            DrawRobotGlyph(HangarRobotBounds(slot),robot,whiteOutline:true,partOpacity:part=>reservationMode?(canReserve?1:.45f):HangarPlaceEnabled(occupant,part)?1:.22f);
            for(int row=0;!reservationMode && row<3;row++)
            {
                var bounds=HangarPartBounds(slot,row);var part=HangarPart(row);
                if(!CanMoveHangarPart(robot,part) || !bounds.Contains(_pointer) || (robot.Parts&part)==0)continue;
                hoveringPart=true;
                bool movable=CanMoveHangarPart(robot,part);
                var color=movable?ControlEdge:new Color(145,156,166);
                _ui.Box(new(bounds.X,bounds.Y,bounds.Width,2),color);_ui.Box(new(bounds.X,bounds.Bottom-2,bounds.Width,2),color);
                _ui.Box(new(bounds.X,bounds.Y,2,bounds.Height),color);_ui.Box(new(bounds.Right-2,bounds.Y,2,bounds.Height),color);
                hover=movable?$"{PartsLabel(part)}{(robot.IsCheap(part)?"（安い）":"")}{(robot.IsDamaged(part)?"（損傷・未所属扱い）":"")}をドラッグ＆ドロップして再配置":"この個体は処理中・予約中、または操作できない所有国です";
            }
            DrawDefenseReservationEffect(cell,robot);
            string partsName=PartsLabel(robot)+$" / #{robot.Owner+1}";
            if(robot.ShoeOwner>=0 && DefenseShoeProgress(robot)>.6f)DrawDefenseReservationLabel(new(cell.X,cell.Y+126,cell.Width,24),robot.ShoeOwner);
            else if(robot.ShoeOwner<0)_ui.Center(partsName,new(cell.X,cell.Y+126,cell.Width,24),Math.Min(.4f,(cell.Width-8)/Math.Max(1,_font.MeasureString(partsName).X)),Cream);
            _ui.Center("ID "+robot.Id+(robot.DamagedParts!=0?" / 損傷":""),new(cell.X,cell.Y+148,90,22),.27f,Cream);
            string reservation=robot.FactoryWorker?"工場作業員":robot.InterceptOnly?"迎撃専用":robot.DefenseOnly?"守備専用":robot.Disposal?"廃棄予約":_setup.Robots.MarchReserved(_populationNode,robot)?"出撃予約":"";
            if(robot.ShoeOwner>=0)reservation="";
            if(reservation.Length>0)_ui.Center(reservation,new(cell.X+88,cell.Y+148,cell.Width-88,22),.27f,Accent);
            if(cellHover){var edge=canReserve?ControlEdge:new Color(184,102,104);_ui.Box(new(cell.X,cell.Y,cell.Width,3),edge);_ui.Box(new(cell.X,cell.Bottom-3,cell.Width,3),edge);_ui.Box(new(cell.X,cell.Y,3,cell.Height),edge);_ui.Box(new(cell.Right-3,cell.Y,3,cell.Height),edge);}
        }
        _ui.Text(_hangarAbandonMode?"部位を選択：パーツを放棄 / 区画の余白：個体を放棄":_hangarMessage.Length>0 && !hoveringPart?_hangarMessage:hover,new(84,838),.32f,Cream);
        _ui.Button(HangarAssemblyButton,"組み立て・分解",!reservationMode?Accent:Muted,.42f);
        if(!VisitingForeignHangar){
            string[] labels={"廃棄予約","守備専用予約","工場作業員予約","迎撃専用予約"};
            for(int i=0;i<4;i++){
                var button=HangarReservationButton(i);var color=_hangarReservation==(HangarReservation)(i+1)?Accent:Muted;
                if(i!=1){_ui.Button(button,labels[i],color,.36f);continue;}
                _ui.Button(button,"",color,.36f);RobotWorkArt.DrawFlag(_ui,new(button.X+6,button.Y+6,24,30),_setup.OwnerColor(_setup.PlayerSlot),DefenseFlagWave());
                _ui.Center(labels[i],new(button.X+32,button.Y,button.Width-36,button.Height),.32f,UiPainter.ContrastInk(color));
            }
        }
        if(_setup.Nodes.All[_populationNode].Owner==_setup.PlayerSlot)_ui.Button(HangarAbandonButton,"放棄",_hangarAbandonMode?Accent:Muted,.42f);
        _ui.Button(HangarSalvageButton,"放棄品置き場",Muted,.38f);
        DrawNodeTabs();
        if(_hangarDrag.HasValue)
        {
            var robot=store.Robots.FirstOrDefault(r=>r.Id==_hangarDrag);if(robot!=null)
            {DrawHangarDrag(robot);}
        }
    }
    private void DrawHangarDrag(Robot robot)
    {
        var origin=_pointer-_hangarGrabOffset;
        var bounds=new Rectangle(origin.X,origin.Y,68,102);
        DrawRobotGlyph(bounds,robot with {Parts=_hangarPart,IntegratedParts=robot.IntegratedParts&_hangarPart},whiteOutline:true);
    }
    private void SyncHangarSlots()
    {
        var robots=_setup.Robots.Nodes[_populationNode].Robots;
        for(int i=0;i<_hangarSlots.Length;i++)if(_hangarSlots[i].HasValue && !robots.Any(r=>r.Id==_hangarSlots[i]))_hangarSlots[i]=null;
        foreach(var robot in robots)if(!_hangarSlots.Contains(robot.Id)){int slot=Array.FindIndex(_hangarSlots,id=>!id.HasValue);if(slot>=0)_hangarSlots[slot]=robot.Id;}
    }
}

