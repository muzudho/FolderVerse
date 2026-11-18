namespace FolderVerse;
using System;
using System.Collections.Generic;
using System.Linq;

internal static class ComputerFactoryManagement
{
    internal static bool Managed(WorldSetup world,Node node,bool automaticAll)=>node.Functional && node.Purpose!=NodePurpose.Relay &&
        node.Owner>=0 && world.Relations.CanAct(node.Owner) && (automaticAll || !world.ManualRulers().Contains(node.Owner)) &&
        !world.Campaign.RobotBattles.LockedNode(node.Id) && world.Robots.Workshops[node.Id].Automation!=NodeAutomation.Manual &&
        !world.Campaign.FunctionTransfers.Orders.Any(o=>o.Source==node.Id || o.Target==node.Id);

    internal static bool Incoming(WorldSetup world,int node,RawMaterial material)
    {
        var stock=world.Robots;var trade=world.Campaign.Trade;
        return stock.MapleShipments.Any(o=>o.Target==node && stock.Nodes[o.Source].MaplePackages.Any(p=>p.Id==o.PackageId && p.Material==material)) ||
            stock.Workshops[node].ProductionLines.Any(l=>!l.Paused && l.HarvestOnly && (l.MapleRemaining>0 || !l.DoNotRepeat) &&
                l.HarvestMaterial==material && stock.RawDeposit(node,material)>0) ||
            stock.PartPlanning.Orders.Any(o=>o.Target==node && o.Role==HangarReservation.Harvest && o.WorkerLane is {MapleLoads:>0} lane && lane.HarvestMaterial==material) ||
            trade.Offers.Any(o=>o.To.Node==node && !o.To.Pocket && o.Give.Raw && trade.Package(world,o.From,o.Give)?.Material==material ||
                o.From.Node==node && !o.From.Pocket && o.Want.Raw && trade.Package(world,o.To,o.Want)?.Material==material);
    }

    internal static bool WaitingWithoutSupply(WorldSetup world,int node,RobotWorkshop line)=>
        line.Manufacturing && !line.Paused && !line.CycleFinished && RobotWorld.RawMaterials.Any(m=>
            RobotWorld.RawCost(line.CurrentProductionPlan.Preview(world.Nodes.All[node].Owner),m)>world.Robots.AvailableRaw(node,m)) &&
        !RobotWorld.RawMaterials.Where(m=>RobotWorld.RawCost(line.CurrentProductionPlan.Preview(world.Nodes.All[node].Owner),m)>
            world.Robots.AvailableRaw(node,m)).Any(m=>Incoming(world,node,m));

    // Purpose retention is independent of each worker's release roll.
    internal static bool Review(WorldSetup world,Node node)
    {
        var stock=world.Robots;var shop=stock.Workshops[node.Id];var policy=world.RulerComputerPolicy(node.Owner);
        for(int i=0;i<shop.ProductionLines.Length;i++)
            if(shop.WorkerAt(i) is long id && stock.Nodes[node.Id].Robots.FirstOrDefault(r=>r.Id==id) is {HarvestWorker:false,FactoryWorker:true} &&
                WaitingWithoutSupply(world,node.Id,shop.ProductionLines[i]) && !stock.PartPlanning.ReservesRobot(id) &&
                !stock.ReservationBusy(node.Id,id) && ComputerHarvestManagement.Roll(policy.ReleaseUnsuppliedManufacturer,
                    world.PlacementSeed,world.Population.Turn,node.Id,id,5))
                stock.PartPlanning.ReserveRole(world,node.Id,id,HangarReservation.Worker,node.Owner);
        if(node.Purpose!=NodePurpose.Manufacturing)return false;
        bool incomingWorker=stock.PartPlanning.Orders.Any(o=>o.Target==node.Id && o.Role==HangarReservation.Worker &&
            !stock.Nodes[node.Id].Robots.Any(r=>r.Id==o.RobotId && r.FactoryWorker && !r.HarvestWorker));
        if(incomingWorker)return true;
        var manufacturers=shop.ProductionLines.Where(l=>!l.HarvestOnly).ToArray();
        int chance,action;
        if(manufacturers.Length==0){chance=policy.ClearUnstaffedFactoryPurpose;action=7;}
        else if(manufacturers.All(l=>WaitingWithoutSupply(world,node.Id,l))){chance=policy.ClearUnsuppliedFactoryPurpose;action=6;}
        else return manufacturers.Any(l=>l.WaitingForMaple(stock,node.Id));
        if(!ComputerHarvestManagement.Roll(chance,world.PlacementSeed,world.Population.Turn,node.Id,0,action))return true;
        node.Purpose=NodePurpose.General;return false;
    }

    private static Robot Candidate(WorldSetup world,Node node)
    {
        var stock=world.Robots;
        var free=stock.Nodes[node.Id].Robots.Where(r=>r.Owner==node.Owner && r.Parts==RobotParts.Complete && !stock.AssemblyLocked(node.Id,r) &&
            stock.CanSetReservation(node.Id,r,HangarReservation.Worker,node.Owner)).ToArray();
        bool front=world.Campaign.RobotBattles.AttackTargets(world,node.Owner,node.Id).Any(t=>
            world.Nodes.All[t].Owner>=0 && !world.Relations.Allied(node.Owner,world.Nodes.All[t].Owner));
        int ready=world.Campaign.RobotBattles.SelectableRobots(world,node.Owner,node.Id,false).Length;
        return free.Where(r=>!front || !stock.Training[node.Owner].Eligible(r) || ready>1)
            .OrderBy(r=>stock.Training[node.Owner].Eligible(r)).ThenBy(r=>r.Equipment(node.Owner,world.EquipmentMode).Total).ThenBy(r=>r.Id).FirstOrDefault();
    }

    internal static Dictionary<int,int> Distances(WorldSetup world,int source)
    {
        // Raw cargo travels from supplier to factory, so traverse the delivery graph backwards.
        var incoming=new Dictionary<int,List<int>>();int owner=world.Nodes.All[source].Owner;
        foreach(var node in world.Nodes.All.Where(n=>n.Functional && n.Owner>=0 && world.Relations.Allied(owner,n.Owner)))
            foreach(int next in RobotLongDelivery.Neighbors(world,node.Id,owner))
            {if(!incoming.TryGetValue(next,out var list))incoming[next]=list=new();list.Add(node.Id);}
        var distance=new Dictionary<int,int>{{source,0}};var queue=new Queue<int>();queue.Enqueue(source);
        while(queue.TryDequeue(out int at))if(incoming.TryGetValue(at,out var previous))foreach(int next in previous)
            if(distance.TryAdd(next,distance[at]+1))queue.Enqueue(next);
        return distance;
    }

    internal static double LocationScore(WorldSetup world,int node,Robot product)
        =>LocationScore(world,node,product,Distances(world,node));
    private static double LocationScore(WorldSetup world,int node,Robot product,Dictionary<int,int> distance)
    {
        var policy=world.RulerComputerPolicy(world.Nodes.All[node].Owner);
        var costs=RobotWorld.RawMaterials.Select(m=>(Material:m,Units:RobotWorld.RawCost(product,m))).Where(x=>x.Units>0).ToArray();
        if(costs.Length==0)return 0;
        var travel=costs.Select(c=>(c.Units,Distance:world.Nodes.All.Where(n=>n.Functional && n.Owner==world.Nodes.All[node].Owner &&
                world.Robots.RawDeposit(n.Id,c.Material)>0 && distance.ContainsKey(n.Id)).Select(n=>distance[n.Id]).DefaultIfEmpty(20).Min())).ToArray();
        return -Math.Clamp(policy.NearResourceFactoryPreference,0,100)*travel.Min(x=>x.Distance) -
            Math.Clamp(policy.CentralResourceFactoryPreference,0,100)*travel.Sum(x=>x.Units*x.Distance)/(double)travel.Sum(x=>x.Units);
    }

    internal static bool EnsureHarvester(WorldSetup world,Node node,HashSet<RawMaterial> planned)
    {
        var stock=world.Robots;var shop=stock.Workshops[node.Id];
        if(planned.Count==0 || stock.Nodes[node.Id].Robots.Any(r=>r.HarvestWorker) ||
            stock.PartPlanning.Orders.Any(o=>o.Target==node.Id && RobotWorld.WorkerRole(o.Role)))return false;
        var material=planned.OrderBy(m=>(int)m).FirstOrDefault(m=>stock.CanHarvestMaple(node.Id,1,m),(RawMaterial)(-1));
        if((int)material<0 || Candidate(world,node) is not {} worker)return false;
        if(!stock.PartPlanning.ReserveRole(world,node.Id,worker.Id,HangarReservation.Harvest,node.Owner))return false;
        var line=shop.CreateWorkerLine(worker with {FactoryWorker=true,HarvestAssignment=true},world.WorldSeed,node.Id,node.Owner);
        line.SelectMapleHarvest(1,material);stock.PartPlanning.SetWorkerLane(worker.Id,line);return true;
    }

    internal static void AppointManufacturers(WorldSetup world,bool automaticAll)
    {
        var stock=world.Robots;
        foreach(var group in world.Nodes.All.Where(n=>Managed(world,n,automaticAll)).GroupBy(n=>n.Owner))
        {
            var choices=new List<(Node Node,Robot Worker,RobotWorkshop Line,int Plan,double Score)>();
            foreach(var node in group)
            {
                var shop=stock.Workshops[node.Id];
                if(shop.ProductionLines.Any(l=>!l.HarvestOnly) || stock.PartPlanning.Orders.Any(o=>o.Target==node.Id && RobotWorld.WorkerRole(o.Role)) ||
                    Candidate(world,node) is not {} worker || !stock.Nodes[node.Id].HasSpace)continue;
                var line=shop.CreateWorkerLine(worker,world.WorldSeed,node.Id,node.Owner);var distance=Distances(world,node.Id);
                var option=Enumerable.Range(0,line.PlanCount).Where(i=>i!=3).Select(i=>(Index:i,Plan:line.PlanAt(i)))
                    .Where(x=>RobotWorld.RawMaterials.All(m=>RobotWorld.RawCost(x.Plan.Preview(node.Owner),m)<=stock.AvailableRaw(node.Id,m)))
                    .Select(x=>(x.Index,Product:x.Plan.Preview(node.Owner),Score:LocationScore(world,node.Id,x.Plan.Preview(node.Owner),distance)))
                    .OrderByDescending(x=>x.Score).ThenBy(x=>x.Index).FirstOrDefault();
                if(option.Product!=null)choices.Add((node,worker,line,option.Index,option.Score));
            }
            foreach(var choice in choices.OrderByDescending(x=>x.Score).ThenBy(x=>x.Node.Id))
                if(stock.PartPlanning.ReserveRole(world,choice.Node.Id,choice.Worker.Id,HangarReservation.Worker,choice.Node.Owner))
                {choice.Line.SelectPlan(choice.Plan);stock.PartPlanning.SetWorkerLane(choice.Worker.Id,choice.Line);choice.Node.Purpose=NodePurpose.Manufacturing;break;}
        }
    }
}
