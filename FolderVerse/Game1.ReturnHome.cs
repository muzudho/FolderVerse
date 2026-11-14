namespace FolderVerse;
using System;
using System.Linq;
using Microsoft.Xna.Framework;

public partial class Game1
{
    private bool _hangarReturnHomeMode;
    private static readonly Rectangle HangarReturnHomeButton=new(84,908,182,42);
    private bool CanSelectReturnHome(Robot robot)
    {
        if(robot==null)return false;
        int source=Array.FindIndex(_setup.Robots.Nodes,s=>s.Robots.Any(r=>r.Id==robot.Id));
        if(source<0)return false;
        var actual=_setup.Robots.Nodes[source].Robots.Single(r=>r.Id==robot.Id);
        return actual.ReturnHomeCommander==_setup.PlayerSlot || _setup.Robots.CanReturnHome(_setup,source,actual,_setup.PlayerSlot);
    }
    private bool ReturnHomeHangarInput(bool click)
    {
        if(!DomesticOperationsAllowed || VisitingForeignHangar || !CanManageNodeFactory(_setup.Nodes.All[_populationNode].Owner)){
            _hangarReturnHomeMode=false;return false;
        }
        if(click && HangarReturnHomeButton.Contains(_pointer)){
            _hangarReturnHomeMode=!_hangarReturnHomeMode;_hangarReservation=HangarReservation.None;_hangarAbandonMode=false;_hangarDrag=null;
            _hangarMessage="個体をクリック：帰国予約 / 同じ個体で出発前の予約を解除";return true;
        }
        if(!_hangarReturnHomeMode || !click)return false;
        if(HangarAssemblyButton.Contains(_pointer) || HangarAbandonButton.Contains(_pointer) || Enumerable.Range(0,_setup.Robots.MapleSupplyEnabled?5:4).Any(i=>HangarReservationButton(i).Contains(_pointer))){_hangarReturnHomeMode=false;return false;}
        var store=PlanningStock.Nodes[HangarPlanningNode];SyncHangarSlots();
        foreach(int slot in VisibleAssemblySlots())if(AssemblyCell(slot).Contains(_pointer)){
            var robot=store.Robots.FirstOrDefault(r=>r.Id==_hangarSlots[slot]);
            if(robot!=null){
                bool accepted=_setup.Robots.ToggleReturnHome(_setup,robot.Id,_setup.PlayerSlot,out _hangarMessage);
                _operations.Write("robot_return_home_reservation",new{robot=robot.Id,accepted,message=_hangarMessage});
            }
            return true;
        }
        return false;
    }
    private void DrawReturnHomeHangar()
    {
        if(!VisitingForeignHangar && CanManageNodeFactory(_setup.Nodes.All[_populationNode].Owner))
            _ui.Button(HangarReturnHomeButton,"国へ帰らせる予約",_hangarReturnHomeMode?Accent:Muted,.29f);
        var journeys=_setup.Robots.ReturningRobots.Where(j=>j.Source==_populationNode && j.Commander==_setup.PlayerSlot).ToArray();
        if(journeys.Length>0){
            string status=journeys.Any(j=>j.Status==RobotWorld.NoHomeCountry)?RobotWorld.NoHomeCountry:
                journeys.Any(j=>j.Status.Contains("満杯"))?"帰国先が満杯：到着待ち":"配送ごとに１地点ずつ移動 / 他国も通過";
            _ui.Text($"帰国中 {journeys.Length} 体 / {status}",new(84,238),.29f,Cream);
        }
    }
}
