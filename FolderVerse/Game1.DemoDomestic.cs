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
    private long[] _demoAssemblySelection;
    private Point DemoDestinationPoint()=>DestinationNodes().Select(DestinationBadge).FirstOrDefault(r=>r!=Rectangle.Empty).Center;
    private bool UpdateDemoDomestic(double elapsed)
    {
        _demoStepAge=0;
        Point target=_demoDomesticStage switch
        {
            0=>new(480,260),1=>NodeTransportTile.Center,2=>DemoDestinationPoint(),3=>NodeAssemblyTile.Center,
            4=>_demoSourceSlot<0?new(480,540):HangarPartBounds(_demoSourceSlot,1).Center,
            5=>_demoEmptySlot<0?new(480,540):HangarCell(_demoEmptySlot).Center,
            6=>_demoEmptySlot<0?new(480,540):HangarPartBounds(_demoEmptySlot,1).Center,
            7=>_demoSourceSlot<0?new(480,540):HangarCell(_demoSourceSlot).Center,
            8=>NodeFactoryTile.Center,9=>FactoryRobotButton(0,3).Center,10=>FactoryPlanButton(0).Center,11=>new(832,190),12=>MovementButton.Center,13=>DemoDestinationPoint(),
            14=>new(402,366),_=>ConfirmMoveButton.Center
        };
        if(_demoSwing<=0)_demoCursor=Vector2.Lerp(_demoCursor,new(target.X,target.Y),1-MathF.Exp(-(float)elapsed*4));
        if(_hangarDrag.HasValue)_pointer=new((int)_demoCursor.X,(int)_demoCursor.Y);
        _demoDomesticAge+=elapsed;if(_demoDomesticAge<1.8)return true;
        _demoDomesticAge=0;_demoCursor=new(target.X,target.Y);_pointer=target;_ui.Pointer=target;StartDemoClick();
        switch(_demoDomesticStage)
        {
            case 0:
                var node=_setup.Nodes.All.Where(n=>n.Owner==_setup.PlayerSlot && !_setup.Campaign.RobotBattles.LockedNode(n.Id)).OrderByDescending(n=>_setup.Robots.AssemblySelection(n.Id,_setup.PlayerSlot,true)!=null).FirstOrDefault();
                if(node==null){_demoDomesticStage=12;return true;}
                OpenNodeInspector(node.Id);OpenNodeOffice();break;
            case 1:OpenNodeTransport();break;
            case 2:
                break; // Show the real transport plan; do not invent demonstration-only orders.
            case 3:
                OpenNodeAssembly();
                _demoAssemblySelection=_setup.Robots.AssemblySelection(_populationNode,_setup.PlayerSlot,true);
                if(_demoAssemblySelection==null){_demoDomesticStage=7;break;}
                _demoSourceSlot=Array.FindIndex(_hangarSlots,id=>id==_demoAssemblySelection[1]);
                _demoEmptySlot=Array.FindIndex(_hangarSlots,id=>id==_demoAssemblySelection[0]);break;
            case 4:
                _hangarMessage="出撃条件を満たす組み合わせを組み立てます";break;
            case 5:
                if(_demoAssemblySelection!=null && _demoAssemblySelection.All(id=>_setup.Robots.Nodes[_populationNode].Robots.Any(r=>r.Id==id && !_setup.Robots.AssemblyLocked(_populationNode,r)))){
                    _setup.Robots.Assemble(_populationNode,_demoAssemblySelection,_setup.PlayerSlot);SyncHangarSlots();_hangarMessage="出撃できるロボットを組み立てました";
                }
                _demoAssemblySelection=null;_demoDomesticStage=7;break;
            case 8:_nodeAssemblyOpen=false;OpenNodeFactory();break;
            case 9:if(_nodeFactoryOpen){_factoryPlanLine=0;_factoryLine=0;}break;
            case 10:if(_nodeFactoryOpen){_factoryPlanLine=-1;}break;
            case 11:ResetInspector();break;
            case 12:OpenMovement();break;
            case 13:MovementClick(target,new KeyboardState());break;
            case 14:MovementClick(target,new KeyboardState());break;
            default:
                if(_selectedMoveNode>=0)MovementClick(target,new KeyboardState());
                else{CloseMovement();_setup.Campaign.Advance(_setup,-1,0);_world.ShowSetup(_setup,true);OpenBattle();}
                if(_selectedMoveNode<0 && _setup.Campaign.PlannedOrders.Count>0)EndPlannedTurn();
                _demoDomesticStage=0;_demoMapVisits++;_demoMapStage=0;_demoMapAge=0;return true;
        }
        _demoDomesticStage++;return true;
    }
}
