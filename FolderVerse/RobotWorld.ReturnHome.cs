namespace FolderVerse;
using System;
using System.Collections.Generic;
using System.Linq;

public sealed record ReturningRobot(Robot Robot,int Node,int Country,int Source,int Commander,string Status="帰国中")
{
    public int? ReservedNode {get;init;}
    public bool ReservedPocket {get;init;}
}
public sealed partial class RobotWorld
{
    public List<ReturningRobot> ReturningRobots {get;}=new();
    public const string NoHomeCountry="帰国先が不明のため待機しています。";
    private static readonly RobotParts[] HomeParts={RobotParts.Head,RobotParts.Body,RobotParts.Legs};
    private sealed record ReturnDestination(int Country,int Node,bool Pocket,int Distance,int Next);
    public bool CanReturnHome(WorldSetup world,int node,Robot robot,int commander)=>CanReturnHomeCore(world,node,robot,commander,true);
    private bool CanReturnHomeCore(WorldSetup world,int node,Robot robot,int commander,bool authority)
        =>node>=0 && node<Nodes.Length && robot!=null && commander>=0 && commander<world.ActiveCount &&
          robot.Parts==RobotParts.Complete && !robot.Disposal &&
          HomeParts.All(p=>robot.Origin(p)!=commander) && !AssemblyLocked(node,robot) &&
          world.Campaign.Phase==CampaignPhase.Domestic && world.Nodes.All[node].Owner>=0 && (authority?world.Relations.Controls(world.Nodes.All[node].Owner):world.Nodes.All[node].Owner==commander) && !world.Campaign.RobotBattles.LockedNode(node);

    public bool ToggleReturnHome(WorldSetup world,long id,int commander,out string message)
    {
        int node=Array.FindIndex(Nodes,s=>s.Robots.Any(r=>r.Id==id));
        var robot=node<0?null:Nodes[node].Robots.Single(r=>r.Id==id);
        if(robot?.ReturnHomeCommander==commander){
            Nodes[node].Replace(Nodes[node].Robots.Select(r=>r.Id==id?r with {ReturnHomeCommander=null,ReturnHomeStatus=null}:r));
            PartPlanning.Invalidate();message="国へ帰らせる予約を取り消しました";return true;
        }
        if(!CanReturnHome(world,node,robot,commander)){message="自国パーツを含まない、完全体を選んでください。ほかの予約中は使えません。";return false;}
        string status=HomeCountries(world,robot).Length==0?NoHomeCountry:"帰国予約 / 通常予約の後で空き枠を確保";
        Nodes[node].Replace(Nodes[node].Robots.Select(r=>r.Id==id?r with {ReturnHomeCommander=commander,ReturnHomeStatus=status}:r));
        PartPlanning.Invalidate();message=status;return true;
    }
    internal bool ReserveComputerReturnHome(WorldSetup world,int node,Robot robot,int owner)
    {
        if(!CanReturnHomeCore(world,node,robot,owner,false) || HomeCountries(world,robot).Length==0)return false;
        Nodes[node].Replace(Nodes[node].Robots.Select(r=>r.Id==robot.Id?r with {ReturnHomeCommander=owner,ReturnHomeStatus="帰国予約 / 空き枠の確保待ち"}:r));PartPlanning.Invalidate();return true;
    }
    private static int ReturnCountry(WorldSetup world,int country)
    {
        var seen=new HashSet<int>();
        while(country>=0 && country<world.ActiveCount && seen.Add(country)){
            if(world.Relations.Powered[country])return country;
            var history=world.Relations.BatteryRemovedBy;
            country=history!=null && country<history.Length?history[country]:-1;
        }
        return -1;
    }
    private static int[] HomeCountries(WorldSetup world,Robot robot)
    {
        var groups=HomeParts.Select(robot.Origin).Where(c=>c>=0 && c<world.ActiveCount).GroupBy(c=>c).ToArray();
        foreach(var tier in groups.GroupBy(g=>g.Count()).OrderByDescending(g=>g.Key)){
            var countries=tier.Select(g=>ReturnCountry(world,g.Key)).Where(c=>c>=0).Distinct().OrderBy(c=>c).ToArray();
            if(countries.Length>0)return countries;
        }
        // Missing elimination history is not permission to give the robot to an arbitrary country.
        return groups.Length>0?Array.Empty<int>():Enumerable.Range(0,world.ActiveCount).Where(c=>world.Relations.Powered[c]).ToArray();
    }
    private static bool HasHomeSite(WorldSetup world,int country)=>world.Nodes.All.Any(n=>n.Functional && n.Owner==country);
    // Repatriation may pass through foreign territory, but never through an active battle.
    private static int[] HomeNeighbors(WorldSetup world,int source)
    {
        var seen=new HashSet<int>{source};var queue=new Queue<int>();var result=new HashSet<int>();queue.Enqueue(source);
        var battles=world.Campaign.RobotBattles;
        while(queue.TryDequeue(out int at))foreach(var route in world.Routes.Neighbors(world.Nodes.All[at])){
            int next=world.Nodes.At(route.Target,route.Entry).Id;
            if(battles.LockedNode(next) || battles.LockedEdge(at,next) || !seen.Add(next))continue;
            if(world.Nodes.All[next].Functional)result.Add(next);else queue.Enqueue(next);
        }
        return result.OrderBy(n=>n).ToArray();
    }
    private static ReturnDestination[] HomeDestinations(WorldSetup world,int start,int[] countries)
    {
        var distance=new Dictionary<int,int>{{start,0}};var first=new Dictionary<int,int>{{start,start}};
        var queue=new Queue<int>();queue.Enqueue(start);
        while(queue.TryDequeue(out int at))foreach(int next in HomeNeighbors(world,at))if(distance.TryAdd(next,distance[at]+1)){
            first[next]=at==start?next:first[at];queue.Enqueue(next);
        }
        var choices=new List<ReturnDestination>();
        foreach(int country in countries){
            var sites=world.Nodes.All.Where(n=>n.Functional && n.Owner==country).ToArray();
            foreach(var node in sites)if(distance.TryGetValue(node.Id,out int d) && !world.Campaign.RobotBattles.LockedNode(node.Id))choices.Add(new(country,node.Id,false,d,first[node.Id]));
            if(sites.Length==0 && world.Nodes.Current(country) is {} queen && distance.TryGetValue(queen.Id,out int qd) && !world.Campaign.RobotBattles.LockedNode(queen.Id))choices.Add(new(country,queen.Id,true,qd,first[queen.Id]));
        }
        return choices.OrderBy(c=>c.Pocket).ThenBy(c=>c.Distance).ThenBy(c=>c.Country).ThenBy(c=>c.Node).ToArray();
    }
    private RobotStore ReturnStore(ReturnDestination target)=>target.Pocket?Retinues[target.Country]:Nodes[target.Node];
    private RobotStore ReservedReturnStore(ReturningRobot journey)=>journey.ReservedPocket?Retinues[journey.Country]:Nodes[journey.ReservedNode.Value];
    private bool HomeSpace(ReturnDestination target)
    {
        var store=ReturnStore(target);
        if(target.Pocket)return store.HasSpace;
        // Normal incoming work keeps priority even if its delivery is currently blocked.
        int incoming=MapleReservedSlots(target.Node)+MapleShipments.Count(o=>o.Target==target.Node)+
            LongDelivery.Orders.Where(o=>o.Target==target.Node && !o.Receiver.HasValue).Select(o=>o.TargetSlot).Distinct().Count()+
            DirectDelivery.Orders.Where(o=>o.Target==target.Node && !o.TargetRobotId.HasValue).Select(o=>o.RobotId).Distinct().Count();
        return store.Count+incoming<store.StorageLimit;
    }
    private ReturningRobot ReserveHome(ReturningRobot journey,ReturnDestination target)
    {
        bool same=journey.ReservedNode.HasValue && journey.Country==target.Country && journey.ReservedPocket==target.Pocket && (target.Pocket || journey.ReservedNode==target.Node);
        if(!same){
            if(!HomeSpace(target))return null;
            ReturnStore(target).ReserveStorage(journey.Robot.Id);
            if(journey.ReservedNode.HasValue)ReservedReturnStore(journey).ReleaseReturn(journey.Robot.Id);
        }
        return journey with {Country=target.Country,ReservedNode=target.Node,ReservedPocket=target.Pocket};
    }
    internal void AdvanceReturnHome(WorldSetup world)
    {
        var moved=new List<(RobotShipment Shipment,int Owner)>();
        // Runs after ordinary delivery/part/production reservations. Only departures take new residual capacity.
        foreach(var journey in ReturningRobots.ToArray()){
            ReturningRobots.Remove(journey);MoveHome(world,journey,moved);
        }
        for(int node=0;node<Nodes.Length;node++)foreach(var robot in Nodes[node].Robots.Where(r=>r.ReturnHomeCommander.HasValue).OrderBy(r=>r.Id).ToArray()){
            if(robot.Stationed || robot.Disposal || AssemblyLocked(node,robot)){
                Nodes[node].Replace(Nodes[node].Robots.Select(r=>r.Id==robot.Id?r with {ReturnHomeCommander=null,ReturnHomeStatus=null}:r));continue;
            }
            if(world.Campaign.RobotBattles.LockedNode(node))continue;
            var countries=HomeCountries(world,robot);var choices=HomeDestinations(world,node,countries);
            var target=choices.FirstOrDefault(t=>(!t.Pocket && t.Node==node) || HomeSpace(t));
            if(target==null){
                string status=countries.Length==0?NoHomeCountry:choices.Length==0?"帰国経路が通れないため出発待ち":"帰国先が満杯のため元の格納庫で出発待ち";
                Nodes[node].Replace(Nodes[node].Robots.Select(r=>r.Id==robot.Id?r with {ReturnHomeStatus=status}:r));continue;
            }
            if(!target.Pocket && target.Node==node){
                Nodes[node].Replace(Nodes[node].Robots.Select(r=>r.Id==robot.Id?r with {Owner=target.Country,ReturnHomeCommander=null,ReturnHomeStatus=null,FunctionDisplayNode=null}:r));continue;
            }
            var departure=ReserveHome(new(robot.Materialize(),node,target.Country,node,robot.ReturnHomeCommander.Value),target);
            if(departure==null)continue;
            try{Nodes[node].Remove(robot.Id);}catch{ReservedReturnStore(departure).ReleaseReturn(robot.Id);throw;}
            MoveHome(world,departure,moved);
        }
        Transport.AppendPlannedTransfers(moved);PartPlanning.Invalidate();Check();
    }
    private void MoveHome(WorldSetup world,ReturningRobot journey,List<(RobotShipment Shipment,int Owner)> moved)
    {
        if(world.Campaign.RobotBattles.LockedNode(journey.Node)){ReturningRobots.Add(journey with {Status="経路上の戦闘が終わるまで待機"});return;}
        int country=ReturnCountry(world,journey.Country);
        var choices=HomeDestinations(world,journey.Node,country>=0?new[]{country}:Array.Empty<int>());
        // Keep an already protected site when it is still a valid home; moving queens retain the same pocket reservation.
        var target=choices.FirstOrDefault(t=>journey.ReservedNode.HasValue && t.Country==journey.Country && t.Pocket==journey.ReservedPocket && (t.Pocket || t.Node==journey.ReservedNode))??choices.FirstOrDefault(HomeSpace);
        if(target==null){ReturningRobots.Add(journey with {Status=country<0?NoHomeCountry:"帰国先の変更待ち / 到着枠は保護中"});return;}
        var reserved=ReserveHome(journey,target);
        if(reserved==null){ReturningRobots.Add(journey with {Status="帰国先の空き待ち / 到着枠は保護中"});return;}
        int next=target.Next;
        if(next!=journey.Node)moved.Add((new RobotShipment(journey.Node,next,journey.Robot.Id,journey.Robot.Parts){Visual=journey.Robot},target.Country));
        if(next==target.Node){
            var store=ReservedReturnStore(reserved);
            store.ReleaseReturn(journey.Robot.Id);
            store.Add(journey.Robot with {Owner=target.Country,ReturnHomeCommander=null,ReturnHomeStatus=null,FunctionDisplayNode=null});return;
        }
        ReturningRobots.Add(reserved with {Node=next,Status=target.Pocket?"クイーンのポケットへ帰国中 / 到着枠保護":"帰国中 / 到着枠保護"});
    }
}
