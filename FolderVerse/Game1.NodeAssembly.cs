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
    private static readonly Rectangle NodeAssemblyTile=new(672,875,182,84),HangarAutoCheck=new(84,240,770,42);
    private static Rectangle HangarCell(int slot)=>new(84+slot%4*196,300+slot/4*182,182,174);
    private static RobotParts HangarPart(int row)=>row==0?RobotParts.Head:row==1?RobotParts.Body:RobotParts.Legs;
    private static Rectangle HangarPartBounds(int slot,int row)
    {var cell=HangarCell(slot);return new(cell.X+56,cell.Y+10+row*38,70,36);}
    private bool CanMoveHangarRobot(Robot robot)=>CanManageNodeFactory(robot.Owner) &&
        !_setup.Campaign.RobotBattles.LockedNode(_populationNode) &&
        _setup.Robots.Workshops[_populationNode].DisposalTarget!=robot.Id &&
        !_setup.Robots.PendingEdits.Any(e=>e.Ids.Contains(robot.Id));
    private void OpenNodeAssembly()
    {
        if(!CanManageNodeFactory(_setup.Nodes.All[_populationNode].Owner))return;
        _nodeAssemblyOpen=true;_nodeFactoryOpen=_nodeTransportOpen=_nodeDisposalOpen=false;
        _hangarDrag=null;_hangarMessage="";Array.Clear(_hangarSlots);
        var robots=_setup.Robots.Nodes[_populationNode].Robots;
        for(int i=0;i<robots.Count;i++)_hangarSlots[i]=robots[i].Id;
    }
    private void NodeAssemblyInput(MouseState mouse,bool click,bool escape)
    {
        if(escape){if(_hangarDrag.HasValue)_hangarDrag=null;else _nodeAssemblyOpen=false;return;}
        if(click && (NodeFactoryTile.Contains(_pointer) || NodeTransportTile.Contains(_pointer) || NodeDisposalTile.Contains(_pointer) || NodeAssemblyTile.Contains(_pointer)))
        {_hangarDrag=null;NodeDialogClick(_pointer,new KeyboardState());return;}
        if(click && new Rectangle(803,169,58,44).Contains(_pointer)){_hangarDrag=null;_nodeAssemblyOpen=false;return;}
        if(click && HangarAutoCheck.Contains(_pointer))
        {
            if(CanManageNodeFactory(_setup.Nodes.All[_populationNode].Owner) && !_setup.Campaign.RobotBattles.LockedNode(_populationNode))
            {var workshop=_setup.Robots.Workshops[_populationNode];workshop.AutoAssemblyDisabled=!workshop.AutoAssemblyDisabled;}
            return;
        }
        var store=_setup.Robots.Nodes[_populationNode];
        if(click)
        {
            for(int slot=0;slot<12;slot++)
            {
                var robot=store.Robots.FirstOrDefault(r=>r.Id==_hangarSlots[slot]);if(robot==null || !CanMoveHangarRobot(robot))continue;
                for(int row=0;row<3;row++)if(HangarPartBounds(slot,row).Contains(_pointer) && (robot.Parts&HangarPart(row))!=0)
                {_hangarDrag=robot.Id;_hangarPart=HangarPart(row);_hangarSource=slot;_hangarMessage="空きセルか、同じ所有国のパーツが重複しないセルへドロップ";return;}
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
            if(!CanManageNodeFactory(_setup.Nodes.All[_populationNode].Owner) || !CanMoveHangarRobot(robot) || target!=null && !CanMoveHangarRobot(target))throw new InvalidOperationException();
            long id=_setup.Robots.MovePart(_populationNode,source,_hangarPart,_hangarSlots[destination]);
            if(destination!=_hangarSource && (id==source || !store.Robots.Any(r=>r.Id==source)))_hangarSlots[_hangarSource]=null;
            _hangarSlots[destination]=id;_hangarMessage="手動でパーツの配置を調整しました";
        }
        catch(InvalidOperationException){_hangarMessage="重複パーツ・所有国・廃棄フラグ・容量・予約を確認してください";}
        catch(ArgumentException){_hangarMessage="そのパーツは移動できません";}
    }
    private void DrawNodeAssemblyTile()
    {
        bool enabled=CanManageNodeFactory(_setup.Nodes.All[_populationNode].Owner);
        _ui.Box(NodeAssemblyTile,_nodeAssemblyOpen || enabled && NodeAssemblyTile.Contains(_pointer)?Accent:Muted);
        _spriteBatch.Draw(_robotAssemblyIcon,new Rectangle(682,891,52,52),enabled?Color.White:Color.White*.35f);
        _ui.Center("ロボット組み立て",new(736,887,112,30),.24f,enabled?Cream:Cream*.4f);
        _ui.Center("・分解",new(736,917,112,26),.28f,enabled?Cream:Cream*.4f);
        if(_nodeAssemblyOpen)_ui.Box(new(NodeAssemblyTile.X,NodeAssemblyTile.Y,NodeAssemblyTile.Width,4),Cream);
    }
    private void DrawNodeAssembly()
    {
        var workshop=_setup.Robots.Workshops[_populationNode];var store=_setup.Robots.Nodes[_populationNode];
        _ui.Box(new(54,150,850,825),new(27,49,62));
        _ui.Text("ロボット組み立て・分解 / 手動での再調整",new(84,178),.62f,Cream);
        _ui.Button(new(803,169,58,44),"×",Muted,.7f);
        _ui.Button(HangarAutoCheck,(workshop.AutoAssemblyDisabled?"✓ ":"□ ")+"自動での組み立て・分解を行わない",workshop.AutoAssemblyDisabled?Accent:Muted,.48f);
        bool hoveringPart=false;
        string hover="頭・胴・脚をつかんで、格納庫のセルへドラッグ＆ドロップ";
        for(int slot=0;slot<12;slot++)
        {
            var cell=HangarCell(slot);_ui.Box(cell,Muted);
            if(_hangarDrag.HasValue && cell.Contains(_pointer))_ui.Box(new(cell.X,cell.Y,cell.Width,3),Cream);
            var robot=store.Robots.FirstOrDefault(r=>r.Id==_hangarSlots[slot]);if(robot==null)continue;
            DrawRobotGlyph(new(cell.X+57,cell.Y+12,68,102),robot.Owner,robot.Parts,robot.Role==RobotRole.Captain);
            for(int row=0;row<3;row++)
            {
                var bounds=HangarPartBounds(slot,row);var part=HangarPart(row);
                if(!bounds.Contains(_pointer) || (robot.Parts&part)==0)continue;
                hoveringPart=true;
                bool movable=CanMoveHangarRobot(robot);
                var color=movable?Cream:new Color(145,156,166);
                _ui.Box(new(bounds.X,bounds.Y,bounds.Width,2),color);_ui.Box(new(bounds.X,bounds.Bottom-2,bounds.Width,2),color);
                _ui.Box(new(bounds.X,bounds.Y,2,bounds.Height),color);_ui.Box(new(bounds.Right-2,bounds.Y,2,bounds.Height),color);
                hover=movable?$"{PartsLabel(part)}をドラッグ＆ドロップして再配置":"この個体は処理中・予約中、または操作できない所有国です";
            }
            _ui.Center(PartsLabel(robot.Parts)+$" / #{robot.Owner+1}",new(cell.X,cell.Y+126,cell.Width,24),.4f,Cream);
            _ui.Center("ID "+robot.Id+(robot.Disposal?" / 廃棄":""),new(cell.X,cell.Y+148,cell.Width,22),.29f,Cream);
        }
        _ui.Text(_hangarMessage.Length>0 && !hoveringPart?_hangarMessage:hover,new(84,838),.32f,Cream);
        DrawNodeFactoryTile(false);DrawNodeTransportTile();DrawNodeDisposalTile();DrawNodeAssemblyTile();
        if(_hangarDrag.HasValue)
        {
            var robot=store.Robots.FirstOrDefault(r=>r.Id==_hangarDrag);if(robot!=null)
            {_ui.Box(new(_pointer.X+12,_pointer.Y+12,56,74),new Color(25,45,57));DrawRobotGlyph(new(_pointer.X+18,_pointer.Y+16,44,62),robot.Owner,_hangarPart);}
        }
    }
}
