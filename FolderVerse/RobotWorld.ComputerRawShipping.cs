namespace FolderVerse;
using System;
using System.Collections.Generic;
using System.Linq;

public sealed partial class RobotWorld
{
    internal bool CanPlanComputerRawShipment(WorldSetup world,MapleShipment order) =>
        world.Relations.CanAct(order.Owner) && Nodes[order.Source].MaplePackages.Any(p=>p.Id==order.PackageId) &&
        world.Nodes.All[order.Source].Functional && world.Nodes.All[order.Target].Functional &&
        world.Relations.Allied(order.Owner,world.Nodes.All[order.Source].Owner) &&
        world.Relations.Allied(order.Owner,world.Nodes.All[order.Target].Owner);

    internal bool ComputerRawCargoLocked(WorldSetup world,int node,long id) =>
        MapleShipping(node,id) || world.Campaign.Trade.Offers.Any(o=>
            o.Give.Raw && o.Give.Id==id || o.Want.Raw && o.Want.Id==id);

    internal bool ReserveComputerRawShipment(WorldSetup world,int source,int target,RawMaterial material,int units)
    {
        if(!MapleSupplyEnabled || world.Campaign.Phase!=CampaignPhase.Domestic ||
            units<=0 || units>MaplePackageSize || material==RawMaterial.Scrap)return false;
        int owner=world.Nodes.All[source].Owner;
        var store=Nodes[source];
        var usable=store.MaplePackages.Where(p=>p.Material==material && !ComputerRawCargoLocked(world,source,p.Id)).ToArray();
        if(usable.Sum(p=>p.Units)<units)return false;

        var exact=usable.FirstOrDefault(p=>p.Units==units);
        var cargo=exact??new MaplePackage(_nextId,units){Material=material};
        var order=new MapleShipment(owner,source,target,cargo.Id){Computer=true};
        if(!ForecastRawArrival(world,target,cargo) || ComputerRawNextHop(world,order,cargo,true)<0)return false;

        if(exact==null)
        {
            // Take loose amounts first, then divide a larger bundle if necessary.
            var stock=store.MaplePackages.ToDictionary(p=>p.Id);
            int remaining=units;
            foreach(var package in usable.OrderBy(p=>p.Sections).ThenBy(p=>p.Units).ThenBy(p=>p.Id))
            {
                int take=Math.Min(remaining,package.Units);
                if(take==package.Units)stock.Remove(package.Id);
                else stock[package.Id]=package with {Units=package.Units-take};
                remaining-=take;
                if(remaining==0)break;
            }
            var repacked=stock.Values.Append(cargo).ToArray();
            if(!store.Fits(store.Robots,repacked,store.ReservedReturns+MapleReservedSlots(source)))return false;
            store.SetMaplePackages(repacked);
            _nextId++;
        }
        _mapleShipments.Add(order);
        return true;
    }

    private bool ForecastRawArrival(WorldSetup world,int target,MaplePackage cargo)
    {
        var store=Nodes[target].PlanningCopy();
        int reserved=MapleReservedSlots(target);
        bool Locked(long id)=>ComputerRawCargoLocked(world,target,id);
        foreach(var order in MapleShipments.Where(o=>o.Target==target))
        {
            var pending=Nodes[order.Source].MaplePackages.FirstOrDefault(p=>p.Id==order.PackageId);
            if(pending!=null && !store.TryReceiveMaple(pending,reserved,Locked))return false;
        }
        return store.TryReceiveMaple(cargo,reserved,Locked);
    }

    private int ComputerRawNextHop(WorldSetup world,MapleShipment order,MaplePackage cargo,bool forecasting,bool checkObstacles=true)
    {
        var battles=world.Campaign.RobotBattles;
        if(checkObstacles && (battles.LockedNode(order.Source) || battles.LockedNode(order.Target)))return -1;
        var first=new Dictionary<int,int>{{order.Source,-1}};
        var queue=new Queue<int>();queue.Enqueue(order.Source);
        while(queue.TryDequeue(out int at))
        foreach(int next in RobotLongDelivery.Neighbors(world,at,order.Owner,checkObstacles))
        {
            if(first.ContainsKey(next) || world.Nodes.All[next].Owner<0 || checkObstacles && battles.LockedEdge(at,next))continue;
            int hop=at==order.Source?next:first[at];
            if(next==order.Target)return hop;
            // Keep the cargo's ID at a relay so it cannot merge into local factory stock.
            int reserved=MapleReservedSlots(next);
            if(forecasting)reserved+=MapleShipments.Count(o=>o.Target==next);
            if(checkObstacles && !Nodes[next].HasMapleSpace(cargo,reserved))continue;
            first.Add(next,hop);queue.Enqueue(next);
        }
        return -1;
    }

    private void AdvanceComputerRawShipment(WorldSetup world,MapleShipment order)
    {
        if(!CanPlanComputerRawShipment(world,order))
        {
            _mapleShipments.Remove(order);
            return;
        }
        var source=Nodes[order.Source];
        var cargo=source.MaplePackages.Single(p=>p.Id==order.PackageId);
        int next=ComputerRawNextHop(world,order,cargo,false);
        if(next<0)
        {
            if(ComputerRawNextHop(world,order,cargo,false,checkObstacles:false)<0)_mapleShipments.Remove(order);
            return;
        }
        bool arrived=next==order.Target;
        if(!Nodes[next].TryReceiveMaple(cargo,MapleReservedSlots(next),id=>!arrived || ComputerRawCargoLocked(world,next,id)))return;
        source.SetMaplePackages(source.MaplePackages.Where(p=>p.Id!=cargo.Id).ToArray());
        int index=_mapleShipments.IndexOf(order);
        if(arrived)_mapleShipments.RemoveAt(index);
        else _mapleShipments[index]=order with {Source=next};
    }
}
