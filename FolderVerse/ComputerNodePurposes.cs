namespace FolderVerse;
using System.Linq;

internal static class ComputerNodePurposes
{
    internal static void Plan(WorldSetup world,bool automaticAll)
    {
        if(!world.Campaign.UseRobotCombat)return;
        var manual=world.ManualRulers().ToHashSet();
        var robots=world.Robots;
        var battles=world.Campaign.RobotBattles;
        foreach(var node in world.Nodes.All){
            int id=node.Id,owner=node.Owner;
            if(!node.Functional || node.Purpose==NodePurpose.Relay || owner<0 ||
                !world.Relations.CanAct(owner) || !automaticAll && manual.Contains(owner) ||
                battles.LockedNode(id) || robots.Workshops[id].Automation==NodeAutomation.Manual)continue;
            var lines=robots.Workshops[id].ProductionLines;
            if(robots.MapleSupplyEnabled && robots.AvailableMaple(id)==0 &&
                lines.Any(line=>line.WaitingForMaple(robots,id)) && !lines.Any(line=>line.MapleRemaining>0) &&
                robots.MapleAvailableToPlan(id,null)>0 && robots.CanHarvestMaple(id,1))
            {
                var idle=lines.FirstOrDefault(line=>!line.Paused && !line.DoNotRepeat &&
                    !line.Manufacturing && line.MapleRemaining==0);
                idle?.SelectMapleHarvest(1);
            }
            bool hostile=battles.MarchTargets(world,id).Any(target=>
                world.Nodes.All[target].Owner is var enemy && enemy>=0 && !world.Relations.Allied(owner,enemy));
            int available=battles.SelectableRobots(world,owner,id,false).Length;
            if(hostile && available>=2)
                node.Purpose=world.Nodes.Current(owner)?.Id==id?NodePurpose.Defense:NodePurpose.Frontline;
            else if(lines.Any(line=>line.MapleRemaining>0))
                node.Purpose=NodePurpose.Harvest;
            else if(lines.Any(line=>line.Manufacturing && !line.Paused && !line.CycleFinished && !line.WaitingForMaple(robots,id)))
                node.Purpose=NodePurpose.Manufacturing;
            else
                node.Purpose=NodePurpose.General;
        }
    }
}
