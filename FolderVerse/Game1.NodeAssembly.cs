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
    private string _hangarMessage="";
    private static readonly Rectangle NodeAssemblyTile=new(560,169,230,60),HangarAutoCheck=new(84,240,770,42);
    private static Rectangle HangarCell(int slot)=>new(84+slot%4*196,300+slot/4*182,182,174);
    private static RobotParts HangarPart(int row)=>row==0?RobotParts.Head:row==1?RobotParts.Body:RobotParts.Legs;
    private static Rectangle HangarPartBounds(int slot,int row)
    {var cell=HangarCell(slot);return new(cell.X+56,cell.Y+10+row*38,70,36);}
    private bool CanOpenHangar=>_setup.Nodes.All[_populationNode].Owner<0 || CanManageNodeFactory(_setup.Nodes.All[_populationNode].Owner);
    private bool CanMoveHangarRobot(Robot robot)=>CanOpenHangar && !_setup.Campaign.RobotBattles.LockedNode(_populationNode) && _setup.Robots.Workshops[_populationNode].DisposalTarget!=robot.Id && !_setup.Robots.PendingEdits.Any(e=>e.Ids.Contains(robot.Id));
    private bool CanMoveHangarPart(Robot robot,RobotParts part)=>CanMoveHangarRobot(robot) && (_setup.Nodes.All[_populationNode].Owner>=0 || robot.Affiliation(part)==_setup.PlayerSlot);
    private void OpenNodeAssembly()
    {
        _nodeAssemblyOpen=true;_nodeFactoryOpen=_nodeTransportOpen=_nodeDisposalOpen=false;
        _hangarDrag=null;_hangarMessage="";Array.Clear(_hangarSlots);
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
        if(click && NodeAutomationClick(_pointer)){_hangarDrag=null;return;}
        if(escape){if(_hangarDrag.HasValue)_hangarDrag=null;else CloseNodeInspector();return;}
        if(click && (NodeFactoryTile.Contains(_pointer) || NodeTransportTile.Contains(_pointer) || NodeDisposalTile.Contains(_pointer) || NodeAssemblyTile.Contains(_pointer)))
        {_hangarDrag=null;NodeDialogClick(new Point(_pointer.X+NodeInspectorOffset,_pointer.Y),new KeyboardState());return;}
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
                var robot=store.Robots.FirstOrDefault(r=>r.Id==_hangarSlots[slot]);if(robot==null || !CanMoveHangarRobot(robot))continue;
                var cell=HangarCell(slot);
                if(new Rectangle(cell.Right-36,cell.Bottom-25,18,18).Contains(_pointer)){ToggleRobotDisposal(_populationNode,robot.Id);return;}
                for(int row=0;row<3;row++)if(HangarPartBounds(slot,row).Contains(_pointer) && (robot.Parts&HangarPart(row))!=0 && CanMoveHangarPart(robot,HangarPart(row)))
                {_hangarDrag=robot.Id;_hangarPart=HangarPart(row);_hangarSource=slot;_hangarMessage="同じパーツがあるセルへドロップすると交換";return;}
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
            if(!CanMoveHangarPart(robot,_hangarPart) || target!=null && !CanMoveHangarRobot(target))throw new InvalidOperationException();
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
        _ui.Center("ロボット組立・分解",NodeAssemblyTile,.36f,enabled?(_nodeAssemblyOpen || NodeAssemblyTile.Contains(_pointer)?SelectionInk:Cream):Cream*.4f);
        if(_nodeAssemblyOpen)_ui.Box(new(NodeAssemblyTile.X,NodeAssemblyTile.Y,NodeAssemblyTile.Width,4),Cream);
    }
    private void DrawNodeAssembly()
    {
        SyncHangarSlots();
        var workshop=_setup.Robots.Workshops[_populationNode];var store=_setup.Robots.Nodes[_populationNode];
        _ui.Box(new(54,150,850,825),new(27,49,62));


        _ui.Button(HangarAutoCheck,(workshop.AutoAssemblyDisabled?"✓ ":"□ ")+"自動での組み立て・分解を行わない",workshop.AutoAssemblyDisabled?Accent:Muted,.48f);
        bool hoveringPart=false;
        string hover=CanOpenHangar?"頭・胴・脚をつかんで、格納庫のセルへドラッグ＆ドロップ":"他国の節点：閲覧のみ / 組立・製造計画は自国・手下の節点で変更できます";
        for(int slot=0;slot<12;slot++)
        {
            var cell=HangarCell(slot);var occupant=store.Robots.FirstOrDefault(r=>r.Id==_hangarSlots[slot]);bool owned=occupant==null || CanMoveHangarRobot(occupant);_ui.Box(cell,owned?Muted:new Color(9,19,26));
            if(_hangarDrag.HasValue && cell.Contains(_pointer))_ui.Box(new(cell.X,cell.Y,cell.Width,3),ControlEdge);
            var robot=store.Robots.FirstOrDefault(r=>r.Id==_hangarSlots[slot]);if(robot==null)continue;
            foreach(var part in new[]{RobotParts.Head,RobotParts.Body,RobotParts.Legs})if((robot.Parts&part)!=0)DrawRobotGlyph(new(cell.X+57,cell.Y+12,68,102),robot.Origin(part),part,robot.Role==RobotRole.Captain,whiteOutline:true,damaged:robot.IsDamaged(part),cheap:robot.IsCheap(part));
            for(int row=0;row<3;row++)
            {
                var bounds=HangarPartBounds(slot,row);var part=HangarPart(row);
                if(!CanMoveHangarPart(robot,part) || !bounds.Contains(_pointer) || (robot.Parts&part)==0)continue;
                hoveringPart=true;
                bool movable=CanMoveHangarRobot(robot);
                var color=movable?ControlEdge:new Color(145,156,166);
                _ui.Box(new(bounds.X,bounds.Y,bounds.Width,2),color);_ui.Box(new(bounds.X,bounds.Bottom-2,bounds.Width,2),color);
                _ui.Box(new(bounds.X,bounds.Y,2,bounds.Height),color);_ui.Box(new(bounds.Right-2,bounds.Y,2,bounds.Height),color);
                hover=movable?$"{PartsLabel(part)}{(robot.IsCheap(part)?"（安い）":"")}{(robot.IsDamaged(part)?"（損傷・未所属扱い）":"")}をドラッグ＆ドロップして再配置":"この個体は処理中・予約中、または操作できない所有国です";
            }
            _ui.Center(PartsLabel(robot.Parts)+$" / #{robot.Owner+1}",new(cell.X,cell.Y+126,cell.Width,24),.4f,Cream);
            _ui.Center("ID "+robot.Id+(robot.DamagedParts!=0?" / 損傷":""),new(cell.X,cell.Y+148,90,22),.27f,Cream);
            DrawDisposalCheck(cell,robot);
        }
        _ui.Text(_hangarMessage.Length>0 && !hoveringPart?_hangarMessage:hover,new(84,838),.32f,Cream);
        DrawNodeFactoryTile(false);DrawNodeTransportTile();DrawNodeDisposalTile();DrawNodeAssemblyTile();
        if(_hangarDrag.HasValue)
        {
            var robot=store.Robots.FirstOrDefault(r=>r.Id==_hangarDrag);if(robot!=null)
            {DrawHangarDrag(robot);}
        }
    }
    private void DrawHangarDrag(Robot robot)
    {
        int row=_hangarPart==RobotParts.Head?0:_hangarPart==RobotParts.Body?1:2;
        int partOffset=row==0?0:row==1?34:76;
        DrawRobotGlyph(new(_pointer.X+18,_pointer.Y+16-partOffset,68,102),robot.Origin(_hangarPart),_hangarPart,robot.Role==RobotRole.Captain,whiteOutline:true,damaged:robot.IsDamaged(_hangarPart),cheap:robot.IsCheap(_hangarPart));
    }
    private void SyncHangarSlots()
    {
        var robots=_setup.Robots.Nodes[_populationNode].Robots;
        for(int i=0;i<_hangarSlots.Length;i++)if(_hangarSlots[i].HasValue && !robots.Any(r=>r.Id==_hangarSlots[i]))_hangarSlots[i]=null;
        foreach(var robot in robots)if(!_hangarSlots.Contains(robot.Id)){int slot=Array.FindIndex(_hangarSlots,id=>!id.HasValue);if(slot>=0)_hangarSlots[slot]=robot.Id;}
    }
}

