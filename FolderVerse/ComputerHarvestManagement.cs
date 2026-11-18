namespace FolderVerse;
using System;
using System.Collections.Generic;
using System.Linq;

// Intention is demand for an actual selected recipe or a useful trade, not an old harvest booking.
internal static class ComputerHarvestManagement
{
    internal static bool Roll(int percent,int seed,int turn,int node,long robot,int action)
    {
        // Stable across saves and repeated planning in the same turn; independent action channels.
        uint x=unchecked((uint)seed ^ (uint)turn*2654435761u ^ (uint)node*2246822519u ^
            (uint)robot*3266489917u ^ (uint)(robot>>32) ^ (uint)action*668265263u);
        x^=x>>16;x*=2246822519u;x^=x>>13;x*=3266489917u;x^=x>>16;
        return x%100<Math.Clamp(percent,0,100);
    }

    private static bool Needs(WorldSetup world,int node,RawMaterial material)=>
        world.Robots.Workshops[node].ProductionLines.Any(line=>line.Manufacturing && !line.Paused && !line.CycleFinished &&
            RobotWorld.RawCost(line.CurrentProductionPlan.Preview(world.Nodes.All[node].Owner),material)>
                world.Robots.AvailableRaw(node,material));

    internal static HashSet<RawMaterial> PlannedMaterials(WorldSetup world,int source)
    {
        int owner=world.Nodes.All[source].Owner;
        var stock=world.Robots;
        var reachable=new HashSet<int>{source};var queue=new Queue<int>();queue.Enqueue(source);
        while(queue.TryDequeue(out int at))
            foreach(int next in RobotLongDelivery.Neighbors(world,at,owner))
                if(reachable.Add(next))queue.Enqueue(next);
        var trade=world.Campaign.Trade;
        var from=new TradeEndpoint(owner,source,false);
        var partners=trade.Partners(world,from).ToArray();
        return RobotWorld.RawMaterials.Where(material=>stock.RawDeposit(source,material)>0 && (
            reachable.Any(n=>world.Nodes.All[n].Owner==owner && Needs(world,n,material)) ||
            trade.Offers.Any(o=>o.From==from && o.Give.Raw && trade.Package(world,from,o.Give)?.Material==material ||
                o.To==from && o.Want.Raw && trade.Package(world,from,o.Want)?.Material==material) ||
            partners.Any(to=>!to.Pocket && Needs(world,to.Node,material) && trade.Cargo(world,to).Any(want=>
                // A concrete useful return cargo, rather than harvesting merely because a neighbor exists.
                trade.Robot(world,to,want) is {} robot && robot.Parts!=RobotParts.Complete &&
                    stock.Nodes[source].Robots.Any(other=>!other.Stationed && !other.Disposal &&
                        (other.Parts&robot.Parts)==0 && (other.Parts|robot.Parts)==RobotParts.Complete) ||
                trade.Package(world,to,want) is {} raw && Needs(world,source,raw.Material))))).ToHashSet();
    }

    // Returns true when the independent purpose decision should keep Harvest this turn.
    internal static bool Review(WorldSetup world,Node node,HashSet<RawMaterial> planned)
    {
        var stock=world.Robots;int id=node.Id;
        bool exhausted=RobotWorld.RawMaterials.All(m=>stock.RawDeposit(id,m)==0);
        if(!exhausted && planned.Count>0)return false;
        var policy=world.RulerComputerPolicy(node.Owner);
        int workerChance=exhausted?policy.ReleaseExhaustedHarvester:policy.ReleaseUnplannedHarvester;
        int purposeChance=exhausted?policy.ClearExhaustedHarvestPurpose:policy.ClearUnplannedHarvestPurpose;
        foreach(var robot in stock.Nodes[id].Robots.Where(r=>r.HarvestWorker).ToArray())
            if(!stock.PartPlanning.ReservesRobot(robot.Id) && !stock.ReservationBusy(id,robot.Id) && !stock.MarchReserved(id,robot) &&
                Roll(workerChance,world.PlacementSeed,world.Population.Turn,id,robot.Id,exhausted?1:3))
                stock.PartPlanning.ReserveRole(world,id,robot.Id,HangarReservation.Harvest,node.Owner);
        if(node.Purpose!=NodePurpose.Harvest)return false;
        bool clear=Roll(purposeChance,world.PlacementSeed,world.Population.Turn,id,0,exhausted?2:4);
        if(clear)node.Purpose=NodePurpose.General;
        return !clear;
    }
}
