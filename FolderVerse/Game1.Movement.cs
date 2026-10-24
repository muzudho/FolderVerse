namespace FolderVerse;
using System;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

public partial class Game1
{
    private bool _movementOpen;
    private long _escort;
    private bool _routesBeforeMovement;
    private int _selectedMoveNode=-1;
    private static readonly Rectangle MovementButton=new(504,1018,160,52);
    private static readonly Rectangle ConfirmMoveButton=new(570,914,274,48);
    private void OpenMovement()=>OpenMovementWithFit(true);
    private void OpenMovementWithFit(bool fit)
    {
        _nodeInspectorClosing=false;_reignView=false;_warPlanning=false;_net.RevealMovementTargets=false;
        if(_transportOverview)CloseTransportOverview();
        _dragging=false;
        _routesBeforeMovement=_net.ShowRoutes;_net.ShowRoutes=true;_movementOpen=true;
        _populationCell=-1;_escort=Math.Min(_escort,AvailableEscort);_selectedMoveNode=-1;
        _net.MovementTargets.Clear();
        _net.RevealMovementTargets=true;
        _net.MovementTargets.UnionWith(_setup.Routes.NodeChoices(_setup.PlayerSlot).Select(r=>_setup.Nodes.At(r.Target,r.Entry).Id).Where(id=>_setup.Campaign.RobotBattles.CanMarch(_setup,_setup.PlayerSlot,id)));
        _net.SelectedTarget=-1;
        if(fit && _statusGlobe)FitDestinationView();
    }
    private void CloseMovement()
    {
        _invasionPicker=-1;_net.AttackTargetMarkers=false;_net.VisibleOperationNodes=null;_net.VisibleOperationBatteries=null;
        _net.RevealMovementTargets=false;
        if(!_movementOpen)return;
        _movementOpen=false;_net.ShowRoutes=_routesBeforeMovement;
        _selectedMoveNode=-1;_net.SelectedTarget=-1;_net.MovementTargets.Clear();
    }
    private void SelectMoveNode(int node)
    {
        if(!_net.MovementTargets.Contains(node))return;
        _withdrawalEncounter=-1;_selectedMoveNode=node;_net.SelectedTarget=node;if(_warPlanning)BuildInvasionSources(node);
    }
    private long AvailableEscort
    {
        get {if(_setup.Campaign.UseRobotCombat)return _setup.Campaign.RobotBattles.Available(_setup,_setup.PlayerSlot);var node=_setup.Nodes.Current(_setup.PlayerSlot);return node!=null && _setup.Relations.Allied(node.Owner,_setup.PlayerSlot)?node.Population.People[0]:0;}
    }
    private bool MovementClick(Point pointer,KeyboardState keyboard)
    {
        if(!InspectorVisible && PopulationTurnButton.Contains(pointer)){EndPlannedTurn();return true;}
        if(SelectContinuation(pointer))return true;
        bool onMap=MapViewBounds.Contains(pointer);if(!onMap)pointer.X-=NodeInspectorOffset;
        if(_warPlanning && _movementOpen && !onMap && InvasionClick(pointer))return true;
        if(_reignView && PocketButton.Contains(pointer)){_statusMenuOpen=false;OpenPocket();return true;}
        if(_reignView && MovementButton.Contains(pointer)){if(_movementOpen)CloseMovement();else OpenMovement();return true;}
        if(!_movementOpen)return false;
        if(!InspectorVisible && PopulationTurnButton.Contains(pointer))return true;
        if(ConfirmMoveButton.Contains(pointer))
        {
            if(_selectedMoveNode<0 || _setup.Routes.ToNode(_setup.PlayerSlot,_selectedMoveNode)==null)return true;
            _setup.Campaign.Plan(new(_setup.PlayerSlot,_selectedMoveNode,0,_setup.Nodes.Current(_setup.PlayerSlot).Id,true,true,Peaceful:true));
            CloseMovement();return true;
        }

        if(onMap)
        {
            int node=_statusGlobe?HitGlobeNode(pointer,_net.MovementTargets):_net.HitNode(_setup,NetPanel,pointer,_net.MovementTargets);
            if(node>=0){SelectMoveNode(node);return true;}
        }
        long step=keyboard.IsKeyDown(Keys.LeftControl)||keyboard.IsKeyDown(Keys.RightControl)?10000:keyboard.IsKeyDown(Keys.LeftShift)||keyboard.IsKeyDown(Keys.RightShift)?1000:100;
        if(_setup.Campaign.UseRobotCombat)step=1;
        if(new Rectangle(84,339,94,60).Contains(pointer))_escort=Math.Max(0,_escort-step);
        else if(new Rectangle(190,339,94,60).Contains(pointer))_escort=Math.Min(AvailableEscort,_escort+step);
        else if(new Rectangle(305,339,200,60).Contains(pointer))_escort=AvailableEscort;
        else if(new Rectangle(519,339,200,60).Contains(pointer))_escort=0;
        return new Rectangle(54,150,850,825).Contains(pointer);
    }
    private void DrawMovementPanel()
    {
        if(_warPlanning){DrawInvasionPanel();return;}
        int ruler=_setup.PlayerSlot;var current=_setup.Nodes.Current(ruler);
        _escort=Math.Min(_escort,AvailableEscort);
        var choices=_setup.Routes.NodeChoices(ruler);
        _ui.Box(new(54,150,850,825),new(27,49,62));
        _ui.Text("征服者の移動",new(84,177),.9f,Cream);
        string city="現在地　"+(current==null?_setup.Routes.LocationLabel(ruler):_setup.Nodes.Label(current));
        _ui.Text(city,new(84,240),Math.Min(.6f,760/_font.MeasureString(city).X),Cream);
        _ui.Text("征服者とポケットのロボットが同行",new(84,289),.62f,Color.White);


        _ui.Text("地図上の明るい地点を選んでください",new(84,470),.65f,Cream);
        _ui.Text("他国の地点にも移動できます / ターン終了で実行",new(84,878),.49f,new(180,209,219));
        string status=choices.Length==0?"交通路でつながる移動先がない":_selectedMoveNode<0?"点滅する地点をクリックして選択":$"選択：Point {_selectedMoveNode+1} / ターン終了で移動";
        _ui.Text(status,new(84,926),Math.Min(.47f,465/_font.MeasureString(status).X),new(180,209,219));
        _ui.Button(ConfirmMoveButton,"移動を予約",_selectedMoveNode>=0?Accent:Muted,.7f);
    }
}

