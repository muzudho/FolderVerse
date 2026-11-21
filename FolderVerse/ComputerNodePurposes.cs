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
        ComputerFactoryManagement.AppointManufacturers(world,automaticAll);
        foreach(var node in world.Nodes.All){
            int id=node.Id,owner=node.Owner;
            if(!node.Functional || node.Purpose==NodePurpose.Relay || owner<0 ||
                !world.Relations.CanAct(owner) || !automaticAll && manual.Contains(owner) ||
                battles.LockedNode(id) || robots.Workshops[id].Automation==NodeAutomation.Manual)continue;
            if(!ComputerFactoryManagement.Managed(world,node,automaticAll))continue;
            var lines=robots.Workshops[id].ProductionLines;
            var planned=ComputerHarvestManagement.PlannedMaterials(world,id);
            bool keepHarvest=ComputerHarvestManagement.Review(world,node,planned);
            bool appointedHarvester=ComputerFactoryManagement.EnsureHarvester(world,node,planned);
            bool keepFactory=ComputerFactoryManagement.Review(world,node);
            RawMaterial? material=planned.OrderBy(m=>(int)m).Cast<RawMaterial?>()
                .FirstOrDefault(m=>robots.MapleAvailableToPlan(id,null,m.Value)>0 &&
                    robots.CanHarvestMaple(id,1,m.Value) && !lines.Any(line=>line.MapleRemaining>0 && line.HarvestMaterial==m));
            if(material.HasValue)
            {
                var idle=lines.FirstOrDefault(line=>!line.Paused && !line.DoNotRepeat &&
                    line.HarvestOnly && !line.Manufacturing && line.MapleRemaining==0);
                idle?.SelectMapleHarvest(1,material.Value);
            }
            if(node.PermanentLandmark || keepHarvest || keepFactory)continue;
            int available=battles.SelectableRobots(world,owner,id,false).Length;
            bool Hostile(int target)=>world.Nodes.All[target].Owner is var enemy && enemy>=0 && !world.Relations.Allied(owner,enemy);
            bool hostile=available>=2 && battles.MarchTargets(world,id).Any(Hostile);
            if(hostile)
                node.Purpose=world.Nodes.Current(owner)?.Id==id?NodePurpose.Defense:NodePurpose.Frontline;
            else if(available>=2 && battles.PotentialOutpostTargets(world,owner,id).Any(target=>Hostile(target) && !battles.LockedNode(target)))
                node.Purpose=NodePurpose.Frontline;
            else if(appointedHarvester || lines.Where((line,index)=>robots.Workshops[id].WorkerAt(index) is not long worker ||
                    !robots.PartPlanning.Orders.Any(o=>o.Target==id && o.RobotId==worker && o.Role==HangarReservation.Harvest))
                .Any(line=>line.MapleRemaining>0 && planned.Contains(line.HarvestMaterial)))
                node.Purpose=NodePurpose.Harvest;
            else if(lines.Any(line=>line.Manufacturing && !line.Paused && !line.CycleFinished && !line.WaitingForMaple(robots,id)))
                node.Purpose=NodePurpose.Manufacturing;
            else
                node.Purpose=NodePurpose.General;
        }
    }
}
