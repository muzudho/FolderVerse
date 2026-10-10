namespace FolderVerse;
using System;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

public partial class Game1
{
    private int _demoDomesticStage;
    private double _demoDomesticAge;
    private int _demoSourceSlot=-1,_demoEmptySlot=-1;
    private bool UpdateDemoDomestic(double elapsed)
    {
        _demoStepAge=0;
        Point target=_demoDomesticStage switch
        {
            0=>new(480,260),1=>NodeTransportTile.Center,2=>new(794,483),3=>NodeAssemblyTile.Center,
            4=>_demoSourceSlot<0?new(480,540):HangarPartBounds(_demoSourceSlot,1).Center,
            5=>_demoEmptySlot<0?new(480,540):HangarCell(_demoEmptySlot).Center,
            6=>_demoEmptySlot<0?new(480,540):HangarPartBounds(_demoEmptySlot,1).Center,
            7=>_demoSourceSlot<0?new(480,540):HangarCell(_demoSourceSlot).Center,
            8=>new(832,190),9=>MovementButton.Center,10=>MarchButton(0).Center,
            11=>new(402,366),_=>ConfirmMoveButton.Center
        };
        if(_demoSwing<=0)_demoCursor=Vector2.Lerp(_demoCursor,new(target.X,target.Y),1-MathF.Exp(-(float)elapsed*4));
        if(_hangarDrag.HasValue)_pointer=new((int)_demoCursor.X,(int)_demoCursor.Y);
        _demoDomesticAge+=elapsed;if(_demoDomesticAge<1.8)return true;
        _demoDomesticAge=0;_demoCursor=new(target.X,target.Y);_pointer=target;_ui.Pointer=target;StartDemoClick();
        switch(_demoDomesticStage)
        {
            case 0:
                var node=_setup.Nodes.All.FirstOrDefault(n=>n.Owner==_setup.PlayerSlot && !_setup.Campaign.RobotBattles.LockedNode(n.Id));
                if(node==null){_demoDomesticStage=9;return true;}
                _populationNode=node.Id;_populationCell=node.Cell;_nodeAssemblyOpen=_nodeTransportOpen=_nodeDisposalOpen=_nodeFactoryOpen=false;break;
            case 1:OpenNodeTransport();break;
            case 2:
                // Use the ordinary +5% control; the plan is retained for subsequent turns.
                NodeTransportClick(target);break;
            case 3:
                OpenNodeAssembly();
                _demoSourceSlot=Enumerable.Range(0,12).FirstOrDefault(i=>_hangarSlots[i].HasValue && _setup.Robots.Nodes[_populationNode].Robots.Any(r=>r.Id==_hangarSlots[i] && r.Parts==RobotParts.Complete && CanMoveHangarRobot(r)),-1);
                _demoEmptySlot=Array.FindIndex(_hangarSlots,id=>!id.HasValue);
                if(_demoSourceSlot<0 || _demoEmptySlot<0)_demoDomesticStage=7;break;
            case 4:case 6:NodeAssemblyInput(new MouseState(target.X,target.Y,0,ButtonState.Pressed,ButtonState.Released,ButtonState.Released,ButtonState.Released,ButtonState.Released),true,false);break;
            case 5:case 7:
                _previousMouse=new MouseState(target.X,target.Y,0,ButtonState.Pressed,ButtonState.Released,ButtonState.Released,ButtonState.Released,ButtonState.Released);
                NodeAssemblyInput(new MouseState(target.X,target.Y,0,ButtonState.Released,ButtonState.Released,ButtonState.Released,ButtonState.Released,ButtonState.Released),false,false);break;
            case 8:_nodeAssemblyOpen=false;_populationCell=-1;break;
            case 9:OpenMovement();break;
            case 10:MovementClick(target,new KeyboardState());break;
            case 11:MovementClick(target,new KeyboardState());break;
            default:
                if(_selectedMoveNode>=0)MovementClick(target,new KeyboardState());
                else{CloseMovement();_setup.Campaign.Advance(_setup,-1,0);_world.ShowSetup(_setup,true);OpenBattle();}
                _demoDomesticStage=0;_demoMapVisits++;_demoMapStage=0;_demoMapAge=0;return true;
        }
        _demoDomesticStage++;return true;
    }
}
