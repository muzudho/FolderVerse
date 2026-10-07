namespace FolderVerse;
using System;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

public partial class Game1
{
    private readonly OperationLog _operations=new();
    private string _inputOutcome;
    private bool? _loggedActive;
    private long _lastDragLog;
    private string _lastDisplayed;
    public string OperationLogPath=>_operations.FilePath;
    private object OperationState()=>new
    {
        screen=_screen.ToString(),phase=IsStatusScreen || _screen is Screen.Battle or Screen.Disposition or Screen.InactiveList or Screen.Hierarchy or Screen.Robots or Screen.Pocket or Screen.Transport?"Game":_screen==Screen.Title?"Title":"Setup",
        superiors=_setup.Relations.Superiors,
        party=_setup.Relations.Party(_setup),powered=_setup.Relations.Powered,released=_setup.Relations.Released,teams=_setup.Relations.Teams,pendingCaptives=_setup.Relations.Pending.ToArray(),submitted=_setup.Campaign.Submitted,menuOpen=_statusMenuOpen,
        battleWave=_battleWave,battleAge=_battleAge,battleFocus=_battleFocus,battleCount=_setup.Campaign.Battles.Count,
        robotCombat=_setup.Campaign.UseRobotCombat,robotNode=_robotNode,pocketSelection=_pocketRobot,pocketFromNode=_pocketFromNode,pocketMessage=_pocketMessage,robotPart=(int)_robotPart,robotSelection=_robotSelection.OrderBy(id=>id).ToArray(),robotMessage=_robotMessage,
        transports=_setup.Robots.Transport.LastTransfers.Count,robotPlaying=_robotPlaying,robotResultAge=_robotResultAge,
        continuedBattles=_setup.Campaign.RobotBattles.Encounters.Where(e=>!e.Finished).Select(e=>new{e.Id,e.Source,e.Target,e.Kind,turns=e.Battles.Select(b=>b.Turn).ToArray()}).ToArray(),
        nodeFactoryOpen=_nodeFactoryOpen,factoryPart=(int)_factoryPart,factoryMessage=_factoryMessage,
        overlay=_seedDialog.IsOpen?"SeedDialog":_movementOpen?"Movement":_populationCell>=0?(_nodeFactoryOpen?"RobotFactory":"NodeDialog"):"None",
        view=_statusGlobe?"Globe":"Net",layer=_net.ShowRoutes?"Traffic":"Grid",flags=_net.ShowFlags,batteries=_net.ShowBatteries,
        worldSeed=_setup.WorldSeed,castSeed=_setup.CastSeed,placementSeed=_setup.PlacementSeed,
        turn=_setup.Population.Turn,player=_setup.PlayerSlot,selectedMoveNode=_selectedMoveNode,escort=_escort,
        currentNode=_setup.PlayerSlot>=0 && _setup.PlayerSlot<_setup.ConquerorLocations.Length && _setup.PlayerSlot<_setup.ConquerorPoints.Length && _setup.Nodes.All.Length>0?_setup.Nodes.Current(_setup.PlayerSlot)?.Id:null,
        focusedCell=_statusCell,populationNode=_populationNode,centerFace=_net.CenterFace,rotation=_net.Rotation,
        yaw=_yaw,pitch=_pitch,zoom=_net.Zoom,pan=new{x=_net.Pan.X,y=_net.Pan.Y},dragging=_dragging,mapDragging=_mapDragging
    };
    private string InputTarget(Point point)
    {
        if(_screen==Screen.Disposition)return "conqueror_disposition";
        if(_screen==Screen.InactiveList)return "inactive_conquerors";
        if(_screen==Screen.Hierarchy)return "conqueror_hierarchy";
        if(_screen==Screen.Pocket)return "pocket";
        if(_screen==Screen.Robots)return "robot_workshop";
        if(_screen==Screen.Transport)return "robot_transport_playback";
        if(IsStatusScreen && StatusMenuButton.Contains(point))return "status_menu";
        if(IsStatusScreen && _statusMenuOpen && InactiveMenuItem.Contains(point))return "open_inactive_conquerors";
        if(IsStatusScreen && _statusMenuOpen && HierarchyMenuItem.Contains(point))return "open_conqueror_hierarchy";
        if(_screen==Screen.Battle)
        {
            if(BattleContinue.Contains(point))return "battle_continue";
            foreach(var tile in _battleTiles)if(tile.Slot>=0 && tile.Bounds.Contains(point))return "battle_cell_"+tile.Slot;
            return "battle_background";
        }
        if(_seedDialog.IsOpen)return "seed_dialog";
        if(!IsStatusScreen)return _screen.ToString();
        if(MapViewButton.Contains(point))return "toggle_globe_net";
        if(RouteLayerButton(2).Contains(point))return "toggle_grid_traffic";
        if(RouteLayerButton(0).Contains(point))return "toggle_flags";
        if(RouteLayerButton(1).Contains(point))return "toggle_batteries";
        if(MovementButton.Contains(point))return "movement";
        if(PocketButton.Contains(point))return "open_pocket";
        if(RobotQuickButton.Contains(point))return "open_robot_transport";
        if(_movementOpen && ConfirmMoveButton.Contains(point))return "confirm_move";
        if(OrientationResetButton.Contains(point))return "reset_orientation";
        if(PopulationTurnButton.Contains(point))return "next_turn";
        if(NetPanel.Contains(point))return _statusGlobe?"globe":"net";
        return "other";
    }
    private void HandleInput(MouseState mouse,KeyboardState keyboard,GameTime time,bool active)
    {
        var pointer=CanvasPoint(mouse);var canvas=CanvasBounds();
        var keys=keyboard.GetPressedKeys();bool changedKeys=!keys.SequenceEqual(_previousKeyboard.GetPressedKeys());
        bool buttons=mouse.LeftButton!=_previousMouse.LeftButton || mouse.RightButton!=_previousMouse.RightButton || mouse.MiddleButton!=_previousMouse.MiddleButton;
        bool wheel=mouse.ScrollWheelValue!=_previousMouse.ScrollWheelValue;
        bool drag=(_dragging || _mapDragging) && (mouse.X!=_previousMouse.X || mouse.Y!=_previousMouse.Y) && _operations.ElapsedMilliseconds-_lastDragLog>=100;
        bool focus=_loggedActive!=active;_loggedActive=active;
        bool record=buttons || changedKeys || wheel || drag || focus;
        if(drag)_lastDragLog=_operations.ElapsedMilliseconds;
        object before=record?OperationState():null;
        var input=new{client=new{x=mouse.X,y=mouse.Y},canvas=new{x=pointer.X,y=pointer.Y},
            canvasBounds=new{x=canvas.X,y=canvas.Y,width=canvas.Width,height=canvas.Height},
            window=new{width=GraphicsDevice.PresentationParameters.BackBufferWidth,height=GraphicsDevice.PresentationParameters.BackBufferHeight},
            active,left=mouse.LeftButton.ToString(),right=mouse.RightButton.ToString(),middle=mouse.MiddleButton.ToString(),
            click=mouse.LeftButton==ButtonState.Pressed && _previousMouse.LeftButton==ButtonState.Released,
            wheelDelta=mouse.ScrollWheelValue-_previousMouse.ScrollWheelValue,keys=keys.Select(k=>k.ToString()).ToArray(),target=InputTarget(pointer)};
        _inputOutcome=null;
        if(record)_operations.Write("input_begin",new{input,state=before});
        try
        {
            HandleInputCore(mouse,keyboard,time,active);
            if(record)
            {
                var after=OperationState();
                string outcome=!active?"ignored_inactive_window":_inputOutcome??
                    (OperationLog.StateKey(before)==OperationLog.StateKey(after)?"no_state_change":"state_changed");
                _operations.Write("input_end",new{input,outcome,state=after});
            }
        }
        catch(Exception error){RecordFailure(error,"input");throw;}
    }
    public void RecordFailure(Exception error,string source)=>_operations.Write("exception",new{source,error=error.ToString(),state=OperationState()});
    private void LogDisplayedScreen()
    {
        var window=GraphicsDevice.PresentationParameters;
        string display=$"{_screen}/{_seedDialog.IsOpen}/{_movementOpen}/{_populationCell>=0}/{_statusGlobe}/{_net.ShowRoutes}/{window.BackBufferWidth}/{window.BackBufferHeight}";
        if(display==_lastDisplayed)return;_lastDisplayed=display;
        _operations.Write("screen_displayed",new{state=OperationState(),window=new{width=window.BackBufferWidth,height=window.BackBufferHeight}});
    }
}
