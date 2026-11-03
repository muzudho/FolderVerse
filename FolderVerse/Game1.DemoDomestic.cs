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
    private Point DemoDestinationPoint()
    {
        foreach(int id in DestinationNodes()){
            if(!_net.NodeVisible(_setup,_setup.Nodes.All[id]) || !OperationPosition(id,out var at) || !MapViewBounds.Contains(at.ToPoint()))continue;
            var point=at.ToPoint();
            int hit=_statusGlobe?HitGlobeNode(point,_net.MovementTargets):_net.HitNode(_setup,NetPanel,point,_net.MovementTargets);
            if(hit==id)return point;
        }
        return Point.Zero;
    }
    private bool UpdateDemoDomestic(double elapsed)
    {
        _demoStepAge=0;
        if(MapTransitionActive || _net.IsAnimating || OrientationRotating)return true;
        // Finish the same inspector animation as a normal click before aiming at the next control.
        if(_nodeInspectorClosing || InspectorVisible && _domesticSlide<1)return true;
        if(_demoDomesticStage is >=30 and <=36)return UpdateDemoWarChecks(elapsed);
        if(_demoDomesticStage==20 && (!NodeCheckButtonVisible || NodeChecked(_populationNode))){_demoDomesticStage=11;_demoDomesticAge=0;}
        Point nodePoint=Point.Zero;
        if(_demoDomesticStage==0 && !DemoNodePoint(out nodePoint))return true;
        Point destinationPoint=Point.Zero;
        if(_demoDomesticStage==15 && _selectedMoveNode<0)_demoDomesticStage=13;
        if(_demoDomesticStage==13){
            destinationPoint=DemoDestinationPoint();
            if(destinationPoint==Point.Zero){
                if(DestinationNodes().Length==0){CloseNodeInspector();_demoDomesticStage=19;}
                else{
                    var node=_setup.Nodes.All[DestinationNodes()[0]];
                    FocusDemoNode(node);
                }
                _demoDomesticAge=0;return true;
            }
        }
        Point target=_demoDomesticStage switch
        {
            0=>nodePoint,3=>NodeAssemblyTile.Center,
            4=>DemoAssemblySourcePoint(),
            5=>_demoEmptySlot<0?new(480,540):HangarCell(_demoEmptySlot).Center,
            6=>_demoEmptySlot<0?new(480,540):HangarPartBounds(_demoEmptySlot,1).Center,
            7=>_demoSourceSlot<0?new(480,540):HangarCell(_demoSourceSlot).Center,
            8=>NodeFactoryTile.Center,9=>FactoryProductionButton(0).Center,10=>FactoryPlanButton(0).Center,
            11 or 16=>InspectorClose.Center,12=>TransportOverviewButton.Center,17=>StrategyToggle.Center,
            18=>MovementButton.Center,13=>destinationPoint,19=>PopulationTurnButton.Center,20=>NodeCheckButton.Center,_=>ConfirmMoveButton.Center
        };
        if(_demoSwing<=0)_demoCursor=Vector2.Lerp(_demoCursor,new(target.X,target.Y),1-MathF.Exp(-(float)elapsed*4));
        if(_hangarDrag.HasValue)_pointer=new((int)_demoCursor.X,(int)_demoCursor.Y);
        _demoDomesticAge+=elapsed;if(_demoDomesticAge<1.8)return true;
        _demoDomesticAge=0;_demoCursor=new(target.X,target.Y);_pointer=target;_ui.Pointer=target;StartDemoClick();
        switch(_demoDomesticStage)
        {
            case 0:
                int hit=_statusGlobe?HitGlobeNode(target):_net.HitNode(_setup,NetPanel,target);
                if(hit<0)return true;
                OpenNodeInspector(hit);OpenNodeOffice();_demoDomesticStage=2;break;
            case 3:
                OpenNodeAssembly();
                _demoAssemblySelection=_setup.Robots.AssemblySelection(_populationNode,_setup.PlayerSlot,true);
                if(_demoAssemblySelection==null){_demoDomesticStage=7;break;}
                _demoEmptySlot=Array.FindIndex(_hangarSlots,id=>id==_demoAssemblySelection[0]);
                if(!PrepareDemoAssemblyDrag())_demoDomesticStage=7;break;
            case 4:
                DemoAssemblyPress();break;
            case 5:
                if(DemoAssemblyRelease() && PrepareDemoAssemblyDrag()){_demoDomesticStage=3;break;}
                _demoAssemblySelection=null;_demoDomesticStage=7;break;
            case 8:_nodeAssemblyOpen=false;OpenNodeFactory();break;
            case 9:if(_nodeFactoryOpen)NodeDialogClick(target,new KeyboardState());break;
            case 10:if(_nodeFactoryOpen)NodeDialogClick(target,new KeyboardState());_demoDomesticStage=19;break;
            case 20:DomesticViewClick(target);_demoDomesticStage=11;return true;
            case 11:DomesticViewClick(target);break;
            // Transport planning now lives in the primary actions, outside the node inspector.
            case 12:MapOverlayClick(target,false);_demoDomesticStage=15;break;
            case 16:DomesticViewClick(target);if(_demoActive && _demoMapVisits%2==0)_demoDomesticStage=29;break;
            case 17:MapOverlayClick(target,false);break;
            case 18:MovementClick(target,new KeyboardState());_demoDomesticStage=12;break;
            case 13:
                MovementClick(target,new KeyboardState());
                if(_selectedMoveNode<0)return true;
                _demoDomesticStage=14;break;
            case 15:
                MovementClick(target,new KeyboardState());
                _demoDomesticStage=_movementOpen?13:19;return true;
            case 19:
                EndPlannedTurn();
                _demoDomesticStage=0;_demoMapVisits++;_demoMapStage=0;_demoMapAge=0;return true;
        }
        _demoDomesticStage++;return true;
    }
}
