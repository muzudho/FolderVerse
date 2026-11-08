namespace FolderVerse;
using System;
using System.Linq;
using System.Collections.Generic;

public sealed record MercyOffer(Captive Captive,int Owned,int[] Nodes);
public sealed record MercyOutcome(int Turn,Captive Captive,int Owned,int Offer,int Demand,bool Success,int[] Nodes);
public sealed partial class ConquerorRelations
{
    public List<MercyOffer> MercyOffers {get;}=new();
    public List<MercyOutcome> MercyResults {get;}=new();
    public const string MercyMinimumExplanation="征服地点が３箇所未満の征服者は見逃されません。";
    public int OwnedMercyNodes(WorldSetup world,Captive captive)=>world.Nodes.All.Count(n=>n.Functional && n.Owner==captive.Ruler);
    public bool CanMercy(WorldSetup world,Captive captive)=>OwnedMercyNodes(world,captive)>=3;
    private int[][] MercyGroups(WorldSetup world,Captive captive)
    {
        var groups=world.Nodes.All.Where(n=>n.Functional && n.Owner==captive.Ruler)
            .Select(n=>NodeFunctionTransfers.Members(world,n.Id))
            .Where(g=>g.All(id=>world.Nodes.All[id].Owner==captive.Ruler && !world.Campaign.RobotBattles.LockedNode(id))).ToList();
        var distances=new Dictionary<int,int>{{captive.Node,0}};var queue=new Queue<int>();queue.Enqueue(captive.Node);
        while(queue.TryDequeue(out int id))foreach(var route in world.Routes.Neighbors(world.Nodes.All[id])){
            int next=world.Nodes.At(route.Target,route.Entry).Id;if(distances.TryAdd(next,distances[id]+1))queue.Enqueue(next);
        }
        var selected=new HashSet<int>();var ordered=new List<int[]>();
        while(groups.Count>0){
            bool Touches(int[] group)=>group.Any(id=>world.Routes.Neighbors(world.Nodes.All[id]).Any(r=>{
                int next=world.Nodes.At(r.Target,r.Entry).Id;return selected.Count>0?selected.Contains(next):world.Nodes.All[next].Owner==captive.Victor;
            }));
            var best=groups.OrderByDescending(Touches).ThenBy(g=>g.Min(id=>distances.GetValueOrDefault(id,int.MaxValue))).ThenBy(g=>g[0]).First();
            groups.Remove(best);ordered.Add(best);selected.UnionWith(best);
        }
        return ordered.ToArray();
    }
    public int[] MercyAmounts(WorldSetup world,Captive captive)
    {
        return Enumerable.Range(0,MercyGroups(world,captive).Length+1).ToArray();
    }
    public int ComputerMercyOffer(WorldSetup world,Captive captive)
    {
        var amounts=MercyAmounts(world,captive);int groups=amounts.Length-1;
        if(groups<=1)return amounts[^1];
        int minimum=Math.Min(groups-1,Math.Max(1,(groups*2+2)/3));
        var random=new SeedRandom(world.PlacementSeed^world.Population.Turn*7919^captive.Ruler*13007^captive.Node);
        if(random.Next(10)==0)return amounts[^1];
        return amounts[minimum+random.Next(groups-minimum)];
    }
    public int ComputerMercyDemand(WorldSetup world,Captive captive)
    {
        int owned=MercyOffers.FirstOrDefault(o=>o.Captive==captive)?.Owned??OwnedMercyNodes(world,captive);
        // Independent of the hidden offer: the captor cannot inspect it to choose a demand.
        var random=new SeedRandom(world.PlacementSeed^world.Population.Turn*13007^captive.Victor*7919^captive.Node^0x43719);
        return owned==0?0:1+random.Next(Math.Max(1,(owned+3)/4));
    }
    public bool SetMercyOffer(WorldSetup world,Captive captive,int amount)
    {
        if(!Pending.Contains(captive))throw new InvalidOperationException("Conqueror disposition is not pending.");
        var groups=MercyGroups(world,captive);
        if(!CanMercy(world,captive) || amount<0 || amount>groups.Length)return false;
        var nodes=groups.Take(amount).SelectMany(g=>g).ToArray();
        MercyOffers.RemoveAll(o=>o.Captive==captive);MercyOffers.Add(new(captive,OwnedMercyNodes(world,captive),nodes));return true;
    }
    public void EnsureComputerMercyOffer(WorldSetup world,Captive captive)
    {if(!MercyOffers.Any(o=>o.Captive==captive))SetMercyOffer(world,captive,ComputerMercyOffer(world,captive));}
    public MercyOutcome ResolveMercy(WorldSetup world,Captive captive,int demand)
    {
        if(!Pending.Contains(captive))throw new InvalidOperationException("Conqueror disposition is not pending.");
        if(!CanMercy(world,captive))throw new InvalidOperationException(MercyMinimumExplanation);
        var offer=MercyOffers.Single(o=>o.Captive==captive);
        if(demand<0 || demand>offer.Owned)throw new ArgumentOutOfRangeException(nameof(demand));
        int offered=offer.Nodes.Count(id=>world.Nodes.All[id].Functional);
        bool success=demand<=offered && offer.Nodes.All(id=>world.Nodes.All[id].Owner==captive.Ruler && !world.Campaign.RobotBattles.LockedNode(id));
        var result=new MercyOutcome(world.Population.Turn,captive,offer.Owned,offered,demand,success,success?offer.Nodes:Array.Empty<int>());
        if(success){
            foreach(int id in offer.Nodes.Where(id=>world.Nodes.All[id].Functional))RobotCampaign.SetOwner(world,world.Nodes.All[id],captive.Victor);
            SetSuperior(captive.Ruler,-1);Released[captive.Ruler]=true;Pending.Remove(captive);MercyOffers.Remove(offer);
            world.Nodes.Recount();world.Robots.DirectDelivery.ReconcileDestinations(world);
            Message=$"見逃し成立 / {result.Offer}地点を譲渡";
        }else{Choose(world,captive,DefeatedChoice.RemoveBattery);Message="見逃し不成立 / 電池を引き抜いた";}
        MercyResults.Add(result);if(MercyResults.Count>64)MercyResults.RemoveRange(0,MercyResults.Count-64);return result;
    }
}
