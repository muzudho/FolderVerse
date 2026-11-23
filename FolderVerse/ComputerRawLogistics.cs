namespace FolderVerse;
using System.Collections.Generic;
using System.Linq;

internal static class ComputerRawLogistics
{
    internal static void Plan(WorldSetup world,bool automaticAll)
    {
        var stock=world.Robots;
        if(!world.Campaign.UseRobotCombat || !stock.MapleSupplyEnabled || world.TutorialStep!=0)return;
        var manual=world.ManualRulers().ToHashSet();
        var sites=world.Nodes.All.Where(n=>n.Functional && n.Owner>=0 && world.Relations.CanAct(n.Owner) &&
            (automaticAll || !manual.Contains(n.Owner)) && stock.Workshops[n.Id].Automation!=NodeAutomation.Manual &&
            stock.Workshops[n.Id].SupervisorLeaveRemaining==0 &&
            !world.Campaign.RobotBattles.LockedNode(n.Id) &&
            !world.Campaign.FunctionTransfers.Orders.Any(o=>o.Source==n.Id || o.Target==n.Id)).ToArray();
        var materials=RobotWorld.RawMaterials.Where(stock.RawRequired).ToArray();
        var recipes=sites.ToDictionary(n=>n.Id,n=>stock.Workshops[n.Id].ProductionLines
            .Where(l=>l.Manufacturing && !l.HarvestOnly && !l.Paused && !l.CycleFinished).ToArray());
        var demand=sites.ToDictionary(n=>n.Id,n=>materials.ToDictionary(m=>m,m=>recipes[n.Id].Sum(l=>l.RawRemaining(m))));
        foreach(var order in stock.MapleShipments.Where(o=>o.Computer).ToArray())
        {
            if(!stock.CanPlanComputerRawShipment(world,order) ||
                !automaticAll && manual.Contains(world.Nodes.All[order.Target].Owner) ||
                stock.Workshops[order.Target].Automation==NodeAutomation.Manual ||
                demand.TryGetValue(order.Target,out var targetDemand) &&
                targetDemand.GetValueOrDefault(stock.Nodes[order.Source].MaplePackages.Single(p=>p.Id==order.PackageId).Material)==0)
                stock.CancelMapleShipment(order.Source,order.PackageId);
        }

        var free=sites.ToDictionary(n=>n.Id,n=>materials.ToDictionary(m=>m,m=>stock.Nodes[n.Id].MaplePackages
            .Where(p=>p.Material==m && !stock.ComputerRawCargoLocked(world,n.Id,p.Id)).Sum(p=>p.Units)));
        var incoming=sites.ToDictionary(n=>n.Id,n=>materials.ToDictionary(m=>m,m=>stock.MapleShipments.Where(o=>o.Target==n.Id)
            .Sum(o=>stock.Nodes[o.Source].MaplePackages.FirstOrDefault(p=>p.Id==o.PackageId && p.Material==m)?.Units??0)));
        int Missing(int site,RawMaterial m)=>demand[site][m]-free[site][m]-incoming[site][m];
        int Surplus(int site,RawMaterial m)=>free[site][m]-demand[site][m];

        var requests=sites.Where(n=>materials.Any(m=>Missing(n.Id,m)>0))
            .Select(n=>(Node:n,Ranges:ComputerFactoryManagement.Distances(world,n.Id),
                Urgent:recipes[n.Id].Select(l=>l.StartWaiting || l.RestRemaining>0?null:l.MissingRawMaterial(stock,n.Id))
                    .Where(m=>m.HasValue).Select(m=>m.Value).ToHashSet()))
            .ToArray();
        double Coverage(int site)=>materials.Sum(m=>System.Math.Min(demand[site][m],free[site][m]+incoming[site][m]))/
            (double)demand[site].Values.Sum();
        bool sent;
        do
        {
            sent=false;
            // Dispatch one load per factory in each pass before topping any of them up.
            foreach(var request in requests.OrderByDescending(r=>r.Urgent.Any(m=>Missing(r.Node.Id,m)>0))
                .ThenBy(r=>Coverage(r.Node.Id)).ThenBy(r=>r.Node.Id))
            {
                int target=request.Node.Id;
                bool supplied=false;
                foreach(var material in materials.Where(m=>Missing(target,m)>0).OrderByDescending(request.Urgent.Contains))
                {
                    foreach(var source in sites.Where(n=>n.Id!=target && request.Ranges.ContainsKey(n.Id) && Surplus(n.Id,material)>0)
                        .OrderBy(n=>request.Ranges[n.Id]).ThenBy(n=>n.Owner!=request.Node.Owner)
                        .ThenByDescending(n=>Surplus(n.Id,material)).ThenBy(n=>n.Id))
                    {
                        int amount=System.Math.Min(RobotWorld.MaplePackageSize,System.Math.Min(Missing(target,material),Surplus(source.Id,material)));
                        if(!stock.ReserveComputerRawShipment(world,source.Id,target,material,amount))continue;
                        free[source.Id][material]-=amount;
                        incoming[target][material]+=amount;
                        supplied=sent=true;
                        break;
                    }
                    if(supplied)break;
                }
            }
        }while(sent);
    }
}
